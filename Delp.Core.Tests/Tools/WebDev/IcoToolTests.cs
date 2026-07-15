using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class IcoToolTests
{
    // A real, minimal 1x1 transparent PNG (68 bytes) — decodes cleanly in any PNG reader and is
    // small enough to hardcode here so these tests need no file-system or WIC dependency.
    private const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    private static byte[] OnePixelPng => Convert.FromBase64String(OnePixelPngBase64);

    [Fact]
    public void Write_SingleFrame_ProducesValidIconDirHeader()
    {
        var png = OnePixelPng;
        var ico = IcoTool.Write([(16, png)]);

        // ICONDIR: reserved(0)=0, type(2)=1, count(2)=1
        Assert.Equal(0, BitConverter.ToUInt16(ico, 0));
        Assert.Equal(1, BitConverter.ToUInt16(ico, 2));
        Assert.Equal(1, BitConverter.ToUInt16(ico, 4));
    }

    [Fact]
    public void Write_SingleFrame_IconDirEntryMatchesFrame()
    {
        var png = OnePixelPng;
        var ico = IcoTool.Write([(32, png)]);

        // ICONDIRENTRY starts right after the 6-byte ICONDIR.
        var width = ico[6];
        var height = ico[7];
        var colorCount = ico[8];
        var reserved = ico[9];
        var planes = BitConverter.ToUInt16(ico, 10);
        var bitCount = BitConverter.ToUInt16(ico, 12);
        var bytesInRes = BitConverter.ToUInt32(ico, 14);
        var offset = BitConverter.ToUInt32(ico, 18);

        Assert.Equal(32, width);
        Assert.Equal(32, height);
        Assert.Equal(0, colorCount);
        Assert.Equal(0, reserved);
        Assert.Equal(1, planes);
        Assert.Equal(32, bitCount);
        Assert.Equal((uint)png.Length, bytesInRes);
        Assert.Equal(22u, offset); // 6-byte ICONDIR + one 16-byte ICONDIRENTRY
    }

    [Fact]
    public void Write_Size256_WrapsWidthByteToZero()
    {
        var ico = IcoTool.Write([(256, OnePixelPng)]);

        Assert.Equal(0, ico[6]); // 256 is stored as 0 per the ICO spec
        Assert.Equal(0, ico[7]);
        Assert.Equal([256], IcoTool.ReadSizes(ico));
    }

    [Fact]
    public void Write_MultipleFrames_OffsetsAndLengthsAreSequential()
    {
        var pngA = OnePixelPng;
        var pngB = OnePixelPng;
        var pngC = OnePixelPng;
        var ico = IcoTool.Write([(16, pngA), (32, pngB), (48, pngC)]);

        var headerAndDirSize = 6 + 16 * 3;

        var offset0 = BitConverter.ToUInt32(ico, 6 + 0 * 16 + 12);
        var offset1 = BitConverter.ToUInt32(ico, 6 + 1 * 16 + 12);
        var offset2 = BitConverter.ToUInt32(ico, 6 + 2 * 16 + 12);

        Assert.Equal((uint)headerAndDirSize, offset0);
        Assert.Equal(offset0 + (uint)pngA.Length, offset1);
        Assert.Equal(offset1 + (uint)pngB.Length, offset2);
        Assert.Equal(ico.Length, offset2 + pngC.Length);
    }

    [Fact]
    public void Write_EmptyFrameList_Throws()
    {
        Assert.Throws<ArgumentException>(() => IcoTool.Write([]));
    }

    [Fact]
    public void Write_FrameWithEmptyPng_Throws()
    {
        Assert.Throws<ArgumentException>(() => IcoTool.Write([(16, [])]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(257)]
    [InlineData(-1)]
    public void Write_SizeOutOfRange_Throws(int size)
    {
        Assert.Throws<ArgumentException>(() => IcoTool.Write([(size, OnePixelPng)]));
    }

    [Fact]
    public void ReadSizes_RoundTripsWriteOutput()
    {
        var ico = IcoTool.Write([(16, OnePixelPng), (32, OnePixelPng), (48, OnePixelPng), (256, OnePixelPng)]);

        Assert.Equal([16, 32, 48, 256], IcoTool.ReadSizes(ico));
    }

    [Fact]
    public void ReadSizes_TooShort_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => IcoTool.ReadSizes([1, 2, 3]));
    }

    [Fact]
    public void ReadSizes_BadHeader_ThrowsFormatException()
    {
        // Valid length, but reserved/type fields are wrong (this is a cursor file: type=2).
        var bogus = new byte[IcoTool.Write([(16, OnePixelPng)]).Length];
        bogus[2] = 2; // type = 2 (cursor), not 1 (icon)
        Assert.Throws<FormatException>(() => IcoTool.ReadSizes(bogus));
    }

    [Fact]
    public void ReadSizes_TruncatedDirectory_ThrowsFormatException()
    {
        var ico = IcoTool.Write([(16, OnePixelPng), (32, OnePixelPng)]);
        var truncated = ico[..10]; // claims 2 entries but only has room for a partial one

        Assert.Throws<FormatException>(() => IcoTool.ReadSizes(truncated));
    }
}
