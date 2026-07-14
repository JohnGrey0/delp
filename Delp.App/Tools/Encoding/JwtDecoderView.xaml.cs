using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Encoding;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.Encoding;

[Tool("jwt-decoder", "JWT Decoder", ToolCategory.Encoding,
    "Decode a JSON Web Token's header, payload, and claims without verifying its signature.",
    Keywords = "jwt,token,bearer,oauth,claims,jose", Order = 40)]
public partial class JwtDecoderView : UserControl
{
    private bool _updating;
    private readonly TextEditor _headerEditor;
    private readonly TextEditor _payloadEditor;

    public JwtDecoderView()
    {
        InitializeComponent();

        _headerEditor = CodeEditors.Create("Json", readOnly: true);
        HeaderEditorHost.Child = _headerEditor;

        _payloadEditor = CodeEditors.Create("Json", readOnly: true);
        PayloadEditorHost.Child = _payloadEditor;
    }

    private void TokenBox_TextChanged(object sender, TextChangedEventArgs e) => Run(() =>
    {
        if (string.IsNullOrWhiteSpace(TokenBox.Text))
        {
            _headerEditor.Text = "";
            _payloadEditor.Text = "";
            ClaimsList.ItemsSource = null;
            return;
        }

        var parts = JwtTool.Decode(TokenBox.Text);
        _headerEditor.Text = parts.HeaderJson;
        _payloadEditor.Text = parts.PayloadJson;
        ClaimsList.ItemsSource = parts.Claims.Select(ToRow).ToList();
    });

    private ClaimRow ToRow(JwtClaim claim)
    {
        var expired = claim.Note?.Contains("expired", StringComparison.OrdinalIgnoreCase) == true;
        var brush = (Brush)FindResource(expired ? "Brush.Warning" : "Brush.Fg2");
        return new ClaimRow(claim.Name, claim.Value, claim.Note, brush,
            claim.Note is null ? Visibility.Collapsed : Visibility.Visible);
    }

    private void CopyHeader_Click(object sender, RoutedEventArgs e) => Ui.Copy(_headerEditor.Text, CopyHeaderBtn);

    private void CopyPayload_Click(object sender, RoutedEventArgs e) => Ui.Copy(_payloadEditor.Text, CopyPayloadBtn);

    /// <summary>Runs a conversion with reentrancy protection and inline error reporting.</summary>
    private void Run(Action convert)
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            convert();
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            _updating = false;
        }
    }

    /// <summary>View-only display row wrapping a <see cref="JwtClaim"/> with WPF-specific presentation state.</summary>
    private sealed record ClaimRow(string Name, string Value, string? Note, Brush NoteBrush, Visibility NoteVisibility);
}
