using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class CertToolTests
{
    // Generates a throwaway self-signed certificate entirely in-memory — no network, no binary fixtures.
    private static string MakeSelfSignedPem(
        string subjectCn = "delp-test.example.com",
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null,
        bool isCa = false,
        string[]? sanDnsNames = null)
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            $"CN={subjectCn}, O=Delp Test, C=US",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(isCa, false, 0, true));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false)); // TLS Server Authentication

        var sanBuilder = new SubjectAlternativeNameBuilder();
        foreach (var name in sanDnsNames ?? [subjectCn])
            sanBuilder.AddDnsName(name);
        req.CertificateExtensions.Add(sanBuilder.Build());

        var nb = notBefore ?? DateTimeOffset.UtcNow.AddDays(-1);
        var na = notAfter ?? DateTimeOffset.UtcNow.AddDays(365);

        using var cert = req.CreateSelfSigned(nb, na);
        var der = cert.Export(X509ContentType.Cert);
        return "-----BEGIN CERTIFICATE-----\n" +
               Convert.ToBase64String(der, Base64FormattingOptions.InsertLineBreaks) +
               "\n-----END CERTIFICATE-----\n";
    }

    [Fact]
    public void DecodePem_SelfSignedCert_DecodesCoreFields()
    {
        var pem = MakeSelfSignedPem("delp-test.example.com");
        var results = CertTool.DecodePem(pem);

        var info = Assert.Single(results);
        Assert.Equal("delp-test.example.com", info.SubjectCommonName);
        Assert.Contains("delp-test.example.com", info.SubjectDn);
        Assert.True(info.IsSelfSigned);
        Assert.Equal("RSA", info.KeyAlgorithm);
        Assert.Equal(2048, info.KeySizeBits);
        Assert.False(string.IsNullOrWhiteSpace(info.SignatureAlgorithm));
        Assert.False(string.IsNullOrWhiteSpace(info.SerialNumber));
    }

    [Fact]
    public void DecodePem_SelfSigned_SubjectEqualsIssuer()
    {
        var pem = MakeSelfSignedPem();
        var info = CertTool.DecodePem(pem).Single();
        Assert.Equal(info.SubjectDn, info.IssuerDn);
    }

    [Fact]
    public void DecodePem_ExtractsSubjectAlternativeNames()
    {
        var pem = MakeSelfSignedPem("delp-test.example.com", sanDnsNames: ["delp-test.example.com", "www.delp-test.example.com"]);
        var info = CertTool.DecodePem(pem).Single();

        Assert.Contains(info.SubjectAlternativeNames, s => s.Contains("delp-test.example.com"));
        Assert.Contains(info.SubjectAlternativeNames, s => s.Contains("www.delp-test.example.com"));
    }

    [Fact]
    public void DecodePem_ExpiredCertificate_ReportsExpired()
    {
        var pem = MakeSelfSignedPem(
            notBefore: DateTimeOffset.UtcNow.AddDays(-400),
            notAfter: DateTimeOffset.UtcNow.AddDays(-10));
        var info = CertTool.DecodePem(pem).Single();

        Assert.True(info.IsExpired);
        Assert.True(info.DaysRemaining < 0);
    }

    [Fact]
    public void DecodePem_ValidCertificate_ReportsDaysRemaining()
    {
        var pem = MakeSelfSignedPem(notAfter: DateTimeOffset.UtcNow.AddDays(90));
        var info = CertTool.DecodePem(pem).Single();

        Assert.False(info.IsExpired);
        Assert.InRange(info.DaysRemaining, 85, 90);
    }

    [Fact]
    public void DecodePem_BasicConstraints_ReflectsCaFlag()
    {
        var caPem = MakeSelfSignedPem("delp-ca.example.com", isCa: true);
        var leafPem = MakeSelfSignedPem("delp-leaf.example.com", isCa: false);

        Assert.True(CertTool.DecodePem(caPem).Single().IsCa);
        Assert.False(CertTool.DecodePem(leafPem).Single().IsCa);
    }

    [Fact]
    public void DecodePem_ExtendedKeyUsage_IncludesServerAuth()
    {
        var pem = MakeSelfSignedPem();
        var info = CertTool.DecodePem(pem).Single();
        Assert.NotEmpty(info.ExtendedKeyUsages);
    }

    [Fact]
    public void DecodePem_Thumbprints_AreColonSeparatedHex()
    {
        var pem = MakeSelfSignedPem();
        var info = CertTool.DecodePem(pem).Single();

        Assert.Matches("^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){19}$", info.Sha1Thumbprint);
        Assert.Matches("^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){31}$", info.Sha256Thumbprint);
    }

    [Fact]
    public void DecodePem_SerialNumber_IsColonSeparatedHex()
    {
        var pem = MakeSelfSignedPem();
        var info = CertTool.DecodePem(pem).Single();
        Assert.Matches("^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2})*$", info.SerialNumber);
    }

    [Fact]
    public void DecodePem_MultiplePemBlocks_DecodesAllInOrder()
    {
        var pem1 = MakeSelfSignedPem("first.example.com");
        var pem2 = MakeSelfSignedPem("second.example.com");
        var combined = pem1 + "\n" + pem2;

        var results = CertTool.DecodePem(combined);

        Assert.Equal(2, results.Count);
        Assert.Equal("first.example.com", results[0].SubjectCommonName);
        Assert.Equal("second.example.com", results[1].SubjectCommonName);
    }

    [Fact]
    public void DecodePem_BareBase64Der_DecodesWithoutPemMarkers()
    {
        var pem = MakeSelfSignedPem("bare-der.example.com");
        var body = pem
            .Replace("-----BEGIN CERTIFICATE-----", "")
            .Replace("-----END CERTIFICATE-----", "")
            .Trim();

        var info = CertTool.DecodePem(body).Single();
        Assert.Equal("bare-der.example.com", info.SubjectCommonName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a certificate at all")]
    [InlineData("-----BEGIN CERTIFICATE-----\nnot-valid-base64!!!\n-----END CERTIFICATE-----")]
    public void DecodePem_GarbageInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => CertTool.DecodePem(input));
    }

    [Fact]
    public void DecodePem_ValidBase64ButNotACertificate_ThrowsFormatException()
    {
        var notACert = Convert.ToBase64String("hello world, this is definitely not a certificate"u8.ToArray());
        Assert.Throws<FormatException>(() => CertTool.DecodePem(notACert));
    }

    [Fact]
    public async Task FetchFromHostAsync_EmptyHost_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CertTool.FetchFromHostAsync(""));
    }
}
