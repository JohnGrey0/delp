using System.Windows;

namespace Delp.App.Tools.Hashing;

/// <summary>Shared single-file drag-and-drop handling for the file-based hashing tools.</summary>
internal static class FileDropSupport
{
    /// <summary>Accepts a drag-over only when the payload is a file drop.</summary>
    public static void PreviewDragOver(DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    /// <summary>Returns the first dropped file's path, or null if the drop wasn't a file.</summary>
    public static string? GetDroppedFile(DragEventArgs e) =>
        e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files ? files[0] : null;
}
