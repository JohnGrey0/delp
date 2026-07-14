using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("semver", "SemVer Comparator", ToolCategory.DevUtilities,
    "Compare two semantic versions by SemVer 2.0 precedence and check a version against a range.",
    Keywords = "semver,version,compare,range,precedence", Order = 40)]
public partial class SemverView : UserControl
{
    public SemverView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Compare();
            CheckRange();
        };
    }

    private void Compare_Changed(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
            Compare();
    }

    private void Range_Changed(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
            CheckRange();
    }

    private void Compare()
    {
        try
        {
            var a = VersionABox.Text;
            var b = VersionBBox.Text;
            var result = SemverTool.Compare(a, b);
            var breakdownA = SemverTool.Parse(a);
            var breakdownB = SemverTool.Parse(b);

            var op = result.Order < 0 ? "<" : result.Order > 0 ? ">" : "=";
            VerdictText.Text = $"{a}  {op}  {b}";
            ExplanationText.Text = result.Explanation;

            var accent = (Brush)FindResource("Brush.Accent");
            var neutral = (Brush)FindResource("Brush.Fg0");

            BreakdownList.ItemsSource = new[]
            {
                new BreakdownRow(
                    "Major", breakdownA.Major.ToString(), breakdownB.Major.ToString(),
                    breakdownA.Major != breakdownB.Major ? accent : neutral,
                    breakdownA.Major != breakdownB.Major ? accent : neutral),
                new BreakdownRow(
                    "Minor", breakdownA.Minor.ToString(), breakdownB.Minor.ToString(),
                    breakdownA.Minor != breakdownB.Minor ? accent : neutral,
                    breakdownA.Minor != breakdownB.Minor ? accent : neutral),
                new BreakdownRow(
                    "Patch", breakdownA.Patch.ToString(), breakdownB.Patch.ToString(),
                    breakdownA.Patch != breakdownB.Patch ? accent : neutral,
                    breakdownA.Patch != breakdownB.Patch ? accent : neutral),
                new BreakdownRow(
                    "Prerelease", breakdownA.Prerelease ?? "(none)", breakdownB.Prerelease ?? "(none)",
                    breakdownA.Prerelease != breakdownB.Prerelease ? accent : neutral,
                    breakdownA.Prerelease != breakdownB.Prerelease ? accent : neutral),
                new BreakdownRow(
                    "Build", breakdownA.Metadata ?? "(none)", breakdownB.Metadata ?? "(none)", neutral, neutral),
            };

            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            VerdictText.Text = "";
            ExplanationText.Text = "";
            BreakdownList.ItemsSource = null;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void CheckRange()
    {
        try
        {
            var result = SemverTool.Satisfies(RangeVersionBox.Text, RangeExprBox.Text);
            RangeResultText.Text = (result.Satisfies ? "✓ " : "✗ ") + result.Note;
            RangeResultText.Foreground = (Brush)FindResource(result.Satisfies ? "Brush.Success" : "Brush.Danger");
        }
        catch (Exception ex)
        {
            RangeResultText.Text = ex.Message;
            RangeResultText.Foreground = (Brush)FindResource("Brush.Danger");
        }
    }

    private sealed record BreakdownRow(string Part, string A, string B, Brush AColor, Brush BColor);
}
