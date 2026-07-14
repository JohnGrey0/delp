using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;

namespace Delp.Core.Tools.DataFormat;

/// <summary>Small helpers shared across the DataFormat tools' Core implementations.</summary>
internal static class DataFormatUtil
{
    /// <summary>
    /// Several writers here (System.Text.Json's indented writer, XmlWriter without an explicit
    /// NewLineChars override) fall back to <see cref="Environment.NewLine"/>; normalize to "\n"
    /// so output is deterministic across platforms.
    /// </summary>
    public static string NormalizeNewLines(string text) => text.Replace("\r\n", "\n");

    /// <summary>
    /// Shared pretty-printer options for xml-json and csv-json. The default encoder escapes every
    /// non-ASCII code point as "\uXXXX", which round-trips correctly but renders unreadable
    /// converted text (accented Latin, CJK, …) in the live JSON editor pane; the relaxed encoder
    /// still escapes the handful of characters that matter for safety (quotes, backslash, control
    /// chars) but leaves ordinary Basic-Multilingual-Plane Unicode text intact. Supplementary-plane
    /// characters (most emoji) are still escaped as surrogate-pair "\uXXXX\uXXXX" — .NET's
    /// JavaScriptEncoder has no allow-list entry above the BMP regardless of encoder choice. This is
    /// a display convenience for a local dev tool, not JSON embedded in HTML/JS, so the relaxed
    /// escaping is appropriate here.
    /// </summary>
    public static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Shared XXE-safe reader settings (DTDs prohibited, no resolver) used by both xml-format and
    /// xml-json. <see cref="XmlReaderSettings"/> is documented as safe to reuse across concurrent
    /// <see cref="XmlReader.Create(TextReader, XmlReaderSettings)"/> calls as long as it is not
    /// mutated after construction, so a single static instance is fine here.
    /// </summary>
    public static readonly XmlReaderSettings SafeXmlReaderSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreWhitespace = true, // insignificant whitespace between tags is re-laid-out by the writer, not preserved
    };

    /// <summary>A <see cref="StringWriter"/> that reports UTF-8 so an emitted XML declaration reads
    /// "utf-8" instead of the StringWriter default "utf-16".</summary>
    public sealed class Utf8StringWriter : StringWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
    }
}
