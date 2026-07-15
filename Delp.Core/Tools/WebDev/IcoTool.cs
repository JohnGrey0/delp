namespace Delp.Core.Tools.WebDev;

/// <summary>
/// Pure byte-level reader/writer for the Windows ICO container (PNG-compressed frames only —
/// every modern favicon size, including 256×256, is well within a PNG-in-ICO reader's support).
/// No WIC/System.Drawing involved, so this is fully unit-testable in Core.
/// </summary>
public static class IcoTool
{
    private const int IconDirSize = 6;
    private const int IconDirEntrySize = 16;
    private const ushort IconType = 1;

    /// <summary>
    /// Packs already-PNG-encoded frames into an ICO file: one ICONDIR header, one ICONDIRENTRY per
    /// frame, then the raw PNG bytes back-to-back in the order given.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The frame list is empty, a frame's PNG bytes are empty, or a frame's size is outside 1-256.
    /// </exception>
    public static byte[] Write((int Size, byte[] Png)[] frames)
    {
        if (frames is null || frames.Length == 0)
            throw new ArgumentException("At least one frame is required.", nameof(frames));

        foreach (var frame in frames)
        {
            if (frame.Size is < 1 or > 256)
                throw new ArgumentException($"Icon size {frame.Size} is out of range (1-256).", nameof(frames));
            if (frame.Png is null || frame.Png.Length == 0)
                throw new ArgumentException("A frame's PNG data cannot be empty.", nameof(frames));
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((ushort)0);           // reserved, must be 0
        writer.Write(IconType);            // 1 = icon (as opposed to 2 = cursor)
        writer.Write((ushort)frames.Length);

        var dataOffset = IconDirSize + IconDirEntrySize * frames.Length;
        foreach (var (size, png) in frames)
        {
            // ICONDIRENTRY packs width/height in a single byte each; 256 wraps to 0 by convention.
            var dim = (byte)(size == 256 ? 0 : size);
            writer.Write(dim);                  // width
            writer.Write(dim);                  // height
            writer.Write((byte)0);              // color count (0 = not palette-indexed)
            writer.Write((byte)0);              // reserved, must be 0
            writer.Write((ushort)1);            // color planes
            writer.Write((ushort)32);           // bits per pixel (PNG frames are stored as 32bpp BGRA)
            writer.Write((uint)png.Length);     // size of this frame's data
            writer.Write((uint)dataOffset);     // offset of this frame's data from the start of the file
            dataOffset += png.Length;
        }

        foreach (var (_, png) in frames)
            writer.Write(png);

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>Returns the declared pixel size (width, which equals height for square icons) of every frame.</summary>
    /// <exception cref="FormatException">The bytes are not a well-formed ICO file.</exception>
    public static IReadOnlyList<int> ReadSizes(byte[] ico)
    {
        if (ico is null || ico.Length < IconDirSize)
            throw new FormatException("Not a valid ICO file — header is truncated.");

        using var stream = new MemoryStream(ico);
        using var reader = new BinaryReader(stream);

        var reserved = reader.ReadUInt16();
        var type = reader.ReadUInt16();
        if (reserved != 0 || type != IconType)
            throw new FormatException("Not a valid ICO file — bad ICONDIR header.");

        var count = reader.ReadUInt16();
        if (ico.Length < IconDirSize + IconDirEntrySize * count)
            throw new FormatException("Not a valid ICO file — directory entries are truncated.");

        var sizes = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            var width = reader.ReadByte();
            reader.ReadByte();   // height (always equal to width for the square icons this tool writes)
            reader.ReadByte();   // color count
            reader.ReadByte();   // reserved
            reader.ReadUInt16(); // color planes
            reader.ReadUInt16(); // bits per pixel
            var length = reader.ReadUInt32();
            var offset = reader.ReadUInt32();

            if (offset + length > ico.Length)
                throw new FormatException($"Not a valid ICO file — frame {i} data extends past the end of the file.");

            sizes.Add(width == 0 ? 256 : width);
        }

        return sizes;
    }
}
