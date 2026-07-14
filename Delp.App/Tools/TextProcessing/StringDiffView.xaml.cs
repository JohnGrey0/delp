using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("string-diff", "String Diff", ToolCategory.TextProcessing,
    "Compare two blocks of text and highlight inserted, deleted, and modified lines.",
    Keywords = "diff,compare,text,delta,changes", Order = 40)]
public partial class StringDiffView : UserControl
{
    // Above this combined input size, DiffPlex runs on a background thread so a
    // large diff can't block the UI thread while it computes.
    private const int OffThreadCharThreshold = 20_000;

    private readonly DispatcherTimer _debounce;
    private int _renderToken;

    public StringDiffView()
    {
        InitializeComponent();

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            _ = RenderAsync();
        };

        _ = RenderAsync();
    }

    private void Input_Changed(object sender, RoutedEventArgs e)
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private async Task RenderAsync()
    {
        // Token-based reentrancy guard (rather than the synchronous "Run()" idiom used
        // elsewhere): overlapping async renders can occur if the debounce timer fires
        // again while a large diff is still computing off-thread, and only the result
        // of the most recent request should ever be applied.
        var token = ++_renderToken;

        var oldText = OldBox.Text;
        var newText = NewBox.Text;
        var options = new DiffToolOptions(
            IgnoreCase: IgnoreCaseBox.IsChecked == true,
            IgnoreWhitespace: IgnoreWhitespaceBox.IsChecked == true);

        try
        {
            var result = oldText.Length + newText.Length > OffThreadCharThreshold
                ? await Task.Run(() => DiffTool.Compute(oldText, newText, options))
                : DiffTool.Compute(oldText, newText, options);

            if (token != _renderToken)
                return;

            var successTint = Tint((SolidColorBrush)FindResource("Brush.Success"));
            var dangerTint = Tint((SolidColorBrush)FindResource("Brush.Danger"));
            DiffList.ItemsSource = BuildRows(result, successTint, dangerTint);
            StatusText.Text = $"+{result.Insertions} −{result.Deletions}";
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            if (token != _renderToken)
                return;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    /// <summary>Zips old/new panes (DiffPlex pads both sides to equal length) into display rows.</summary>
    private static List<DiffRowVm> BuildRows(DiffResult result, Brush successTint, Brush dangerTint)
    {
        var oldLines = result.Old.Lines;
        var newLines = result.New.Lines;
        var count = Math.Max(oldLines.Count, newLines.Count);

        var rows = new List<DiffRowVm>(count);
        for (var i = 0; i < count; i++)
        {
            var oldLine = i < oldLines.Count ? oldLines[i] : null;
            var newLine = i < newLines.Count ? newLines[i] : null;

            rows.Add(new DiffRowVm(
                oldLine?.Number?.ToString(CultureInfo.InvariantCulture) ?? "",
                oldLine?.Text ?? "",
                BackgroundFor(oldLine?.Kind, successTint, dangerTint),
                newLine?.Number?.ToString(CultureInfo.InvariantCulture) ?? "",
                newLine?.Text ?? "",
                BackgroundFor(newLine?.Kind, successTint, dangerTint)));
        }
        return rows;
    }

    private static Brush BackgroundFor(DiffKind? kind, Brush successTint, Brush dangerTint) => kind switch
    {
        DiffKind.Inserted or DiffKind.Modified => successTint,
        DiffKind.Deleted => dangerTint,
        _ => Brushes.Transparent,
    };

    /// <summary>Derives a low-alpha row tint from a theme brush — never a hardcoded hue.</summary>
    private static Brush Tint(SolidColorBrush baseBrush)
    {
        var c = baseBrush.Color;
        return new SolidColorBrush(Color.FromArgb(0x30, c.R, c.G, c.B));
    }

    /// <summary>One paired display row: the old-side line alongside its new-side counterpart.</summary>
    private sealed record DiffRowVm(
        string OldNumber, string OldText, Brush OldBackground,
        string NewNumber, string NewText, Brush NewBackground);
}
