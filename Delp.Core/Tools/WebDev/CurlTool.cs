using System.Text;
using System.Text.Json;
using Delp.Core.Tools.Common;

namespace Delp.Core.Tools.WebDev;

public enum CurlBodyKind
{
    None,
    Raw,
    UrlEncodedForm,
    Multipart,
    Json,
}

public enum CurlTarget
{
    CSharp,
    Python,
    JavaScript,
    PowerShell,
    Go,
}

public sealed record CurlHeader(string Name, string Value);

public sealed record CurlFormPart(string Name, string Value, bool IsFile);

public sealed record CurlRequest(
    string Method,
    string Url,
    IReadOnlyList<CurlHeader> Headers,
    IReadOnlyList<CurlHeader> Cookies,
    CurlBodyKind BodyKind,
    string? Body,
    IReadOnlyList<CurlFormPart> FormParts,
    (string User, string Password)? UserAuth,
    bool Insecure,
    bool FollowRedirects,
    bool Compressed,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Parses a pasted <c>curl</c> command line into a structured <see cref="CurlRequest"/> and
/// generates equivalent HTTP-client code in several languages, or a canonical curl line back.
/// Parsing never throws — unrecognized flags are collected as warnings instead, since this
/// tool exists to make sense of whatever a human pasted in, not to validate it.
/// </summary>
public static class CurlTool
{
    public static CurlRequest Parse(string command)
    {
        var tokens = ShellTokenizer.Tokenize(command ?? "");
        var warnings = new List<string>();
        var headers = new List<CurlHeader>();
        var cookies = new List<CurlHeader>();
        var dataPieces = new List<string>();
        var urlEncodedPieces = new List<string>();
        var formParts = new List<CurlFormPart>();
        string? url = null;
        string? explicitMethod = null;
        var headFlag = false;
        var getFlag = false;
        string? jsonBody = null;
        (string User, string Password)? userAuth = null;
        var insecure = false;
        var followRedirects = false;
        var compressed = false;

        var i = 0;
        // People usually paste the whole command including the leading "curl" — skip it.
        if (tokens.Count > 0 && tokens[0].Equals("curl", StringComparison.OrdinalIgnoreCase))
            i = 1;

        string? NextArg(string flag)
        {
            if (i + 1 < tokens.Count)
                return tokens[++i];
            warnings.Add($"'{flag}' expects a value but none was given — ignored.");
            return null;
        }

        for (; i < tokens.Count; i++)
        {
            var tok = tokens[i];
            if (tok.Length == 0)
                continue;

            switch (tok)
            {
                case "-X" or "--request":
                {
                    var v = NextArg(tok);
                    if (v is not null) explicitMethod = v.ToUpperInvariant();
                    break;
                }
                case "-H" or "--header":
                {
                    var v = NextArg(tok);
                    if (v is not null) AddHeader(headers, v, warnings);
                    break;
                }
                case "-d" or "--data" or "--data-ascii":
                {
                    var v = NextArg(tok);
                    if (v is not null) dataPieces.Add(StripFileMarker(v, warnings, tok));
                    break;
                }
                case "--data-raw":
                {
                    var v = NextArg(tok);
                    if (v is not null) dataPieces.Add(v);
                    break;
                }
                case "--data-binary":
                {
                    var v = NextArg(tok);
                    if (v is not null) dataPieces.Add(StripFileMarker(v, warnings, tok));
                    break;
                }
                case "--data-urlencode":
                {
                    var v = NextArg(tok);
                    if (v is not null) urlEncodedPieces.Add(EncodeDataUrlencodePiece(v, warnings));
                    break;
                }
                case "--json":
                {
                    var v = NextArg(tok);
                    if (v is not null) jsonBody = v;
                    break;
                }
                case "-F" or "--form":
                {
                    var v = NextArg(tok);
                    if (v is not null) formParts.Add(ParseFormPart(v));
                    break;
                }
                case "-u" or "--user":
                {
                    var v = NextArg(tok);
                    if (v is not null) userAuth = SplitUser(v);
                    break;
                }
                case "-b" or "--cookie":
                {
                    var v = NextArg(tok);
                    if (v is not null) cookies.AddRange(ParseCookies(v));
                    break;
                }
                case "-A" or "--user-agent":
                {
                    var v = NextArg(tok);
                    if (v is not null) headers.Add(new CurlHeader("User-Agent", v));
                    break;
                }
                case "-e" or "--referer":
                {
                    var v = NextArg(tok);
                    if (v is not null) headers.Add(new CurlHeader("Referer", v));
                    break;
                }
                case "-k" or "--insecure":
                    insecure = true;
                    break;
                case "-L" or "--location":
                    followRedirects = true;
                    break;
                case "--compressed":
                    compressed = true;
                    break;
                case "-I" or "--head":
                    headFlag = true;
                    break;
                case "-G" or "--get":
                    getFlag = true;
                    break;
                case "--url":
                {
                    var v = NextArg(tok);
                    if (v is not null) url = v;
                    break;
                }
                case "-o" or "--output":
                    NextArg(tok); // filename — ignored, but still consume the argument.
                    break;
                case "-s" or "--silent":
                case "-v" or "--verbose":
                    break; // ignored, no argument.
                default:
                    if (tok.Length > 1 && tok[0] == '-')
                        warnings.Add($"Unrecognized flag '{tok}' ignored.");
                    else
                        url ??= tok;
                    break;
            }
        }

        url ??= "";
        if (url.Length == 0)
            warnings.Add("No URL found in the command.");

        CurlBodyKind bodyKind;
        string? body = null;

        if (formParts.Count > 0)
        {
            bodyKind = CurlBodyKind.Multipart;
            if (dataPieces.Count > 0 || urlEncodedPieces.Count > 0 || jsonBody is not null)
                warnings.Add("Multiple body sources given — using the multipart form (-F) and ignoring the rest.");
        }
        else if (jsonBody is not null)
        {
            bodyKind = CurlBodyKind.Json;
            body = PrettyPrintIfJson(jsonBody);
            if (!headers.Any(h => h.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)))
                headers.Add(new CurlHeader("Content-Type", "application/json"));
            if (!headers.Any(h => h.Name.Equals("Accept", StringComparison.OrdinalIgnoreCase)))
                headers.Add(new CurlHeader("Accept", "application/json"));
            if (dataPieces.Count > 0 || urlEncodedPieces.Count > 0)
                warnings.Add("Both --json and -d/--data given — using --json and ignoring the rest.");
        }
        else if (dataPieces.Count > 0 || urlEncodedPieces.Count > 0)
        {
            var allPieces = new List<string>(dataPieces.Count + urlEncodedPieces.Count);
            allPieces.AddRange(dataPieces);
            allPieces.AddRange(urlEncodedPieces);
            var combined = string.Join("&", allPieces);

            if (getFlag)
            {
                url = AppendQuery(url, combined);
                bodyKind = CurlBodyKind.None;
            }
            else
            {
                bodyKind = LooksLikeFormPairs(allPieces) ? CurlBodyKind.UrlEncodedForm : CurlBodyKind.Raw;
                body = combined;
            }
        }
        else
        {
            bodyKind = CurlBodyKind.None;
        }

        var method = explicitMethod
            ?? (headFlag ? "HEAD" : null)
            ?? (bodyKind != CurlBodyKind.None ? "POST" : "GET");

        return new CurlRequest(
            method, url, headers, cookies, bodyKind, body, formParts,
            userAuth, insecure, followRedirects, compressed, warnings);
    }

    /// <summary>Generates idiomatic client code for <paramref name="request"/> in the given language.</summary>
    public static string Generate(CurlRequest request, CurlTarget target)
    {
        ArgumentNullException.ThrowIfNull(request);
        return target switch
        {
            CurlTarget.CSharp => GenerateCSharp(request),
            CurlTarget.Python => GeneratePython(request),
            CurlTarget.JavaScript => GenerateJavaScript(request),
            CurlTarget.PowerShell => GeneratePowerShell(request),
            CurlTarget.Go => GenerateGo(request),
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };
    }

    /// <summary>Emits a canonical, single-line <c>curl</c> command equivalent to <paramref name="request"/>.
    /// Re-parsing the result reproduces the same method/URL/headers/body — a stable round trip.</summary>
    public static string ToCurl(CurlRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sb = new StringBuilder("curl");

        sb.Append(" -X ").Append(request.Method);
        sb.Append(' ').Append(Quote(request.Url));

        foreach (var h in request.Headers)
            sb.Append(" -H ").Append(Quote($"{h.Name}: {h.Value}"));

        if (request.Cookies.Count > 0)
            sb.Append(" -b ").Append(Quote(string.Join("; ", request.Cookies.Select(c => $"{c.Name}={c.Value}"))));

        if (request.UserAuth is { } auth)
            sb.Append(" -u ").Append(Quote($"{auth.User}:{auth.Password}"));

        if (request.Insecure) sb.Append(" -k");
        if (request.FollowRedirects) sb.Append(" -L");
        if (request.Compressed) sb.Append(" --compressed");

        switch (request.BodyKind)
        {
            case CurlBodyKind.Json:
                sb.Append(" --json ").Append(Quote(request.Body ?? ""));
                break;
            case CurlBodyKind.UrlEncodedForm or CurlBodyKind.Raw:
                sb.Append(" --data-raw ").Append(Quote(request.Body ?? ""));
                break;
            case CurlBodyKind.Multipart:
                foreach (var p in request.FormParts)
                    sb.Append(" -F ").Append(Quote(p.IsFile ? $"{p.Name}=@{p.Value}" : $"{p.Name}={p.Value}"));
                break;
        }

        return sb.ToString();
    }

    // ---- flag parsing helpers ----

    private static void AddHeader(List<CurlHeader> headers, string raw, List<string> warnings)
    {
        var idx = raw.IndexOf(':');
        if (idx < 0)
        {
            warnings.Add($"Header '{raw}' is missing a ':' — ignored.");
            return;
        }
        var name = raw[..idx].Trim();
        var value = raw[(idx + 1)..].Trim();
        if (name.Length == 0)
        {
            warnings.Add($"Header '{raw}' has an empty name — ignored.");
            return;
        }
        headers.Add(new CurlHeader(name, value));
    }

    private static string StripFileMarker(string value, List<string> warnings, string flag)
    {
        if (value.StartsWith('@'))
            warnings.Add($"{flag} references a file ('{value}') — file contents aren't read; used as literal text.");
        return value;
    }

    /// <summary>Implements the --data-urlencode content forms: <c>content</c>, <c>=content</c>,
    /// <c>name=content</c>, <c>@file</c> and <c>name@file</c> (the last two can't read a file here,
    /// so they degrade to an empty value with a warning).</summary>
    private static string EncodeDataUrlencodePiece(string raw, List<string> warnings)
    {
        var atIdx = raw.IndexOf('@');
        if (atIdx >= 0)
        {
            warnings.Add($"--data-urlencode '{raw}' references a file — file contents aren't read; treated as empty.");
            var name = raw[..atIdx];
            return name.Length > 0 ? $"{name}=" : "";
        }
        if (raw.StartsWith('='))
            return Uri.EscapeDataString(raw[1..]);

        var eqIdx = raw.IndexOf('=');
        if (eqIdx >= 0)
        {
            var name = raw[..eqIdx];
            var value = raw[(eqIdx + 1)..];
            return $"{name}={Uri.EscapeDataString(value)}";
        }
        return Uri.EscapeDataString(raw);
    }

    private static CurlFormPart ParseFormPart(string raw)
    {
        var eq = raw.IndexOf('=');
        if (eq < 0)
            return new CurlFormPart(raw, "", false);

        var name = raw[..eq];
        var rest = raw[(eq + 1)..];
        if (rest.StartsWith('@'))
        {
            var semi = rest.IndexOf(';');
            var path = semi >= 0 ? rest[1..semi] : rest[1..];
            return new CurlFormPart(name, path, true);
        }
        return new CurlFormPart(name, rest, false);
    }

    private static (string User, string Password) SplitUser(string raw)
    {
        var idx = raw.IndexOf(':');
        return idx < 0 ? (raw, "") : (raw[..idx], raw[(idx + 1)..]);
    }

    private static IEnumerable<CurlHeader> ParseCookies(string raw)
    {
        foreach (var part in raw.Split(';'))
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0) continue;
            var eq = trimmed.IndexOf('=');
            yield return eq < 0
                ? new CurlHeader(trimmed, "")
                : new CurlHeader(trimmed[..eq].Trim(), trimmed[(eq + 1)..].Trim());
        }
    }

    /// <summary>True when every non-blank piece looks like a <c>key=value</c> form pair rather than
    /// a raw payload (JSON/XML/plain text) — decides UrlEncodedForm vs. Raw body classification.</summary>
    private static bool LooksLikeFormPairs(IReadOnlyList<string> pieces)
    {
        var any = false;
        foreach (var piece in pieces)
        {
            var trimmed = piece.TrimStart();
            if (trimmed.Length == 0) continue;
            any = true;
            if (trimmed[0] is '{' or '[') return false;
            if (!piece.Contains('=')) return false;
        }
        return any;
    }

    private static string AppendQuery(string url, string query)
    {
        if (query.Length == 0) return url;
        if (url.Length == 0) return "?" + query;
        return url.Contains('?') ? $"{url}&{query}" : $"{url}?{query}";
    }

    private static string PrettyPrintIfJson(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return text;
        }
    }

    private static IEnumerable<(string Key, string Value)> ParseFormPairs(string body)
    {
        if (string.IsNullOrEmpty(body)) yield break;
        foreach (var pair in body.Split('&'))
        {
            if (pair.Length == 0) continue;
            var eq = pair.IndexOf('=');
            if (eq < 0) { yield return (Uri.UnescapeDataString(pair), ""); continue; }
            yield return (Uri.UnescapeDataString(pair[..eq]), Uri.UnescapeDataString(pair[(eq + 1)..]));
        }
    }

    private static bool IsContentHeader(string name) =>
        name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase);

    private static string Quote(string s) => "'" + s.Replace("'", "'\\''") + "'";

    // ---- code generators ----

    private static string GenerateCSharp(CurlRequest r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Net.Http.Headers;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine();
        sb.AppendLine("var handler = new HttpClientHandler();");
        if (r.Insecure)
            sb.AppendLine("handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;");
        if (!r.FollowRedirects)
            sb.AppendLine("handler.AllowAutoRedirect = false;");
        sb.AppendLine("using var client = new HttpClient(handler);");
        sb.AppendLine();
        sb.AppendLine($"using var request = new HttpRequestMessage(new HttpMethod(\"{EscapeCSharp(r.Method)}\"), \"{EscapeCSharp(r.Url)}\");");

        foreach (var h in r.Headers.Where(h => !IsContentHeader(h.Name)))
            sb.AppendLine($"request.Headers.TryAddWithoutValidation(\"{EscapeCSharp(h.Name)}\", \"{EscapeCSharp(h.Value)}\");");

        if (r.UserAuth is { } auth)
        {
            sb.AppendLine($"var authBytes = Encoding.UTF8.GetBytes(\"{EscapeCSharp(auth.User)}:{EscapeCSharp(auth.Password)}\");");
            sb.AppendLine("request.Headers.Authorization = new AuthenticationHeaderValue(\"Basic\", Convert.ToBase64String(authBytes));");
        }

        switch (r.BodyKind)
        {
            case CurlBodyKind.Json:
                sb.AppendLine($"request.Content = new StringContent(@\"{EscapeVerbatim(r.Body ?? "")}\", Encoding.UTF8, \"application/json\");");
                break;
            case CurlBodyKind.UrlEncodedForm:
                sb.AppendLine("request.Content = new FormUrlEncodedContent(new Dictionary<string, string>");
                sb.AppendLine("{");
                foreach (var (k, v) in ParseFormPairs(r.Body ?? ""))
                    sb.AppendLine($"    [\"{EscapeCSharp(k)}\"] = \"{EscapeCSharp(v)}\",");
                sb.AppendLine("});");
                break;
            case CurlBodyKind.Raw:
                sb.AppendLine($"request.Content = new StringContent(@\"{EscapeVerbatim(r.Body ?? "")}\");");
                break;
            case CurlBodyKind.Multipart:
                sb.AppendLine("var form = new MultipartFormDataContent();");
                foreach (var p in r.FormParts)
                    sb.AppendLine(p.IsFile
                        ? $"form.Add(new StreamContent(File.OpenRead(\"{EscapeCSharp(p.Value)}\")), \"{EscapeCSharp(p.Name)}\", Path.GetFileName(\"{EscapeCSharp(p.Value)}\"));"
                        : $"form.Add(new StringContent(\"{EscapeCSharp(p.Value)}\"), \"{EscapeCSharp(p.Name)}\");");
                sb.AppendLine("request.Content = form;");
                break;
        }

        sb.AppendLine();
        sb.AppendLine("using var response = await client.SendAsync(request);");
        sb.AppendLine("var body = await response.Content.ReadAsStringAsync();");
        sb.AppendLine("Console.WriteLine(body);");

        return sb.ToString().TrimEnd() + "\n";
    }

    private static string GeneratePython(CurlRequest r)
    {
        var sb = new StringBuilder();
        var headerNames = r.Headers.Where(h => !IsContentHeader(h.Name)).ToList();
        var nonFileFormParts = r.FormParts.Where(p => !p.IsFile).ToList();

        sb.AppendLine("import requests");
        if (r.BodyKind == CurlBodyKind.Json) sb.AppendLine("import json");
        sb.AppendLine();
        sb.AppendLine($"url = \"{EscapePython(r.Url)}\"");

        if (headerNames.Count > 0)
        {
            sb.AppendLine("headers = {");
            foreach (var h in headerNames)
                sb.AppendLine($"    \"{EscapePython(h.Name)}\": \"{EscapePython(h.Value)}\",");
            sb.AppendLine("}");
        }

        switch (r.BodyKind)
        {
            case CurlBodyKind.Json:
                sb.AppendLine($"payload = json.loads(r\"\"\"{r.Body}\"\"\")");
                break;
            case CurlBodyKind.UrlEncodedForm:
                sb.AppendLine("data = {");
                foreach (var (k, v) in ParseFormPairs(r.Body ?? ""))
                    sb.AppendLine($"    \"{EscapePython(k)}\": \"{EscapePython(v)}\",");
                sb.AppendLine("}");
                break;
            case CurlBodyKind.Raw:
                sb.AppendLine($"data = r\"\"\"{r.Body}\"\"\"");
                break;
            case CurlBodyKind.Multipart:
                sb.AppendLine("files = {");
                foreach (var p in r.FormParts.Where(p => p.IsFile))
                    sb.AppendLine($"    \"{EscapePython(p.Name)}\": open(\"{EscapePython(p.Value)}\", \"rb\"),");
                sb.AppendLine("}");
                if (nonFileFormParts.Count > 0)
                {
                    sb.AppendLine("data = {");
                    foreach (var p in nonFileFormParts)
                        sb.AppendLine($"    \"{EscapePython(p.Name)}\": \"{EscapePython(p.Value)}\",");
                    sb.AppendLine("}");
                }
                break;
        }

        sb.Append($"response = requests.request(\"{r.Method.ToLowerInvariant()}\", url");
        if (headerNames.Count > 0) sb.Append(", headers=headers");
        switch (r.BodyKind)
        {
            case CurlBodyKind.Json: sb.Append(", json=payload"); break;
            case CurlBodyKind.UrlEncodedForm or CurlBodyKind.Raw: sb.Append(", data=data"); break;
            case CurlBodyKind.Multipart:
                sb.Append(", files=files");
                if (nonFileFormParts.Count > 0) sb.Append(", data=data");
                break;
        }
        if (r.UserAuth is { } auth) sb.Append($", auth=(\"{EscapePython(auth.User)}\", \"{EscapePython(auth.Password)}\")");
        if (r.Insecure) sb.Append(", verify=False");
        if (!r.FollowRedirects) sb.Append(", allow_redirects=False");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("print(response.text)");

        return sb.ToString().TrimEnd() + "\n";
    }

    private static string GenerateJavaScript(CurlRequest r)
    {
        var sb = new StringBuilder();
        var headerNames = r.Headers
            .Where(h => !IsContentHeader(h.Name) && !h.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            .ToList();

        sb.AppendLine("const options = {");
        sb.AppendLine($"  method: \"{r.Method}\",");
        if (headerNames.Count > 0 || r.UserAuth is not null)
        {
            sb.AppendLine("  headers: {");
            foreach (var h in headerNames)
                sb.AppendLine($"    \"{EscapeJs(h.Name)}\": \"{EscapeJs(h.Value)}\",");
            if (r.UserAuth is { } a)
                sb.AppendLine($"    \"Authorization\": \"Basic \" + btoa(\"{EscapeJs(a.User)}:{EscapeJs(a.Password)}\"),");
            sb.AppendLine("  },");
        }
        if (!r.FollowRedirects)
            sb.AppendLine("  redirect: \"manual\",");

        switch (r.BodyKind)
        {
            case CurlBodyKind.Json:
                sb.AppendLine($"  body: JSON.stringify({r.Body ?? "{}"}),");
                break;
            case CurlBodyKind.UrlEncodedForm:
                sb.AppendLine("  body: new URLSearchParams({");
                foreach (var (k, v) in ParseFormPairs(r.Body ?? ""))
                    sb.AppendLine($"    \"{EscapeJs(k)}\": \"{EscapeJs(v)}\",");
                sb.AppendLine("  }),");
                break;
            case CurlBodyKind.Raw:
                sb.AppendLine($"  body: {JsTemplateLiteral(r.Body ?? "")},");
                break;
            case CurlBodyKind.Multipart:
                sb.AppendLine("  body: (() => {");
                sb.AppendLine("    const form = new FormData();");
                foreach (var p in r.FormParts)
                    sb.AppendLine(p.IsFile
                        ? $"    // form.append(\"{EscapeJs(p.Name)}\", fileInput.files[0]); // source file: {EscapeJs(p.Value)}"
                        : $"    form.append(\"{EscapeJs(p.Name)}\", \"{EscapeJs(p.Value)}\");");
                sb.AppendLine("    return form;");
                sb.AppendLine("  })(),");
                break;
        }

        sb.AppendLine("};");
        sb.AppendLine();
        if (r.Insecure)
            sb.AppendLine("// Note: fetch has no per-request TLS-verification toggle (-k / --insecure has no equivalent).");
        sb.AppendLine($"const response = await fetch(\"{EscapeJs(r.Url)}\", options);");
        sb.AppendLine("const body = await response.text();");
        sb.AppendLine("console.log(body);");

        return sb.ToString().TrimEnd() + "\n";
    }

    private static string GeneratePowerShell(CurlRequest r)
    {
        var sb = new StringBuilder();
        var headerNames = r.Headers.Where(h => !IsContentHeader(h.Name)).ToList();

        sb.AppendLine("$params = @{");
        sb.AppendLine($"    Uri    = \"{EscapePs(r.Url)}\"");
        sb.AppendLine($"    Method = \"{EscapePs(r.Method)}\"");

        if (headerNames.Count > 0)
        {
            sb.AppendLine("    Headers = @{");
            foreach (var h in headerNames)
                sb.AppendLine($"        \"{EscapePs(h.Name)}\" = \"{EscapePs(h.Value)}\"");
            sb.AppendLine("    }");
        }

        switch (r.BodyKind)
        {
            case CurlBodyKind.Json:
                sb.Append("    Body = @'\n").Append(r.Body).AppendLine("\n'@");
                sb.AppendLine("    ContentType = \"application/json\"");
                break;
            case CurlBodyKind.UrlEncodedForm:
                sb.AppendLine("    Body = @{");
                foreach (var (k, v) in ParseFormPairs(r.Body ?? ""))
                    sb.AppendLine($"        \"{EscapePs(k)}\" = \"{EscapePs(v)}\"");
                sb.AppendLine("    }");
                break;
            case CurlBodyKind.Raw:
                sb.Append("    Body = @'\n").Append(r.Body).AppendLine("\n'@");
                break;
            case CurlBodyKind.Multipart:
                sb.AppendLine("    Form = @{");
                foreach (var p in r.FormParts)
                    sb.AppendLine(p.IsFile
                        ? $"        \"{EscapePs(p.Name)}\" = Get-Item -Path \"{EscapePs(p.Value)}\""
                        : $"        \"{EscapePs(p.Name)}\" = \"{EscapePs(p.Value)}\"");
                sb.AppendLine("    }");
                break;
        }

        if (r.UserAuth is { } auth)
        {
            sb.AppendLine($"    Credential = [PSCredential]::new(\"{EscapePs(auth.User)}\", (ConvertTo-SecureString \"{EscapePs(auth.Password)}\" -AsPlainText -Force))");
            sb.AppendLine("    Authentication = \"Basic\"");
        }
        if (r.Insecure)
            sb.AppendLine("    SkipCertificateCheck = $true");
        if (!r.FollowRedirects)
            sb.AppendLine("    MaximumRedirection = 0");

        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Invoke-RestMethod @params");

        return sb.ToString().TrimEnd() + "\n";
    }

    private static string GenerateGo(CurlRequest r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("package main");
        sb.AppendLine();
        sb.AppendLine("import (");
        sb.AppendLine("\t\"fmt\"");
        sb.AppendLine("\t\"io\"");
        sb.AppendLine("\t\"net/http\"");
        if (r.BodyKind is CurlBodyKind.Json or CurlBodyKind.Raw or CurlBodyKind.UrlEncodedForm)
            sb.AppendLine("\t\"strings\"");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("func main() {");

        var bodyExpr = "nil";
        if (r.BodyKind is CurlBodyKind.Json or CurlBodyKind.Raw or CurlBodyKind.UrlEncodedForm)
        {
            sb.AppendLine($"\tpayload := strings.NewReader({GoBodyLiteral(r.Body ?? "")})");
            bodyExpr = "payload";
        }
        else if (r.BodyKind == CurlBodyKind.Multipart)
        {
            sb.AppendLine("\t// multipart/form-data bodies need a multipart.Writer — build it, then pass its Reader here.");
        }

        sb.AppendLine();
        sb.AppendLine($"\treq, err := http.NewRequest(\"{r.Method}\", \"{GoQuoteSafe(r.Url)}\", {bodyExpr})");
        sb.AppendLine("\tif err != nil {");
        sb.AppendLine("\t\tpanic(err)");
        sb.AppendLine("\t}");
        sb.AppendLine();

        foreach (var h in r.Headers.Where(h => !IsContentHeader(h.Name)))
            sb.AppendLine($"\treq.Header.Set(\"{GoQuoteSafe(h.Name)}\", \"{GoQuoteSafe(h.Value)}\")");
        if (r.BodyKind == CurlBodyKind.Json)
            sb.AppendLine("\treq.Header.Set(\"Content-Type\", \"application/json\")");
        if (r.BodyKind == CurlBodyKind.UrlEncodedForm)
            sb.AppendLine("\treq.Header.Set(\"Content-Type\", \"application/x-www-form-urlencoded\")");
        if (r.UserAuth is { } auth)
            sb.AppendLine($"\treq.SetBasicAuth(\"{GoQuoteSafe(auth.User)}\", \"{GoQuoteSafe(auth.Password)}\")");

        sb.AppendLine();
        sb.AppendLine("\tclient := &http.Client{}");
        if (r.Insecure)
            sb.AppendLine("\t// Note: -k/--insecure — set client.Transport with TLSClientConfig{InsecureSkipVerify: true} to match.");
        if (!r.FollowRedirects)
            sb.AppendLine("\tclient.CheckRedirect = func(req *http.Request, via []*http.Request) error { return http.ErrUseLastResponse }");
        sb.AppendLine("\tresp, err := client.Do(req)");
        sb.AppendLine("\tif err != nil {");
        sb.AppendLine("\t\tpanic(err)");
        sb.AppendLine("\t}");
        sb.AppendLine("\tdefer resp.Body.Close()");
        sb.AppendLine();
        sb.AppendLine("\tbody, _ := io.ReadAll(resp.Body)");
        sb.AppendLine("\tfmt.Println(string(body))");
        sb.Append('}');

        return sb.ToString() + "\n";
    }

    // ---- per-language escaping ----

    private static string EscapeCSharp(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string EscapeVerbatim(string s) => s.Replace("\"", "\"\"");

    private static string EscapePython(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string EscapeJs(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string JsTemplateLiteral(string s) =>
        "`" + s.Replace("\\", "\\\\").Replace("`", "\\`").Replace("${", "\\${") + "`";

    private static string EscapePs(string s) => s.Replace("`", "``").Replace("\"", "`\"").Replace("$", "`$");

    private static string GoQuoteSafe(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>Go raw string literals (backticks) can't contain a backtick; fall back to an
    /// interpreted string when the payload has one.</summary>
    private static string GoBodyLiteral(string s)
    {
        if (!s.Contains('`'))
            return "`" + s + "`";
        return "\"" + GoQuoteSafe(s).Replace("\n", "\\n").Replace("\r", "") + "\"";
    }
}
