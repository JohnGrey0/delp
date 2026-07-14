namespace Delp.Core.Tools.DevUtilities;

/// <summary>One file-extension-to-MIME-type mapping.</summary>
public sealed record MimeEntry(string Extension, string MimeType);

/// <summary>
/// Reference data mapping file extensions to MIME types and back. Extensions are unique keys
/// (an extension has exactly one canonical MIME type); several extensions may share a MIME type
/// (e.g. .jpg and .jpeg both map to image/jpeg).
/// </summary>
public static class MimeData
{
    // Lazy<T>: the ~280-entry map is only built the first time a caller actually touches it (i.e.
    // when the mime-lookup tool is first opened), not at assembly/type load time. There is exactly
    // one shared instance for the process's lifetime; Search() filters over the derived list below
    // without cloning it.
    private static readonly Lazy<Dictionary<string, string>> ExtToMimeLazy = new(BuildMap);

    private static IReadOnlyDictionary<string, string> ExtToMime => ExtToMimeLazy.Value;

    private static readonly Lazy<List<MimeEntry>> AllLazy = new(() =>
        ExtToMimeLazy.Value.Select(kv => new MimeEntry(kv.Key, kv.Value))
            .OrderBy(e => e.Extension, StringComparer.Ordinal)
            .ToList());

    public static IReadOnlyList<MimeEntry> All => AllLazy.Value;

    /// <summary>Looks up the MIME type for an extension. The leading dot is optional; matching is case-insensitive.</summary>
    public static string? LookupByExtension(string extension)
    {
        var ext = Normalize(extension);
        return ext.Length > 0 && ExtToMime.TryGetValue(ext, out var mime) ? mime : null;
    }

    /// <summary>Looks up every extension registered for a MIME type, sorted alphabetically.</summary>
    public static IReadOnlyList<string> LookupByMime(string mimeType)
    {
        var mime = (mimeType ?? "").Trim().ToLowerInvariant();
        if (mime.Length == 0)
            return [];

        return ExtToMime.Where(kv => string.Equals(kv.Value, mime, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Matches a substring against either the extension or the MIME type.</summary>
    public static IReadOnlyList<MimeEntry> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return All;

        var extQuery = Normalize(query);
        var raw = query.Trim();
        return All.Where(e =>
                e.Extension.Contains(extQuery, StringComparison.OrdinalIgnoreCase) ||
                e.MimeType.Contains(raw, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string Normalize(string extension)
    {
        var s = (extension ?? "").Trim().ToLowerInvariant();
        return s.StartsWith('.') ? s[1..] : s;
    }

    private static Dictionary<string, string> BuildMap() => new(StringComparer.Ordinal)
    {
        // ---- Web / text ----
        ["html"] = "text/html",
        ["htm"] = "text/html",
        ["css"] = "text/css",
        ["js"] = "text/javascript",
        ["mjs"] = "text/javascript",
        ["cjs"] = "text/javascript",
        ["json"] = "application/json",
        ["jsonc"] = "application/json",
        ["jsonld"] = "application/ld+json",
        ["json5"] = "application/json5",
        ["jsonl"] = "application/x-ndjson",
        ["har"] = "application/json",
        ["xml"] = "application/xml",
        ["xhtml"] = "application/xhtml+xml",
        ["svg"] = "image/svg+xml",
        ["svgz"] = "image/svg+xml",
        ["wasm"] = "application/wasm",
        ["webmanifest"] = "application/manifest+json",
        ["manifest"] = "text/cache-manifest",
        ["csv"] = "text/csv",
        ["tsv"] = "text/tab-separated-values",
        ["txt"] = "text/plain",
        ["md"] = "text/markdown",
        ["markdown"] = "text/markdown",
        ["yaml"] = "application/x-yaml",
        ["yml"] = "application/x-yaml",
        ["toml"] = "application/toml",
        ["ini"] = "text/plain",
        ["graphql"] = "application/graphql",
        ["gql"] = "application/graphql",
        ["ts"] = "video/mp2t",
        ["rss"] = "application/rss+xml",
        ["atom"] = "application/atom+xml",
        ["rtf"] = "application/rtf",
        ["tex"] = "application/x-tex",
        ["asc"] = "application/pgp-signature",
        ["sig"] = "application/pgp-signature",
        ["srt"] = "application/x-subrip",
        ["vtt"] = "text/vtt",
        ["m3u"] = "audio/x-mpegurl",
        ["m3u8"] = "application/vnd.apple.mpegurl",
        ["ics"] = "text/calendar",
        ["vcf"] = "text/vcard",
        ["patch"] = "text/x-diff",
        ["diff"] = "text/x-diff",

        // ---- Images ----
        ["png"] = "image/png",
        ["jpg"] = "image/jpeg",
        ["jpeg"] = "image/jpeg",
        ["jfif"] = "image/jpeg",
        ["gif"] = "image/gif",
        ["webp"] = "image/webp",
        ["bmp"] = "image/bmp",
        ["tif"] = "image/tiff",
        ["tiff"] = "image/tiff",
        ["ico"] = "image/vnd.microsoft.icon",
        ["cur"] = "image/x-icon",
        ["avif"] = "image/avif",
        ["heic"] = "image/heic",
        ["heif"] = "image/heif",
        ["psd"] = "image/vnd.adobe.photoshop",
        ["ai"] = "application/postscript",
        ["eps"] = "application/postscript",
        ["ps"] = "application/postscript",
        ["jp2"] = "image/jp2",
        ["jxl"] = "image/jxl",
        ["apng"] = "image/apng",
        ["cr2"] = "image/x-canon-cr2",
        ["nef"] = "image/x-nikon-nef",
        ["dng"] = "image/x-adobe-dng",
        ["djvu"] = "image/vnd.djvu",

        // ---- Audio ----
        ["mp3"] = "audio/mpeg",
        ["wav"] = "audio/wav",
        ["ogg"] = "audio/ogg",
        ["oga"] = "audio/ogg",
        ["flac"] = "audio/flac",
        ["aac"] = "audio/aac",
        ["m4a"] = "audio/mp4",
        ["wma"] = "audio/x-ms-wma",
        ["opus"] = "audio/opus",
        ["mid"] = "audio/midi",
        ["midi"] = "audio/midi",
        ["weba"] = "audio/webm",
        ["aiff"] = "audio/aiff",
        ["amr"] = "audio/amr",

        // ---- Video ----
        ["mp4"] = "video/mp4",
        ["m4v"] = "video/x-m4v",
        ["mov"] = "video/quicktime",
        ["avi"] = "video/x-msvideo",
        ["wmv"] = "video/x-ms-wmv",
        ["flv"] = "video/x-flv",
        ["webm"] = "video/webm",
        ["mkv"] = "video/x-matroska",
        ["mpeg"] = "video/mpeg",
        ["mpg"] = "video/mpeg",
        ["3gp"] = "video/3gpp",
        ["3g2"] = "video/3gpp2",
        ["ogv"] = "video/ogg",
        ["mts"] = "video/mp2t",

        // ---- Fonts ----
        ["woff"] = "font/woff",
        ["woff2"] = "font/woff2",
        ["ttf"] = "font/ttf",
        ["ttc"] = "font/collection",
        ["otf"] = "font/otf",
        ["eot"] = "application/vnd.ms-fontobject",
        ["sfnt"] = "font/sfnt",

        // ---- Documents ----
        ["pdf"] = "application/pdf",
        ["doc"] = "application/msword",
        ["dot"] = "application/msword",
        ["docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ["xls"] = "application/vnd.ms-excel",
        ["xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ["xlsm"] = "application/vnd.ms-excel.sheet.macroenabled.12",
        ["ppt"] = "application/vnd.ms-powerpoint",
        ["pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ["odt"] = "application/vnd.oasis.opendocument.text",
        ["ods"] = "application/vnd.oasis.opendocument.spreadsheet",
        ["odp"] = "application/vnd.oasis.opendocument.presentation",
        ["epub"] = "application/epub+zip",
        ["mobi"] = "application/x-mobipocket-ebook",
        ["azw3"] = "application/vnd.amazon.ebook",

        // ---- Archives ----
        ["zip"] = "application/zip",
        ["rar"] = "application/vnd.rar",
        ["7z"] = "application/x-7z-compressed",
        ["tar"] = "application/x-tar",
        ["gz"] = "application/gzip",
        ["tgz"] = "application/gzip",
        ["bz2"] = "application/x-bzip2",
        ["xz"] = "application/x-xz",
        ["zst"] = "application/zstd",
        ["iso"] = "application/x-iso9660-image",
        ["cab"] = "application/vnd.ms-cab-compressed",
        ["jar"] = "application/java-archive",
        ["war"] = "application/java-archive",
        ["apk"] = "application/vnd.android.package-archive",
        ["deb"] = "application/vnd.debian.binary-package",
        ["rpm"] = "application/x-rpm",
        ["lz"] = "application/x-lzip",
        ["lzma"] = "application/x-lzma",
        ["z"] = "application/x-compress",

        // ---- Code ----
        ["py"] = "text/x-python",
        ["pyc"] = "application/x-python-code",
        ["rb"] = "text/x-ruby",
        ["java"] = "text/x-java-source",
        ["class"] = "application/java-vm",
        ["c"] = "text/x-c",
        ["h"] = "text/x-c",
        ["cpp"] = "text/x-c++src",
        ["cc"] = "text/x-c++src",
        ["cxx"] = "text/x-c++src",
        ["hpp"] = "text/x-c++hdr",
        ["cs"] = "text/x-csharp",
        ["go"] = "text/x-go",
        ["rs"] = "text/rust",
        ["php"] = "application/x-httpd-php",
        ["sh"] = "application/x-sh",
        ["bash"] = "application/x-sh",
        ["bat"] = "application/x-bat",
        ["cmd"] = "application/x-bat",
        ["ps1"] = "text/plain",
        ["swift"] = "text/x-swift",
        ["kt"] = "text/x-kotlin",
        ["kts"] = "text/x-kotlin",
        ["sql"] = "application/sql",
        ["pl"] = "text/x-perl",
        ["lua"] = "text/x-lua",
        ["r"] = "text/x-r",
        ["dart"] = "application/vnd.dart",
        ["scala"] = "text/x-scala",
        ["asm"] = "text/x-asm",
        ["vue"] = "text/x-vue",
        ["jsx"] = "text/jsx",
        ["tsx"] = "text/tsx",
        ["proto"] = "text/x-protobuf",

        // ---- Data ----
        ["plist"] = "application/x-plist",
        ["parquet"] = "application/vnd.apache.parquet",
        ["sqlite"] = "application/vnd.sqlite3",
        ["sqlite3"] = "application/vnd.sqlite3",
        ["db"] = "application/octet-stream",
        ["bin"] = "application/octet-stream",
        ["dat"] = "application/octet-stream",
        ["log"] = "text/plain",

        // ---- Security / certificates ----
        ["pem"] = "application/x-pem-file",
        ["crt"] = "application/x-x509-ca-cert",
        ["cer"] = "application/x-x509-ca-cert",
        ["der"] = "application/x-x509-ca-cert",
        ["p12"] = "application/x-pkcs12",
        ["pfx"] = "application/x-pkcs12",
        ["csr"] = "application/pkcs10",
        ["key"] = "application/pkcs8",
        ["jwk"] = "application/jwk+json",
        ["p7b"] = "application/x-pkcs7-certificates",
        ["crl"] = "application/pkix-crl",

        // ---- Executables ----
        ["exe"] = "application/x-msdownload",
        ["dll"] = "application/x-msdownload",
        ["msi"] = "application/x-msi",
        ["dmg"] = "application/x-apple-diskimage",

        // ---- Misc ----
        ["torrent"] = "application/x-bittorrent",
        ["crx"] = "application/x-chrome-extension",
        ["xpi"] = "application/x-xpinstall",
        ["swf"] = "application/x-shockwave-flash",
    };
}
