using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class MarkdownToolTests
{
    [Fact]
    public void ToHtml_Heading_ProducesH1()
    {
        var html = MarkdownTool.ToHtml("# Hello");
        Assert.Contains("<h1", html);
        Assert.Contains("Hello</h1>", html);
    }

    [Fact]
    public void ToHtml_Bold_ProducesStrong()
    {
        var html = MarkdownTool.ToHtml("this is **bold** text");
        Assert.Contains("<strong>bold</strong>", html);
    }

    [Fact]
    public void ToHtml_Table_ProducesTableMarkup()
    {
        const string md = "| A | B |\n| - | - |\n| 1 | 2 |\n";
        var html = MarkdownTool.ToHtml(md);
        Assert.Contains("<table>", html);
        Assert.Contains("<th>A</th>", html);
        Assert.Contains("<td>1</td>", html);
    }

    [Fact]
    public void ToHtml_TaskList_ProducesCheckboxInput()
    {
        const string md = "- [x] done\n- [ ] todo\n";
        var html = MarkdownTool.ToHtml(md);
        Assert.Contains("type=\"checkbox\"", html);
        Assert.Contains("checked=\"checked\"", html);
    }

    [Fact]
    public void ToHtml_EmptyInput_ReturnsEmptyOrWhitespace()
    {
        Assert.Equal("", MarkdownTool.ToHtml("").Trim());
    }

    [Fact]
    public void WrapDocument_ContainsCssAndBody()
    {
        var doc = MarkdownTool.WrapDocument("<p>hi</p>");
        Assert.Contains("<style>", doc);
        Assert.Contains("background:#1E2126", doc);
        Assert.Contains("<body><p>hi</p></body>", doc);
        Assert.Contains("<!doctype html>", doc);
    }
}
