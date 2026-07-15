using System.Text.Json;

namespace Delp.Core.Tools.DataFormat;

public enum JsonNodeKind
{
    Object,
    Array,
    String,
    Number,
    Bool,
    Null,
}

/// <summary>
/// One node in a lazily-explored JSON document. Wraps a <see cref="JsonElement"/> view into the
/// owning <see cref="JsonTree"/>'s <see cref="JsonDocument"/> — cheap to create (no recursion, no
/// eager child materialization), valid only while that document is alive.
/// </summary>
public sealed class JsonTreeNode
{
    private readonly JsonElement _element;

    /// <summary>The untruncated leaf text used for search matching ("" for Object/Array); kept
    /// separate from <see cref="Preview"/> so a substring past the 80-char preview cutoff is still
    /// found by <see cref="JsonTreeTool.Search"/>.</summary>
    private readonly string _matchText;

    internal JsonTreeNode(JsonElement element, string? key, string path, string pointer)
    {
        _element = element;
        Key = key;
        Path = path;
        Pointer = pointer;
        Kind = KindOf(element);
        ChildCount = Kind switch
        {
            JsonNodeKind.Object => element.GetPropertyCount(),
            JsonNodeKind.Array => element.GetArrayLength(),
            _ => 0,
        };
        _matchText = Kind switch
        {
            JsonNodeKind.String => element.GetString() ?? "",
            JsonNodeKind.Number => element.GetRawText(),
            JsonNodeKind.Bool => element.GetRawText(),
            JsonNodeKind.Null => "null",
            _ => "",
        };
        Preview = BuildPreview(Kind, _matchText, ChildCount);
    }

    /// <summary>Property name for an object member; index-less and null for array items and root
    /// (array items carry no "key" for <see cref="JsonTreeTool.Search"/>'s key-matching purposes —
    /// a caller displaying array items should use their position instead).</summary>
    public string? Key { get; }

    public JsonNodeKind Kind { get; }

    /// <summary>Strings are truncated at 80 chars (with an ellipsis) and quoted; numbers/bools/null
    /// render verbatim; objects render "{N props}"; arrays render "[N items]".</summary>
    public string Preview { get; }

    /// <summary>JSONPath to this node, e.g. "$.a.b[0]"; keys that aren't simple identifiers are
    /// bracket-quoted, e.g. "$['weird key']".</summary>
    public string Path { get; }

    /// <summary>RFC 6901 JSON Pointer to this node, e.g. "/a/b/0" (~ and / in member names are
    /// escaped to ~0 and ~1 respectively).</summary>
    public string Pointer { get; }

    /// <summary>Number of immediate children (0 for scalars) — cheap (<see cref="JsonElement.GetPropertyCount"/>
    /// / <see cref="JsonElement.GetArrayLength"/>), computed without materializing any child node.</summary>
    public int ChildCount { get; }

    internal static JsonTreeNode CreateRoot(JsonElement root) => new(root, null, "$", "");

    /// <summary>
    /// Materializes this node's immediate children only — grandchildren stay unvisited JsonElements
    /// until their own <see cref="Children"/> is called. This one-level-at-a-time materialization is
    /// what keeps loading a large document cheap: nothing below the root is ever walked unless a
    /// caller asks for it.
    /// </summary>
    public IReadOnlyList<JsonTreeNode> Children()
    {
        if (ChildCount == 0)
            return [];

        var list = new List<JsonTreeNode>(ChildCount);
        switch (Kind)
        {
            case JsonNodeKind.Object:
                foreach (var prop in _element.EnumerateObject())
                {
                    var childPath = Path + PathSegment(prop.Name);
                    var childPointer = Pointer + "/" + EscapePointerSegment(prop.Name);
                    list.Add(new JsonTreeNode(prop.Value, prop.Name, childPath, childPointer));
                }
                break;
            case JsonNodeKind.Array:
                var i = 0;
                foreach (var item in _element.EnumerateArray())
                {
                    list.Add(new JsonTreeNode(item, null, $"{Path}[{i}]", $"{Pointer}/{i}"));
                    i++;
                }
                break;
        }
        return list;
    }

    internal bool MatchesKey(string query) =>
        Key is not null && Key.Contains(query, StringComparison.OrdinalIgnoreCase);

    internal bool MatchesValue(string query) =>
        Kind is not (JsonNodeKind.Object or JsonNodeKind.Array) &&
        _matchText.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static JsonNodeKind KindOf(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => JsonNodeKind.Object,
        JsonValueKind.Array => JsonNodeKind.Array,
        JsonValueKind.String => JsonNodeKind.String,
        JsonValueKind.Number => JsonNodeKind.Number,
        JsonValueKind.True or JsonValueKind.False => JsonNodeKind.Bool,
        _ => JsonNodeKind.Null,
    };

    private static string BuildPreview(JsonNodeKind kind, string matchText, int childCount) => kind switch
    {
        JsonNodeKind.String => PreviewString(matchText),
        JsonNodeKind.Number => matchText,
        JsonNodeKind.Bool => matchText,
        JsonNodeKind.Null => "null",
        JsonNodeKind.Object => $"{{{childCount} props}}",
        JsonNodeKind.Array => $"[{childCount} items]",
        _ => "",
    };

    private static string PreviewString(string s)
    {
        var truncated = s.Length > 80 ? s[..80] + "…" : s;
        return $"\"{truncated}\"";
    }

    /// <summary>".name" for a simple identifier, else "['escaped name']".</summary>
    private static string PathSegment(string name) =>
        IsSimpleIdentifier(name) ? "." + name : "['" + name.Replace("\\", "\\\\").Replace("'", "\\'") + "']";

    private static bool IsSimpleIdentifier(string s)
    {
        if (s.Length == 0 || !(char.IsAsciiLetter(s[0]) || s[0] == '_'))
            return false;
        for (var i = 1; i < s.Length; i++)
            if (!(char.IsAsciiLetterOrDigit(s[i]) || s[i] == '_'))
                return false;
        return true;
    }

    private static string EscapePointerSegment(string s) => s.Replace("~", "~0").Replace("/", "~1");
}

/// <summary>Owns the parsed <see cref="JsonDocument"/>; dispose when done (e.g. before loading a
/// new document) — every <see cref="JsonTreeNode"/> produced from <see cref="Root"/> is a view into
/// this document and becomes invalid once it is disposed.</summary>
public sealed class JsonTree : IDisposable
{
    private readonly JsonDocument _document;
    private bool _disposed;

    internal JsonTree(JsonDocument document)
    {
        _document = document;
        Root = JsonTreeNode.CreateRoot(document.RootElement);
    }

    public JsonTreeNode Root { get; }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _document.Dispose();
    }
}

/// <summary>Result of a single search walk: every matched node's <see cref="JsonTreeNode.Path"/> (up
/// to the requested cap) plus the root-to-first-match chain, if any — see
/// <see cref="JsonTreeTool.SearchAll"/>.</summary>
public sealed record JsonSearchResult(IReadOnlyList<string> Paths, IReadOnlyList<JsonTreeNode>? FirstChain);

/// <summary>Parses and lazily explores a JSON document as a tree, without ever walking the whole
/// thing up front — the point is that opening a large document stays fast.</summary>
public static class JsonTreeTool
{
    // Default MaxDepth is only 64; raised so legitimately deep (if unusual) documents parse
    // instead of throwing, and so a malicious/pathological depth doesn't need special-casing here —
    // JsonDocument's reader tracks depth with a heap-allocated bit stack, not C# call recursion, so
    // there's no stack-overflow risk from raising this.
    private static readonly JsonDocumentOptions Options = new()
    {
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        MaxDepth = 4096,
    };

    /// <exception cref="FormatException">The input is not valid JSON.</exception>
    public static JsonTree Load(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            return new JsonTree(JsonDocument.Parse(json, Options));
        }
        catch (JsonException ex)
        {
            var line = (int)(ex.LineNumber ?? 0) + 1;
            var col = (int)(ex.BytePositionInLine ?? 0) + 1;
            var idx = ex.Message.IndexOf(" LineNumber:", StringComparison.Ordinal);
            var message = idx >= 0 ? ex.Message[..idx] : ex.Message;
            throw new FormatException($"Line {line}, Col {col}: {message}");
        }
    }

    /// <summary>Case-insensitive substring search over keys and leaf values. Iterative (an explicit
    /// stack, not C# recursion) so it's safe over very deeply nested documents; stops as soon as
    /// <paramref name="max"/> matches are found.</summary>
    public static IReadOnlyList<string> Search(JsonTree tree, string query, int max = 500) =>
        SearchAll(tree, query, max).Paths;

    /// <summary>Same traversal/matching as <see cref="Search"/>, but returns the full root-to-match
    /// node chain for the first hit so a caller (the tree view) can expand every ancestor and reveal
    /// it — reusing the exact same matching logic keeps "which node gets revealed" and "how many
    /// matches were found" from ever disagreeing. Null when there is no match.</summary>
    public static IReadOnlyList<JsonTreeNode>? FindFirstMatchChain(JsonTree tree, string query) =>
        SearchAll(tree, query, max: 1).FirstChain;

    /// <summary>
    /// Combines what <see cref="Search"/> and <see cref="FindFirstMatchChain"/> each compute into a
    /// single tree walk. A caller (the tree view) that needs both the match count/paths *and* the
    /// chain to reveal the first hit should call this once instead of calling both of those — over a
    /// large tree (tens of thousands of nodes) doing two independent full walks back-to-back roughly
    /// doubles a synchronous, UI-thread search's cost for no benefit, since both walks visit the same
    /// nodes in the same order.
    /// </summary>
    public static JsonSearchResult SearchAll(JsonTree tree, string query, int max = 500)
    {
        ArgumentNullException.ThrowIfNull(tree);
        var paths = new List<string>(Math.Min(max, 64));
        List<JsonTreeNode>? firstChain = null;
        foreach (var chain in SearchChains(tree, query, max))
        {
            firstChain ??= chain;
            paths.Add(chain[^1].Path);
        }
        return new JsonSearchResult(paths, firstChain);
    }

    private sealed class ChainLink(JsonTreeNode node, ChainLink? parent)
    {
        public JsonTreeNode Node { get; } = node;
        public ChainLink? Parent { get; } = parent;
    }

    private static IEnumerable<List<JsonTreeNode>> SearchChains(JsonTree tree, string query, int max)
    {
        if (string.IsNullOrEmpty(query) || max <= 0)
            yield break;

        var count = 0;
        var stack = new Stack<ChainLink>();
        stack.Push(new ChainLink(tree.Root, null));

        while (stack.Count > 0 && count < max)
        {
            var link = stack.Pop();
            var node = link.Node;

            if (node.MatchesKey(query) || node.MatchesValue(query))
            {
                count++;
                yield return BuildChain(link);
                if (count >= max)
                    yield break;
            }

            if (node.ChildCount > 0)
            {
                var children = node.Children();
                for (var i = children.Count - 1; i >= 0; i--) // reverse push -> pop order matches document order
                    stack.Push(new ChainLink(children[i], link));
            }
        }
    }

    private static List<JsonTreeNode> BuildChain(ChainLink link)
    {
        var list = new List<JsonTreeNode>();
        for (var l = link; l is not null; l = l.Parent)
            list.Add(l.Node);
        list.Reverse();
        return list;
    }
}
