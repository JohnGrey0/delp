using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class HttpStatusDataTests
{
    [Fact]
    public void All_HasAtLeast60Entries()
    {
        Assert.True(HttpStatusData.All.Count >= 60, $"Expected >= 60 entries, got {HttpStatusData.All.Count}");
    }

    [Theory]
    [InlineData(100)]
    [InlineData(101)]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(204)]
    [InlineData(301)]
    [InlineData(302)]
    [InlineData(304)]
    [InlineData(307)]
    [InlineData(308)]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(405)]
    [InlineData(409)]
    [InlineData(410)]
    [InlineData(418)]
    [InlineData(422)]
    [InlineData(425)]
    [InlineData(429)]
    [InlineData(451)]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    [InlineData(511)]
    public void All_ContainsSpecificCode(int code)
    {
        Assert.Contains(HttpStatusData.All, e => e.Code == code);
    }

    [Fact]
    public void All_CodesAreUnique()
    {
        var codes = HttpStatusData.All.Select(e => e.Code).ToList();
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void All_EveryEntryHasNonEmptyFields()
    {
        foreach (var entry in HttpStatusData.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Name), $"{entry.Code} has no name");
            Assert.False(string.IsNullOrWhiteSpace(entry.Class), $"{entry.Code} has no class");
            Assert.False(string.IsNullOrWhiteSpace(entry.Summary), $"{entry.Code} has no summary");
            Assert.False(string.IsNullOrWhiteSpace(entry.When), $"{entry.Code} has no guidance");
            Assert.False(string.IsNullOrWhiteSpace(entry.Rfc), $"{entry.Code} has no RFC reference");
        }
    }

    [Fact]
    public void Search_ByTeapot_FindsCode418()
    {
        var results = HttpStatusData.Search("teapot");
        Assert.Contains(results, e => e.Code == 418);
    }

    [Fact]
    public void Search_By404_FindsNotFound()
    {
        var results = HttpStatusData.Search("404");
        Assert.Contains(results, e => e.Code == 404 && e.Name == "Not Found");
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAll()
    {
        Assert.Equal(HttpStatusData.All.Count, HttpStatusData.Search("").Count);
        Assert.Equal(HttpStatusData.All.Count, HttpStatusData.Search(null!).Count);
    }

    [Fact]
    public void GroupLabel_ComputesXxGroup()
    {
        Assert.Equal("4xx", HttpStatusData.GroupLabel(404));
        Assert.Equal("2xx", HttpStatusData.GroupLabel(200));
    }
}
