using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>A decoded X.509 certificate, formatted for display.</summary>
public sealed record CertInfo(
    string SubjectCommonName,
    string SubjectDn,
    string IssuerDn,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    int DaysRemaining,
    bool IsExpired,
    IReadOnlyList<string> SubjectAlternativeNames,
    string KeyAlgorithm,
    int? KeySizeBits,
    string SignatureAlgorithm,
    string SerialNumber,
    string Sha1Thumbprint,
    string Sha256Thumbprint,
    bool IsCa,
    IReadOnlyList<string> ExtendedKeyUsages,
    bool IsSelfSigned);

/// <summary>
/// Decodes X.509 certificates from PEM/DER, either pasted directly or fetched live from a TLS host.
/// PEM decoding is fully offline. <see cref="FetchFromHostAsync"/> is the only network call anywhere
/// in this tool, and only runs when the caller explicitly asks for it.
/// </summary>
public static class CertTool
{
    private const int FetchTimeoutSeconds = 5;

    private static readonly Regex PemBlockRegex = new(
        @"-----BEGIN CERTIFICATE-----(?<body>.*?)-----END CERTIFICATE-----",
        RegexOptions.Singleline | RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    /// <summary>Decodes one or more certificates from PEM text (possibly several concatenated blocks) or bare base64 DER.</summary>
    /// <exception cref="FormatException">The input contains no parsable certificate.</exception>
    public static IReadOnlyList<CertInfo> DecodePem(string pemOrBase64)
    {
        if (string.IsNullOrWhiteSpace(pemOrBase64))
            throw new FormatException("Paste a PEM certificate (or chain) or a bare base64 DER certificate.");

        var matches = PemBlockRegex.Matches(pemOrBase64);
        var results = new List<CertInfo>();

        if (matches.Count > 0)
        {
            foreach (Match match in matches)
            {
                var der = DecodeBase64Body(match.Groups["body"].Value, "a PEM block");
                using var cert = LoadCertificate(der, "a PEM block");
                results.Add(BuildCertInfo(cert));
            }

            return results;
        }

        var bareDer = DecodeBase64Body(pemOrBase64, "the input");
        using var bareCert = LoadCertificate(bareDer, "the input");
        results.Add(BuildCertInfo(bareCert));
        return results;
    }

    /// <summary>
    /// Connects to <paramref name="host"/>:<paramref name="port"/> over TLS and returns the certificate
    /// chain the server presents. Never validates trust (the callback always accepts) — this tool only
    /// inspects certificates, it doesn't vouch for them. Bounded to a 5-second connect + handshake.
    /// </summary>
    public static async Task<IReadOnlyList<CertInfo>> FetchFromHostAsync(string host, int port = 443)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Enter a host name.", nameof(host));
        if (port is < 1 or > 65535)
            throw new ArgumentException("Port must be between 1 and 65535.", nameof(port));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(FetchTimeoutSeconds));
        using var client = new TcpClient();

        try
        {
            await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Connecting to {host}:{port} timed out after {FetchTimeoutSeconds} seconds.");
        }
        catch (SocketException ex)
        {
            throw new FormatException($"Could not connect to {host}:{port} — {ex.Message}");
        }

        var results = new List<CertInfo>();

        using var ssl = new SslStream(client.GetStream(), leaveInnerStreamOpen: false,
            (_, certificate, chain, _) =>
            {
                if (chain is not null && chain.ChainElements.Count > 0)
                {
                    // element.Certificate instances are owned by the chain (built and disposed by
                    // SslStream/the OS chain-building APIs) — we only read from them, never dispose.
                    foreach (var element in chain.ChainElements)
                        results.Add(BuildCertInfo(element.Certificate));
                }
                else if (certificate is not null)
                {
                    using var cert = LoadCertificate(certificate.GetRawCertData(), "the server certificate");
                    results.Add(BuildCertInfo(cert));
                }

                return true; // Inspection only — never fails the handshake on trust errors.
            });

        try
        {
            await ssl.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions { TargetHost = host },
                cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"TLS handshake with {host}:{port} timed out after {FetchTimeoutSeconds} seconds.");
        }
        catch (AuthenticationException ex)
        {
            throw new FormatException($"TLS handshake with {host}:{port} failed — {ex.Message}");
        }

        if (results.Count == 0)
            throw new FormatException($"{host}:{port} did not present a certificate.");

        return results;
    }

    // ---------------------------------------------------------------- decoding helpers

    private static byte[] DecodeBase64Body(string body, string what)
    {
        var cleaned = new string(body.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (cleaned.Length == 0)
            throw new FormatException($"{what} is empty.");

        try
        {
            return Convert.FromBase64String(cleaned);
        }
        catch (FormatException)
        {
            throw new FormatException($"{what} is not valid base64.");
        }
    }

    private static X509Certificate2 LoadCertificate(byte[] der, string what)
    {
        try
        {
            return X509CertificateLoader.LoadCertificate(der);
        }
        catch (CryptographicException ex)
        {
            throw new FormatException($"Could not parse {what} as an X.509 certificate — {ex.Message}");
        }
    }

    private static CertInfo BuildCertInfo(X509Certificate2 cert)
    {
        var notBefore = new DateTimeOffset(cert.NotBefore.ToUniversalTime(), TimeSpan.Zero);
        var notAfter = new DateTimeOffset(cert.NotAfter.ToUniversalTime(), TimeSpan.Zero);
        var now = DateTimeOffset.UtcNow;

        var (keyAlgorithm, keyBits) = DescribeKey(cert);

        return new CertInfo(
            SubjectCommonName: FirstNonEmpty(cert.GetNameInfo(X509NameType.SimpleName, forIssuer: false), cert.Subject),
            SubjectDn: cert.Subject,
            IssuerDn: cert.Issuer,
            NotBefore: notBefore,
            NotAfter: notAfter,
            DaysRemaining: (int)Math.Floor((notAfter - now).TotalDays),
            IsExpired: now > notAfter,
            SubjectAlternativeNames: ExtractSans(cert),
            KeyAlgorithm: keyAlgorithm,
            KeySizeBits: keyBits,
            SignatureAlgorithm: FirstNonEmpty(cert.SignatureAlgorithm.FriendlyName, cert.SignatureAlgorithm.Value),
            SerialNumber: ColonHex(cert.SerialNumber),
            Sha1Thumbprint: ColonHex(cert.GetCertHashString(HashAlgorithmName.SHA1)),
            Sha256Thumbprint: ColonHex(cert.GetCertHashString(HashAlgorithmName.SHA256)),
            IsCa: cert.Extensions.OfType<X509BasicConstraintsExtension>().FirstOrDefault()?.CertificateAuthority ?? false,
            ExtendedKeyUsages: ExtractEkus(cert),
            IsSelfSigned: string.Equals(cert.Subject, cert.Issuer, StringComparison.Ordinal));
    }

    private static (string Algorithm, int? Bits) DescribeKey(X509Certificate2 cert)
    {
        using (var rsa = cert.GetRSAPublicKey())
            if (rsa is not null)
                return ("RSA", rsa.KeySize);

        using (var ecdsa = cert.GetECDsaPublicKey())
            if (ecdsa is not null)
            {
                string curveName;
                try
                {
                    curveName = ecdsa.ExportParameters(false).Curve.Oid?.FriendlyName ?? "unknown curve";
                }
                catch (CryptographicException)
                {
                    curveName = "unknown curve";
                }

                return ($"ECDSA ({curveName})", ecdsa.KeySize);
            }

        using (var dsa = cert.GetDSAPublicKey())
            if (dsa is not null)
                return ("DSA", dsa.KeySize);

        return (FirstNonEmpty(cert.PublicKey.Oid.FriendlyName, cert.PublicKey.Oid.Value), null);
    }

    private static List<string> ExtractSans(X509Certificate2 cert)
    {
        var raw = cert.Extensions["2.5.29.17"];
        if (raw is null)
            return [];

        try
        {
            var sanExtension = new X509SubjectAlternativeNameExtension(raw.RawData, raw.Critical);
            var names = new List<string>();
            names.AddRange(sanExtension.EnumerateDnsNames().Select(n => $"DNS:{n}"));
            names.AddRange(sanExtension.EnumerateIPAddresses().Select(ip => $"IP:{ip}"));
            if (names.Count > 0)
                return names;
        }
        catch (CryptographicException)
        {
            // Fall through to the generic formatter below for name types EnumerateX doesn't cover.
        }

        var formatted = raw.Format(false);
        return string.IsNullOrWhiteSpace(formatted)
            ? []
            : formatted.Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static List<string> ExtractEkus(X509Certificate2 cert)
    {
        var eku = cert.Extensions.OfType<X509EnhancedKeyUsageExtension>().FirstOrDefault();
        if (eku is null)
            return [];

        return eku.EnhancedKeyUsages
            .Cast<Oid>()
            .Select(o => FirstNonEmpty(o.FriendlyName, o.Value))
            .Where(s => s.Length > 0)
            .ToList();
    }

    private static string ColonHex(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return "";

        var pairs = new List<string>(hex.Length / 2);
        for (var i = 0; i + 1 < hex.Length; i += 2)
            pairs.Add(hex.Substring(i, 2));
        return string.Join(':', pairs);
    }

    private static string FirstNonEmpty(string? a, string? b) =>
        !string.IsNullOrEmpty(a) ? a : (b ?? "");
}
