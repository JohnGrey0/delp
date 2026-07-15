using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.WebDev;

[Tool("curl-convert", "cURL ↔ Code Converter", ToolCategory.WebDev,
    "Parse a pasted curl command into its method, headers and body, and generate equivalent " +
    "HttpClient, requests, fetch, Invoke-RestMethod or Go code — or emit a canonical curl line back.",
    Keywords = "curl,http,request,convert,code,python,fetch,powershell,httpclient", Order = 110)]
public partial class CurlConvertView : UserControl
{
    private readonly TextEditor _output;
    private readonly DispatcherTimer _debounce;
    private CurlRequest? _lastRequest;

    public CurlConvertView()
    {
        InitializeComponent();

        _output = CodeEditors.Create(null, readOnly: true);
        OutputHost.Child = _output;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Convert();
        };

        Unloaded += (_, _) => _debounce.Stop();
    }

    private CurlTarget SelectedTarget => (CurlTarget)TargetCombo.SelectedIndex;

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Target_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) RenderOutput();
    }

    private void Convert()
    {
        if (string.IsNullOrWhiteSpace(InputBox.Text))
        {
            _lastRequest = null;
            BreakdownList.ItemsSource = null;
            _output.Text = "";
            HideError();
            return;
        }

        try
        {
            _lastRequest = CurlTool.Parse(InputBox.Text);
            UpdateBreakdown(_lastRequest);
            RenderOutput();
            UpdateNotice(_lastRequest.Warnings);
        }
        catch (Exception ex)
        {
            _lastRequest = null;
            BreakdownList.ItemsSource = null;
            _output.Text = "";
            ShowError(ex.Message);
        }
    }

    private void RenderOutput() =>
        _output.Text = _lastRequest is null ? "" : CurlTool.Generate(_lastRequest, SelectedTarget);

    private void UpdateBreakdown(CurlRequest r)
    {
        BreakdownList.ItemsSource = new List<string>
        {
            r.Method,
            string.IsNullOrEmpty(r.Url) ? "(no URL)" : r.Url,
            $"{r.Headers.Count} header{(r.Headers.Count == 1 ? "" : "s")}",
            BodyKindLabel(r.BodyKind),
        };
    }

    private static string BodyKindLabel(CurlBodyKind kind) => kind switch
    {
        CurlBodyKind.None => "no body",
        CurlBodyKind.Raw => "raw body",
        CurlBodyKind.UrlEncodedForm => "form body",
        CurlBodyKind.Multipart => "multipart body",
        CurlBodyKind.Json => "JSON body",
        _ => kind.ToString(),
    };

    private void UpdateNotice(IReadOnlyList<string> warnings)
    {
        if (warnings.Count == 0)
        {
            HideError();
            return;
        }
        ErrorText.Text = "Note: " + string.Join(" ", warnings);
        ErrorText.Foreground = (Brush)FindResource("Brush.Warning");
        ErrorText.Visibility = Visibility.Visible;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.ClearValue(TextBlock.ForegroundProperty);
        ErrorText.Visibility = Visibility.Visible;
    }

    private void HideError() => ErrorText.Visibility = Visibility.Collapsed;

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => Ui.Copy(_output.Text, CopyOutputBtn);
}
