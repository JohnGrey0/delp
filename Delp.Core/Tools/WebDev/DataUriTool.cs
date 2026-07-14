namespace Delp.Core.Tools.WebDev;

public sealed record DataUriParts(string MimeType, bool IsBase64, byte[] Data);

/// <summary>Converts between raw bytes/text and RFC 2397 data URIs.</summary>
public static class DataUriTool
{
    /// <summary>Encodes bytes as a Base64 data URI.</summary>
    public static string Encode(byte[] data, string mimeType)
    {
        ArgumentNullException.ThrowIfNull(data);
        var mime = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType.Trim();
        return $"data:{mime};base64,{Convert.ToBase64String(data)}";
    }

    /// <summary>Encodes text (charset=utf-8) as a data URI, either percent-encoded (default) or Base64.</summary>
    public static string EncodeText(string text, string mimeType, bool asBase64 = false)
    {
        text ??= "";
        var mime = string.IsNullOrWhiteSpace(mimeType) ? "text/plain" : mimeType.Trim();
        return asBase64
            ? $"data:{mime};charset=utf-8;base64,{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text))}"
            : $"data:{mime};charset=utf-8,{Uri.EscapeDataString(text)}";
    }

    /// <exception cref="FormatException">The input is not a well-formed data URI.</exception>
    public static DataUriParts Decode(string dataUri)
    {
        if (string.IsNullOrWhiteSpace(dataUri))
            throw new FormatException("Data URI is empty.");

        var s = dataUri.Trim();
        if (!s.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("Not a data URI — it must start with 'data:'.");

        var rest = s["data:".Length..];
        var commaIndex = rest.IndexOf(',');
        if (commaIndex < 0)
            throw new FormatException("Malformed data URI — missing ',' between the header and the payload.");

        var header = rest[..commaIndex];
        var payload = rest[(commaIndex + 1)..];

        var headerParts = header.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var mime = headerParts.Length > 0 && !headerParts[0].Contains('=') ? headerParts[0] : "text/plain";
        if (string.IsNullOrEmpty(mime))
            mime = "text/plain";
        var isBase64 = headerParts.Any(p => p.Equals("base64", StringComparison.OrdinalIgnoreCase));

        byte[] data;
        try
        {
            data = isBase64
                ? Convert.FromBase64String(payload)
                : System.Text.Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
        }
        catch (FormatException)
        {
            throw new FormatException("Malformed data URI — the payload is not valid Base64.");
        }

        return new DataUriParts(mime, isBase64, data);
    }

    /// <summary>Guesses a MIME type from a file extension (with or without a leading dot).</summary>
    public static string GuessMime(string fileExtension)
    {
        var ext = (fileExtension ?? "").TrimStart('.');
        return MimeMap.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
    }

    private static readonly Dictionary<string, string> MimeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["png"] = "image/png",
        ["jpg"] = "image/jpeg",
        ["jpeg"] = "image/jpeg",
        ["gif"] = "image/gif",
        ["webp"] = "image/webp",
        ["svg"] = "image/svg+xml",
        ["ico"] = "image/x-icon",
        ["bmp"] = "image/bmp",
        ["tif"] = "image/tiff",
        ["tiff"] = "image/tiff",
        ["avif"] = "image/avif",
        ["css"] = "text/css",
        ["js"] = "application/javascript",
        ["mjs"] = "application/javascript",
        ["json"] = "application/json",
        ["woff2"] = "font/woff2",
        ["woff"] = "font/woff",
        ["ttf"] = "font/ttf",
        ["otf"] = "font/otf",
        ["eot"] = "application/vnd.ms-fontobject",
        ["pdf"] = "application/pdf",
        ["txt"] = "text/plain",
        ["html"] = "text/html",
        ["htm"] = "text/html",
        ["xml"] = "application/xml",
        ["csv"] = "text/csv",
        ["md"] = "text/markdown",
        ["zip"] = "application/zip",
        ["gz"] = "application/gzip",
        ["tar"] = "application/x-tar",
        ["7z"] = "application/x-7z-compressed",
        ["mp3"] = "audio/mpeg",
        ["wav"] = "audio/wav",
        ["ogg"] = "audio/ogg",
        ["mp4"] = "video/mp4",
        ["webm"] = "video/webm",
        ["avi"] = "video/x-msvideo",
        ["wasm"] = "application/wasm",
        ["yaml"] = "application/yaml",
        ["yml"] = "application/yaml",
        ["toml"] = "application/toml",
        ["doc"] = "application/msword",
        ["docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ["xls"] = "application/vnd.ms-excel",
        ["exe"] = "application/x-msdownload",
        ["bin"] = "application/octet-stream",
        ["rtf"] = "application/rtf",
    };
}
