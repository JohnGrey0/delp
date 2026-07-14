using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class YamlFormatToolTests
{
    [Fact]
    public void Format_FlowStyleInput_BecomesCanonicalBlockStyle()
    {
        var result = YamlFormatTool.Format("{a: 1, b: [1, 2, 3]}");
        Assert.DoesNotContain("{", result);
        Assert.Contains("a: 1", result);
        Assert.Contains("- 1", result);
    }

    [Fact]
    public void Format_IndentOption_IsRespected()
    {
        var result = YamlFormatTool.Format("a:\n  b: 1\n", indent: 4);
        Assert.Contains("a:\n    b: 1", result);
    }

    [Fact]
    public void Format_MultiDocument_ReemitsAllWithSeparators()
    {
        var result = YamlFormatTool.Format("a: 1\n---\nb: 2\n");
        Assert.Equal(2, result.Split("---").Length - 1);
        Assert.Contains("a: 1", result);
        Assert.Contains("b: 2", result);
    }

    [Fact]
    public void Format_EmptyDocument_ReturnsEmptyString()
    {
        Assert.Equal("", YamlFormatTool.Format(""));
    }

    [Fact]
    public void Validate_ValidYaml_ReturnsNull()
    {
        Assert.Null(YamlFormatTool.Validate("a: 1\nb: 2\n"));
    }

    [Fact]
    public void Validate_DuplicateKey_ReturnsLineCol()
    {
        var error = YamlFormatTool.Validate("a: 1\na: 2\n");
        Assert.NotNull(error);
        Assert.Equal(2, error!.Line);
    }

    [Fact]
    public void Validate_BadIndentation_ReturnsError()
    {
        var error = YamlFormatTool.Validate("a: 1\n b: 2\n");
        Assert.NotNull(error);
        Assert.Equal(2, error!.Line);
        Assert.Equal(3, error.Col);
    }

    [Fact]
    public void Format_InvalidYaml_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => YamlFormatTool.Format("a: 1\n b: 2\n"));
    }

    [Fact]
    public void Format_PreservesScalarTypesThroughReemission()
    {
        var result = YamlFormatTool.Format("a: 42\nb: \"42\"\nc: true\n");
        Assert.Contains("a: 42", result);
        Assert.Contains("b: \"42\"", result);
        Assert.Contains("c: true", result);
    }
}
