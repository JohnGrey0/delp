using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("nanoid", "ID Generator", ToolCategory.Hashing,
    "Generate and decode Nano ID, ULID, Snowflake, and MongoDB ObjectId identifiers.",
    Keywords = "nanoid,id,short,url-safe,ulid,snowflake,objectid,mongodb,sortable,decode", Order = 200)]
public partial class NanoIdView : UserControl
{
    private enum IdKind { NanoId, Ulid, Snowflake, ObjectId }

    private readonly ErrorBox _error;
    private readonly ErrorBox _decodeError;
    private readonly ObservableCollection<DecodedRow> _decodeResults = [];

    public NanoIdView()
    {
        InitializeComponent();
        _error = new ErrorBox(ErrorText);
        _decodeError = new ErrorBox(DecodeErrorText);
        DecodeResultsList.ItemsSource = _decodeResults;
        UpdatePanelVisibility();
        RunDecode();
    }

    /// <summary>
    /// Belt-and-suspenders: the constructor already initializes panel visibility and the sample
    /// decode once (XAML-declared defaults can otherwise briefly disagree with the Kind combo's
    /// selection before layout settles) — re-running here on Loaded is a harmless no-op re-render.
    /// </summary>
    private void View_Loaded(object sender, RoutedEventArgs e)
    {
        UpdatePanelVisibility();
        RunDecode();
    }

    // ---- GENERATE tab ----

    private IdKind SelectedKind => (IdKind)KindCombo.SelectedIndex;

    private static Visibility Show(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;

    private void Kind_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        UpdatePanelVisibility();
        _error.HideError();
        OutputBox.Text = "";
        CollisionNote.Text = "";
    }

    private void Epoch_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        CustomEpochPanel.Visibility = Show(EpochCombo.SelectedIndex == 2);
    }

    private void UpdatePanelVisibility()
    {
        var kind = SelectedKind;

        SizePanel.Visibility = Show(kind == IdKind.NanoId);
        AlphabetRow.Visibility = Show(kind == IdKind.NanoId);
        AlphabetNote.Visibility = Show(kind == IdKind.NanoId);

        EpochPanel.Visibility = Show(kind == IdKind.Snowflake);
        CustomEpochPanel.Visibility = Show(kind == IdKind.Snowflake && EpochCombo.SelectedIndex == 2);
        WorkerPanel.Visibility = Show(kind == IdKind.Snowflake);
        ProcessPanel.Visibility = Show(kind == IdKind.Snowflake);

        KindDescription.Text = kind switch
        {
            IdKind.NanoId => "Nano ID: compact, URL-safe random strings with a configurable size and alphabet.",
            IdKind.Ulid => "ULID: 26-character, lexically sortable ids — a millisecond timestamp plus 80 bits of randomness. Monotonic within a generated batch.",
            IdKind.Snowflake => "Snowflake: a 64-bit integer packing a timestamp, worker id, process id, and sequence — as used by Twitter and Discord.",
            IdKind.ObjectId => "MongoDB ObjectId: a 24-hex-digit id packing a Unix-second timestamp, a per-process random value, and an incrementing counter.",
            _ => "",
        };
    }

    private void Generate_Click(object sender, RoutedEventArgs e) => _error.Run(() =>
    {
        if (!int.TryParse(CountBox.Text.Trim(), out var count) || count < 1 || count > 1000)
            throw new FormatException("Count must be a whole number between 1 and 1000.");

        switch (SelectedKind)
        {
            case IdKind.NanoId:
                GenerateNanoId(count);
                break;

            case IdKind.Ulid:
                CollisionNote.Text = "";
                OutputBox.Text = string.Join(Environment.NewLine, UlidTool.GenerateBatch(count));
                break;

            case IdKind.Snowflake:
            {
                CollisionNote.Text = "";
                var epoch = ResolveEpoch(EpochCombo.SelectedIndex, CustomEpochBox.Text);
                var worker = ParseBitField(WorkerBox.Text, "Worker");
                var process = ParseBitField(ProcessBox.Text, "Process");
                var ids = SnowflakeTool.GenerateBatch(count, epoch, worker, process);
                OutputBox.Text = string.Join(Environment.NewLine, ids.Select(i => i.ToString(CultureInfo.InvariantCulture)));
                break;
            }

            case IdKind.ObjectId:
                CollisionNote.Text = "";
                OutputBox.Text = string.Join(Environment.NewLine, ObjectIdTool.GenerateBatch(count));
                break;
        }
    });

    private void GenerateNanoId(int count)
    {
        if (!int.TryParse(SizeBox.Text.Trim(), out var size))
            throw new FormatException("Size must be a whole number.");

        var alphabet = string.IsNullOrEmpty(AlphabetBox.Text) ? NanoIdTool.DefaultAlphabet : AlphabetBox.Text;

        var lines = Enumerable.Range(0, count).Select(_ => NanoIdTool.Generate(size, alphabet));
        OutputBox.Text = string.Join(Environment.NewLine, lines);

        var years = NanoIdTool.YearsFor1PercentCollision(size, alphabet.Distinct().Count(), idsPerHour: 1000);
        CollisionNote.Text = FormatCollisionNote(years);
    }

    private static string FormatCollisionNote(double years) => years switch
    {
        <= 0 => "Collision odds: not applicable for this alphabet.",
        >= 1_000_000_000 => "At 1,000 IDs/hour, it would take over a billion years to reach a 1% collision probability.",
        _ => $"~{years:N0} years needed, at 1,000 IDs/hour, to reach a 1% probability of a collision.",
    };

    private static int ParseBitField(string text, string name)
    {
        if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) || v < 0 || v > 31)
            throw new FormatException($"{name} must be a whole number from 0 to 31.");
        return v;
    }

    private static long ResolveEpoch(int selectedIndex, string customText) => selectedIndex switch
    {
        0 => SnowflakeTool.TwitterEpochMs,
        1 => SnowflakeTool.DiscordEpochMs,
        _ => ParseCustomEpoch(customText),
    };

    private static long ParseCustomEpoch(string text)
    {
        if (!long.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ms))
            throw new FormatException("Custom epoch must be a whole number of milliseconds since 1970-01-01T00:00:00Z.");
        return ms;
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(OutputBox.Text, CopyBtn);

    // ---- DECODE tab ----

    private void DecodeEpoch_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        DecodeCustomEpochPanel.Visibility = Show(DecodeEpochCombo.SelectedIndex == 2);
        RunDecode();
    }

    private void DecodeInput_Changed(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        RunDecode();
    }

    private void RunDecode() => _decodeError.Run(() =>
    {
        var lines = DecodeInputBox.Text
            .Split('\n')
            .Select(l => l.Trim('\r', ' ', '\t'))
            .Where(l => l.Length > 0)
            .ToList();

        _decodeResults.Clear();
        if (lines.Count == 0)
            return;

        var epoch = ResolveEpoch(DecodeEpochCombo.SelectedIndex, DecodeCustomEpochBox.Text);

        var errors = new List<string>();
        foreach (var line in lines)
        {
            try
            {
                _decodeResults.Add(new DecodedRow(IdDecodeTool.Decode(line, epoch)));
            }
            catch (Exception ex)
            {
                errors.Add($"'{line}': {ex.Message}");
            }
        }

        if (errors.Count > 0)
            throw new FormatException(string.Join(Environment.NewLine, errors));
    });

    private sealed record DecodedRow(DecodedId Decoded)
    {
        public string Input => Decoded.Input;
        public string TypeLabel => Decoded.TypeLabel;

        public string TimestampText => Decoded.Timestamp is { } ts
            ? $"{ts.UtcDateTime:yyyy-MM-dd HH:mm:ss.fff} UTC"
            : "No embedded timestamp.";

        public IReadOnlyList<string> FieldLines => Decoded.Fields.Select(f => $"{f.Label}: {f.Value}").ToList();
    }
}
