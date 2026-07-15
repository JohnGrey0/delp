using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("unix-time", "Date & Time Converter", ToolCategory.TextProcessing,
    "Convert between Unix timestamps, FILETIME, and .NET ticks; compare times across zones; and compute date/time deltas, with a live ticking clock and batch conversion of pasted timestamp lists.",
    Keywords = "unix,epoch,timestamp,date,utc,batch,timestamps,convert,logs,epoch-batch,timezone,zones,delta,duration,filetime,ldap,iso 8601,milliseconds,ticks", Order = 100)]
public partial class UnixTimeView : UserControl
{
    private static readonly string[] UnitLabels = ["Auto", "Seconds", "Milliseconds", "Microseconds", "FILETIME–LDAP", ".NET ticks"];
    private static readonly string[] MathUnitLabels = ["Seconds", "Minutes", "Hours", "Days", "Weeks", "Months", "Years"];
    private static readonly DateMathUnit[] MathUnitValues =
        [DateMathUnit.Seconds, DateMathUnit.Minutes, DateMathUnit.Hours, DateMathUnit.Days, DateMathUnit.Weeks, DateMathUnit.Months, DateMathUnit.Years];

    private static readonly EpochUnit?[] BatchUnits = [null, EpochUnit.Seconds, EpochUnit.Millis, EpochUnit.Micros];

    private bool _updating;
    private bool _zonesUpdating;
    private bool _deltaUpdating;
    private DispatcherTimer? _timer;
    private long _nowSeconds;
    private long _nowMillis;

    private IReadOnlyList<EpochRow> _batchRows = [];
    private readonly DispatcherTimer _batchDebounceTimer;

    // ZONES tab
    private readonly ObservableCollection<ZoneRow> _zoneRows = [];
    private readonly List<string> _allZoneNames;
    private readonly Dictionary<string, TimeZoneInfo> _zonesByDisplayName;
    private bool _zoneFilterGuard;

    public UnixTimeView()
    {
        InitializeComponent();
        UnitBox.ItemsSource = UnitLabels;
        UnitBox.SelectedIndex = 0;

        BatchUnitBox.ItemsSource = new[] { "Auto", "s", "ms", "µs" };
        BatchUnitBox.SelectedIndex = 0;

        _batchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _batchDebounceTimer.Tick += (_, _) =>
        {
            _batchDebounceTimer.Stop();
            RefreshBatch();
        };

        var allZones = DateTimeTool.GetAllZones();
        _allZoneNames = allZones.Select(z => z.DisplayName).ToList();
        _zonesByDisplayName = allZones
            .GroupBy(z => z.DisplayName)
            .ToDictionary(g => g.Key, g => g.First());

        ZoneRowsList.ItemsSource = _zoneRows;
        InitializeZoneCombo(SourceZoneBox);
        InitializeZoneCombo(AddZoneBox);
        SourceZoneBox.Text = TimeZoneInfo.Local.DisplayName;
        InitializePinnedZones();

        MathUnitBox.ItemsSource = MathUnitLabels;
        MathUnitBox.SelectedIndex = 3; // Days

        UpdateNow();
        RefreshZones();
        RefreshDelta();
        RefreshMath();
    }

    // View instances are cached and re-attached (ToolHost.Content flips back to them) when
    // the user revisits this tool, which fires Loaded again — so the ticker has to restart
    // here, not just once in the constructor, or the NOW card goes stale forever after the
    // first time the user navigates away.
    private void UnixTimeView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_timer is not null)
            return; // already running (defensive: Loaded shouldn't fire twice without Unloaded)

        UpdateNow();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            UpdateNow();
            // Blank "date/time" inputs mean "now" — keep those live too, same as the NOW card.
            if (string.IsNullOrWhiteSpace(ZonesDateInputBox.Text))
                RefreshZones();
            if (string.IsNullOrWhiteSpace(DeltaFromBox.Text) || string.IsNullOrWhiteSpace(DeltaToBox.Text))
                RefreshDelta();
            if (string.IsNullOrWhiteSpace(MathBaseBox.Text))
                RefreshMath();
        };
        _timer.Start();
    }

    // Stops the ticker so the view doesn't keep updating (or leak) while it's not visible.
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
        4 => EpochUnit.FileTime,
        5 => EpochUnit.Ticks,
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
            EpochSecondsOutText.Text = EpochMillisOutText.Text = FileTimeOutText.Text = TicksOutText.Text = "";
            return;
        }

        var (value, detectedUnit) = EpochTool.Detect(text);
        var unit = SelectedUnit ?? detectedUnit;
        var date = EpochTool.ToDate(value, unit);
        var described = EpochTool.Describe(date, DateTimeOffset.Now);

        LocalIsoText.Text = described.LocalIso;
        UtcIsoText.Text = described.UtcIso;
        Rfc1123Text.Text = described.Rfc1123;
        RelativeText.Text = described.Relative;
        DetectedUnitText.Text = SelectedUnit is null
            ? $"Auto-detected unit: {UnitLabel(unit)}"
            : $"Unit: {UnitLabel(unit)}";

        EpochSecondsOutText.Text = $"{described.Seconds} s";
        EpochMillisOutText.Text = $"{described.Millis} ms";
        FileTimeOutText.Text = described.FileTime is { } fileTime
            ? fileTime.ToString(CultureInfo.InvariantCulture)
            : "N/A (before 1601-01-01, the FILETIME epoch)";
        TicksOutText.Text = described.Ticks.ToString(CultureInfo.InvariantCulture);
    });

    private static string UnitLabel(EpochUnit unit) => unit switch
    {
        EpochUnit.Seconds => "seconds",
        EpochUnit.Millis => "milliseconds",
        EpochUnit.Micros => "microseconds",
        EpochUnit.FileTime => "FILETIME/LDAP (100 ns since 1601)",
        EpochUnit.Ticks => ".NET ticks (100 ns since 0001-01-01)",
        _ => unit.ToString(),
    };

    private void CopyEpochSecondsOut_Click(object sender, RoutedEventArgs e) => Ui.Copy(EpochSecondsOutText.Text, CopyEpochSecondsOutBtn);
    private void CopyEpochMillisOut_Click(object sender, RoutedEventArgs e) => Ui.Copy(EpochMillisOutText.Text, CopyEpochMillisOutBtn);
    private void CopyFileTimeOut_Click(object sender, RoutedEventArgs e) => Ui.Copy(FileTimeOutText.Text, CopyFileTimeOutBtn);
    private void CopyTicksOut_Click(object sender, RoutedEventArgs e) => Ui.Copy(TicksOutText.Text, CopyTicksOutBtn);

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

        var date = DateTimeTool.ParseFlexible(text, DateTimeOffset.Now);
        var values = EpochTool.FromDate(date);
        DateSecondsText.Text = values.Seconds.ToString(CultureInfo.InvariantCulture);
        DateMillisText.Text = values.Millis.ToString(CultureInfo.InvariantCulture);
    });

    private void CopyDateSeconds_Click(object sender, RoutedEventArgs e) => Ui.Copy(DateSecondsText.Text, CopyDateSecondsBtn);
    private void CopyDateMillis_Click(object sender, RoutedEventArgs e) => Ui.Copy(DateMillisText.Text, CopyDateMillisBtn);

    // ------------------------------------------------------------------------------ ZONES tab

    private void InitializeZoneCombo(ComboBox box)
    {
        box.ItemsSource = _allZoneNames;
        // The editable ComboBox's internal PART_EditableTextBox is a real TextBox, so its
        // TextChanged bubbles up to the ComboBox itself — that's how we get a live filter
        // without a package (WPF's built-in text search only jumps to a match, it doesn't
        // narrow the dropdown).
        box.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => FilterZoneCombo(box)));
    }

    private void FilterZoneCombo(ComboBox box)
    {
        if (_zoneFilterGuard)
            return;

        var text = box.Text;
        _zoneFilterGuard = true;
        try
        {
            var filtered = string.IsNullOrWhiteSpace(text)
                ? _allZoneNames
                : _allZoneNames.Where(n => n.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();

            box.ItemsSource = filtered;
            box.Text = text; // reassigning ItemsSource can otherwise clear the typed text
            if (box.Template.FindName("PART_EditableTextBox", box) is TextBox editableTextBox)
                editableTextBox.CaretIndex = text.Length;
            box.IsDropDownOpen = filtered.Count > 0 && box.IsKeyboardFocusWithin;
        }
        finally
        {
            _zoneFilterGuard = false;
        }

        // Live-refresh the ZONES tab as the source zone is typed, once it resolves.
        if (ReferenceEquals(box, SourceZoneBox) && IsLoaded)
            RefreshZones();
    }

    private void InitializePinnedZones()
    {
        foreach (var id in DateTimeTool.DefaultPinnedZoneIds)
        {
            try
            {
                AddPinnedZone(id, DateTimeTool.FindZone(id));
            }
            catch (FormatException)
            {
                // Not installed on this machine — skip it rather than fail the whole tab.
            }
        }
    }

    private void AddPinnedZone(string zoneId, TimeZoneInfo zone)
    {
        if (_zoneRows.Any(r => r.ZoneId == zoneId))
            return;
        _zoneRows.Add(new ZoneRow(zoneId, zone, isLocal: zoneId == DateTimeTool.LocalZoneId));
    }

    private TimeZoneInfo ResolveZoneFromBox(ComboBox box, TimeZoneInfo fallback) =>
        _zonesByDisplayName.TryGetValue(box.Text, out var zone) ? zone : fallback;

    private void SourceZoneBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_zoneFilterGuard)
            return;
        if (IsLoaded)
            RefreshZones();
    }

    private void AddZoneBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_zoneFilterGuard)
            return;
        if (AddZoneBox.SelectedItem is not string name || !_zonesByDisplayName.TryGetValue(name, out var zone))
            return;

        AddPinnedZone(zone.Id, zone);
        if (IsLoaded)
            RefreshZones();

        // Reset the picker so it's ready to add another zone.
        _zoneFilterGuard = true;
        try
        {
            AddZoneBox.SelectedIndex = -1;
            AddZoneBox.ItemsSource = _allZoneNames;
            AddZoneBox.Text = "";
        }
        finally
        {
            _zoneFilterGuard = false;
        }
    }

    private void RemoveZone_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ZoneRow row })
            _zoneRows.Remove(row);
    }

    private void ZonesDateInputBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshZones();

    private void RefreshZones() => RunZones(() =>
    {
        var now = DateTimeOffset.Now;
        var sourceZone = ResolveZoneFromBox(SourceZoneBox, TimeZoneInfo.Local);
        var instant = DateTimeTool.ParseFlexible(ZonesDateInputBox.Text, now, sourceZone);

        foreach (var row in _zoneRows)
        {
            var conversion = DateTimeTool.ConvertToZone(instant, row.Zone);
            row.LocalTimeText = conversion.LocalTime.ToString("yyyy-MM-dd HH:mm:ss dddd", CultureInfo.InvariantCulture);
            row.OffsetText = FormatOffset(conversion.UtcOffset);
            row.IsDst = conversion.IsDaylightSaving;
        }
    });

    private static string FormatOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var abs = offset.Duration();
        return $"UTC{sign}{abs.Hours:00}:{abs.Minutes:00}";
    }

    // ------------------------------------------------------------------------------ DELTA tab

    private void DeltaBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshDelta();

    private void RefreshDelta() => RunDelta(() =>
    {
        var now = DateTimeOffset.Now;
        var a = DateTimeTool.ParseFlexible(DeltaFromBox.Text, now);
        var b = DateTimeTool.ParseFlexible(DeltaToBox.Text, now);

        var delta = DateTimeTool.Delta(a, b);
        DeltaHumanText.Text = delta.Human;
        DeltaDaysText.Text = delta.TotalDays.ToString("F2", CultureInfo.InvariantCulture);
        DeltaHoursText.Text = delta.TotalHours.ToString("F2", CultureInfo.InvariantCulture);
        DeltaMinutesText.Text = delta.TotalMinutes.ToString("F2", CultureInfo.InvariantCulture);
        DeltaSecondsText.Text = delta.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture);
        DeltaIsoText.Text = delta.Iso8601;
    });

    private void MathBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshMath();

    private void MathUnit_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            RefreshMath();
    }

    private void RefreshMath() => RunDelta(() =>
    {
        var now = DateTimeOffset.Now;
        var baseDate = DateTimeTool.ParseFlexible(MathBaseBox.Text, now);

        var amountText = MathAmountBox.Text;
        if (string.IsNullOrWhiteSpace(amountText))
        {
            MathResultLocalText.Text = MathResultUtcText.Text = "";
            return;
        }
        if (!double.TryParse(amountText, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount))
            throw new FormatException($"'{amountText}' is not a valid number.");

        var unit = MathUnitValues[Math.Max(MathUnitBox.SelectedIndex, 0)];
        var result = DateTimeTool.AddUnits(baseDate, amount, unit);

        MathResultLocalText.Text = result.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        MathResultUtcText.Text = result.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);
    });

    // ------------------------------------------------------------------------------ BATCH tab

    // Conversion cost scales with the number of pasted lines (a whole log file's worth),
    // so debounce rather than re-converting on every keystroke.
    private void BatchInputBox_TextChanged(object sender, TextChangedEventArgs e) => DebounceBatch();

    private void BatchOption_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            DebounceBatch();
    }

    private void DebounceBatch()
    {
        _batchDebounceTimer.Stop();
        _batchDebounceTimer.Start();
    }

    private void RefreshBatch()
    {
        try
        {
            var unit = BatchUnits[Math.Max(BatchUnitBox.SelectedIndex, 0)];
            _batchRows = EpochBatchTool.Convert(BatchInputBox.Text, unit);
            BatchOutputBox.Text = EpochBatchTool.ToTable(_batchRows);

            var errors = _batchRows.Count(r => r.Error is not null);
            BatchStatusText.Text = $"{_batchRows.Count - errors} converted, {errors} error{(errors == 1 ? "" : "s")}";
            BatchErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            BatchErrorText.Text = ex.Message;
            BatchErrorText.Visibility = Visibility.Visible;
        }
    }

    private void BatchCopyTable_Click(object sender, RoutedEventArgs e) => Ui.Copy(EpochBatchTool.ToTable(_batchRows), BatchCopyTableBtn);
    private void BatchCopyCsv_Click(object sender, RoutedEventArgs e) => Ui.Copy(EpochBatchTool.ToCsv(_batchRows), BatchCopyCsvBtn);

    // ------------------------------------------------------------------------------- Guards

    /// <summary>Runs a conversion with reentrancy protection and inline error reporting.</summary>
    private void Run(Action convert) => RunGuarded(ref _updating, ErrorText, convert);

    private void RunZones(Action convert) => RunGuarded(ref _zonesUpdating, ZonesErrorText, convert);

    private void RunDelta(Action convert) => RunGuarded(ref _deltaUpdating, DeltaErrorText, convert);

    private static void RunGuarded(ref bool guard, TextBlock errorText, Action convert)
    {
        if (guard)
            return;
        guard = true;
        try
        {
            convert();
            errorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            errorText.Text = ex.Message;
            errorText.Visibility = Visibility.Visible;
        }
        finally
        {
            guard = false;
        }
    }
}

/// <summary>Bindable pinned-zone row for the ZONES tab's results list.</summary>
public sealed class ZoneRow(string zoneId, TimeZoneInfo zone, bool isLocal) : INotifyPropertyChanged
{
    public string ZoneId { get; } = zoneId;

    public TimeZoneInfo Zone { get; } = zone;

    public string DisplayName { get; } = isLocal ? $"Local — {zone.DisplayName}" : zone.DisplayName;

    private string _localTimeText = "";
    public string LocalTimeText
    {
        get => _localTimeText;
        set { _localTimeText = value; OnChanged(); }
    }

    private string _offsetText = "";
    public string OffsetText
    {
        get => _offsetText;
        set { _offsetText = value; OnChanged(); }
    }

    private bool _isDst;
    public bool IsDst
    {
        get => _isDst;
        set { _isDst = value; OnChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
