using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("cron-parse", "Cron Expression Parser", ToolCategory.DevUtilities,
    "Explain a cron expression in plain English and preview its next scheduled runs.",
    Keywords = "cron,crontab,schedule,quartz", Order = 20)]
public partial class CronParseView : UserControl
{
    // Explain() re-parses with Cronos and re-runs CronExpressionDescriptor; too expensive per keystroke, so debounce.
    private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(300) };

    public CronParseView()
    {
        InitializeComponent();
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Explain();
        };
        Loaded += (_, _) => Explain();
    }

    private void Input_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            _debounce.Stop();
            _debounce.Start();
        }
    }

    private void Explain()
    {
        try
        {
            var report = CronTool.Explain(ExpressionBox.Text);
            HumanText.Text = report.Human;
            FieldsList.ItemsSource = report.Fields;

            var now = DateTime.Now;
            NextRunsList.ItemsSource = report.NextLocal
                .Select(dt => new NextRunRow(
                    dt.ToString("ddd, MMM d yyyy  HH:mm:ss", CultureInfo.CurrentCulture),
                    Relative(dt, now)))
                .ToList();

            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            HumanText.Text = "";
            FieldsList.ItemsSource = null;
            NextRunsList.ItemsSource = null;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private static string Relative(DateTime target, DateTime now)
    {
        var delta = target - now;
        if (delta.TotalSeconds < 0)
            return "just ran";
        if (delta.TotalMinutes < 1)
            return "in <1 min";
        if (delta.TotalHours < 1)
            return $"in {(int)delta.TotalMinutes} min";
        if (delta.TotalDays < 1)
            return $"in {(int)delta.TotalHours} h";
        return $"in {(int)delta.TotalDays} d";
    }

    private sealed record NextRunRow(string When, string Relative);
}
