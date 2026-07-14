using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.WebDev;

[Tool("markdown-preview", "Markdown Live Preview", ToolCategory.WebDev,
    "Render Markdown to HTML with a live, sandboxed rich preview.",
    Keywords = "markdown,md,preview,render", Order = 50)]
public partial class MarkdownPreviewView : UserControl
{
    private readonly TextEditor _editor;
    private readonly DispatcherTimer _debounce;
    private bool _webViewReady;
    private string? _lastRenderedHtml;

    private const string Sample = "# Hello, Delp\n\nType *Markdown* on the left to see a live **preview** here.\n\n- [x] Live rendering\n- [ ] Feed the cat\n\n| Feature | Status |\n| --- | --- |\n| Tables | Yes |\n";

    public MarkdownPreviewView()
    {
        InitializeComponent();

        _editor = CodeEditors.Create();
        EditorHost.Child = _editor;
        _editor.TextChanged += (_, _) => DebounceRender();
        _editor.Text = Sample;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Render();
        };

        Loaded += MarkdownPreviewView_Loaded;
        Unloaded += (_, _) => _debounce.Stop();
    }

    private async void MarkdownPreviewView_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await Preview.EnsureCoreWebView2Async();
            Preview.CoreWebView2.Settings.IsScriptEnabled = false;
            Preview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Preview.CoreWebView2.Settings.AreDevToolsEnabled = false;
            Preview.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            Preview.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            _webViewReady = true;
            Render();
        }
        catch
        {
            // The WebView2 Evergreen runtime isn't installed (or failed to initialize) —
            // never crash the tool, just fall back to a plain message in its place.
            Preview.Visibility = Visibility.Collapsed;
            PreviewFallback.Text =
                "WebView2 runtime unavailable — install the Microsoft Edge WebView2 runtime to see a live preview.";
            PreviewFallback.Visibility = Visibility.Visible;
        }
    }

    private void DebounceRender()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Render()
    {
        if (!_webViewReady)
            return;
        try
        {
            var html = MarkdownTool.WrapDocument(MarkdownTool.ToHtml(_editor.Text));
            // Skip the (relatively costly) WebView2 navigation entirely when the rendered
            // document hasn't actually changed since the last render.
            if (html != _lastRenderedHtml)
            {
                Preview.CoreWebView2.NavigateToString(html);
                _lastRenderedHtml = html;
            }
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void CopyHtml_Click(object sender, RoutedEventArgs e)
    {
        string html;
        try
        {
            html = MarkdownTool.ToHtml(_editor.Text);
        }
        catch
        {
            html = "";
        }
        Ui.Copy(html, CopyHtmlBtn);
    }
}
