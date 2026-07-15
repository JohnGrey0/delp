using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace Delp.Core.Tools.DataFormat;

/// <summary>Runs an XPath 1.0 expression against an XML document.</summary>
public static class XPathTool
{
    /// <summary>Hard cap on the number of node matches returned, so a broad expression (e.g.
    /// "//*") against a huge document can't blow up the UI or the response.</summary>
    private const int MaxResults = 1000;

    private const int SnippetMaxLength = 200;

    /// <summary>One reported match. <see cref="IsValue"/> is true for scalar-ish results (an
    /// evaluated function like count()/boolean(), or a selected attribute/text node) where
    /// <see cref="Snippet"/> is the plain value rather than an XML fragment.</summary>
    public sealed record XPathMatch(string Path, string Snippet, bool IsValue);

    public sealed record XPathResult(int Count, bool Truncated, IReadOnlyList<XPathMatch> Matches);

    /// <summary>Evaluates <paramref name="expression"/> against <paramref name="xml"/>.</summary>
    /// <exception cref="FormatException">The XML or the XPath expression is invalid; the message
    /// says which. XML with a DOCTYPE is always rejected (DTD/XXE is never processed).</exception>
    public static XPathResult Evaluate(string xml, string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new FormatException("Enter an XPath expression.");

        var navigator = Parse(xml);

        object evaluated;
        try
        {
            evaluated = navigator.Evaluate(expression);
        }
        catch (XPathException ex)
        {
            throw new FormatException($"Invalid XPath expression — {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            throw new FormatException($"Invalid XPath expression — {ex.Message}");
        }

        if (evaluated is XPathNodeIterator iterator)
            return CollectNodes(iterator);

        return new XPathResult(1, false, [new XPathMatch(expression, ScalarText(evaluated), IsValue: true)]);
    }

    /// <summary>Parses <paramref name="xml"/> with XXE hardening: DTDs are prohibited outright
    /// (<see cref="DtdProcessing.Prohibit"/>) and there is no <see cref="XmlResolver"/> to resolve
    /// any external reference even if one slipped through, so external entities / external DTD
    /// subsets / SSRF-via-XML can never be processed.</summary>
    private static XPathNavigator Parse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new FormatException("Enter XML to query.");

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };

        try
        {
            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader, settings);
            var document = new XPathDocument(xmlReader);
            return document.CreateNavigator();
        }
        catch (XmlException ex)
        {
            throw new FormatException($"Invalid XML — {ex.Message}");
        }
    }

    private static XPathResult CollectNodes(XPathNodeIterator iterator)
    {
        var matches = new List<XPathMatch>();
        var truncated = false;

        while (iterator.MoveNext())
        {
            if (matches.Count >= MaxResults)
            {
                truncated = true;
                break;
            }

            var node = iterator.Current!;
            matches.Add(BuildMatch(node));
        }

        return new XPathResult(matches.Count, truncated, matches);
    }

    private static XPathMatch BuildMatch(XPathNavigator node)
    {
        var path = BuildPath(node.Clone());
        return node.NodeType switch
        {
            XPathNodeType.Attribute => new XPathMatch(path, Truncate(node.Value), IsValue: true),
            XPathNodeType.Text => new XPathMatch(path, Truncate(node.Value), IsValue: true),
            _ => new XPathMatch(path, Truncate(node.OuterXml), IsValue: false),
        };
    }

    private static string Truncate(string text) =>
        text.Length > SnippetMaxLength ? string.Concat(text.AsSpan(0, SnippetMaxLength), "…") : text;

    private static string ScalarText(object evaluated) => evaluated switch
    {
        bool b => b ? "true" : "false",
        double d => d.ToString(CultureInfo.InvariantCulture),
        string s => s,
        null => "",
        _ => Convert.ToString(evaluated, CultureInfo.InvariantCulture) ?? "",
    };

    /// <summary>Builds an absolute, index-qualified path such as "/store/books/book[2]/@id" by
    /// walking up through ancestors. Every element segment carries a 1-based index among its
    /// same-name siblings so the path always identifies exactly one node.</summary>
    private static string BuildPath(XPathNavigator node)
    {
        var segments = new List<string>();

        if (node.NodeType == XPathNodeType.Attribute)
        {
            segments.Add("@" + QualifiedName(node));
            if (!node.MoveToParent())
                return "/" + segments[0];
        }
        else if (node.NodeType == XPathNodeType.Text)
        {
            segments.Add("text()");
            if (!node.MoveToParent())
                return "/" + segments[0];
        }
        else if (node.NodeType == XPathNodeType.Comment)
        {
            segments.Add("comment()");
            if (!node.MoveToParent())
                return "/" + segments[0];
        }
        else if (node.NodeType == XPathNodeType.ProcessingInstruction)
        {
            segments.Add("processing-instruction()");
            if (!node.MoveToParent())
                return "/" + segments[0];
        }

        while (node.NodeType == XPathNodeType.Element)
        {
            segments.Add(ElementSegment(node));
            if (!node.MoveToParent())
                break;
        }

        segments.Reverse();
        return "/" + string.Join("/", segments);
    }

    private static string ElementSegment(XPathNavigator node)
    {
        var name = QualifiedName(node);
        var index = 1;

        var sibling = node.Clone();
        while (sibling.MoveToPrevious())
        {
            if (sibling.NodeType == XPathNodeType.Element && QualifiedName(sibling) == name)
                index++;
        }

        return $"{name}[{index.ToString(CultureInfo.InvariantCulture)}]";
    }

    private static string QualifiedName(XPathNavigator node) =>
        string.IsNullOrEmpty(node.Prefix) ? node.LocalName : $"{node.Prefix}:{node.LocalName}";
}
