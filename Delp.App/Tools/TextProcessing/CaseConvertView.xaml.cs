using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.TextProcessing;

namespace Delp.App.Tools.TextProcessing;

[Tool("case-convert", "Case Converter", ToolCategory.TextProcessing,
    "Convert identifiers and phrases between camelCase, snake_case, kebab-case, and other common case styles.",
    Keywords = "case,camel,snake,kebab,pascal,title", Order = 10)]
public partial class CaseConvertView : UserControl
{
    private bool _updating;

    public CaseConvertView()
    {
        InitializeComponent();
        Run(() => ResultsList.ItemsSource = CaseTool.ConvertAll(InputBox.Text));
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Run(() => ResultsList.ItemsSource = CaseTool.ConvertAll(InputBox.Text));

    private void CopyRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value } button)
            Ui.Copy(value, button);
    }

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
}
