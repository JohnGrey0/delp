using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("shell-cheatsheet", "Shell Command Cheat Sheet", ToolCategory.DevUtilities,
    "Look up bash and PowerShell equivalents for common command-line tasks.",
    Keywords = "bash,powershell,linux,unix,commands,cheatsheet,terminal", Order = 140)]
public partial class ShellCheatSheetView : UserControl
{
    private bool _ready;

    /// <summary>The result set currently bound to <see cref="ResultsList"/>, so a
    /// filter re-run that produces the same rows can skip touching ItemsSource
    /// (and therefore skip regenerating every card's containers).</summary>
    private IReadOnlyList<ShellEntry> _shown = [];

    public ShellCheatSheetView()
    {
        InitializeComponent();

        CategoryBox.Items.Add("All");
        foreach (var category in ShellCheatSheetData.Categories)
            CategoryBox.Items.Add(category);
        CategoryBox.SelectedIndex = 0;

        _ready = true;
        ApplyFilter();
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (_ready)
            ApplyFilter();
    }

    private void ApplyFilter()
    {
        var category = CategoryBox.SelectedItem as string;
        var results = ShellCheatSheetData.Search(SearchBox.Text, category == "All" ? null : category);

        if (!results.SequenceEqual(_shown))
        {
            _shown = results;
            ResultsList.ItemsSource = results;
        }

        StatusText.Text = $"{results.Count} of {ShellCheatSheetData.All.Count} shown";
    }

    /// <summary>Handles both the "bash $" and "PS &gt;" Copy buttons; <c>Tag</c> picks
    /// which field of the bound <see cref="ShellEntry"/> to copy.</summary>
    private void CopyCommand_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string field, DataContext: ShellEntry entry } button)
            return;

        Ui.Copy(field == "Bash" ? entry.Bash : entry.PowerShell, button);
    }
}
