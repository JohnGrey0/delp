using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace Delp.App.Controls;

/// <summary>
/// Factory for dark-themed AvalonEdit editors.
/// Pass syntax "Json" for the bundled dark-tuned JSON definition; other AvalonEdit
/// built-ins ("XML", "HTML", "JavaScript", "CSS") work but use light-theme colors —
/// prefer no highlighting over unreadable highlighting.
/// </summary>
public static class CodeEditors
{
    private static readonly IHighlightingDefinition? JsonDark = LoadJsonDark();

    public static TextEditor Create(string? syntax = null, bool readOnly = false, bool wordWrap = false)
    {
        var editor = new TextEditor
        {
            FontFamily = new FontFamily("Cascadia Mono, Cascadia Code, Consolas"),
            FontSize = 13,
            Background = Brushes.Transparent,
            ShowLineNumbers = true,
            WordWrap = wordWrap,
            IsReadOnly = readOnly,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(4),
        };
        editor.SetResourceReference(Control.ForegroundProperty, "Brush.Fg0");
        editor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(0x50, 0x0A, 0x84, 0xFF));
        editor.TextArea.SelectionBorder = null;
        editor.TextArea.Caret.CaretBrush = Brushes.White;
        editor.LineNumbersForeground = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF));
        editor.Options.EnableHyperlinks = false;
        editor.Options.EnableEmailHyperlinks = false;

        if (!string.IsNullOrEmpty(syntax))
            editor.SyntaxHighlighting = syntax.Equals("Json", StringComparison.OrdinalIgnoreCase) && JsonDark is not null
                ? JsonDark
                : HighlightingManager.Instance.GetDefinition(syntax);

        return editor;
    }

    private static IHighlightingDefinition? LoadJsonDark()
    {
        try
        {
            using var stream = typeof(CodeEditors).Assembly
                .GetManifestResourceStream("Delp.App.Assets.Json.xshd");
            if (stream is null)
                return null;
            using var reader = XmlReader.Create(stream);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        catch
        {
            return null;
        }
    }
}
