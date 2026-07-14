namespace Delp.Core.Tools.TextProcessing;

/// <summary>One cheat-sheet entry: a documented, self-verifying pattern.</summary>
public sealed record RegexEntry(string Name, string Pattern, string Description, string Example, string Category);

/// <summary>
/// A curated library of commonly needed regular expressions. Every pattern is
/// covered by a test that asserts it compiles (with the standard 2s match
/// timeout) and matches its own <see cref="RegexEntry.Example"/>.
/// </summary>
public static class RegexLibrary
{
    public static readonly IReadOnlyList<RegexEntry> All =
    [
        new("Email",
            @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
            "Basic email address (local part @ domain with a TLD).",
            "user.name+tag@example.co.uk",
            "Web"),

        new("URL (http/https)",
            @"^https?:\/\/[\w.-]+(?:\.[A-Za-z]{2,})+(?::\d+)?(?:\/[^\s]*)?$",
            "HTTP or HTTPS URL, optional port and path/query.",
            "https://example.com:8080/path?query=1",
            "Web"),

        new("IPv4 Address",
            @"^(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)$",
            "Dotted-quad IPv4 address, each octet 0-255.",
            "192.168.1.1",
            "Network"),

        new("IPv6 Address",
            @"^(([0-9A-Fa-f]{1,4}:){7}[0-9A-Fa-f]{1,4}|([0-9A-Fa-f]{1,4}:){1,7}:|([0-9A-Fa-f]{1,4}:){1,6}:[0-9A-Fa-f]{1,4}|([0-9A-Fa-f]{1,4}:){1,5}(:[0-9A-Fa-f]{1,4}){1,2}|([0-9A-Fa-f]{1,4}:){1,4}(:[0-9A-Fa-f]{1,4}){1,3}|([0-9A-Fa-f]{1,4}:){1,3}(:[0-9A-Fa-f]{1,4}){1,4}|([0-9A-Fa-f]{1,4}:){1,2}(:[0-9A-Fa-f]{1,4}){1,5}|[0-9A-Fa-f]{1,4}:((:[0-9A-Fa-f]{1,4}){1,6})|:((:[0-9A-Fa-f]{1,4}){1,7}|:))$",
            "IPv6 address, full or zero-compressed (::) form.",
            "2001:0db8:85a3:0000:0000:8a2e:0370:7334",
            "Network"),

        new("UUID",
            @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
            "Canonical 8-4-4-4-12 hyphenated UUID/GUID.",
            "123e4567-e89b-12d3-a456-426614174000",
            "Identifiers"),

        new("ISO Date",
            @"^\d{4}-\d{2}-\d{2}$",
            "ISO 8601 calendar date (YYYY-MM-DD).",
            "2024-01-15",
            "Date & Time"),

        new("ISO Date-Time",
            @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})?$",
            "ISO 8601 date-time, optional fractional seconds and offset/Z.",
            "2024-01-15T13:45:30Z",
            "Date & Time"),

        new("24-Hour Time",
            @"^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$",
            "24-hour clock time, HH:mm with optional seconds.",
            "23:59:59",
            "Date & Time"),

        new("Semantic Version",
            @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
            "SemVer 2.0.0: major.minor.patch with optional -prerelease and +build.",
            "1.2.3-alpha.1+build.5",
            "Versioning"),

        new("Hex Color",
            @"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
            "CSS hex color: #rgb, #rgba, #rrggbb or #rrggbbaa.",
            "#1E90FF",
            "Design"),

        new("URL Slug",
            @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            "Lowercase, hyphen-separated URL slug.",
            "my-blog-post-title",
            "Web"),

        new("US Phone Number",
            @"^(?:\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}$",
            "US/Canada phone number, common punctuation styles.",
            "(555) 123-4567",
            "Contact"),

        new("E.164 Phone Number",
            @"^\+[1-9]\d{1,14}$",
            "E.164 international phone number: + then up to 15 digits.",
            "+14155552671",
            "Contact"),

        new("Credit Card Number",
            @"^\d{13,19}$",
            "Generic payment card number, 13-19 digits (no Luhn check).",
            "4111111111111111",
            "Finance"),

        new("IBAN",
            @"^[A-Z]{2}\d{2}[A-Z0-9]{11,30}$",
            "Generic IBAN shape: country code, check digits, BBAN.",
            "GB29NWBK60161331926819",
            "Finance"),

        new("MAC Address",
            @"^(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$",
            "Colon- or hyphen-separated 48-bit MAC address.",
            "00:1A:2B:3C:4D:5E",
            "Network"),

        new("Windows File Path",
            @"^[a-zA-Z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*[^\\/:*?""<>|\r\n]*$",
            "Absolute Windows path starting with a drive letter.",
            @"C:\Users\John\Documents\file.txt",
            "Filesystem"),

        new("HTML Tag",
            @"^<\/?[a-zA-Z][a-zA-Z0-9]*(?:\s+[a-zA-Z-]+(?:=(?:""[^""]*""|'[^']*'|[^\s'"">]+))?)*\s*\/?>$",
            "A single opening, closing, or self-closing HTML tag.",
            "<div class=\"container\" id='main'>",
            "Markup"),

        new("Trailing Whitespace",
            @"[ \t]+$",
            "One or more spaces/tabs at the end of a line.",
            "example line   ",
            "Text Quality"),

        new("Duplicated Word",
            @"\b(\w+)\s+\1\b",
            "The same word repeated back-to-back (e.g. \"the the\").",
            "this is is a test",
            "Text Quality"),

        new("Number (Int/Float)",
            @"^[+-]?(?:\d+\.\d+|\d+|\.\d+)(?:[eE][+-]?\d+)?$",
            "Signed integer or floating point number, optional exponent.",
            "-3.14e10",
            "Numbers"),

        new("Base64 String",
            @"^(?:[A-Za-z0-9+\/]{4})*(?:[A-Za-z0-9+\/]{2}==|[A-Za-z0-9+\/]{3}=)?$",
            "Well-formed Base64 payload (standard alphabet, correct padding).",
            "SGVsbG8sIFdvcmxkIQ==",
            "Encoding"),

        new("JWT Shape",
            @"^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$",
            "Three base64url segments separated by dots (header.payload.signature).",
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U",
            "Security"),
    ];

    /// <summary>Filters entries by a case-insensitive substring match against name or description.</summary>
    public static IReadOnlyList<RegexEntry> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return All;

        var q = query.Trim();
        return All
            .Where(e =>
                e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
