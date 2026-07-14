using System.Text.RegularExpressions;
using Delp.Core.Tools.Hashing;

namespace Delp.Core.Tests.Tools.Hashing;

public class PasswordToolTests
{
    [Fact]
    public void Generate_RespectsRequestedLength()
    {
        var password = PasswordTool.Generate(new PasswordOptions(24, true, true, true, false, false, false));
        Assert.Equal(24, password.Length);
    }

    [Fact]
    public void Generate_OnlyDigitsSelected_ProducesOnlyDigits()
    {
        var password = PasswordTool.Generate(new PasswordOptions(40, false, false, true, false, false, false));
        Assert.All(password, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void Generate_ExcludeAmbiguous_NeverIncludesAmbiguousChars()
    {
        const string ambiguous = "Il1O0o";
        for (var i = 0; i < 200; i++)
        {
            var password = PasswordTool.Generate(new PasswordOptions(50, true, true, true, false, true, false));
            Assert.All(password, c => Assert.DoesNotContain(c, ambiguous));
        }
    }

    [Fact]
    public void Generate_RequireEachClass_HoldsAcrossManyGenerations()
    {
        var options = new PasswordOptions(16, true, true, true, true, false, true);
        for (var i = 0; i < 200; i++)
        {
            var password = PasswordTool.Generate(options);
            Assert.Contains(password, c => char.IsLower(c));
            Assert.Contains(password, c => char.IsUpper(c));
            Assert.Contains(password, c => char.IsDigit(c));
            Assert.Contains(password, c => !char.IsLetterOrDigit(c));
        }
    }

    [Fact]
    public void Generate_NoClassSelected_Throws()
    {
        Assert.Throws<ArgumentException>(() => PasswordTool.Generate(new PasswordOptions(10, false, false, false, false, false, false)));
    }

    [Fact]
    public void Generate_ZeroLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => PasswordTool.Generate(new PasswordOptions(0, true, false, false, false, false, false)));
    }

    [Fact]
    public void Generate_RequireEachClass_LengthTooShort_Throws()
    {
        Assert.Throws<ArgumentException>(() => PasswordTool.Generate(new PasswordOptions(2, true, true, true, true, false, true)));
    }

    [Fact]
    public void EntropyBits_Password_MatchesLog2Formula()
    {
        var options = new PasswordOptions(10, true, false, false, false, false, false);
        var expected = 10 * Math.Log2(26);
        Assert.Equal(expected, PasswordTool.EntropyBits(options), precision: 6);
    }

    [Fact]
    public void EntropyBits_Passphrase_MatchesLog2Formula()
    {
        var noNumber = new PassphraseOptions(5, '-', false, false);
        Assert.Equal(5 * Math.Log2(1296), PasswordTool.EntropyBits(noNumber), precision: 6);

        var withNumber = new PassphraseOptions(5, '-', false, true);
        Assert.Equal(5 * Math.Log2(1296) + Math.Log2(100), PasswordTool.EntropyBits(withNumber), precision: 6);
    }

    [Theory]
    [InlineData(0, PasswordStrength.Weak)]
    [InlineData(49.9, PasswordStrength.Weak)]
    [InlineData(50, PasswordStrength.Fair)]
    [InlineData(69.9, PasswordStrength.Fair)]
    [InlineData(70, PasswordStrength.Strong)]
    [InlineData(89.9, PasswordStrength.Strong)]
    [InlineData(90, PasswordStrength.Excellent)]
    [InlineData(200, PasswordStrength.Excellent)]
    public void StrengthLabel_BoundariesMatchSpec(double bits, PasswordStrength expected)
    {
        Assert.Equal(expected, PasswordTool.StrengthLabel(bits));
    }

    [Fact]
    public void GeneratePassphrase_Shape_UsesWordListAndSeparator()
    {
        var phrase = PasswordTool.GeneratePassphrase(new PassphraseOptions(4, '-', false, false));
        var words = phrase.Split('-');
        Assert.Equal(4, words.Length);
        Assert.All(words, w => Assert.Contains(w, PassphraseWords.List));
    }

    [Fact]
    public void GeneratePassphrase_Capitalize_UppercasesFirstLetterOnly()
    {
        var phrase = PasswordTool.GeneratePassphrase(new PassphraseOptions(3, '-', true, false));
        foreach (var word in phrase.Split('-'))
        {
            Assert.True(char.IsUpper(word[0]));
            Assert.Equal(word[1..].ToLowerInvariant(), word[1..]);
        }
    }

    [Fact]
    public void GeneratePassphrase_AppendNumber_AddsTwoDigitSuffix()
    {
        var phrase = PasswordTool.GeneratePassphrase(new PassphraseOptions(3, '-', false, true));
        var parts = phrase.Split('-');
        Assert.Equal(4, parts.Length);
        Assert.Matches(new Regex(@"^\d{2}$"), parts[^1]);
    }

    [Fact]
    public void GeneratePassphrase_ZeroWords_Throws()
    {
        Assert.Throws<ArgumentException>(() => PasswordTool.GeneratePassphrase(new PassphraseOptions(0, '-', false, false)));
    }

    [Fact]
    public void PassphraseWords_ListSize_Is1296AndAllUniqueLowercase()
    {
        Assert.Equal(1296, PassphraseWords.List.Count);
        Assert.Equal(1296, PassphraseWords.List.Distinct().Count());
        Assert.All(PassphraseWords.List, w => Assert.Matches(new Regex("^[a-z]{4,6}$"), w));
    }
}
