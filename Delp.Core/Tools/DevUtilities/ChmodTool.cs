using System.Text;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>
/// One Unix permission set: the classic read/write/execute bits for owner, group, and other,
/// plus the three special bits (setuid, setgid, sticky).
/// </summary>
public sealed record ChmodPermissions(
    bool OwnerRead, bool OwnerWrite, bool OwnerExecute,
    bool GroupRead, bool GroupWrite, bool GroupExecute,
    bool OtherRead, bool OtherWrite, bool OtherExecute,
    bool Setuid, bool Setgid, bool Sticky);

/// <summary>
/// Pure conversions between the four ways a Unix file mode is expressed: the 3x3 permission
/// grid (as a <see cref="ChmodPermissions"/>), a 3-or-4-digit octal string ("755", "4755"), a
/// 9-character symbolic string ("rwxr-xr-x", "rwsr-xr-x"), and a read-only <c>chmod</c> command
/// line. Every parse method throws <see cref="FormatException"/> with a human-readable message
/// on bad input rather than crashing.
/// </summary>
public static class ChmodTool
{
    /// <summary>A conventional 755 (rwxr-xr-x) starting point for a fresh UI.</summary>
    public static readonly ChmodPermissions Default = new(
        OwnerRead: true, OwnerWrite: true, OwnerExecute: true,
        GroupRead: true, GroupWrite: false, GroupExecute: true,
        OtherRead: true, OtherWrite: false, OtherExecute: true,
        Setuid: false, Setgid: false, Sticky: false);

    /// <summary>Renders the octal mode: 3 digits when no special bit is set, otherwise a leading 4th digit.</summary>
    public static string ToOctalString(ChmodPermissions p)
    {
        var owner = Digit(p.OwnerRead, p.OwnerWrite, p.OwnerExecute);
        var group = Digit(p.GroupRead, p.GroupWrite, p.GroupExecute);
        var other = Digit(p.OtherRead, p.OtherWrite, p.OtherExecute);
        var special = (p.Setuid ? 4 : 0) | (p.Setgid ? 2 : 0) | (p.Sticky ? 1 : 0);

        return special == 0 ? $"{owner}{group}{other}" : $"{special}{owner}{group}{other}";
    }

    /// <summary>Renders the 9-character symbolic mode (no leading file-type character), e.g. "rwsr-xr-x".</summary>
    public static string ToSymbolic(ChmodPermissions p)
    {
        var sb = new StringBuilder(9);
        sb.Append(p.OwnerRead ? 'r' : '-');
        sb.Append(p.OwnerWrite ? 'w' : '-');
        sb.Append(ExecChar(p.OwnerExecute, p.Setuid, 's', 'S'));
        sb.Append(p.GroupRead ? 'r' : '-');
        sb.Append(p.GroupWrite ? 'w' : '-');
        sb.Append(ExecChar(p.GroupExecute, p.Setgid, 's', 'S'));
        sb.Append(p.OtherRead ? 'r' : '-');
        sb.Append(p.OtherWrite ? 'w' : '-');
        sb.Append(ExecChar(p.OtherExecute, p.Sticky, 't', 'T'));
        return sb.ToString();
    }

    /// <summary>Renders a read-only sample command line, e.g. "chmod 755 file".</summary>
    public static string ToCommand(ChmodPermissions p) => $"chmod {ToOctalString(p)} file";

    /// <summary>Parses a 3 or 4 digit octal mode ("755", "0755" as 4 digits, "4755"). Throws on invalid input.</summary>
    public static ChmodPermissions FromOctal(string? input)
    {
        var s = (input ?? string.Empty).Trim();
        if (s.Length is not (3 or 4))
            throw new FormatException($"'{input}' must be 3 or 4 octal digits, e.g. 755 or 4755.");

        foreach (var c in s)
            if (c is < '0' or > '7')
                throw new FormatException($"'{input}' contains a non-octal digit '{c}' — only 0-7 are valid.");

        var special = s.Length == 4 ? s[0] - '0' : 0;
        var owner = s[^3] - '0';
        var group = s[^2] - '0';
        var other = s[^1] - '0';

        return new ChmodPermissions(
            (owner & 4) != 0, (owner & 2) != 0, (owner & 1) != 0,
            (group & 4) != 0, (group & 2) != 0, (group & 1) != 0,
            (other & 4) != 0, (other & 2) != 0, (other & 1) != 0,
            (special & 4) != 0, (special & 2) != 0, (special & 1) != 0);
    }

    /// <summary>Parses a 9-character symbolic mode ("rwxr-xr-x", "rwsr-xr-x", "rwxr-xr-t", …). Throws on invalid input.</summary>
    public static ChmodPermissions FromSymbolic(string? input)
    {
        var s = input ?? string.Empty;
        if (s.Length != 9)
            throw new FormatException($"'{input}' must be exactly 9 characters, e.g. rwxr-xr-x (got {s.Length}).");

        var ownerRead = ReadFlag(s, 0, 'r');
        var ownerWrite = ReadFlag(s, 1, 'w');
        var (ownerExec, setuid) = ReadExecFlag(s, 2, 's', 'S');
        var groupRead = ReadFlag(s, 3, 'r');
        var groupWrite = ReadFlag(s, 4, 'w');
        var (groupExec, setgid) = ReadExecFlag(s, 5, 's', 'S');
        var otherRead = ReadFlag(s, 6, 'r');
        var otherWrite = ReadFlag(s, 7, 'w');
        var (otherExec, sticky) = ReadExecFlag(s, 8, 't', 'T');

        return new ChmodPermissions(
            ownerRead, ownerWrite, ownerExec,
            groupRead, groupWrite, groupExec,
            otherRead, otherWrite, otherExec,
            setuid, setgid, sticky);
    }

    private static int Digit(bool r, bool w, bool x) => (r ? 4 : 0) | (w ? 2 : 0) | (x ? 1 : 0);

    private static char ExecChar(bool exec, bool special, char withExec, char withoutExec) =>
        special ? (exec ? withExec : withoutExec) : (exec ? 'x' : '-');

    private static bool ReadFlag(string s, int index, char expected)
    {
        var c = s[index];
        if (c == expected) return true;
        if (c == '-') return false;
        throw new FormatException($"Invalid character '{c}' at position {index + 1}: expected '{expected}' or '-'.");
    }

    private static (bool Exec, bool Special) ReadExecFlag(string s, int index, char specialWithExec, char specialWithoutExec)
    {
        var c = s[index];
        if (c == 'x') return (true, false);
        if (c == '-') return (false, false);
        if (c == specialWithExec) return (true, true);
        if (c == specialWithoutExec) return (false, true);
        throw new FormatException(
            $"Invalid character '{c}' at position {index + 1}: expected 'x', '-', '{specialWithExec}', or '{specialWithoutExec}'.");
    }
}
