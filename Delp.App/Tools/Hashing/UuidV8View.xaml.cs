using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("uuid-v8", "UUID v8 (Custom)", ToolCategory.Hashing,
    "Generate RFC 9562 version 8 UUIDs with your own payload in the 122 free bits.",
    Keywords = "uuid,guid,v8,custom,vendor,rfc9562", Order = 180)]
public partial class UuidV8View : UserControl
{
    private readonly UuidBatchController _batch;
    private readonly ErrorBox _error;

    public UuidV8View()
    {
        InitializeComponent();
        _batch = new UuidBatchController(CountBox, OutputBox);
        _error = new ErrorBox(ErrorText);
    }

    private UuidStyle FormatStyle => new(
        Uppercase: UppercaseBox.IsChecked == true,
        Braces: BracesBox.IsChecked == true,
        NoHyphens: NoHyphensBox.IsChecked == true);

    private void Generate_Click(object sender, RoutedEventArgs e) => _error.Run(() =>
    {
        var count = _batch.ParseCount();
        var customHex = CustomHexBox.Text;
        var randomFill = RandomFillBox.IsChecked == true;
        _batch.GenerateAndRender(count, () => UuidV8.Generate(customHex, randomFill), FormatStyle);
    });

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            _batch.Reformat(FormatStyle);
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.Copy(OutputBox, CopyBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.CopyAsJson(OutputBox, CopyJsonBtn);
}
