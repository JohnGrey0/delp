using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class MimeDataTests
{
    [Fact]
    public void All_HasAtLeast180Entries()
    {
        Assert.True(MimeData.All.Count >= 180, $"Expected >= 180 entries, got {MimeData.All.Count}");
    }

    [Fact]
    public void LookupByExtension_KnownExtension_ReturnsMime()
    {
        Assert.Equal("image/png", MimeData.LookupByExtension("png"));
        Assert.Equal("application/pdf", MimeData.LookupByExtension("pdf"));
    }

    [Fact]
    public void LookupByExtension_LeadingDotIsOptional()
    {
        Assert.Equal(MimeData.LookupByExtension("png"), MimeData.LookupByExtension(".png"));
    }

    [Fact]
    public void LookupByExtension_IsCaseInsensitive()
    {
        Assert.Equal("image/png", MimeData.LookupByExtension("PNG"));
        Assert.Equal("image/png", MimeData.LookupByExtension(".Png"));
    }

    [Fact]
    public void LookupByExtension_Unknown_ReturnsNull()
    {
        Assert.Null(MimeData.LookupByExtension("not-a-real-extension-xyz"));
    }

    [Fact]
    public void LookupByMime_MultiExtensionMime_ReturnsAllExtensions()
    {
        var extensions = MimeData.LookupByMime("image/jpeg");
        Assert.Contains("jpg", extensions);
        Assert.Contains("jpeg", extensions);
    }

    [Fact]
    public void LookupByMime_IsCaseInsensitive()
    {
        var extensions = MimeData.LookupByMime("IMAGE/PNG");
        Assert.Contains("png", extensions);
    }

    [Fact]
    public void LookupByMime_Unknown_ReturnsEmpty()
    {
        Assert.Empty(MimeData.LookupByMime("application/does-not-exist"));
    }

    [Fact]
    public void Search_BySubstringOfExtension_Matches()
    {
        var results = MimeData.Search("jpe");
        Assert.Contains(results, e => e.Extension == "jpeg");
    }

    [Fact]
    public void Search_BySubstringOfMime_Matches()
    {
        var results = MimeData.Search("javascript");
        Assert.Contains(results, e => e.Extension == "js");
    }

    [Fact]
    public void Search_Unknown_ReturnsEmpty()
    {
        Assert.Empty(MimeData.Search("totally-unknown-thing-zzz"));
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAll()
    {
        Assert.Equal(MimeData.All.Count, MimeData.Search("").Count);
    }

    [Fact]
    public void All_ExtensionsAreUnique()
    {
        var exts = MimeData.All.Select(e => e.Extension).ToList();
        Assert.Equal(exts.Count, exts.Distinct().Count());
    }
}
