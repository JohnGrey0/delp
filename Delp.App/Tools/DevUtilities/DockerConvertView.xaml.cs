using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DevUtilities;

[Tool("docker-convert", "Docker Run ↔ Compose", ToolCategory.DevUtilities,
    "Convert a docker run command to an equivalent Compose service definition, and back.",
    Keywords = "docker,compose,container,convert,composerize,yaml", Order = 45)]
public partial class DockerConvertView : UserControl
{
    private readonly TextEditor _top;
    private readonly TextEditor _bottom;
    private readonly DispatcherTimer _debounce;
    private bool _settingTop;
    private bool _settingBottom;
    private bool _topIsDockerRun = true;
    private Action? _pendingConvert;

    public DockerConvertView()
    {
        InitializeComponent();

        _top = CodeEditors.Create();
        _bottom = CodeEditors.Create();
        TopHost.Child = _top;
        BottomHost.Child = _bottom;

        _top.TextChanged += (_, _) => { if (!_settingTop) Debounce(ConvertTopToBottom); };
        _bottom.TextChanged += (_, _) => { if (!_settingBottom) Debounce(ConvertBottomToTop); };

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            _pendingConvert?.Invoke();
        };

        // Stop pending work when navigated away so a cached-but-hidden view doesn't keep
        // computing/writing to itself.
        Unloaded += (_, _) => _debounce.Stop();
    }

    // Only genuine edits reach here (see the _settingTop/_settingBottom guards above), so
    // programmatic writes of a conversion result never re-trigger themselves.
    private void Debounce(Action convert)
    {
        _pendingConvert = convert;
        _debounce.Stop();
        _debounce.Start();
    }

    private void ConvertTopToBottom()
    {
        var text = _top.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetBottom("");
            HideError();
            return;
        }
        Run(() =>
        {
            var result = _topIsDockerRun ? DockerTool.RunToCompose(text) : DockerTool.ComposeToRun(text);
            SetBottom(result);
        });
    }

    private void ConvertBottomToTop()
    {
        var text = _bottom.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetTop("");
            HideError();
            return;
        }
        Run(() =>
        {
            var result = _topIsDockerRun ? DockerTool.ComposeToRun(text) : DockerTool.RunToCompose(text);
            SetTop(result);
        });
    }

    private void Swap_Click(object sender, RoutedEventArgs e)
    {
        var topText = _top.Text;
        var bottomText = _bottom.Text;

        _topIsDockerRun = !_topIsDockerRun;
        UpdateLabels();

        // A swap just relocates whatever text was already there — no reconversion needed,
        // same as the Data Converter tool's swap button.
        SetTop(bottomText);
        SetBottom(topText);
        HideError();
    }

    private void UpdateLabels()
    {
        TopLabel.Text = _topIsDockerRun ? "DOCKER RUN" : "COMPOSE.YAML";
        BottomLabel.Text = _topIsDockerRun ? "COMPOSE.YAML" : "DOCKER RUN";
        DirectionText.Text = _topIsDockerRun ? "docker run  →  compose.yaml" : "compose.yaml  →  docker run";
    }

    private void SetTop(string text)
    {
        _settingTop = true;
        _top.Text = text;
        _settingTop = false;
    }

    private void SetBottom(string text)
    {
        _settingBottom = true;
        _bottom.Text = text;
        _settingBottom = false;
    }

    /// <summary>Runs a conversion with inline error reporting — never a MessageBox, never a throw
    /// across the UI boundary.</summary>
    private void Run(Action convert)
    {
        try
        {
            convert();
            HideError();
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void HideError() => ErrorText.Visibility = Visibility.Collapsed;

    private void CopyTop_Click(object sender, RoutedEventArgs e) => Ui.Copy(_top.Text, CopyTopBtn);

    private void CopyBottom_Click(object sender, RoutedEventArgs e) => Ui.Copy(_bottom.Text, CopyBottomBtn);
}
