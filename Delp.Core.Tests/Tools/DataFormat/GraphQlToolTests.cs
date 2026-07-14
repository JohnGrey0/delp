using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class GraphQlToolTests
{
    private const string Query = "query Foo($x: Int = 1) {\n  ...Bar\n  a(b: $x) { c d }\n}\nfragment Bar on Query { z }";

    [Fact]
    public void Format_QueryWithVariablesAndFragments_IsStable()
    {
        const string expected =
            "query Foo($x: Int = 1) {\n" +
            "  ...Bar\n" +
            "  a(b: $x) {\n" +
            "    c\n" +
            "    d\n" +
            "  }\n" +
            "}\n" +
            "\n" +
            "fragment Bar on Query {\n" +
            "  z\n" +
            "}\n";

        Assert.Equal(expected, GraphQlTool.Format(Query));
    }

    [Fact]
    public void Format_SchemaSdl_IsStable()
    {
        const string schema = "type Query { user(id: ID!): User }";
        Assert.Equal("type Query {\n  user(id: ID!): User\n}\n", GraphQlTool.Format(schema));
    }

    [Fact]
    public void Minify_StripsInsignificantWhitespace()
    {
        Assert.Equal(
            "query Foo($x:Int=1){...Bar a(b:$x){c d}}fragment Bar on Query{z}",
            GraphQlTool.Minify(Query));
    }

    [Fact]
    public void Minify_ProducesReparsableDocument()
    {
        var minified = GraphQlTool.Minify(Query);
        // Round-tripping through Format again must reproduce the same canonical output.
        Assert.Equal(GraphQlTool.Format(Query), GraphQlTool.Format(minified));
    }

    [Fact]
    public void Validate_SyntaxError_CarriesLineAndColumn()
    {
        var error = GraphQlTool.Validate("query { a { }");
        Assert.NotNull(error);
        Assert.Equal(1, error!.Line);
        Assert.Equal(13, error.Column);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public void Validate_ValidDocument_ReturnsNull()
    {
        Assert.Null(GraphQlTool.Validate("query { a b }"));
    }

    [Fact]
    public void Format_SyntaxError_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => GraphQlTool.Format("query { a { }"));
        Assert.Contains("Expected Name", ex.Message);
    }

    [Fact]
    public void Format_EmptyInput_ReturnsEmptyString()
    {
        Assert.Equal("", GraphQlTool.Format(""));
    }
}
