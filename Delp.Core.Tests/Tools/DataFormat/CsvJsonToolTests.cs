using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class CsvJsonToolTests
{
    [Fact]
    public void CsvToJson_InfersIntDoubleBoolAndNull()
    {
        const string csv = "name,age,score,active,note\nAlice,30,1.5,true,\n";
        var json = CsvJsonTool.CsvToJson(csv, new CsvOptions());

        Assert.Equal(
            "[\n  {\n    \"name\": \"Alice\",\n    \"age\": 30,\n    \"score\": 1.5,\n    \"active\": true,\n    \"note\": null\n  }\n]",
            json);
    }

    [Fact]
    public void CsvToJson_InferTypesFalse_KeepsAllFieldsAsStrings()
    {
        const string csv = "a,b\n1,true\n";
        var json = CsvJsonTool.CsvToJson(csv, new CsvOptions(InferTypes: false));
        Assert.Equal("[\n  {\n    \"a\": \"1\",\n    \"b\": \"true\"\n  }\n]", json);
    }

    [Fact]
    public void CsvToJson_AutoDetectsSemicolonDelimiter()
    {
        const string csv = "name;age\nAlice;30\n";
        var json = CsvJsonTool.CsvToJson(csv, new CsvOptions());
        Assert.Equal("[\n  {\n    \"name\": \"Alice\",\n    \"age\": 30\n  }\n]", json);
    }

    [Fact]
    public void CsvToJson_QuotedFieldWithEmbeddedDelimiterAndNewline()
    {
        const string csv = "name,note\n\"Alice\",\"hello, world\nsecond line\"\n";
        var json = CsvJsonTool.CsvToJson(csv, new CsvOptions());
        Assert.Contains("\"note\": \"hello, world\\nsecond line\"", json);
    }

    [Fact]
    public void CsvToJson_NoHeader_UsesColNColumnNames()
    {
        const string csv = "a,1\nb,2\n";
        var json = CsvJsonTool.CsvToJson(csv, new CsvOptions(HasHeader: false));
        Assert.Equal(
            "[\n  {\n    \"col1\": \"a\",\n    \"col2\": 1\n  },\n  {\n    \"col1\": \"b\",\n    \"col2\": 2\n  }\n]",
            json);
    }

    [Fact]
    public void CsvToJson_RaggedRow_ThrowsWithRowNumber()
    {
        var ex = Assert.Throws<FormatException>(() => CsvJsonTool.CsvToJson("a,b,c\n1,2,3\n4,5\n", new CsvOptions()));
        Assert.Contains("Row 3", ex.Message);
    }

    [Fact]
    public void CsvToJson_StripsLeadingBom()
    {
        const string csv = "﻿name,age\nAlice,30\n";
        var json = CsvJsonTool.CsvToJson(csv, new CsvOptions());
        Assert.Equal("[\n  {\n    \"name\": \"Alice\",\n    \"age\": 30\n  }\n]", json);
    }

    [Fact]
    public void JsonToCsv_RoundTripsThroughCsvToJson()
    {
        const string csv = "name,age,active\nAlice,30,true\nBob,25,false\n";
        var json = CsvJsonTool.CsvToJson(csv, new CsvOptions());
        var backToCsv = CsvJsonTool.JsonToCsv(json, ',');
        var jsonAgain = CsvJsonTool.CsvToJson(backToCsv, new CsvOptions());
        Assert.Equal(json, jsonAgain);
    }

    [Fact]
    public void JsonToCsv_UnionOfKeysAndJsonStringifiesNestedValues()
    {
        const string json = "[{\"a\":1,\"b\":2},{\"a\":3,\"c\":{\"x\":1}}]";
        var csv = CsvJsonTool.JsonToCsv(json, ',');
        Assert.Equal("a,b,c\r\n1,2,\r\n3,,\"{\"\"x\"\":1}\"\r\n", csv);
    }

    [Fact]
    public void JsonToCsv_QuotesFieldsWithEmbeddedDelimiter()
    {
        const string json = "[{\"a\":\"hello, world\"}]";
        var csv = CsvJsonTool.JsonToCsv(json, ',');
        Assert.Equal("a\r\n\"hello, world\"\r\n", csv);
    }

    [Fact]
    public void JsonToCsv_NonArrayRoot_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CsvJsonTool.JsonToCsv("{\"a\":1}", ','));
    }

    [Fact]
    public void CsvToJson_EmptyInput_ReturnsEmptyArray()
    {
        Assert.Equal("[]", CsvJsonTool.CsvToJson("", new CsvOptions()));
    }
}
