using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("unicode-inspect", "Unicode String Inspector", ToolCategory.TextProcessing,
    "Inspect a string codepoint by codepoint and grapheme by grapheme, flagging invisible characters.",
    Keywords = "unicode,codepoint,utf8,grapheme,invisible,debug", Order = 130)]
public partial class UnicodeInspectView : UserControl
{
    private const int DisplayCap = 500;

    public UnicodeInspectView()
    {
        InitializeComponent();
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        try
        {
            var report = UnicodeTool.Inspect(InputBox.Text);
            SummaryText.Text =
                $"{report.Utf16Units} UTF-16 units · {report.Codepoints} codepoints · " +
                $"{report.Graphemes} graphemes · {report.Utf8Bytes} UTF-8 bytes";

            var normalBrush = (Brush)FindResource("Brush.Fg0");
            var dimBrush = (Brush)FindResource("Brush.Fg2");
            var warningBrush = (Brush)FindResource("Brush.Warning");

            var rows = report.Chars.Take(DisplayCap).Select(c => new DisplayRow(
                GlyphDisplay: c.Invisible ? c.Warning ?? "?" : c.Glyph,
                GlyphFontSize: c.Invisible ? 10 : 15,
                GlyphBrush: c.Invisible ? warningBrush : normalBrush,
                CodepointHex: c.CodepointHex,
                Utf8Hex: c.Utf8Hex,
                CategoryDisplay: FormatCategory(c.Category),
                CategoryBrush: c.Invisible ? warningBrush : dimBrush)).ToList();

            RowsList.ItemsSource = rows;

            if (report.Chars.Count > DisplayCap)
            {
                CapNoteText.Text = $"Showing the first {DisplayCap} of {report.Chars.Count} codepoints.";
                CapNoteText.Visibility = Visibility.Visible;
            }
            else
            {
                CapNoteText.Visibility = Visibility.Collapsed;
            }

            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private static string FormatCategory(UnicodeCategory category)
    {
        var raw = category.ToString();
        var sb = new StringBuilder(raw.Length + 4);
        for (var i = 0; i < raw.Length; i++)
        {
            if (i > 0 && char.IsUpper(raw[i]))
                sb.Append(' ');
            sb.Append(raw[i]);
        }
        return sb.ToString();
    }

    private sealed record DisplayRow(
        string GlyphDisplay,
        double GlyphFontSize,
        Brush GlyphBrush,
        string CodepointHex,
        string Utf8Hex,
        string CategoryDisplay,
        Brush CategoryBrush);
}
