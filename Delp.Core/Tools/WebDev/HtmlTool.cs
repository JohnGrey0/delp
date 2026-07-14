using NUglify;
using NUglify.Html;

namespace Delp.Core.Tools.WebDev;

public sealed record HtmlMinifyOptions(bool RemoveComments, bool CollapseWhitespace);

/// <summary>HTML minifier backed by NUglify.</summary>
public static class HtmlTool
{
    public static MinifyResult Minify(string html, HtmlMinifyOptions options)
    {
        html ??= "";
        var before = System.Text.Encoding.UTF8.GetByteCount(html);

        var settings = new HtmlSettings
        {
            RemoveComments = options.RemoveComments,
            CollapseWhitespaces = options.CollapseWhitespace,
        };

        var result = Uglify.Html(html, settings);
        var code = result.Code;
        var after = string.IsNullOrEmpty(code) ? 0 : System.Text.Encoding.UTF8.GetByteCount(code);
        return new MinifyResult(code, NUglifyErrors.Format(result.Errors), before, after);
    }
}
