using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

/// <summary>
/// Wraps a view's inline error TextBlock with the reentrancy-free "run and report" pattern used by every
/// tool view (see Base64View.Run): run an action, hide the error banner on success, or show the thrown
/// exception's message on failure. All ten views in this batch repeated this verbatim; extracted here so
/// there is exactly one implementation to test and fix.
/// </summary>
internal sealed class ErrorBox(TextBlock errorText)
{
    public void HideError() => errorText.Visibility = Visibility.Collapsed;

    public void ShowError(Exception ex)
    {
        errorText.Text = ex.Message;
        errorText.Visibility = Visibility.Visible;
    }

    public void Run(Action action)
    {
        try
        {
            action();
            HideError();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }
}

/// <summary>Copy / Copy-as-JSON-array handlers shared by every UUID view's OUTPUT pane.</summary>
internal static class UuidOutputCopy
{
    public static void Copy(TextBox outputBox, Button button) => Ui.Copy(outputBox.Text, button);

    public static void CopyAsJson(TextBox outputBox, Button button)
    {
        var lines = outputBox.Text.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
        Ui.Copy(System.Text.Json.JsonSerializer.Serialize(lines), button);
    }
}

/// <summary>
/// Batch-generation state shared by the count-driven UUID views (v1, v2, v4, v6, v7, v8): captures every
/// produced GUID so a formatting-only option change (uppercase/braces/no-hyphens) can re-render the
/// existing batch via <see cref="Reformat"/> instead of silently generating a brand new one, and always
/// writes the whole batch into the output box with a single <c>string.Join</c> -- never an incremental
/// append loop, even at the 1000-line batch cap.
/// </summary>
internal sealed class UuidBatchController(TextBox countBox, TextBox outputBox)
{
    private readonly List<Guid> _guids = [];

    public IReadOnlyList<Guid> Guids => _guids;

    /// <exception cref="FormatException">The Count textbox is not a whole number.</exception>
    public int ParseCount()
    {
        if (!int.TryParse(countBox.Text.Trim(), out var count))
            throw new FormatException("Count must be a whole number.");
        return count;
    }

    /// <summary>Generates <paramref name="count"/> GUIDs, stashes each one, and renders the batch.</summary>
    public void GenerateAndRender(int count, Func<Guid> generator, UuidStyle style)
    {
        _guids.Clear();
        Guid Capture()
        {
            var g = generator();
            _guids.Add(g);
            return g;
        }
        var formatted = UuidBatch.Generate(Capture, count, style);
        outputBox.Text = string.Join(Environment.NewLine, formatted);
    }

    /// <summary>Re-renders the already-generated batch under new formatting options, without generating new values.</summary>
    public void Reformat(UuidStyle style)
    {
        if (_guids.Count > 0)
            outputBox.Text = string.Join(Environment.NewLine, _guids.Select(g => UuidFormat.Apply(g, style)));
    }

    /// <summary>Drops the stashed batch and blanks the output box -- used when a selector switch (e.g. UUID version) makes the previous batch's contents stale.</summary>
    public void Clear()
    {
        _guids.Clear();
        outputBox.Text = "";
    }
}
