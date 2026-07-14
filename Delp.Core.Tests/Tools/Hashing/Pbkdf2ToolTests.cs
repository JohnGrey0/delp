using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class Pbkdf2ToolTests
{
    [Fact]
    public void Derive_Rfc6070Vector1_Sha1_MatchesKnownDigest()
    {
        var salt = System.Text.Encoding.ASCII.GetBytes("salt");
        var dk = Pbkdf2Tool.Derive("password", salt, 1, "SHA1", 20);
        Assert.Equal("0c60c80f961f0e71f3a9b524af6012062fe037a6", Convert.ToHexString(dk).ToLowerInvariant());
    }

    [Fact]
    public void Derive_Rfc6070Vector2_Sha1_TwoIterations_MatchesKnownDigest()
    {
        var salt = System.Text.Encoding.ASCII.GetBytes("salt");
        var dk = Pbkdf2Tool.Derive("password", salt, 2, "SHA1", 20);
        Assert.Equal("ea6c014dc72d6f8ccd1ed92ace1d41f0d8de8957", Convert.ToHexString(dk).ToLowerInvariant());
    }

    [Fact]
    public void GenerateSalt_RespectsRequestedLength()
    {
        var salt = Pbkdf2Tool.GenerateSalt(16);
        Assert.Equal(16, salt.Length);
    }

    [Fact]
    public void GenerateSalt_ProducesDifferentValuesEachCall()
    {
        var a = Pbkdf2Tool.GenerateSalt(16);
        var b = Pbkdf2Tool.GenerateSalt(16);
        Assert.NotEqual(Convert.ToHexString(a), Convert.ToHexString(b));
    }

    [Fact]
    public void FormatPhc_ProducesExpectedShape()
    {
        var salt = new byte[] { 1, 2, 3, 4 };
        var hash = new byte[] { 5, 6, 7, 8 };
        var phc = Pbkdf2Tool.FormatPhc("SHA256", 600000, salt, hash);

        Assert.Matches(@"^\$pbkdf2-sha256\$i=600000\$[A-Za-z0-9+/=]+\$[A-Za-z0-9+/=]+$", phc);
        Assert.Equal($"$pbkdf2-sha256$i=600000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}", phc);
    }

    [Fact]
    public void Derive_UnsupportedAlgorithm_Md5_Throws()
    {
        Assert.Throws<ArgumentException>(() => Pbkdf2Tool.Derive("pw", [1, 2, 3], 1, "MD5", 16));
    }

    [Fact]
    public void Derive_ZeroIterations_Throws()
    {
        Assert.Throws<ArgumentException>(() => Pbkdf2Tool.Derive("pw", [1, 2, 3], 0, "SHA256", 16));
    }

    [Fact]
    public void Derive_EmptyPassword_Allowed()
    {
        var dk = Pbkdf2Tool.Derive("", [1, 2, 3, 4], 10, "SHA256", 16);
        Assert.Equal(16, dk.Length);
    }
}
