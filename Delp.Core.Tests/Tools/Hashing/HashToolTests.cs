using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class HashToolTests
{
    [Fact]
    public void ComputeBytes_Md5OfEmptyString_MatchesKnownDigest()
    {
        var hex = Convert.ToHexString(HashTool.ComputeBytes("MD5", [])).ToLowerInvariant();
        Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", hex);
    }

    [Fact]
    public void ComputeAll_Sha256OfAbc_MatchesKnownDigest()
    {
        var results = HashTool.ComputeAll(System.Text.Encoding.UTF8.GetBytes("abc"));
        var sha256 = results.Single(r => r.Algorithm == "SHA256");
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", sha256.Hex);
    }

    [Fact]
    public void ComputeAll_ReturnsAllFiveAlgorithmsInOrder()
    {
        var results = HashTool.ComputeAll(System.Text.Encoding.UTF8.GetBytes("hello"));
        Assert.Equal(["MD5", "SHA1", "SHA256", "SHA384", "SHA512"], results.Select(r => r.Algorithm).ToArray());
    }

    [Fact]
    public void ComputeAll_EmptyInput_StillProducesKnownDigests()
    {
        var results = HashTool.ComputeAll([]);
        Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", results.Single(r => r.Algorithm == "MD5").Hex);
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", results.Single(r => r.Algorithm == "SHA256").Hex);
    }

    [Fact]
    public void Compute_AcceptsHyphenatedAlgorithmNames()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("abc"));
        var hex = HashTool.Compute("SHA-256", stream);
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hex);
    }

    [Fact]
    public void Compute_StreamMatchesComputeBytes_ForSameData()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog");
        var expectedHex = Convert.ToHexString(HashTool.ComputeBytes("SHA256", data)).ToLowerInvariant();

        using var stream = new MemoryStream(data);
        var streamHex = HashTool.Compute("SHA256", stream);

        Assert.Equal(expectedHex, streamHex);
    }

    [Fact]
    public void ComputeAllFromStream_MatchesComputeAll_ForSameData()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog");
        var expected = HashTool.ComputeAll(data);

        using var stream = new MemoryStream(data);
        var actual = HashTool.ComputeAllFromStream(stream);

        Assert.Equal(expected.Select(r => (r.Algorithm, r.Hex)), actual.Select(r => (r.Algorithm, r.Hex)));
    }

    [Fact]
    public void ComputeBytes_UnknownAlgorithm_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => HashTool.ComputeBytes("SHA3-256-NOPE", []));
    }
}
