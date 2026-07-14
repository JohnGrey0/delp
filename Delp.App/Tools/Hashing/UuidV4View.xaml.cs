using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("uuid-v4", "UUID v4 (Random)", ToolCategory.Hashing,
    "Generate cryptographically random version 4 UUIDs in bulk.",
    Keywords = "uuid,guid,v4,random,batch", Order = 140)]
public partial class UuidV4View : UserControl
{
    private readonly UuidBatchController _batch;
    private readonly ErrorBox _error;

    public UuidV4View()
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
        _batch.GenerateAndRender(count, UuidV4.Generate, FormatStyle);
    });

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            _batch.Reformat(FormatStyle);
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.Copy(OutputBox, CopyBtn);

    private void CopyJson_Click(object sender, RoutedEventArgs e) => UuidOutputCopy.CopyAsJson(OutputBox, CopyJsonBtn);
}
