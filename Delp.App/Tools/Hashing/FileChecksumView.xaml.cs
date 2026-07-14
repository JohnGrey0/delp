using System.IO;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;
using Microsoft.Win32;

namespace Delp.App.Tools.Hashing;

[Tool("file-checksum", "File Checksum Verifier", ToolCategory.Hashing,
    "Hash a file and compare it against an expected checksum.",
    Keywords = "checksum,verify,file,integrity,sha256", Order = 40)]
public partial class FileChecksumView : UserControl
{
    private string? _filePath;
    private int _computeToken;

    public FileChecksumView()
    {
        InitializeComponent();
    }

    private string Algorithm => (AlgoCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "SHA-256";

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Choose a file to verify" };
        if (dialog.ShowDialog() == true)
            _ = HashFileAsync(dialog.FileName);
    }

    private void DropZone_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
            _ = HashFileAsync(files[0]);
    }

    private void AlgoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && _filePath is not null)
            _ = HashFileAsync(_filePath);
    }

    private async Task HashFileAsync(string path)
    {
        var token = ++_computeToken;
        _filePath = path;
        ErrorText.Visibility = Visibility.Collapsed;
        HashBox.Text = "Hashing…";
        UpdateMatch();

        try
        {
            var info = new FileInfo(path);
            DropHint.Visibility = Visibility.Collapsed;
            FileInfoText.Text = $"{info.Name} ({FileSize.Format(info.Length)})";
            FileInfoText.Visibility = Visibility.Visible;

            var algorithm = Algorithm;
            var hex = await Task.Run(() =>
            {
                using var stream = File.OpenRead(path);
                return HashTool.Compute(algorithm, stream);
            });

            if (token != _computeToken)
                return;

            HashBox.Text = hex;
            UpdateMatch();
        }
        catch (Exception ex)
        {
            if (token != _computeToken)
                return;
            HashBox.Text = "";
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
            UpdateMatch();
        }
    }

    private void ExpectedBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateMatch();

    private void UpdateMatch()
    {
        var expected = ExpectedBox.Text;
        var hasHash = !string.IsNullOrEmpty(HashBox.Text) && HashBox.Text != "Hashing…";

        if (string.IsNullOrWhiteSpace(expected) || !hasHash)
        {
            MatchOkText.Visibility = Visibility.Collapsed;
            MatchFailText.Visibility = Visibility.Collapsed;
            return;
        }

        var isMatch = ChecksumTool.Verify(HashBox.Text, expected);
        MatchOkText.Visibility = isMatch ? Visibility.Visible : Visibility.Collapsed;
        MatchFailText.Visibility = isMatch ? Visibility.Collapsed : Visibility.Visible;
    }

    private void CopyHash_Click(object sender, RoutedEventArgs e) => Ui.Copy(HashBox.Text, CopyHashBtn);
}
