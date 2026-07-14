using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Delp.App.Infrastructure;

public static class Ui
{
    /// <summary>
    /// Copies text to the clipboard. Pass the invoking button to flash
    /// "Copied ✓" on it for a moment as feedback.
    /// </summary>
    public static void Copy(string? text, Button? feedback = null)
    {
        try
        {
            Clipboard.SetDataObject(text ?? "");
        }
        catch
        {
            // Clipboard can be transiently locked by another process; ignore.
        }

        if (feedback is null)
            return;

        var original = feedback.Content;
        var originalMinWidth = feedback.MinWidth;
        feedback.MinWidth = feedback.ActualWidth;
        feedback.Content = "Copied ✓";
        feedback.IsEnabled = false;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) };
        timer.Tick += (_, _) =>
        {
            feedback.Content = original;
            feedback.IsEnabled = true;
            feedback.MinWidth = originalMinWidth;
            timer.Stop();
        };
        timer.Start();
    }
}
