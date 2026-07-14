using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class ChecksumToolTests
{
    private const string Md5EmptyHex = "d41d8cd98f00b204e9800998ecf8427e";

    [Theory]
    [InlineData("d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("D41D8CD98F00B204E9800998ECF8427E")]
    [InlineData("  d41d8cd98f00b204e9800998ecf8427e  ")]
    [InlineData("md5:d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("*d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("d41d8cd98f00b204e9800998ecf8427e  file.bin")]
    [InlineData("d41d8cd98f00b204e9800998ecf8427e *file.bin")]
    public void Verify_NormalizesDecorations_AndMatches(string expected)
    {
        Assert.True(ChecksumTool.Verify(Md5EmptyHex, expected));
    }

    [Fact]
    public void Verify_MismatchedHash_ReturnsFalse()
    {
        Assert.False(ChecksumTool.Verify(Md5EmptyHex, "00000000000000000000000000000000"));
    }

    [Fact]
    public void Verify_EmptyExpected_ReturnsFalse()
    {
        Assert.False(ChecksumTool.Verify(Md5EmptyHex, ""));
    }

    [Fact]
    public async Task Compute_HashingTempFile_MatchesHashDataOfItsBytes()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("Delp file checksum test content ☃");
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, bytes);

            var expected = Convert.ToHexString(HashTool.ComputeBytes("SHA256", bytes)).ToLowerInvariant();

            string actual;
            using (var stream = File.OpenRead(path))
                actual = HashTool.Compute("SHA256", stream);

            Assert.Equal(expected, actual);
            Assert.True(ChecksumTool.Verify(actual, expected));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
