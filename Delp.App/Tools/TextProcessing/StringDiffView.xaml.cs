using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("string-diff", "String Diff", ToolCategory.TextProcessing,
    "Compare two blocks of text and highlight inserted, deleted, and modified lines.",
    Keywords = "diff,compare,text,delta,changes", Order = 40)]
public partial class StringDiffView : UserControl
{
    private bool _updating;

    public StringDiffView()
    {
        InitializeComponent();
        Run(Render);
    }

    private void Input_Changed(object sender, RoutedEventArgs e) => Run(Render);

    private void Render()
    {
        var options = new DiffToolOptions(
            IgnoreCase: IgnoreCaseBox.IsChecked == true,
            IgnoreWhitespace: IgnoreWhitespaceBox.IsChecked == true);

        var result = DiffTool.Compute(OldBox.Text, NewBox.Text, options);

        OldList.Items.Clear();
        NewList.Items.Clear();
        foreach (var line in result.Old.Lines)
            OldList.Items.Add(BuildRow(line));
        foreach (var line in result.New.Lines)
            NewList.Items.Add(BuildRow(line));

        StatusText.Text = $"+{result.Insertions} −{result.Deletions}";
    }

    private UIElement BuildRow(DiffLine line)
    {
        var background = line.Kind switch
        {
            DiffKind.Inserted or DiffKind.Modified => Tint((SolidColorBrush)FindResource("Brush.Success")),
            DiffKind.Deleted => Tint((SolidColorBrush)FindResource("Brush.Danger")),
            _ => Brushes.Transparent,
        };

        var mono = (FontFamily)FindResource("Font.Mono");

        var numberText = new TextBlock
        {
            Text = line.Number?.ToString() ?? "",
            Width = 36,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 0, 8, 0),
            Foreground = (Brush)FindResource("Brush.Fg2"),
            FontFamily = mono,
            FontSize = 12,
        };
        DockPanel.SetDock(numberText, Dock.Left);

        var text = new TextBlock
        {
            Text = line.Text,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = mono,
            FontSize = 13,
            Foreground = (Brush)FindResource("Brush.Fg0"),
        };

        var row = new DockPanel();
        row.Children.Add(numberText);
        row.Children.Add(text);

        return new Border { Background = background, Padding = new Thickness(4, 2, 4, 2), Child = row };
    }

    /// <summary>Derives a low-alpha row tint from a theme brush — never a hardcoded hue.</summary>
    private static Brush Tint(SolidColorBrush baseBrush)
    {
        var c = baseBrush.Color;
        return new SolidColorBrush(Color.FromArgb(0x30, c.R, c.G, c.B));
    }

    /// <summary>Runs a render pass with reentrancy protection and inline error reporting.</summary>
    private void Run(Action render)
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            render();
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            _updating = false;
        }
    }
}
