using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("unix-time", "Unix Timestamp ↔ Date", ToolCategory.TextProcessing,
    "Convert Unix timestamps to dates and back, with a live ticking clock.",
    Keywords = "unix,epoch,timestamp,date,utc", Order = 100)]
public partial class UnixTimeView : UserControl
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm", "yyyy-MM-dd",
    ];

    private bool _updating;
    private DispatcherTimer? _timer;
    private long _nowSeconds;
    private long _nowMillis;

    public UnixTimeView()
    {
        InitializeComponent();
        UnitBox.ItemsSource = new[] { "Auto", "s", "ms", "µs" };
        UnitBox.SelectedIndex = 0;

        UpdateNow();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateNow();
        _timer.Start();
    }

    // Stops the ticker so the view doesn't keep updating (or leak) after being closed.
    private void UnixTimeView_Unloaded(object sender, RoutedEventArgs e)
    {
        _timer?.Stop();
        _timer = null;
    }

    private void UpdateNow()
    {
        var now = DateTimeOffset.UtcNow;
        _nowSeconds = now.ToUnixTimeSeconds();
        _nowMillis = now.ToUnixTimeMilliseconds();
        NowSecondsText.Text = $"{_nowSeconds} s";
        NowMillisText.Text = $"{_nowMillis} ms";
    }

    private void CopyNowSeconds_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(_nowSeconds.ToString(CultureInfo.InvariantCulture), CopyNowSecondsBtn);

    private void CopyNowMillis_Click(object sender, RoutedEventArgs e) =>
        Ui.Copy(_nowMillis.ToString(CultureInfo.InvariantCulture), CopyNowMillisBtn);

    private EpochUnit? SelectedUnit => UnitBox.SelectedIndex switch
    {
        1 => EpochUnit.Seconds,
        2 => EpochUnit.Millis,
        3 => EpochUnit.Micros,
        _ => null,
    };

    private void EpochUnit_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            RefreshEpochToDate();
    }

    private void EpochInputBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshEpochToDate();

    private void RefreshEpochToDate() => Run(() =>
    {
        var text = EpochInputBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            LocalIsoText.Text = UtcIsoText.Text = Rfc1123Text.Text = RelativeText.Text = DetectedUnitText.Text = "";
            return;
        }

        var (value, detectedUnit) = EpochTool.Detect(text);
        var unit = SelectedUnit ?? detectedUnit;
        var date = EpochTool.ToDate(value, unit);

        LocalIsoText.Text = date.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        UtcIsoText.Text = date.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);
        Rfc1123Text.Text = date.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);
        RelativeText.Text = EpochTool.Humanize(date, DateTimeOffset.Now);
        DetectedUnitText.Text = SelectedUnit is null
            ? $"Auto-detected unit: {UnitLabel(unit)}"
            : $"Unit: {UnitLabel(unit)}";
    });

    private static string UnitLabel(EpochUnit unit) => unit switch
    {
        EpochUnit.Seconds => "seconds",
        EpochUnit.Millis => "milliseconds",
        EpochUnit.Micros => "microseconds",
        _ => unit.ToString(),
    };

    private void NowButton_Click(object sender, RoutedEventArgs e) =>
        DateInputBox.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private void DateInputBox_TextChanged(object sender, TextChangedEventArgs e) => Run(() =>
    {
        var text = DateInputBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            DateSecondsText.Text = DateMillisText.Text = "";
            return;
        }

        DateTimeOffset date;
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var withOffset))
        {
            date = withOffset;
        }
        else if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            date = new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Local));
        }
        else
        {
            throw new FormatException($"'{text}' is not a recognized date/time format.");
        }

        var values = EpochTool.FromDate(date);
        DateSecondsText.Text = values.Seconds.ToString(CultureInfo.InvariantCulture);
        DateMillisText.Text = values.Millis.ToString(CultureInfo.InvariantCulture);
    });

    private void CopyDateSeconds_Click(object sender, RoutedEventArgs e) => Ui.Copy(DateSecondsText.Text, CopyDateSecondsBtn);
    private void CopyDateMillis_Click(object sender, RoutedEventArgs e) => Ui.Copy(DateMillisText.Text, CopyDateMillisBtn);

    /// <summary>Runs a conversion with reentrancy protection and inline error reporting.</summary>
    private void Run(Action convert)
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            convert();
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
