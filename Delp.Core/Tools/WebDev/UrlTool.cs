using System.Globalization;
using System.Text;

namespace Delp.Core.Tools.WebDev;

public sealed record QueryParam(string Key, string Value);

public sealed record UrlParts(
    string Scheme,
    string Host,
    string HostUnicode,
    int? Port,
    string Path,
    IReadOnlyList<QueryParam> Query,
    string Fragment,
    string? UserInfo);

/// <summary>Hand-rolled URL parser/builder that preserves query-string order, duplicate keys and empty values.</summary>
public static class UrlTool
{
    /// <exception cref="FormatException">The input is not a URL, even after assuming an "https://" scheme.</exception>
    public static UrlParts Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new FormatException("URL is empty.");

        var raw = input.Trim();

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            if (!Uri.TryCreate("https://" + raw, UriKind.Absolute, out uri))
                throw new FormatException($"'{input}' is not a valid URL.");
        }

        var idn = new IdnMapping();
        string asciiHost;
        string unicodeHost;
        if (uri.HostNameType == UriHostNameType.Dns)
        {
            asciiHost = uri.IdnHost;
            try { unicodeHost = idn.GetUnicode(asciiHost); }
            catch (ArgumentException) { unicodeHost = asciiHost; }
        }
        else
        {
            asciiHost = uri.Host;
            unicodeHost = uri.Host;
        }

        string? userInfo = string.IsNullOrEmpty(uri.UserInfo) ? null : Uri.UnescapeDataString(uri.UserInfo);
        int? port = uri.IsDefaultPort ? null : uri.Port;
        var query = ParseQuery(uri.Query);
        var fragment = uri.Fragment.Length > 0
            ? Uri.UnescapeDataString(uri.Fragment.TrimStart('#'))
            : "";

        return new UrlParts(uri.Scheme, asciiHost, unicodeHost, port, uri.AbsolutePath, query, fragment, userInfo);
    }

    /// <summary>Rebuilds a canonical URL string from its parts, re-encoding as needed.</summary>
    public static string Build(UrlParts parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var sb = new StringBuilder();
        sb.Append(parts.Scheme).Append("://");

        if (!string.IsNullOrEmpty(parts.UserInfo))
            sb.Append(EncodeUserInfo(parts.UserInfo)).Append('@');

        sb.Append(parts.Host);

        if (parts.Port is int port)
            sb.Append(':').Append(port.ToString(CultureInfo.InvariantCulture));

        sb.Append(string.IsNullOrEmpty(parts.Path) ? "/" : parts.Path);

        if (parts.Query.Count > 0)
        {
            sb.Append('?');
            sb.Append(string.Join("&", parts.Query.Select(EncodeQueryParam)));
        }

        if (!string.IsNullOrEmpty(parts.Fragment))
            sb.Append('#').Append(Uri.EscapeDataString(parts.Fragment));

        return sb.ToString();
    }

    private static IReadOnlyList<QueryParam> ParseQuery(string queryString)
    {
        var list = new List<QueryParam>();
        if (string.IsNullOrEmpty(queryString))
            return list;

        var s = queryString.TrimStart('?');
        if (s.Length == 0)
            return list;

        foreach (var pair in s.Split('&'))
        {
            if (pair.Length == 0)
                continue;

            var eq = pair.IndexOf('=');
            if (eq < 0)
                list.Add(new QueryParam(Uri.UnescapeDataString(pair), ""));
            else
                list.Add(new QueryParam(
                    Uri.UnescapeDataString(pair[..eq]),
                    Uri.UnescapeDataString(pair[(eq + 1)..])));
        }

        return list;
    }

    private static string EncodeQueryParam(QueryParam q)
    {
        var key = Uri.EscapeDataString(q.Key ?? "");
        return string.IsNullOrEmpty(q.Value) ? key : $"{key}={Uri.EscapeDataString(q.Value)}";
    }

    private static string EncodeUserInfo(string userInfo)
    {
        var idx = userInfo.IndexOf(':');
        if (idx < 0)
            return Uri.EscapeDataString(userInfo);
        return Uri.EscapeDataString(userInfo[..idx]) + ":" + Uri.EscapeDataString(userInfo[(idx + 1)..]);
    }
}
