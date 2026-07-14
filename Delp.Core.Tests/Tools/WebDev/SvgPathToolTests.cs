using Delp.Core.Tools.WebDev;

namespace Delp.Core.Tests.Tools.WebDev;

public class SvgPathToolTests
{
    [Fact]
    public void Tokenize_ExtractsCommandsInOrder()
    {
        var commands = SvgPathTool.Tokenize("M10 10 L20 20 Z");
        Assert.Equal(new[] { "M 10 10", "L 20 20", "Z" }, commands);
    }

    [Fact]
    public void Analyze_ReportsCommandCount()
    {
        var info = SvgPathTool.Analyze("M10 10 L20 20 L30 30 Z");
        Assert.Equal(4, info.CommandCount);
        Assert.Equal(4, info.Commands.Count);
    }

    [Fact]
    public void Tokenize_InvalidCommandLetter_ThrowsNamingIt()
    {
        var ex = Assert.Throws<FormatException>(() => SvgPathTool.Tokenize("M10 10 P20 20"));
        Assert.Contains("'P'", ex.Message);
    }

    [Fact]
    public void Tokenize_ScientificNotationNumber_Parsed()
    {
        var commands = SvgPathTool.Tokenize("M1e-5 2.5e3");
        Assert.Equal(new[] { "M 1e-5 2.5e3" }, commands);
    }

    [Fact]
    public void Tokenize_RelativeAndAbsoluteCommands_BothRecognized()
    {
        var commands = SvgPathTool.Tokenize("m0,0 c10,10 20,20 30,30 C1,1 2,2 3,3");
        Assert.Equal(new[] { "m 0 0", "c 10 10 20 20 30 30", "C 1 1 2 2 3 3" }, commands);
    }

    [Fact]
    public void Tokenize_ArcCommand_ParsesAllArguments()
    {
        var commands = SvgPathTool.Tokenize("A5 5 0 0 1 10 10");
        Assert.Equal(new[] { "A 5 5 0 0 1 10 10" }, commands);
    }

    [Fact]
    public void Tokenize_EmptyPath_ReturnsNoCommands()
    {
        Assert.Empty(SvgPathTool.Tokenize(""));
        Assert.Empty(SvgPathTool.Tokenize("   "));
    }

    [Fact]
    public void Tokenize_CommandWithNoArguments_ReturnsBareLetter()
    {
        var commands = SvgPathTool.Tokenize("M10 10 Z");
        Assert.Equal("Z", commands[^1]);
    }
}
