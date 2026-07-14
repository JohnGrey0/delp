namespace Delp.Core.Tools.Hashing;

public static class ChecksumTool
{
    /// <summary>
    /// Compares a computed hash against a user-supplied expected value, tolerating the
    /// decorations commonly found in .sha256/.sha1 checksum files and tool output: case
    /// differences, surrounding whitespace, an "algo:" prefix (e.g. "sha256:"), a leading
    /// '*' (binary-mode marker), and a trailing " filename" / " *filename".
    /// </summary>
    public static bool Verify(string actualHex, string expectedHex)
    {
        var actual = Normalize(actualHex);
        var expected = Normalize(expectedHex);
        return actual.Length > 0 && string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? hex)
    {
        var s = (hex ?? "").Trim();
        if (s.Length == 0)
            return "";

        // Drop a trailing " filename" / " *filename" (sha256sum-style checksum file lines):
        // keep only the first whitespace-separated token.
        var spaceIndex = s.IndexOfAny([' ', '\t']);
        if (spaceIndex >= 0)
            s = s[..spaceIndex];

        // Strip an "algo:" prefix, e.g. "sha256:<hex>".
        var colonIndex = s.IndexOf(':');
        if (colonIndex > 0 && colonIndex < s.Length - 1)
            s = s[(colonIndex + 1)..];

        // Strip a leading '*' (binary-mode marker some tools place before the hash).
        return s.TrimStart('*').Trim();
    }
}
