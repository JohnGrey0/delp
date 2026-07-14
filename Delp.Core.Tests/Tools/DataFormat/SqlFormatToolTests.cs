using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tests.Tools.DataFormat;

public class SqlFormatToolTests
{
    private static readonly SqlFormatOptions Default = new(UppercaseKeywords: true, IndentSize: 2);

    [Fact]
    public void Format_SimpleSelect_PinnedOutput()
    {
        const string sql = "SELECT id, name FROM users WHERE active = 1 ORDER BY name;";
        const string expected =
            "SELECT\n" +
            "  id,\n" +
            "  name\n" +
            "FROM users\n" +
            "WHERE active = 1\n" +
            "ORDER BY name;";

        Assert.Equal(expected, SqlFormatTool.Format(sql, Default));
    }

    [Fact]
    public void Format_JoinWhereAnd_PinnedOutput()
    {
        const string sql = "SELECT u.id, u.name, o.total FROM users u INNER JOIN orders o ON u.id = o.user_id WHERE u.active = 1 AND o.total > 100;";
        const string expected =
            "SELECT\n" +
            "  u.id,\n" +
            "  u.name,\n" +
            "  o.total\n" +
            "FROM users u\n" +
            "INNER JOIN orders o ON u.id = o.user_id\n" +
            "WHERE u.active = 1\n" +
            "  AND o.total > 100;";

        Assert.Equal(expected, SqlFormatTool.Format(sql, Default));
    }

    [Fact]
    public void Format_InsertValues_PinnedOutput()
    {
        const string sql = "INSERT INTO users (id, name, email) VALUES (1, 'Alice', 'alice@example.com');";
        const string expected =
            "INSERT INTO users (id, name, email)\n" +
            "VALUES (1, 'Alice', 'alice@example.com');";

        Assert.Equal(expected, SqlFormatTool.Format(sql, Default));
    }

    [Fact]
    public void Format_Subquery_PinnedOutput()
    {
        const string sql = "SELECT id, name FROM users WHERE id IN (SELECT user_id FROM orders WHERE total > 100);";
        const string expected =
            "SELECT\n" +
            "  id,\n" +
            "  name\n" +
            "FROM users\n" +
            "WHERE id IN (\n" +
            "  SELECT\n" +
            "    user_id\n" +
            "  FROM orders\n" +
            "  WHERE total > 100\n" +
            ");";

        Assert.Equal(expected, SqlFormatTool.Format(sql, Default));
    }

    [Fact]
    public void Format_Cte_PinnedOutput()
    {
        const string sql = "WITH active_users AS (SELECT id, name FROM users WHERE active = 1) SELECT * FROM active_users;";
        const string expected =
            "WITH active_users AS (\n" +
            "  SELECT\n" +
            "    id,\n" +
            "    name\n" +
            "  FROM users\n" +
            "  WHERE active = 1\n" +
            ")\n" +
            "SELECT\n" +
            "  *\n" +
            "FROM active_users;";

        Assert.Equal(expected, SqlFormatTool.Format(sql, Default));
    }

    [Fact]
    public void Format_LowercaseOption_PreservesOriginalKeywordCasing()
    {
        const string sql = "select id from users;";
        var result = SqlFormatTool.Format(sql, Default with { UppercaseKeywords = false });
        Assert.Equal("select\n  id\nfrom users;", result);
    }

    [Fact]
    public void Format_StringContainingKeywords_PassesThroughUntouched()
    {
        const string sql = "SELECT 'SELECT this' AS note;";
        var result = SqlFormatTool.Format(sql, Default);
        Assert.Contains("'SELECT this'", result);
    }

    [Fact]
    public void Format_PreservesComments_OnOwnLine()
    {
        const string sql = "SELECT id -- trailing comment\nFROM t;";
        var result = SqlFormatTool.Format(sql, Default);
        Assert.Contains("-- trailing comment", result);
    }

    [Fact]
    public void Minify_StripsCommentsAndCollapsesWhitespace()
    {
        const string sql = "SELECT   id, -- comment\n  name\nFROM   t /* block */ WHERE x=1;";
        var result = SqlFormatTool.Minify(sql);
        Assert.DoesNotContain("--", result);
        Assert.DoesNotContain("/*", result);
        Assert.DoesNotContain("  ", result); // no double spaces
        Assert.Equal("SELECT id, name FROM t WHERE x = 1;", result);
    }

    [Fact]
    public void Minify_QuotedIdentifiersAndDoubledQuoteEscapeSurviveVerbatim()
    {
        const string sql = "SELECT [My Col] FROM t WHERE name = 'O''Brien';";
        var result = SqlFormatTool.Minify(sql);
        Assert.Equal("SELECT [My Col] FROM t WHERE name = 'O''Brien';", result);
    }

    [Fact]
    public void Tokenize_EmptyInput_ProducesNoTokens()
    {
        Assert.Empty(SqlFormatTool.Format("", Default));
    }

    [Fact]
    public void Format_UnicodeStringLiteral_IsPreservedVerbatim()
    {
        const string sql = "SELECT * FROM users WHERE name = 'Müller 日本語 🎉';";
        var result = SqlFormatTool.Format(sql, Default);
        Assert.Contains("'Müller 日本語 🎉'", result);
    }
}
