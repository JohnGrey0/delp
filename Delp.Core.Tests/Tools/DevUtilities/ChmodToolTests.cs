using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class ChmodToolTests
{
    private static ChmodPermissions Rwxr_xr_x() => new(
        true, true, true,
        true, false, true,
        true, false, true,
        false, false, false);

    [Fact]
    public void ToOctalString_755_NoSpecialBits()
    {
        Assert.Equal("755", ChmodTool.ToOctalString(Rwxr_xr_x()));
    }

    [Fact]
    public void ToOctalString_WithSetuid_Produces4Digits()
    {
        var p = Rwxr_xr_x() with { Setuid = true };
        Assert.Equal("4755", ChmodTool.ToOctalString(p));
    }

    [Fact]
    public void ToOctalString_WithSetgidAndSticky()
    {
        var p = Rwxr_xr_x() with { Setgid = true, Sticky = true };
        Assert.Equal("3755", ChmodTool.ToOctalString(p));
    }

    [Fact]
    public void ToSymbolic_755_MatchesExpected()
    {
        Assert.Equal("rwxr-xr-x", ChmodTool.ToSymbolic(Rwxr_xr_x()));
    }

    [Fact]
    public void ToSymbolic_WithSetuidAndExecute_LowercaseS()
    {
        var p = Rwxr_xr_x() with { Setuid = true };
        Assert.Equal("rwsr-xr-x", ChmodTool.ToSymbolic(p));
    }

    [Fact]
    public void ToSymbolic_WithSetuidNoOwnerExecute_UppercaseS()
    {
        var p = Rwxr_xr_x() with { OwnerExecute = false, Setuid = true };
        Assert.Equal("rwSr-xr-x", ChmodTool.ToSymbolic(p));
    }

    [Fact]
    public void ToSymbolic_WithStickyAndOtherExecute_LowercaseT()
    {
        var p = Rwxr_xr_x() with { Sticky = true };
        Assert.Equal("rwxr-xr-t", ChmodTool.ToSymbolic(p));
    }

    [Fact]
    public void ToSymbolic_WithStickyNoOtherExecute_UppercaseT()
    {
        var p = Rwxr_xr_x() with { OtherExecute = false, Sticky = true };
        Assert.Equal("rwxr-xr-T", ChmodTool.ToSymbolic(p));
    }

    [Fact]
    public void ToCommand_FormatsChmodOctalFile()
    {
        Assert.Equal("chmod 755 file", ChmodTool.ToCommand(Rwxr_xr_x()));
    }

    [Fact]
    public void ToCommand_WithSpecialBits()
    {
        var p = Rwxr_xr_x() with { Setuid = true };
        Assert.Equal("chmod 4755 file", ChmodTool.ToCommand(p));
    }

    [Theory]
    [InlineData("755")]
    [InlineData("644")]
    [InlineData("000")]
    [InlineData("777")]
    public void FromOctal_RoundTripsThroughToOctalString(string octal)
    {
        var p = ChmodTool.FromOctal(octal);
        Assert.Equal(octal, ChmodTool.ToOctalString(p));
    }

    [Fact]
    public void FromOctal_4755_SetsSetuidAndPerms()
    {
        var p = ChmodTool.FromOctal("4755");
        Assert.True(p.Setuid);
        Assert.False(p.Setgid);
        Assert.False(p.Sticky);
        Assert.Equal(Rwxr_xr_x(), p with { Setuid = false });
    }

    [Theory]
    [InlineData("")]
    [InlineData("75")]
    [InlineData("75555")]
    [InlineData("abc")]
    [InlineData("789")]
    public void FromOctal_InvalidInput_Throws(string bad)
    {
        Assert.Throws<FormatException>(() => ChmodTool.FromOctal(bad));
    }

    [Fact]
    public void FromSymbolic_RoundTripsThroughGrid()
    {
        var p = ChmodTool.FromSymbolic("rwxr-xr-x");
        Assert.Equal(Rwxr_xr_x(), p);
    }

    [Fact]
    public void FromSymbolic_SetuidLowercaseS_SetsExecuteAndSetuid()
    {
        var p = ChmodTool.FromSymbolic("rwsr-xr-x");
        Assert.True(p.OwnerExecute);
        Assert.True(p.Setuid);
    }

    [Fact]
    public void FromSymbolic_SetuidUppercaseS_SetsSetuidWithoutExecute()
    {
        var p = ChmodTool.FromSymbolic("rwSr-xr-x");
        Assert.False(p.OwnerExecute);
        Assert.True(p.Setuid);
    }

    [Fact]
    public void FromSymbolic_StickyLowercaseT_SetsExecuteAndSticky()
    {
        var p = ChmodTool.FromSymbolic("rwxr-xr-t");
        Assert.True(p.OtherExecute);
        Assert.True(p.Sticky);
    }

    [Theory]
    [InlineData("rwxr-xr-")]      // 8 chars, too short
    [InlineData("rwxr-xr-xx")]    // 10 chars, too long
    [InlineData("zwxr-xr-x")]     // invalid char in a read position
    public void FromSymbolic_InvalidInput_Throws(string bad)
    {
        Assert.Throws<FormatException>(() => ChmodTool.FromSymbolic(bad));
    }

    [Fact]
    public void Default_Is755()
    {
        Assert.Equal("755", ChmodTool.ToOctalString(ChmodTool.Default));
    }

    [Theory]
    [InlineData("644")]
    [InlineData("600")]
    [InlineData("777")]
    [InlineData("4755")]
    [InlineData("2755")]
    [InlineData("1777")]
    public void OctalSymbolicGridRoundTrip_StaysConsistent(string octal)
    {
        var fromOctal = ChmodTool.FromOctal(octal);
        var symbolic = ChmodTool.ToSymbolic(fromOctal);
        var fromSymbolic = ChmodTool.FromSymbolic(symbolic);
        Assert.Equal(fromOctal, fromSymbolic);
        Assert.Equal(octal, ChmodTool.ToOctalString(fromSymbolic));
    }
}
