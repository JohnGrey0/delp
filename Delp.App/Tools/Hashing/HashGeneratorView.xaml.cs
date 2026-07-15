using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;
using Microsoft.Win32;

namespace Delp.App.Tools.Hashing;

[Tool("hash-generator", "Hash Generator & Checksum", ToolCategory.Hashing,
    "Compute MD5, SHA-1, SHA-256, SHA-384, and SHA-512 digests of text or a file, and verify against an expected checksum.",
    Keywords = "md5,sha,sha256,sha512,digest,checksum,verify,integrity,file,file-checksum", Order = 10)]
public partial class HashGeneratorView : UserControl
{
    private readonly ObservableCollection<HashRow> _rows;
    private readonly Dictionary<string, string> _rawHex = new();
    private readonly Brush _successBrush;
    private string? _filePath;
    private int _computeToken;

    public HashGeneratorView()
    {
        InitializeComponent();
        _successBrush = (Brush)FindResource("Brush.Success");
        _rows = new ObservableCollection<HashRow>(HashTool.Algorithms.Select(a => new HashRow(a)));
        RowsList.ItemsSource = _rows;
        ComputeFromText();
    }

    private bool Uppercase => UppercaseBox.IsChecked == true;

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Bump the generation token so a file hash still running in the background can't
        // land after this text result and clobber the rows the user is now looking at.
        _computeToken++;
        if (_filePath is not null)
        {
            _filePath = null;
            FileInfoText.Visibility = Visibility.Collapsed;
        }
        ComputeFromText();
    }

    private void ComputeFromText()
    {
        try
        {
            var data = System.Text.Encoding.UTF8.GetBytes(InputBox.Text);
            foreach (var result in HashTool.ComputeAll(data))
                _rawHex[result.Algorithm] = result.Hex;
            ApplyCase();
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }

        UpdateMatches();
    }

    private void ApplyCase()
    {
        foreach (var row in _rows)
        {
            if (_rawHex.TryGetValue(row.Algorithm, out var hex))
                row.Hex = Uppercase ? hex.ToUpperInvariant() : hex;
        }
    }

    private void Uppercase_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            ApplyCase();
    }

    private void PickFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Choose a file to hash" };
        if (dialog.ShowDialog() == true)
            _ = HashFileAsync(dialog.FileName);
    }

    private void InputBox_PreviewDragOver(object sender, DragEventArgs e) => FileDropSupport.PreviewDragOver(e);

    private void InputBox_Drop(object sender, DragEventArgs e)
    {
        if (FileDropSupport.GetDroppedFile(e) is { } path)
            _ = HashFileAsync(path);
    }

    private async Task HashFileAsync(string path)
    {
        var token = ++_computeToken;
        _filePath = path;
        ErrorText.Visibility = Visibility.Collapsed;
        FileInfoText.Visibility = Visibility.Visible;

        foreach (var row in _rows)
            row.Hex = "Hashing…";
        // Stale digests can't be verified against while a new hash is in flight.
        UpdateMatches();

        try
        {
            var info = new FileInfo(path);
            FileInfoText.Text = $"File: {info.Name} ({FileSize.Format(info.Length)})";

            var results = await Task.Run(() =>
            {
                using var stream = File.OpenRead(path);
                return HashTool.ComputeAllFromStream(stream);
            });

            if (token != _computeToken)
                return;

            foreach (var result in results)
                _rawHex[result.Algorithm] = result.Hex;
            ApplyCase();
        }
        catch (Exception ex)
        {
            if (token != _computeToken)
                return;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
            foreach (var row in _rows)
                row.Hex = "";
            _rawHex.Clear();
        }

        UpdateMatches();
    }

    private void CopyRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: HashRow row } button)
            Ui.Copy(row.Hex, button);
    }

    private void ExpectedBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateMatches();

    /// <summary>
    /// Compares the pasted EXPECTED value (tolerant of case, whitespace, "algo:" prefixes, and
    /// "*name"/" name" suffixes via ChecksumTool.Verify) against every currently computed digest,
    /// and flags whichever row(s) match.
    /// </summary>
    private void UpdateMatches()
    {
        var expected = ExpectedBox.Text;

        if (string.IsNullOrWhiteSpace(expected))
        {
            foreach (var row in _rows)
                row.MatchText = "";
            NoMatchText.Visibility = Visibility.Collapsed;
            return;
        }

        var anyMatch = false;
        foreach (var row in _rows)
        {
            var matches = _rawHex.TryGetValue(row.Algorithm, out var hex) && ChecksumTool.Verify(hex, expected);
            if (matches)
            {
                row.MatchText = $"✓ Matches {DisplayAlgorithm(row.Algorithm)}";
                row.MatchBrush = _successBrush;
                anyMatch = true;
            }
            else
            {
                row.MatchText = "";
            }
        }

        NoMatchText.Visibility = anyMatch ? Visibility.Collapsed : Visibility.Visible;
    }

    private static string DisplayAlgorithm(string algorithm) =>
        algorithm == "MD5" ? algorithm : $"{algorithm[..3]}-{algorithm[3..]}";
}

/// <summary>Bindable [algorithm, hex, match badge] row for the hash-generator results list.</summary>
public sealed class HashRow(string algorithm) : INotifyPropertyChanged
{
    private string _hex = "";
    private string _matchText = "";
    private Brush _matchBrush = Brushes.Transparent;

    public string Algorithm { get; } = algorithm;

    public string Hex
    {
        get => _hex;
        set
        {
            _hex = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Hex)));
        }
    }

    /// <summary>e.g. "✓ Matches SHA-256"; empty when this row isn't the EXPECTED match.</summary>
    public string MatchText
    {
        get => _matchText;
        set
        {
            _matchText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MatchText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasMatch)));
        }
    }

    public Brush MatchBrush
    {
        get => _matchBrush;
        set
        {
            _matchBrush = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MatchBrush)));
        }
    }

    public bool HasMatch => _matchText.Length > 0;

    public event PropertyChangedEventHandler? PropertyChanged;
}
