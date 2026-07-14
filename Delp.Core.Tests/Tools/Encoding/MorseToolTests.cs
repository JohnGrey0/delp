using Delp.Core.Tools.Encoding;

namespace Delp.Core.Tests.Tools.Encoding;

public class MorseToolTests
{
    [Fact]
    public void Encode_SosHelp_MatchesKnownMorse()
    {
        Assert.Equal("... --- ... / .... . .-.. .--.", MorseTool.Encode("SOS HELP", skipUnknown: false));
    }

    [Fact]
    public void Decode_KnownMorse_RoundTripsBackToUppercaseText()
    {
        Assert.Equal("SOS HELP", MorseTool.Decode("... --- ... / .... . .-.. .--."));
    }

    [Fact]
    public void Encode_IsCaseInsensitive()
    {
        Assert.Equal(MorseTool.Encode("sos", false), MorseTool.Encode("SOS", false));
    }

    [Fact]
    public void Encode_Punctuation()
    {
        Assert.Equal(".-.-.-", MorseTool.Encode(".", skipUnknown: false));
        Assert.Equal("--..--", MorseTool.Encode(",", skipUnknown: false));
    }

    [Fact]
    public void Encode_UnknownCharacter_ThrowsWhenNotSkipping()
    {
        var ex = Assert.Throws<FormatException>(() => MorseTool.Encode("A~B", skipUnknown: false));
        Assert.Contains("~", ex.Message);
    }

    [Fact]
    public void Encode_UnknownCharacter_SkippedWhenRequested()
    {
        Assert.Equal(".- -...", MorseTool.Encode("A~B", skipUnknown: true));
    }

    [Fact]
    public void Decode_PipeSeparator_TreatedAsWordBoundary()
    {
        Assert.Equal("SOS HELP", MorseTool.Decode("... --- ... | .... . .-.. .--."));
    }

    [Fact]
    public void Decode_UnknownGroup_ThrowsNamingGroup()
    {
        var ex = Assert.Throws<FormatException>(() => MorseTool.Decode(".......")); // not a valid ITU group
        Assert.Contains(".......", ex.Message);
    }

    [Fact]
    public void Encode_ConsecutiveSpacesCollapseToOneWordGap()
    {
        Assert.Equal(MorseTool.Encode("SOS   HELP", skipUnknown: false), MorseTool.Encode("SOS HELP", skipUnknown: false));
    }

    [Fact]
    public void Decode_ConsecutiveWordSeparators_EmptyGroupsFilteredNotThrown()
    {
        // Back-to-back "/" separators (no space between) produce an empty word internally;
        // it must be silently dropped rather than throwing "Unknown Morse group ''".
        Assert.Equal("S O", MorseTool.Decode("... // ---"));
    }
}
