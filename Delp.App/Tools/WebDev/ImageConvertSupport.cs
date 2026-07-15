using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Delp.Core.Tools.WebDev;

namespace Delp.App.Tools.WebDev;

/// <summary>Raster formats the Image Converter can encode to.</summary>
public enum ImageOutputFormat
{
    Png,
    Jpeg,
    Bmp,
    Gif,
    Tiff,
}

public enum FlipDirection
{
    Horizontal,
    Vertical,
}

/// <summary>Display-facing metadata for the INFO panel.</summary>
public sealed record ImageInfo(
    string Format,
    int Width,
    int Height,
    double DpiX,
    double DpiY,
    int FrameCount,
    long FileSizeBytes,
    string? CameraModel,
    DateTime? DateTaken,
    int? Orientation,
    bool HasGps);

public sealed record LoadedImage(BitmapSource Bitmap, ImageInfo Info, BitmapMetadata? Metadata);

/// <summary>
/// WIC-backed image loading, transforms, and encoding for the Image Converter tool. BitmapDecoder/
/// BitmapEncoder live in PresentationCore (WPF), not in the UI-free Delp.Core, so — per Core's
/// pure/no-WPF-references rule — this logic lives in the App layer beside the view instead.
/// </summary>
public static class ImageConvertSupport
{
    /// <summary>Standard favicon export sizes, smallest to largest.</summary>
    public static readonly int[] FaviconSizes = [16, 24, 32, 48, 64, 128, 256];

    /// <exception cref="FormatException">The file can't be read or isn't a decodable raster image.</exception>
    public static LoadedImage Load(string path)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new FormatException($"Couldn't read '{Path.GetFileName(path)}': {ex.Message}");
        }

        if (bytes.Length == 0)
            throw new FormatException($"'{Path.GetFileName(path)}' is empty.");

        BitmapDecoder decoder;
        try
        {
            var stream = new MemoryStream(bytes);
            decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        }
        catch (Exception ex) when (ex is NotSupportedException or FileFormatException or ArgumentException)
        {
            throw new FormatException(
                "Unrecognized or unsupported image format. WebP/HEIC decode only when the matching Windows codec extension is installed.");
        }

        var frame = decoder.Frames[0];
        frame.Freeze();

        var metadata = frame.Metadata as BitmapMetadata;
        var formatName = decoder.CodecInfo?.FriendlyName ?? "Unknown";
        var info = BuildInfo(frame, metadata, formatName, decoder.Frames.Count, bytes.LongLength);
        return new LoadedImage(frame, info, metadata);
    }

    private static ImageInfo BuildInfo(BitmapFrame frame, BitmapMetadata? metadata, string formatName, int frameCount, long fileSize)
    {
        string? camera = null;
        DateTime? dateTaken = null;
        int? orientation = null;
        var hasGps = false;

        if (metadata is not null)
        {
            try
            {
                camera = string.IsNullOrWhiteSpace(metadata.CameraModel) ? null : metadata.CameraModel;
            }
            catch (NotSupportedException)
            {
                // Format doesn't support this built-in query path (e.g. plain PNG/BMP).
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(metadata.DateTaken) &&
                    DateTime.TryParse(metadata.DateTaken, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    dateTaken = dt;
            }
            catch (NotSupportedException)
            {
            }

            try
            {
                if (metadata.ContainsQuery("/app1/ifd/{ushort=274}") &&
                    metadata.GetQuery("/app1/ifd/{ushort=274}") is { } raw)
                    orientation = Convert.ToInt32(raw, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is NotSupportedException or IOException or ArgumentException)
            {
            }

            try
            {
                hasGps = metadata.ContainsQuery("/app1/ifd/gps");
            }
            catch (Exception ex) when (ex is NotSupportedException or IOException or ArgumentException)
            {
                hasGps = false;
            }
        }

        return new ImageInfo(
            formatName, frame.PixelWidth, frame.PixelHeight, frame.DpiX, frame.DpiY,
            frameCount, fileSize, camera, dateTaken, orientation, hasGps);
    }

    /// <exception cref="ArgumentException">Width or height is not positive.</exception>
    public static BitmapSource Resize(BitmapSource source, int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive.");

        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        using (var dc = visual.RenderOpen())
            dc.DrawImage(source, new System.Windows.Rect(0, 0, width, height));

        var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);
        target.Freeze();
        return target;
    }

    /// <exception cref="ArgumentException">Degrees isn't 90, 180, or 270.</exception>
    public static BitmapSource Rotate(BitmapSource source, int degrees)
    {
        if (degrees != 90 && degrees != 180 && degrees != 270)
            throw new ArgumentException("Rotation must be 90, 180, or 270 degrees.");

        var transformed = new TransformedBitmap(source, new RotateTransform(degrees));
        transformed.Freeze();
        return transformed;
    }

    public static BitmapSource Flip(BitmapSource source, FlipDirection direction)
    {
        var scale = direction == FlipDirection.Horizontal ? new ScaleTransform(-1, 1) : new ScaleTransform(1, -1);
        var transformed = new TransformedBitmap(source, scale);
        transformed.Freeze();
        return transformed;
    }

    /// <summary>
    /// Encodes <paramref name="source"/> to the given format (JPEG quality is clamped to 1-100).
    /// When <paramref name="stripMetadata"/> is false and <paramref name="metadata"/> is available,
    /// it's carried into the output frame (falling back to no metadata if the target format rejects
    /// that metadata schema).
    /// </summary>
    public static byte[] Encode(BitmapSource source, ImageOutputFormat format, int jpegQuality, BitmapMetadata? metadata, bool stripMetadata)
    {
        BitmapEncoder encoder = format switch
        {
            ImageOutputFormat.Png => new PngBitmapEncoder(),
            ImageOutputFormat.Jpeg => new JpegBitmapEncoder { QualityLevel = Math.Clamp(jpegQuality, 1, 100) },
            ImageOutputFormat.Bmp => new BmpBitmapEncoder(),
            ImageOutputFormat.Gif => new GifBitmapEncoder(),
            ImageOutputFormat.Tiff => new TiffBitmapEncoder(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown image format."),
        };

        var frame = CreateFrame(source, metadata, stripMetadata);
        encoder.Frames.Add(frame);

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static BitmapFrame CreateFrame(BitmapSource source, BitmapMetadata? metadata, bool stripMetadata)
    {
        if (!stripMetadata && metadata is not null)
        {
            try
            {
                return BitmapFrame.Create(source, null, (BitmapMetadata)metadata.Clone(), null);
            }
            catch (Exception ex) when (ex is NotSupportedException or ArgumentException)
            {
                // Not every target format accepts every source metadata schema (e.g. EXIF GPS on a
                // BMP) — fall back to a plain frame rather than fail the whole export.
            }
        }

        return BitmapFrame.Create(source);
    }

    /// <summary>Renders every <see cref="FaviconSizes"/> size and packs them into one multi-size .ico.</summary>
    public static byte[] ExportFavicon(BitmapSource source)
    {
        var frames = new (int Size, byte[] Png)[FaviconSizes.Length];
        for (var i = 0; i < FaviconSizes.Length; i++)
        {
            var size = FaviconSizes[i];
            var resized = Resize(source, size, size);
            frames[i] = (size, Encode(resized, ImageOutputFormat.Png, 100, null, stripMetadata: true));
        }

        return IcoTool.Write(frames);
    }
}
