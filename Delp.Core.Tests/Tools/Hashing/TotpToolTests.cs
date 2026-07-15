using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class TotpToolTests
{
    // RFC 6238 Appendix B seeds: 20/32/64 ASCII bytes for SHA1/SHA256/SHA512 respectively.
    // (Fully qualified: this test namespace's sibling Delp.Core.Tests.Tools.Encoding would otherwise
    // shadow the System.Text.Encoding a "using System.Text" would bring in.)
    private static readonly byte[] Seed20 = System.Text.Encoding.ASCII.GetBytes("12345678901234567890");
    private static readonly byte[] Seed32 = System.Text.Encoding.ASCII.GetBytes("12345678901234567890123456789012");
    private static readonly byte[] Seed64 = System.Text.Encoding.ASCII.GetBytes("1234567890123456789012345678901234567890123456789012345678901234");

    public static IEnumerable<object[]> Rfc6238Vectors()
    {
        // (unix seconds, algorithm, seed, expected 8-digit code)
        yield return [59L, TotpAlgorithm.Sha1, Seed20, "94287082"];
        yield return [59L, TotpAlgorithm.Sha256, Seed32, "46119246"];
        yield return [59L, TotpAlgorithm.Sha512, Seed64, "90693936"];

        yield return [1111111109L, TotpAlgorithm.Sha1, Seed20, "07081804"];
        yield return [1111111109L, TotpAlgorithm.Sha256, Seed32, "68084774"];
        yield return [1111111109L, TotpAlgorithm.Sha512, Seed64, "25091201"];

        yield return [1111111111L, TotpAlgorithm.Sha1, Seed20, "14050471"];
        yield return [1111111111L, TotpAlgorithm.Sha256, Seed32, "67062674"];
        yield return [1111111111L, TotpAlgorithm.Sha512, Seed64, "99943326"];

        yield return [1234567890L, TotpAlgorithm.Sha1, Seed20, "89005924"];
        yield return [1234567890L, TotpAlgorithm.Sha256, Seed32, "91819424"];
        yield return [1234567890L, TotpAlgorithm.Sha512, Seed64, "93441116"];

        yield return [2000000000L, TotpAlgorithm.Sha1, Seed20, "69279037"];
        yield return [2000000000L, TotpAlgorithm.Sha256, Seed32, "90698825"];
        yield return [2000000000L, TotpAlgorithm.Sha512, Seed64, "38618901"];

        yield return [20000000000L, TotpAlgorithm.Sha1, Seed20, "65353130"];
        yield return [20000000000L, TotpAlgorithm.Sha256, Seed32, "77737706"];
        yield return [20000000000L, TotpAlgorithm.Sha512, Seed64, "47863826"];
    }

    [Theory]
    [MemberData(nameof(Rfc6238Vectors))]
    public void TotpCode_MatchesRfc6238AppendixB(long unixSeconds, TotpAlgorithm algorithm, byte[] seed, string expected)
    {
        var time = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        Assert.Equal(expected, TotpTool.TotpCode(seed, time, digits: 8, periodSeconds: 30, algorithm));
    }

    public static IEnumerable<object[]> Rfc4226Vectors()
    {
        string[] codes = ["755224", "287082", "359152", "969429", "338314", "254676", "287922", "162583", "399871", "520489"];
        for (var i = 0; i < codes.Length; i++)
            yield return [(long)i, codes[i]];
    }

    [Theory]
    [MemberData(nameof(Rfc4226Vectors))]
    public void HotpCode_MatchesRfc4226AppendixD(long counter, string expected)
    {
        Assert.Equal(expected, TotpTool.HotpCode(Seed20, counter, digits: 6));
    }

    [Fact]
    public void HotpCode_InvalidDigits_Throws()
    {
        Assert.Throws<ArgumentException>(() => TotpTool.HotpCode(Seed20, 0, digits: 5));
        Assert.Throws<ArgumentException>(() => TotpTool.HotpCode(Seed20, 0, digits: 9));
    }

    [Theory]
    [InlineData("JBSWY3DPEHPK3PXP")]
    [InlineData("jbswy3dpehpk3pxp")]
    [InlineData("JBSW Y3DP EHPK 3PXP")]
    [InlineData("JBSWY3DPEHPK3PXP========")]
    public void DecodeBase32_TolerantOfCaseSpaceAndPadding(string input)
    {
        var expected = TotpTool.DecodeBase32("JBSWY3DPEHPK3PXP");
        Assert.Equal(expected, TotpTool.DecodeBase32(input));
    }

    [Fact]
    public void DecodeBase32_InvalidCharacter_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => TotpTool.DecodeBase32("Not1Valid!"));
    }

    [Fact]
    public void DecodeBase32_EmptyInput_ReturnsEmptyArray()
    {
        Assert.Empty(TotpTool.DecodeBase32(""));
    }

    [Fact]
    public void Base32_RoundTripsArbitraryBytes()
    {
        byte[] data = [0, 1, 2, 3, 4, 5, 250, 251, 252, 253, 254, 255];
        Assert.Equal(data, TotpTool.DecodeBase32(TotpTool.EncodeBase32(data)));
    }

    [Fact]
    public void ParseOtpAuthUri_ExtractsAllFields()
    {
        var config = TotpTool.ParseOtpAuthUri(
            "otpauth://totp/Example:alice@example.com?secret=JBSWY3DPEHPK3PXP&issuer=Example&algorithm=SHA256&digits=8&period=60");

        Assert.Equal("JBSWY3DPEHPK3PXP", config.Secret);
        Assert.Equal("Example", config.Issuer);
        Assert.Equal("alice@example.com", config.Account);
        Assert.Equal(8, config.Digits);
        Assert.Equal(60, config.PeriodSeconds);
        Assert.Equal(TotpAlgorithm.Sha256, config.Algorithm);
    }

    [Fact]
    public void ParseOtpAuthUri_DefaultsWhenOptionalParamsMissing()
    {
        var config = TotpTool.ParseOtpAuthUri("otpauth://totp/alice@example.com?secret=JBSWY3DPEHPK3PXP");

        Assert.Null(config.Issuer);
        Assert.Equal("alice@example.com", config.Account);
        Assert.Equal(6, config.Digits);
        Assert.Equal(30, config.PeriodSeconds);
        Assert.Equal(TotpAlgorithm.Sha1, config.Algorithm);
    }

    [Fact]
    public void ParseOtpAuthUri_MissingSecret_Throws()
    {
        Assert.Throws<FormatException>(() => TotpTool.ParseOtpAuthUri("otpauth://totp/alice@example.com"));
    }

    [Fact]
    public void ParseOtpAuthUri_NotOtpAuthScheme_Throws()
    {
        Assert.Throws<FormatException>(() => TotpTool.ParseOtpAuthUri("https://example.com?secret=ABC"));
    }

    [Fact]
    public void BuildOtpAuthUri_RoundTripsThroughParse()
    {
        var original = new OtpConfig("JBSWY3DPEHPK3PXP", "MyApp", "bob@example.com", 7, 45, TotpAlgorithm.Sha512);
        var uri = TotpTool.BuildOtpAuthUri(original);
        var parsed = TotpTool.ParseOtpAuthUri(uri);

        Assert.Equal(original, parsed);
    }

    [Fact]
    public void SecondsRemaining_CountsDownWithinWindow()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1111111111L); // 1 second into a 30s window (1111111110 is a boundary)
        Assert.Equal(29, TotpTool.SecondsRemaining(now, 30));

        var boundary = DateTimeOffset.FromUnixTimeSeconds(1111111110L); // exactly on a 30s boundary
        Assert.Equal(30, TotpTool.SecondsRemaining(boundary, 30));
    }

    [Fact]
    public void TotpCode_DifferentPeriodsProduceDifferentCodes()
    {
        var time = DateTimeOffset.FromUnixTimeSeconds(1000000L);
        var code30 = TotpTool.TotpCode(Seed20, time, 6, 30, TotpAlgorithm.Sha1);
        var code60 = TotpTool.TotpCode(Seed20, time, 6, 60, TotpAlgorithm.Sha1);
        Assert.NotEqual(code30, code60);
    }
}
