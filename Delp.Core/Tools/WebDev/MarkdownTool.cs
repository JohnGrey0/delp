using Markdig;

namespace Delp.Core.Tools.WebDev;

/// <summary>Markdown → HTML rendering (Markdig, advanced extensions) for the live preview tool.</summary>
public static class MarkdownTool
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    /// <summary>Renders Markdown to an HTML body fragment (tables, task lists, autolinks, …).</summary>
    public static string ToHtml(string markdown) => Markdown.ToHtml(markdown ?? "", Pipeline);

    /// <summary>Wraps a rendered body fragment in a full HTML document with embedded dark CSS.</summary>
    public static string WrapDocument(string bodyHtml) =>
        "<!doctype html><html><head><meta charset=\"utf-8\" />" +
        "<style>" + Css + "</style></head><body>" + bodyHtml + "</body></html>";

    private const string Css = @"
body{background:#1E2126;color:#F2F4F7;font-family:'Segoe UI',sans-serif;max-width:66ch;margin:24px auto;line-height:1.6;padding:0 16px;word-wrap:break-word;}
a{color:#0A84FF;}
h1,h2,h3,h4,h5,h6{color:#F2F4F7;line-height:1.3;}
p,ul,ol,blockquote,table{margin:0 0 14px 0;}
pre{background:#23262B;padding:10px 12px;border-radius:6px;overflow-x:auto;}
code{font-family:Consolas,'Cascadia Mono',monospace;background:#20242A;padding:2px 5px;border-radius:4px;font-size:0.92em;}
pre code{background:transparent;padding:0;}
table{border-collapse:collapse;width:100%;}
th,td{border:1px solid #38434D;padding:6px 10px;text-align:left;}
blockquote{border-left:3px solid #0A84FF;margin-left:0;padding:2px 12px;color:#B7BDC6;}
img{max-width:100%;}
hr{border:none;border-top:1px solid #38434D;margin:20px 0;}
input[type=checkbox]{margin-right:6px;}
";
}
