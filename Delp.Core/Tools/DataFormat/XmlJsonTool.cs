using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

namespace Delp.Core.Tools.DataFormat;

/// <summary>
/// Converts between XML and JSON using the common "Badgerfish-lite" convention:
/// attributes become <c>"@name"</c> keys, mixed text content becomes a <c>"#text"</c> key
/// (or the element's value directly when it has neither attributes nor children),
/// and repeated sibling elements collapse into a JSON array. The mapping is lossy with
/// respect to element ordering across distinct tag names and does not track XML
/// namespaces (only local names are used).
/// </summary>
public static class XmlJsonTool
{
    /// <summary>Fixed layout for JSON → XML output; JsonToXml takes no indent option, so this never varies.</summary>
    private static readonly XmlWriterSettings JsonToXmlWriterSettings = new() { Indent = true, IndentChars = "  ", NewLineChars = "\n" };

    /// <summary>
    /// Converts an XML document to JSON. The XML root element's own tag name is not
    /// preserved in the output (use <see cref="JsonToXml"/>'s <c>rootName</c> to restore it).
    /// </summary>
    public static string XmlToJson(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        XDocument doc;
        try
        {
            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader, DataFormatUtil.SafeXmlReaderSettings);
            doc = XDocument.Load(xmlReader, LoadOptions.None);
        }
        catch (XmlException ex)
        {
            throw new FormatException(ex.Message, ex);
        }

        var root = doc.Root ?? throw new FormatException("XML document has no root element.");
        var node = ElementToNode(root);
        return DataFormatUtil.NormalizeNewLines(node?.ToJsonString(DataFormatUtil.JsonWriteOptions) ?? "null");
    }

    /// <summary>Converts a JSON document to XML, wrapping it in a single root element named <paramref name="rootName"/>.</summary>
    public static string JsonToXml(string json, string rootName)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(rootName);

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid JSON: {ex.Message}", ex);
        }

        var root = NodeToElement(string.IsNullOrWhiteSpace(rootName) ? "root" : rootName, node);
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);

        var sw = new DataFormatUtil.Utf8StringWriter();
        using (var writer = XmlWriter.Create(sw, JsonToXmlWriterSettings))
            doc.Save(writer);
        return sw.ToString();
    }

    private static JsonNode? ElementToNode(XElement element)
    {
        bool hasAttributes = element.HasAttributes;
        bool hasChildren = element.HasElements;

        if (!hasAttributes && !hasChildren)
            return JsonValue.Create(element.Value);

        var obj = new JsonObject();
        foreach (var attribute in element.Attributes())
        {
            if (attribute.IsNamespaceDeclaration) continue;
            obj[$"@{attribute.Name.LocalName}"] = attribute.Value;
        }

        foreach (var group in element.Elements().GroupBy(e => e.Name.LocalName))
        {
            var items = group.Select(ElementToNode).ToList();
            obj[group.Key] = items.Count == 1 ? items[0] : new JsonArray(items.ToArray());
        }

        var text = string.Concat(element.Nodes().OfType<XText>().Select(t => t.Value)).Trim();
        if (text.Length > 0)
            obj["#text"] = text;

        return obj;
    }

    private static XElement NodeToElement(string name, JsonNode? node)
    {
        var tagName = SanitizeElementName(name, out var originalName);
        var element = new XElement(tagName);
        if (originalName is not null)
            element.Add(new XAttribute("name", originalName));

        switch (node)
        {
            case null:
                break;

            case JsonObject obj:
                foreach (var (key, value) in obj)
                {
                    if (key == "#text")
                    {
                        element.Add(new XText(ScalarToString(value)));
                    }
                    else if (key.StartsWith('@'))
                    {
                        var attrName = SanitizeAttributeName(key[1..]);
                        element.Add(new XAttribute(attrName, ScalarToString(value)));
                    }
                    else if (value is JsonArray array)
                    {
                        foreach (var item in array)
                            element.Add(NodeToElement(key, item));
                    }
                    else
                    {
                        element.Add(NodeToElement(key, value));
                    }
                }
                break;

            case JsonArray array:
                foreach (var item in array)
                    element.Add(NodeToElement("item", item));
                break;

            default:
                element.Value = ScalarToString(node);
                break;
        }

        return element;
    }

    private static string ScalarToString(JsonNode? node)
    {
        if (node is null) return "";
        if (node is JsonValue value && value.TryGetValue<string>(out var s)) return s;
        return node.ToJsonString();
    }

    private static string SanitizeElementName(string raw, out string? originalName)
    {
        if (IsValidXmlName(raw))
        {
            originalName = null;
            return raw;
        }
        originalName = raw;
        return "item";
    }

    private static string SanitizeAttributeName(string raw) =>
        IsValidXmlName(raw) ? raw : XmlConvert.EncodeLocalName(raw);

    private static bool IsValidXmlName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        try
        {
            XmlConvert.VerifyName(name);
            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }
}
