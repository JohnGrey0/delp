using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("nanoid", "Nano ID Generator", ToolCategory.Hashing,
    "Generate compact, URL-safe Nano IDs with a configurable size and alphabet.",
    Keywords = "nanoid,id,short,url-safe", Order = 200)]
public partial class NanoIdView : UserControl
{
    public NanoIdView()
    {
        InitializeComponent();
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(SizeBox.Text.Trim(), out var size))
                throw new FormatException("Size must be a whole number.");
            if (!int.TryParse(CountBox.Text.Trim(), out var count) || count < 1 || count > 1000)
                throw new FormatException("Count must be a whole number between 1 and 1000.");

            var alphabet = string.IsNullOrEmpty(AlphabetBox.Text) ? NanoIdTool.DefaultAlphabet : AlphabetBox.Text;

            var lines = Enumerable.Range(0, count).Select(_ => NanoIdTool.Generate(size, alphabet));
            OutputBox.Text = string.Join(Environment.NewLine, lines);

            var years = NanoIdTool.YearsFor1PercentCollision(size, alphabet.Distinct().Count(), idsPerHour: 1000);
            CollisionNote.Text = FormatCollisionNote(years);
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private static string FormatCollisionNote(double years) => years switch
    {
        <= 0 => "Collision odds: not applicable for this alphabet.",
        >= 1_000_000_000 => "At 1,000 IDs/hour, it would take over a billion years to reach a 1% collision probability.",
        _ => $"~{years:N0} years needed, at 1,000 IDs/hour, to reach a 1% probability of a collision.",
    };

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputBox.Text, CopyBtn);
}
