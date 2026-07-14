using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class JsonTypesToolTests
{
    [Fact]
    public void Infer_NestedObjectsAndArrays_EmitBothLanguagesCorrectly()
    {
        const string json = """{"user":{"name":"Ann","age":30},"tags":["a","b"]}""";
        var schema = JsonTypesTool.Infer(json, "Root");

        var csharp = JsonTypesTool.ToCSharp(schema, new CSharpOptions());
        Assert.Contains("public record Root(", csharp);
        Assert.Contains("User User,", csharp);
        Assert.Contains("string[] Tags", csharp);
        Assert.Contains("public record User(", csharp);
        Assert.Contains("string Name,", csharp);
        Assert.Contains("int Age", csharp);

        var ts = JsonTypesTool.ToTypeScript(schema, new TsOptions());
        Assert.Contains("export interface Root {", ts);
        Assert.Contains("user: User;", ts);
        Assert.Contains("tags: string[];", ts);
        Assert.Contains("export interface User {", ts);
        Assert.Contains("name: string;", ts);
        Assert.Contains("age: number;", ts);
    }

    [Fact]
    public void Infer_KeyMissingInSomeElements_IsOptional()
    {
        const string json = """[{"id":1,"name":"A"},{"id":2}]""";
        var schema = JsonTypesTool.Infer(json, "Item");

        var ts = JsonTypesTool.ToTypeScript(schema, new TsOptions());
        Assert.Contains("id: number;", ts);
        Assert.Contains("name?: string;", ts);

        var cs = JsonTypesTool.ToCSharp(schema, new CSharpOptions());
        Assert.Contains("string? Name", cs);
    }

    [Fact]
    public void Infer_AllIntegral_UsesIntNotDouble()
    {
        const string json = """[{"n":1},{"n":2},{"n":3}]""";
        var schema = JsonTypesTool.Infer(json, "Item");
        var cs = JsonTypesTool.ToCSharp(schema, new CSharpOptions());
        Assert.Contains("int N", cs);
    }

    [Fact]
    public void Infer_MixedIntAndFractional_WidensToDouble()
    {
        const string json = """[{"n":1},{"n":2.5}]""";
        var schema = JsonTypesTool.Infer(json, "Item");
        var cs = JsonTypesTool.ToCSharp(schema, new CSharpOptions());
        Assert.Contains("double N", cs);
    }

    [Fact]
    public void Infer_NameCollision_SecondObjectGetsNumberedSuffix()
    {
        const string json = """{"data":{"user":{"id":1}},"meta":{"user":{"id":2}}}""";
        var schema = JsonTypesTool.Infer(json, "Root");
        var cs = JsonTypesTool.ToCSharp(schema, new CSharpOptions());
        Assert.Contains("public record User(", cs);
        Assert.Contains("public record User2(", cs);
    }

    [Fact]
    public void Infer_KebabCaseKeys_MapToPascalCaseWithMapping()
    {
        const string json = """{"first-name":"Ann","last-name":"Lee"}""";
        var schema = JsonTypesTool.Infer(json, "Root");

        var cs = JsonTypesTool.ToCSharp(schema, new CSharpOptions());
        Assert.Contains("JsonPropertyName(\"first-name\")", cs);
        Assert.Contains("string FirstName", cs);

        var ts = JsonTypesTool.ToTypeScript(schema, new TsOptions());
        Assert.Contains("\"first-name\": string;", ts);
    }

    [Fact]
    public void Infer_EmptyArray_ProducesObjectArrayAndUnknownArray()
    {
        const string json = """{"tags":[]}""";
        var schema = JsonTypesTool.Infer(json, "Root");

        var cs = JsonTypesTool.ToCSharp(schema, new CSharpOptions());
        Assert.Contains("object[] Tags", cs);

        var ts = JsonTypesTool.ToTypeScript(schema, new TsOptions());
        Assert.Contains("tags: unknown[];", ts);
    }

    [Fact]
    public void Infer_InvalidJson_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => JsonTypesTool.Infer("{not valid json", "Root"));
    }

    [Fact]
    public void ToCSharp_RecordsFalse_EmitsPlainClass()
    {
        const string json = """{"name":"Ann"}""";
        var schema = JsonTypesTool.Infer(json, "Root");
        var cs = JsonTypesTool.ToCSharp(schema, new CSharpOptions(Records: false));
        Assert.Contains("public class Root", cs);
        Assert.Contains("{ get; set; }", cs);
        Assert.DoesNotContain("public record", cs);
    }

    [Fact]
    public void ToCSharp_JsonPropertyNamesFalse_OmitsAttributesAndUsing()
    {
        const string json = """{"first-name":"Ann"}""";
        var schema = JsonTypesTool.Infer(json, "Root");
        var cs = JsonTypesTool.ToCSharp(schema, new CSharpOptions(JsonPropertyNames: false));
        Assert.DoesNotContain("JsonPropertyName", cs);
        Assert.DoesNotContain("using System.Text.Json.Serialization;", cs);
    }

    [Fact]
    public void ToTypeScript_InterfacesFalse_EmitsTypeAlias()
    {
        const string json = """{"name":"Ann"}""";
        var schema = JsonTypesTool.Infer(json, "Root");
        var ts = JsonTypesTool.ToTypeScript(schema, new TsOptions(Interfaces: false));
        Assert.Contains("export type Root = {", ts);
        Assert.DoesNotContain("export interface", ts);
    }

    [Fact]
    public void Infer_NullOnlyProperty_MapsToNullableObjectInCSharp()
    {
        const string json = """{"x":null}""";
        var schema = JsonTypesTool.Infer(json, "Root");
        var cs = JsonTypesTool.ToCSharp(schema, new CSharpOptions());
        Assert.Contains("object? X", cs);
    }
}
