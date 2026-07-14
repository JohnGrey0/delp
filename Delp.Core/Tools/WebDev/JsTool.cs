using NUglify;

namespace Delp.Core.Tools.WebDev;

/// <summary>JavaScript minifier backed by NUglify.</summary>
public static class JsTool
{
    /// <summary>
    /// Minifies JavaScript. Never throws for malformed input — parse errors are returned in
    /// <see cref="MinifyResult.Errors"/> alongside whatever best-effort code NUglify produced.
    /// </summary>
    public static MinifyResult Minify(string js)
    {
        js ??= "";
        var before = System.Text.Encoding.UTF8.GetByteCount(js);
        var result = Uglify.Js(js);
        var code = result.Code;
        var after = string.IsNullOrEmpty(code) ? 0 : System.Text.Encoding.UTF8.GetByteCount(code);
        return new MinifyResult(code, NUglifyErrors.Format(result.Errors), before, after);
    }
}
