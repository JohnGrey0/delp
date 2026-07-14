using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;
using Microsoft.Win32;

namespace Delp.App.Tools.Hashing;

[Tool("hash-generator", "Hash Generator", ToolCategory.Hashing,
    "Compute MD5, SHA-1, SHA-256, SHA-384, and SHA-512 digests of text or a file.",
    Keywords = "md5,sha,sha256,sha512,digest,checksum", Order = 10)]
public partial class HashGeneratorView : UserControl
{
    private readonly ObservableCollection<HashRow> _rows;
    private readonly Dictionary<string, string> _rawHex = new();
    private string? _filePath;
    private int _computeToken;

    public HashGeneratorView()
    {
        InitializeComponent();
        _rows = new ObservableCollection<HashRow>(HashTool.Algorithms.Select(a => new HashRow(a)));
        RowsList.ItemsSource = _rows;
        ComputeFromText();
    }

    private bool Uppercase => UppercaseBox.IsChecked == true;

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
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

    private void InputBox_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void InputBox_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
            _ = HashFileAsync(files[0]);
    }

    private async Task HashFileAsync(string path)
    {
        var token = ++_computeToken;
        _filePath = path;
        ErrorText.Visibility = Visibility.Collapsed;
        FileInfoText.Visibility = Visibility.Visible;

        foreach (var row in _rows)
            row.Hex = "Hashing…";

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
        }
    }

    private void CopyRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: HashRow row } button)
            Ui.Copy(row.Hex, button);
    }
}

/// <summary>Bindable [algorithm, hex] row for the hash-generator results list.</summary>
public sealed class HashRow(string algorithm) : INotifyPropertyChanged
{
    private string _hex = "";

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

    public event PropertyChangedEventHandler? PropertyChanged;
}
