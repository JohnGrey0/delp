using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Delp.Core.Tools.Hashing;

/// <summary>The HMAC hash algorithm underlying a TOTP/HOTP code (RFC 6238 §1.2).</summary>
public enum TotpAlgorithm
{
    Sha1,
    Sha256,
    Sha512,
}

/// <summary>
/// The fields of an <c>otpauth://</c> URI (Key URI Format, as used by Google Authenticator and
/// compatible apps). <see cref="Secret"/> is the Base32-encoded shared secret, not decoded bytes —
/// decode it with <see cref="TotpTool.DecodeBase32"/> before computing codes.
/// </summary>
public sealed record OtpConfig(
    string Secret,
    string? Issuer,
    string? Account,
    int Digits,
    int PeriodSeconds,
    TotpAlgorithm Algorithm)
{
    public static readonly OtpConfig Default = new("", null, null, 6, 30, TotpAlgorithm.Sha1);
}

/// <summary>
/// RFC 6238 (TOTP) / RFC 4226 (HOTP) code generation. Pure and time-injected — nothing here reads the
/// system clock, so every method is fully unit-testable against the RFCs' published test vectors.
/// </summary>
public static class TotpTool
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>Decodes a Base32 (RFC 4648 §6) string. Tolerates mixed case, embedded whitespace, and missing/extra '=' padding.</summary>
    /// <exception cref="FormatException">The input contains a character outside the Base32 alphabet.</exception>
    public static byte[] DecodeBase32(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var cleaned = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (!char.IsWhiteSpace(c) && c != '=')
                cleaned.Append(char.ToUpperInvariant(c));
        }

        if (cleaned.Length == 0)
            return [];

        var bits = 0;
        var buffer = 0;
        var output = new List<byte>(cleaned.Length * 5 / 8);
        foreach (var c in cleaned.ToString())
        {
            var index = Base32Alphabet.IndexOf(c);
            if (index < 0)
                throw new FormatException($"'{c}' is not a valid Base32 character.");

            buffer = (buffer << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                output.Add((byte)((buffer >> bits) & 0xFF));
            }
        }

        return output.ToArray();
    }

    /// <summary>Encodes bytes as Base32 (RFC 4648 §6), uppercase, with '=' padding to a multiple of 8 characters.</summary>
    public static string EncodeBase32(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
            return "";

        var sb = new StringBuilder((data.Length + 4) / 5 * 8);
        var bits = 0;
        var buffer = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                sb.Append(Base32Alphabet[(buffer >> bits) & 0x1F]);
            }
        }

        if (bits > 0)
            sb.Append(Base32Alphabet[(buffer << (5 - bits)) & 0x1F]);

        while (sb.Length % 8 != 0)
            sb.Append('=');

        return sb.ToString();
    }

    /// <summary>Computes an HOTP code (RFC 4226) for the given counter value, using HMAC-SHA1.</summary>
    /// <exception cref="ArgumentException"><paramref name="digits"/> is outside 6–8.</exception>
    public static string HotpCode(byte[] secret, long counter, int digits) =>
        ComputeCode(secret, counter, digits, TotpAlgorithm.Sha1);

    /// <summary>Computes a TOTP code (RFC 6238) for the given point in time.</summary>
    /// <exception cref="ArgumentException"><paramref name="digits"/> is outside 6–8, or <paramref name="periodSeconds"/> is less than 1.</exception>
    public static string TotpCode(byte[] secret, DateTimeOffset time, int digits, int periodSeconds, TotpAlgorithm algorithm)
    {
        if (periodSeconds < 1)
            throw new ArgumentException("Period must be at least 1 second.", nameof(periodSeconds));

        var counter = time.ToUnixTimeSeconds() / periodSeconds;
        return ComputeCode(secret, counter, digits, algorithm);
    }

    /// <summary>Seconds remaining in the current time-step window, in the range [1, periodSeconds].</summary>
    /// <exception cref="ArgumentException"><paramref name="periodSeconds"/> is less than 1.</exception>
    public static int SecondsRemaining(DateTimeOffset now, int periodSeconds)
    {
        if (periodSeconds < 1)
            throw new ArgumentException("Period must be at least 1 second.", nameof(periodSeconds));

        var elapsed = (int)(((now.ToUnixTimeSeconds() % periodSeconds) + periodSeconds) % periodSeconds);
        return periodSeconds - elapsed;
    }

    private static string ComputeCode(byte[] secret, long counter, int digits, TotpAlgorithm algorithm)
    {
        ArgumentNullException.ThrowIfNull(secret);
        if (digits is < 6 or > 8)
            throw new ArgumentException("Digits must be 6, 7, or 8.", nameof(digits));

        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        var hash = algorithm switch
        {
            TotpAlgorithm.Sha1 => HMACSHA1.HashData(secret, counterBytes),
            TotpAlgorithm.Sha256 => HMACSHA256.HashData(secret, counterBytes),
            TotpAlgorithm.Sha512 => HMACSHA512.HashData(secret, counterBytes),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
        };

        // RFC 4226 §5.4 dynamic truncation.
        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
                        | ((hash[offset + 1] & 0xFF) << 16)
                        | ((hash[offset + 2] & 0xFF) << 8)
                        | (hash[offset + 3] & 0xFF);

        var modulus = (int)Math.Pow(10, digits);
        var code = binaryCode % modulus;
        return code.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
    }

    /// <summary>Parses an <c>otpauth://totp/...</c> or <c>otpauth://hotp/...</c> Key URI.</summary>
    /// <exception cref="FormatException">The URI isn't a well-formed otpauth URI, or is missing a secret.</exception>
    public static OtpConfig ParseOtpAuthUri(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (!Uri.TryCreate(uri.Trim(), UriKind.Absolute, out var parsed)
            || !parsed.Scheme.Equals("otpauth", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("Not a valid otpauth:// URI.");
        }

        var label = Uri.UnescapeDataString(parsed.AbsolutePath.TrimStart('/'));
        string? issuer = null;
        string? account = label.Length == 0 ? null : label;
        var colon = label.IndexOf(':');
        if (colon >= 0)
        {
            issuer = label[..colon];
            account = label[(colon + 1)..];
        }

        var query = ParseQuery(parsed.Query);

        if (!query.TryGetValue("secret", out var secret) || secret.Length == 0)
            throw new FormatException("otpauth URI is missing a 'secret' parameter.");

        if (query.TryGetValue("issuer", out var queryIssuer) && queryIssuer.Length > 0)
            issuer = queryIssuer;

        var digits = 6;
        if (query.TryGetValue("digits", out var digitsText) && int.TryParse(digitsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDigits))
            digits = parsedDigits;

        var period = 30;
        if (query.TryGetValue("period", out var periodText) && int.TryParse(periodText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPeriod))
            period = parsedPeriod;

        var algorithm = TotpAlgorithm.Sha1;
        if (query.TryGetValue("algorithm", out var algorithmText))
            algorithm = ParseAlgorithm(algorithmText);

        return new OtpConfig(secret, issuer, account, digits, period, algorithm);
    }

    /// <summary>Builds an <c>otpauth://totp/...</c> Key URI from a config, suitable for rendering as an enrollment QR code.</summary>
    public static string BuildOtpAuthUri(OtpConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var label = string.IsNullOrEmpty(config.Issuer)
            ? config.Account ?? ""
            : $"{config.Issuer}:{config.Account}";

        var sb = new StringBuilder("otpauth://totp/");
        sb.Append(Uri.EscapeDataString(label));
        sb.Append("?secret=").Append(config.Secret.Replace(" ", ""));
        if (!string.IsNullOrEmpty(config.Issuer))
            sb.Append("&issuer=").Append(Uri.EscapeDataString(config.Issuer));
        sb.Append("&algorithm=").Append(AlgorithmName(config.Algorithm));
        sb.Append("&digits=").Append(config.Digits.ToString(CultureInfo.InvariantCulture));
        sb.Append("&period=").Append(config.PeriodSeconds.ToString(CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    private static TotpAlgorithm ParseAlgorithm(string text) => text.Trim().ToUpperInvariant() switch
    {
        "SHA1" => TotpAlgorithm.Sha1,
        "SHA256" => TotpAlgorithm.Sha256,
        "SHA512" => TotpAlgorithm.Sha512,
        _ => throw new FormatException($"Unsupported algorithm '{text}'."),
    };

    private static string AlgorithmName(TotpAlgorithm algorithm) => algorithm switch
    {
        TotpAlgorithm.Sha1 => "SHA1",
        TotpAlgorithm.Sha256 => "SHA256",
        TotpAlgorithm.Sha512 => "SHA512",
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
    };

    /// <summary>Hand-rolled query-string parser (no System.Web dependency in Delp.Core): splits on '&amp;' and '=', percent-decoding both sides.</summary>
    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var text = query.StartsWith('?') ? query[1..] : query;
        if (text.Length == 0)
            return result;

        foreach (var pair in text.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            var key = eq >= 0 ? pair[..eq] : pair;
            var value = eq >= 0 ? pair[(eq + 1)..] : "";
            result[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value.Replace("+", " "));
        }

        return result;
    }
}
