using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class BranchToolTests
{
    [Fact]
    public void Make_DefaultTemplate_ProducesTypeTicketSlug()
    {
        var options = new BranchOptions("feature", "abc-123", "{type}/{ticket}-{slug}", 60);
        var name = BranchTool.Make("Add login page", options);
        Assert.Equal("feature/ABC-123-add-login-page", name);
    }

    [Fact]
    public void Make_TicketNormalized_ToUppercase()
    {
        var options = new BranchOptions("bugfix", "proj-42", "{type}/{ticket}-{slug}", 60);
        var name = BranchTool.Make("Fix crash", options);
        Assert.StartsWith("bugfix/PROJ-42-", name);
    }

    [Fact]
    public void Make_NonMatchingTicketPattern_KeptAsIs()
    {
        var options = new BranchOptions("chore", "not-a-ticket-format", "{type}/{ticket}-{slug}", 60);
        var name = BranchTool.Make("Update", options);
        Assert.StartsWith("chore/not-a-ticket-format-", name);
    }

    [Fact]
    public void Make_NoTicket_OmitsTicketSegment()
    {
        var options = new BranchOptions("chore", null, "{type}/{ticket}-{slug}", 60);
        var name = BranchTool.Make("Update dependencies", options);
        Assert.Equal("chore/update-dependencies", name);
    }

    [Fact]
    public void Make_CustomTemplate_Substitutes()
    {
        var options = new BranchOptions("hotfix", "SEC-1", "{ticket}/{type}/{slug}", 60);
        var name = BranchTool.Make("Patch vulnerability", options);
        Assert.Equal("SEC-1/hotfix/patch-vulnerability", name);
    }

    [Fact]
    public void Make_StripsGitIllegalCharacters()
    {
        var options = new BranchOptions("feature", null, "{type}/{slug}", 60);
        var name = BranchTool.Make("Fix ~weird^ path:name?*[x]\\y", options);
        Assert.DoesNotContain('~', name);
        Assert.DoesNotContain('^', name);
        Assert.DoesNotContain(':', name);
        Assert.DoesNotContain('?', name);
        Assert.DoesNotContain('*', name);
        Assert.DoesNotContain('[', name);
        Assert.DoesNotContain('\\', name);
    }

    [Fact]
    public void Make_LongDescription_TrimsAtWordBoundary()
    {
        var options = new BranchOptions("feature", null, "{type}/{slug}", 24);
        var name = BranchTool.Make("this description is definitely far too long to fit", options);
        Assert.True(name.Length <= 24);
        Assert.False(name.EndsWith('-'));
        Assert.False(name.EndsWith('/'));
    }

    [Fact]
    public void Make_DiacriticsAreStripped()
    {
        var options = new BranchOptions("feature", null, "{type}/{slug}", 60);
        var name = BranchTool.Make("Café résumé", options);
        Assert.Equal("feature/cafe-resume", name);
    }

    [Fact]
    public void Make_EmptyDescriptionAndTicket_Throws()
    {
        var options = new BranchOptions("feature", null, "{type}/{ticket}-{slug}", 60);
        Assert.Throws<FormatException>(() => BranchTool.Make("", options));
    }

    [Fact]
    public void CheckoutCommand_FormatsGitCommand()
    {
        Assert.Equal("git checkout -b feature/x", BranchTool.CheckoutCommand("feature/x"));
    }

    [Theory]
    [InlineData("feature/has..dots")]
    [InlineData("/leading-slash")]
    [InlineData("trailing-slash/")]
    [InlineData("-leading-dash")]
    [InlineData("trailing-dash-")]
    [InlineData("has space")]
    [InlineData("has~tilde")]
    [InlineData("has^caret")]
    [InlineData("has:colon")]
    [InlineData("has?question")]
    [InlineData("has*star")]
    [InlineData("has[bracket")]
    [InlineData("has\\backslash")]
    [InlineData("weird@{ref}")]
    [InlineData("name.lock")]
    [InlineData("double//slash")]
    [InlineData("")]
    public void Validate_CatchesEachRuleViolation(string badName)
    {
        var violations = BranchTool.Validate(badName);
        Assert.NotEmpty(violations);
    }

    [Fact]
    public void Validate_GoodName_NoViolations()
    {
        var violations = BranchTool.Validate("feature/abc-123-add-login-page");
        Assert.Empty(violations);
    }
}
