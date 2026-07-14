using System.Globalization;
using System.Text.RegularExpressions;
using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class MockDataToolTests
{
    private static readonly FieldSpec[] PersonFields =
    {
        new("firstName", FieldKind.FirstName),
        new("lastName", FieldKind.LastName),
        new("fullName", FieldKind.FullName),
        new("email", FieldKind.Email),
        new("username", FieldKind.Username),
    };

    [Fact]
    public void Generate_SameSeed_IsDeterministic()
    {
        var a = MockDataTool.Generate(PersonFields, 25, seed: 42);
        var b = MockDataTool.Generate(PersonFields, 25, seed: 42);
        Assert.Equal(MockDataTool.ToJson(a), MockDataTool.ToJson(b));
    }

    [Fact]
    public void Generate_DifferentSeed_UsuallyDiffers()
    {
        var a = MockDataTool.Generate(PersonFields, 25, seed: 1);
        var b = MockDataTool.Generate(PersonFields, 25, seed: 2);
        Assert.NotEqual(MockDataTool.ToJson(a), MockDataTool.ToJson(b));
    }

    [Fact]
    public void Generate_CorrelatesPersonFieldsWithinARow()
    {
        var rows = MockDataTool.Generate(PersonFields, 50, seed: 7);
        foreach (var row in rows)
        {
            var first = ((string)row["firstName"]!).ToLowerInvariant();
            var last = ((string)row["lastName"]!).ToLowerInvariant();
            var email = (string)row["email"]!;
            var username = (string)row["username"]!;
            var fullName = (string)row["fullName"]!;

            Assert.Contains(first, email);
            Assert.Contains(last, email);
            Assert.StartsWith(first, username, StringComparison.OrdinalIgnoreCase);
            Assert.Equal($"{row["firstName"]} {row["lastName"]}", fullName);
        }
    }

    [Fact]
    public void Generate_EachFieldKind_ProducesSaneValues()
    {
        var fields = new FieldSpec[]
        {
            new("phone", FieldKind.Phone),
            new("street", FieldKind.StreetAddress),
            new("city", FieldKind.City),
            new("state", FieldKind.State),
            new("zip", FieldKind.ZipCode),
            new("country", FieldKind.Country),
            new("company", FieldKind.Company),
            new("job", FieldKind.JobTitle),
            new("id", FieldKind.Uuid),
            new("active", FieldKind.Bool),
            new("age", FieldKind.IntRange, "18..65"),
            new("price", FieldKind.DecimalRange, "1.50..99.50..2"),
            new("signupDate", FieldKind.DateBetween, "2020-01-01..2020-01-05"),
            new("createdAt", FieldKind.IsoDateTime),
            new("ip", FieldKind.Ipv4),
            new("site", FieldKind.Url),
            new("color", FieldKind.HexColor),
            new("bio", FieldKind.LoremWords, "5"),
            new("pwd", FieldKind.Password),
        };

        var rows = MockDataTool.Generate(fields, 30, seed: 99);

        foreach (var row in rows)
        {
            Assert.Matches(new Regex(@"^\(\d{3}\) \d{3}-\d{4}$"), (string)row["phone"]!);
            Assert.False(string.IsNullOrWhiteSpace((string)row["street"]!));
            Assert.Contains((string)row["city"]!, MockCorpus.Cities);
            Assert.Contains((string)row["state"]!, MockCorpus.UsStates);
            Assert.Matches(new Regex(@"^\d{5}$"), (string)row["zip"]!);
            Assert.Contains((string)row["country"]!, MockCorpus.Countries);
            Assert.False(string.IsNullOrWhiteSpace((string)row["company"]!));
            Assert.Contains((string)row["job"]!, MockCorpus.JobTitles);
            Assert.True(Guid.TryParse((string)row["id"]!, out _));
            Assert.IsType<bool>(row["active"]);

            var age = Assert.IsType<int>(row["age"]);
            Assert.InRange(age, 18, 65);

            var price = Assert.IsType<double>(row["price"]);
            Assert.InRange(price, 1.50, 99.50);

            var date = DateTime.ParseExact((string)row["signupDate"]!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            Assert.InRange(date, new DateTime(2020, 1, 1), new DateTime(2020, 1, 5));

            Assert.Matches(new Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$"), (string)row["createdAt"]!);

            var ipParts = ((string)row["ip"]!).Split('.');
            Assert.Equal(4, ipParts.Length);
            Assert.All(ipParts, p => Assert.InRange(int.Parse(p, CultureInfo.InvariantCulture), 0, 255));

            Assert.True(Uri.TryCreate((string)row["site"]!, UriKind.Absolute, out _));
            Assert.Matches(new Regex("^#[0-9A-Fa-f]{6}$"), (string)row["color"]!);
            Assert.Equal(5, ((string)row["bio"]!).Split(' ').Length);
            Assert.Equal(12, ((string)row["pwd"]!).Length);
        }
    }

    [Fact]
    public void Generate_IntRange_InvalidOptions_Throws()
    {
        var fields = new[] { new FieldSpec("n", FieldKind.IntRange, "not-a-range") };
        Assert.Throws<FormatException>(() => MockDataTool.Generate(fields, 1, seed: 1));
    }

    [Fact]
    public void Generate_DecimalRange_InvalidOptions_Throws()
    {
        var fields = new[] { new FieldSpec("n", FieldKind.DecimalRange, "nope") };
        Assert.Throws<FormatException>(() => MockDataTool.Generate(fields, 1, seed: 1));
    }

    [Fact]
    public void Generate_DateBetween_InvalidOptions_Throws()
    {
        var fields = new[] { new FieldSpec("d", FieldKind.DateBetween, "not-a-date..also-not") };
        Assert.Throws<FormatException>(() => MockDataTool.Generate(fields, 1, seed: 1));
    }

    [Fact]
    public void Generate_ZeroRows_Throws_OnNegative()
    {
        Assert.Throws<ArgumentException>(() => MockDataTool.Generate(PersonFields, -1, seed: 1));
    }

    [Fact]
    public void Generate_RowsExceedingMaximum_Throws()
    {
        Assert.Throws<ArgumentException>(() => MockDataTool.Generate(PersonFields, MockDataTool.MaxRows + 1, seed: 1));
    }

    [Fact]
    public void Generate_RowsAtMaximum_Succeeds()
    {
        var rows = MockDataTool.Generate(PersonFields, MockDataTool.MaxRows, seed: 1);
        Assert.Equal(MockDataTool.MaxRows, rows.Count);
    }

    [Fact]
    public void ToJson_ProducesPrettyArrayOfRows()
    {
        var rows = MockDataTool.Generate(new[] { new FieldSpec("id", FieldKind.IntRange, "1..1") }, 2, seed: 3);
        var json = MockDataTool.ToJson(rows);
        Assert.Contains("[", json);
        Assert.Contains("\"id\": 1", json);
    }

    [Fact]
    public void ToCsv_QuotesFieldsContainingCommas()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["name"] = "Smith, John", ["age"] = 30 },
        };
        var csv = MockDataTool.ToCsv(rows);
        Assert.Contains("\"Smith, John\"", csv);
        Assert.StartsWith("name,age", csv);
    }

    [Fact]
    public void ToSqlInserts_EscapesEmbeddedApostrophe_OBrien()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["last_name"] = "O'Brien", ["age"] = 40 },
        };
        var sql = MockDataTool.ToSqlInserts(rows, "people");
        Assert.Contains("'O''Brien'", sql);
        Assert.StartsWith("INSERT INTO people", sql);
        Assert.Contains("VALUES ('O''Brien', 40)", sql);
    }

    [Fact]
    public void ToSqlInserts_NullValue_EmitsNullLiteral()
    {
        var rows = new List<Dictionary<string, object?>> { new() { ["x"] = null } };
        var sql = MockDataTool.ToSqlInserts(rows, "t");
        Assert.Contains("VALUES (NULL)", sql);
    }

    [Fact]
    public void ToSqlInserts_EmbeddedNewlineAndQuote_StaysASingleValidStatement()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["note"] = "line one\nline two's end" },
        };
        var sql = MockDataTool.ToSqlInserts(rows, "notes");

        Assert.Single(Regex.Matches(sql, "INSERT INTO"));
        Assert.Contains("'line one\nline two''s end'", sql);
    }

    [Fact]
    public void ToCsv_EmbeddedNewline_QuotesTheWholeField()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["note"] = "line one\nline two" },
        };
        var csv = MockDataTool.ToCsv(rows);
        Assert.Contains("\"line one\nline two\"", csv);
    }

    [Theory]
    [InlineData(nameof(MockCorpus.FirstNames), 100)]
    [InlineData(nameof(MockCorpus.LastNames), 100)]
    [InlineData(nameof(MockCorpus.Cities), 60)]
    [InlineData(nameof(MockCorpus.Countries), 15)]
    [InlineData(nameof(MockCorpus.CompanyPatterns), 40)]
    [InlineData(nameof(MockCorpus.JobTitles), 30)]
    [InlineData(nameof(MockCorpus.StreetNames), 25)]
    public void Corpus_MeetsMinimumSize(string arrayName, int minimum)
    {
        var array = arrayName switch
        {
            nameof(MockCorpus.FirstNames) => MockCorpus.FirstNames,
            nameof(MockCorpus.LastNames) => MockCorpus.LastNames,
            nameof(MockCorpus.Cities) => MockCorpus.Cities,
            nameof(MockCorpus.Countries) => MockCorpus.Countries,
            nameof(MockCorpus.CompanyPatterns) => MockCorpus.CompanyPatterns,
            nameof(MockCorpus.JobTitles) => MockCorpus.JobTitles,
            nameof(MockCorpus.StreetNames) => MockCorpus.StreetNames,
            _ => throw new InvalidOperationException(),
        };
        Assert.True(array.Length >= minimum, $"{arrayName} has {array.Length}, expected >= {minimum}");
        Assert.Equal(array.Length, array.Distinct().Count());
    }

    [Fact]
    public void Corpus_UsStates_HasExactlyFifty()
    {
        Assert.Equal(50, MockCorpus.UsStates.Length);
        Assert.Equal(50, MockCorpus.UsStates.Distinct().Count());
    }
}
