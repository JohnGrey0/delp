using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("epoch-batch", "Epoch Batch Converter", ToolCategory.TextProcessing,
    "Convert a multi-line block of pasted timestamps (e.g. from a log file) to local and UTC dates at once.",
    Keywords = "epoch,batch,timestamps,convert,logs", Order = 110)]
public partial class EpochBatchView : UserControl
{
    private static readonly EpochUnit?[] Units = [null, EpochUnit.Seconds, EpochUnit.Millis, EpochUnit.Micros];

    private IReadOnlyList<EpochRow> _rows = [];
    private readonly DispatcherTimer _debounceTimer;

    public EpochBatchView()
    {
        InitializeComponent();
        UnitBox.ItemsSource = new[] { "Auto", "s", "ms", "µs" };
        UnitBox.SelectedIndex = 0;

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            Refresh();
        };
    }

    // Conversion cost scales with the number of pasted lines (a whole log file's worth),
    // so debounce rather than re-converting on every keystroke.
    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) => Debounce();

    private void Option_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            Debounce();
    }

    private void Debounce()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void Refresh()
    {
        try
        {
            var unit = Units[Math.Max(UnitBox.SelectedIndex, 0)];
            _rows = EpochBatchTool.Convert(InputBox.Text, unit);
            OutputBox.Text = EpochBatchTool.ToTable(_rows);

            var errors = _rows.Count(r => r.Error is not null);
            StatusText.Text = $"{_rows.Count - errors} converted, {errors} error{(errors == 1 ? "" : "s")}";
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void CopyTable_Click(object sender, RoutedEventArgs e) => Ui.Copy(EpochBatchTool.ToTable(_rows), CopyTableBtn);
    private void CopyCsv_Click(object sender, RoutedEventArgs e) => Ui.Copy(EpochBatchTool.ToCsv(_rows), CopyCsvBtn);
}
