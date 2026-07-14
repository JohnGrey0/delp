using System.Xml;
using System.Xml.Linq;

namespace Delp.Core.Tools.DataFormat;

/// <summary>Options for <see cref="XmlFormatTool.Format"/>.</summary>
/// <param name="IndentSize">Number of indent characters per level (ignored when <see cref="UseTabs"/> is true).</param>
/// <param name="UseTabs">Indent with a single tab character per level instead of spaces.</param>
/// <param name="OmitDeclaration">Drop the leading <c>&lt;?xml ...?&gt;</c> declaration from the output.</param>
public sealed record XmlFormatOptions(int IndentSize = 2, bool UseTabs = false, bool OmitDeclaration = false);

/// <summary>A parse error location, as reported by the underlying <see cref="XmlException"/>.</summary>
public sealed record XmlError(int Line, int Col, string Message);

/// <summary>
/// Pretty-prints, minifies and validates XML documents. Parsing is XXE-safe: DTDs are
/// prohibited outright (<see cref="DtdProcessing.Prohibit"/>) and no <see cref="XmlResolver"/>
/// is attached, so external entities and external DTD subsets can never be fetched.
/// </summary>
public static class XmlFormatTool
{
    public static string Format(string xml, XmlFormatOptions options)
    {
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(options);

        var doc = ParseOrThrowFormatException(xml);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = options.UseTabs ? "\t" : new string(' ', Math.Max(0, options.IndentSize)),
            NewLineChars = "\n",
            OmitXmlDeclaration = options.OmitDeclaration || doc.Declaration is null,
            Encoding = System.Text.Encoding.UTF8,
        };

        var sw = new DataFormatUtil.Utf8StringWriter();
        using (var writer = XmlWriter.Create(sw, settings))
            doc.Save(writer);
        return sw.ToString();
    }

    public static string Minify(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        var doc = ParseOrThrowFormatException(xml);

        var settings = new XmlWriterSettings
        {
            Indent = false,
            NewLineChars = "",
            OmitXmlDeclaration = doc.Declaration is null,
            Encoding = System.Text.Encoding.UTF8,
        };

        var sw = new DataFormatUtil.Utf8StringWriter();
        using (var writer = XmlWriter.Create(sw, settings))
            doc.Save(writer);
        return sw.ToString();
    }

    /// <summary>Returns null when <paramref name="xml"/> is well-formed, otherwise the error location.</summary>
    public static XmlError? Validate(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        try
        {
            SafeParse(xml);
            return null;
        }
        catch (XmlException ex)
        {
            return new XmlError(ex.LineNumber, ex.LinePosition, ex.Message);
        }
    }

    private static XDocument ParseOrThrowFormatException(string xml)
    {
        try
        {
            return SafeParse(xml);
        }
        catch (XmlException ex)
        {
            throw new FormatException(ex.Message, ex);
        }
    }

    /// <summary>Loads XML with DTD processing prohibited and no resolver, guarding against XXE.</summary>
    private static XDocument SafeParse(string xml)
    {
        try
        {
            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader, DataFormatUtil.SafeXmlReaderSettings);
            return XDocument.Load(xmlReader, LoadOptions.None);
        }
        catch (XmlException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            // XDocument.Load wraps some XmlReader failures (e.g. no root element) this way.
            throw new XmlException(ex.Message);
        }
    }
}
