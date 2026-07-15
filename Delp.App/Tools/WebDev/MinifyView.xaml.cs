using System.Windows;
using System.Windows.Controls;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.WebDev;

[Tool("minify", "Minifier / Beautifier", ToolCategory.WebDev,
    "Minify or beautify CSS, JavaScript, or HTML, with before/after size savings.",
    Keywords = "css,js,javascript,html,minify,beautify,uglify,css-minify,js-minify,html-minify,compress,format",
    Order = 20)]
public partial class MinifyView : UserControl
{
    // Named MinifyLanguage (not Language) to avoid hiding FrameworkElement.Language.
    private enum MinifyLanguage { Css, JavaScript, Html }

    private static readonly (MinifyLanguage Language, string Label)[] Languages =
    [
        (MinifyLanguage.Css, "CSS"),
        (MinifyLanguage.JavaScript, "JavaScript"),
        (MinifyLanguage.Html, "HTML"),
    ];

    private static readonly Dictionary<MinifyLanguage, string> SampleText = new()
    {
        [MinifyLanguage.Css] = ".card {\n  display: flex;\n  padding: 12px 16px;\n}\n",
        [MinifyLanguage.JavaScript] = "function greet(name) {\n  return \"Hello, \" + name + \"!\";\n}\n",
        [MinifyLanguage.Html] = "<!-- greeting -->\n<div>\n  <p>Hello   World</p>\n</div>\n",
    };

    private readonly TextEditor _inputEditor;
    private readonly TextEditor _outputEditor;

    public MinifyView()
    {
        InitializeComponent();

        _inputEditor = CodeEditors.Create();
        InputEditorHost.Child = _inputEditor;

        _outputEditor = CodeEditors.Create(readOnly: true);
        OutputEditorHost.Child = _outputEditor;

        LanguageBox.ItemsSource = Languages.Select(l => l.Label).ToList();
        LanguageBox.SelectedIndex = 0;

        _inputEditor.Text = SampleText[SelectedLanguage];
    }

    private MinifyLanguage SelectedLanguage => Languages[Math.Max(LanguageBox.SelectedIndex, 0)].Language;

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateOptionsVisibility();
        StatusText.Text = "";
        MinifierUi.ShowErrors(ErrorsText, []);
    }

    private void UpdateOptionsVisibility()
    {
        var language = SelectedLanguage;
        BeautifyBtn.Visibility = language == MinifyLanguage.Css ? Visibility.Visible : Visibility.Collapsed;
        HtmlOptionsPanel.Visibility = language == MinifyLanguage.Html ? Visibility.Visible : Visibility.Collapsed;
        JsNoteText.Visibility = language == MinifyLanguage.JavaScript ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void Minify_Click(object sender, RoutedEventArgs e)
    {
        var code = _inputEditor.Text;
        var language = SelectedLanguage;
        var htmlOptions = new HtmlMinifyOptions(
            RemoveComments: RemoveCommentsBox.IsChecked == true,
            CollapseWhitespace: CollapseWhitespaceBox.IsChecked == true);

        SetBusy(true);
        try
        {
            var result = await Task.Run(() => language switch
            {
                MinifyLanguage.Css => CssTool.Minify(code),
                MinifyLanguage.JavaScript => JsTool.Minify(code),
                MinifyLanguage.Html => HtmlTool.Minify(code, htmlOptions),
                _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown minify language."),
            });
            _outputEditor.Text = result.Code ?? "";
            MinifierUi.ShowResult(StatusText, ErrorsText, result);
        }
        catch (Exception ex)
        {
            MinifierUi.ShowError(ErrorsText, ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Beautify_Click(object sender, RoutedEventArgs e)
    {
        var css = _inputEditor.Text;
        SetBusy(true);
        try
        {
            var before = System.Text.Encoding.UTF8.GetByteCount(css);
            var beautified = await Task.Run(() => CssTool.Beautify(css, 2));
            var after = System.Text.Encoding.UTF8.GetByteCount(beautified);
            _outputEditor.Text = beautified;

            var pct = before == 0 ? 0 : Math.Round((1 - (double)after / before) * 100, 1);
            StatusText.Text = MinifierUi.FormatSavings(before, after, pct);
            MinifierUi.ShowErrors(ErrorsText, []);
        }
        catch (Exception ex)
        {
            MinifierUi.ShowError(ErrorsText, ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => Ui.Copy(_outputEditor.Text, CopyOutputBtn);

    private void SetBusy(bool busy)
    {
        MinifyBtn.IsEnabled = !busy;
        BeautifyBtn.IsEnabled = !busy;
    }
}
