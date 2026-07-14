using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Delp.Core.Tools.WebDev;

namespace Delp.App.Tools.WebDev;

/// <summary>
/// Shared result/status plumbing for the CSS/JS/HTML minifier views (all backed by
/// <see cref="MinifyResult"/>), so byte formatting, savings text, and error display stay
/// in one place instead of being copy-pasted per view.
/// </summary>
internal static class MinifierUi
{
    public static string FormatBytes(int bytes) =>
        bytes >= 1024 ? $"{(bytes / 1024.0).ToString("0.0", CultureInfo.InvariantCulture)} KB" : $"{bytes} B";

    /// <summary>Formats a "before → after (-x.x%)" status line from a byte count pair and a
    /// pre-computed savings percentage (pass <see cref="MinifyResult.SavingsPercent"/> so the
    /// "empty output after an error" case is handled consistently with Core).</summary>
    public static string FormatSavings(int beforeBytes, int afterBytes, double savingsPercent)
    {
        var sign = savingsPercent >= 0 ? "-" : "+";
        var pct = Math.Abs(savingsPercent).ToString("0.0", CultureInfo.InvariantCulture);
        return $"{FormatBytes(beforeBytes)} → {FormatBytes(afterBytes)} ({sign}{pct}%)";
    }

    public static void ShowResult(TextBlock statusText, TextBlock errorsText, MinifyResult result)
    {
        statusText.Text = FormatSavings(result.BeforeBytes, result.AfterBytes, result.SavingsPercent);
        ShowErrors(errorsText, result.Errors);
    }

    public static void ShowErrors(TextBlock errorsText, IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            errorsText.Visibility = Visibility.Collapsed;
            return;
        }

        errorsText.Text = string.Join("\n", errors);
        errorsText.Visibility = Visibility.Visible;
    }

    public static void ShowError(TextBlock errorsText, string message)
    {
        errorsText.Text = message;
        errorsText.Visibility = Visibility.Visible;
    }
}
