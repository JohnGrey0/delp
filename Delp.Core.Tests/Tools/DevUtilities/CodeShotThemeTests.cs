using System.Text.RegularExpressions;
using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class CodeShotThemeTests
{
    private static readonly Regex HexColor = new(
        "^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.None, TimeSpan.FromSeconds(2));

    [Fact]
    public void All_HasAtLeastFourThemes()
    {
        Assert.True(CodeShotThemes.All.Count >= 4, $"Expected >= 4 themes, got {CodeShotThemes.All.Count}");
    }

    [Fact]
    public void All_NamesAreUnique()
    {
        var names = CodeShotThemes.All.Select(t => t.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [InlineData("Midnight")]
    [InlineData("Slate")]
    [InlineData("Sunset")]
    [InlineData("Paper-light")]
    public void All_ContainsExpectedTheme(string name)
    {
        Assert.Contains(CodeShotThemes.All, t => t.Name == name);
    }

    [Fact]
    public void All_EveryThemeHasNonEmptyName()
    {
        foreach (var theme in CodeShotThemes.All)
            Assert.False(string.IsNullOrWhiteSpace(theme.Name));
    }

    [Fact]
    public void All_EveryThemeHasAtLeastOneGradientStop()
    {
        foreach (var theme in CodeShotThemes.All)
            Assert.NotEmpty(theme.GradientStops);
    }

    [Fact]
    public void All_GradientStopOffsetsAreWithinZeroToOne()
    {
        foreach (var theme in CodeShotThemes.All)
        foreach (var stop in theme.GradientStops)
            Assert.InRange(stop.Offset, 0.0, 1.0);
    }

    [Fact]
    public void All_GradientStopOffsetsAreSortedAscending()
    {
        foreach (var theme in CodeShotThemes.All)
        {
            var offsets = theme.GradientStops.Select(s => s.Offset).ToList();
            Assert.Equal(offsets.OrderBy(o => o), offsets);
        }
    }

    [Fact]
    public void All_EveryColorFieldIsValidHex()
    {
        foreach (var theme in CodeShotThemes.All)
        {
            foreach (var stop in theme.GradientStops)
                Assert.Matches(HexColor, stop.Hex);

            Assert.Matches(HexColor, theme.CardBg);
            Assert.Matches(HexColor, theme.DefaultFg);
            Assert.Matches(HexColor, theme.LineNumberFg);
            Assert.Matches(HexColor, theme.TitleFg);
            Assert.Matches(HexColor, theme.ChromeDotRed);
            Assert.Matches(HexColor, theme.ChromeDotYellow);
            Assert.Matches(HexColor, theme.ChromeDotGreen);
        }
    }

    [Fact]
    public void All_HasBothLightAndDarkThemes()
    {
        Assert.Contains(CodeShotThemes.All, t => t.IsLight);
        Assert.Contains(CodeShotThemes.All, t => !t.IsLight);
    }
}
