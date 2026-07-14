using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class SemverToolTests
{
    [Fact]
    public void Parse_ExtractsAllComponents()
    {
        var b = SemverTool.Parse("1.2.3-beta.4+build.5");
        Assert.Equal(1, (int)b.Major);
        Assert.Equal(2, (int)b.Minor);
        Assert.Equal(3, (int)b.Patch);
        Assert.Equal("beta.4", b.Prerelease);
        Assert.Equal("build.5", b.Metadata);
        Assert.True(b.IsPrerelease);
    }

    [Fact]
    public void Parse_ReleaseVersion_HasNullPrereleaseAndMetadata()
    {
        var b = SemverTool.Parse("2.0.0");
        Assert.Null(b.Prerelease);
        Assert.Null(b.Metadata);
        Assert.False(b.IsPrerelease);
    }

    [Theory]
    [InlineData("not-a-version")]
    [InlineData("1.2")]
    [InlineData("v1.2.3")]
    [InlineData("01.2.3")]
    public void Parse_InvalidVersion_ThrowsFormatException(string version)
    {
        Assert.Throws<FormatException>(() => SemverTool.Parse(version));
    }

    // SemVer 2.0 spec precedence example: each version precedes the next.
    [Theory]
    [InlineData("1.0.0-alpha", "1.0.0-alpha.1")]
    [InlineData("1.0.0-alpha.1", "1.0.0-alpha.beta")]
    [InlineData("1.0.0-alpha.beta", "1.0.0-beta")]
    [InlineData("1.0.0-beta", "1.0.0-beta.2")]
    [InlineData("1.0.0-beta.2", "1.0.0-beta.11")]
    [InlineData("1.0.0-beta.11", "1.0.0-rc.1")]
    [InlineData("1.0.0-rc.1", "1.0.0")]
    public void Compare_Sem2PrecedenceChain_EachStepIsLessThanNext(string lower, string higher)
    {
        Assert.Equal(-1, SemverTool.Compare(lower, higher).Order);
        Assert.Equal(1, SemverTool.Compare(higher, lower).Order);
    }

    [Fact]
    public void Compare_BuildMetadata_IgnoredInPrecedence()
    {
        var result = SemverTool.Compare("1.0.0+build1", "1.0.0+build2");
        Assert.Equal(0, result.Order);
    }

    [Fact]
    public void Compare_EqualVersions_OrderZero()
    {
        Assert.Equal(0, SemverTool.Compare("1.2.3", "1.2.3").Order);
    }

    [Fact]
    public void Compare_DiffersAtMajor_ExplanationNamesMajor()
    {
        var result = SemverTool.Compare("2.0.0", "1.9.9");
        Assert.Equal(1, result.Order);
        Assert.Contains("major", result.Explanation);
    }

    [Fact]
    public void Compare_DiffersAtMinor_ExplanationNamesMinor()
    {
        var result = SemverTool.Compare("1.2.3", "1.5.0");
        Assert.Equal(-1, result.Order);
        Assert.Contains("minor", result.Explanation);
    }

    [Fact]
    public void Compare_DiffersAtPatch_ExplanationNamesPatch()
    {
        var result = SemverTool.Compare("1.2.3", "1.2.9");
        Assert.Equal(-1, result.Order);
        Assert.Contains("patch", result.Explanation);
    }

    [Fact]
    public void Compare_InvalidVersion_Throws()
    {
        Assert.Throws<FormatException>(() => SemverTool.Compare("nope", "1.0.0"));
    }

    [Theory]
    [InlineData("1.2.3", "^1.2.0", true)]
    [InlineData("1.9.9", "^1.2.0", true)]
    [InlineData("2.0.0", "^1.2.0", false)]
    [InlineData("1.1.9", "^1.2.0", false)]
    [InlineData("0.2.5", "^0.2.3", true)]
    [InlineData("0.3.0", "^0.2.3", false)]
    [InlineData("0.0.3", "^0.0.3", true)]
    [InlineData("0.0.4", "^0.0.3", false)]
    public void Satisfies_Caret(string version, string range, bool expected)
    {
        Assert.Equal(expected, SemverTool.Satisfies(version, range).Satisfies);
    }

    [Theory]
    [InlineData("1.2.5", "~1.2.0", true)]
    [InlineData("1.3.0", "~1.2.0", false)]
    [InlineData("1.1.9", "~1.2.0", false)]
    public void Satisfies_Tilde(string version, string range, bool expected)
    {
        Assert.Equal(expected, SemverTool.Satisfies(version, range).Satisfies);
    }

    [Theory]
    [InlineData("1.5.0", ">=1.0.0", true)]
    [InlineData("0.9.0", ">=1.0.0", false)]
    [InlineData("1.0.0", ">1.0.0", false)]
    [InlineData("1.0.1", ">1.0.0", true)]
    [InlineData("1.0.0", "<=1.0.0", true)]
    [InlineData("1.0.1", "<1.0.0", false)]
    [InlineData("0.9.0", "<1.0.0", true)]
    [InlineData("1.0.0", "=1.0.0", true)]
    [InlineData("1.0.0", "1.0.0", true)]
    [InlineData("1.0.1", "1.0.0", false)]
    public void Satisfies_ComparisonOperators(string version, string range, bool expected)
    {
        Assert.Equal(expected, SemverTool.Satisfies(version, range).Satisfies);
    }

    // npm semantics: a pre-release version only satisfies a range when the range has a bound on the SAME
    // major.minor.patch that is itself a pre-release — raw SemVer 2.0 precedence alone is not enough.
    [Theory]
    [InlineData("1.0.1-alpha", ">=1.0.0", false)] // 1.0.1-alpha outranks 1.0.0 by precedence, but no comparator shares its 1.0.1 tuple
    [InlineData("1.0.1-alpha", "^1.0.0", false)] // same exclusion applies to caret ranges
    [InlineData("1.0.1-alpha", ">=1.0.1-alpha", true)] // comparator shares the 1.0.1 tuple and is itself a pre-release
    [InlineData("1.0.1-alpha", ">=1.0.1-beta", false)] // shares the tuple but alpha precedes beta
    [InlineData("1.5.0", ">=1.0.0", true)] // non-prerelease versions are never subject to the exclusion
    public void Satisfies_PrereleaseExclusion_MatchesNpmSemantics(string version, string range, bool expected)
    {
        Assert.Equal(expected, SemverTool.Satisfies(version, range).Satisfies);
    }

    [Fact]
    public void Satisfies_SpaceJoinedAnd_RequiresAllClauses()
    {
        Assert.True(SemverTool.Satisfies("1.5.0", ">=1.0.0 <2.0.0").Satisfies);
        Assert.False(SemverTool.Satisfies("2.5.0", ">=1.0.0 <2.0.0").Satisfies);
    }

    [Fact]
    public void Satisfies_InvalidVersion_Throws()
    {
        Assert.Throws<FormatException>(() => SemverTool.Satisfies("nope", ">=1.0.0"));
    }

    [Fact]
    public void Satisfies_InvalidRangeOperand_Throws()
    {
        Assert.Throws<FormatException>(() => SemverTool.Satisfies("1.0.0", ">=nope"));
    }

    [Fact]
    public void Satisfies_EmptyRange_Throws()
    {
        Assert.Throws<FormatException>(() => SemverTool.Satisfies("1.0.0", ""));
    }
}
