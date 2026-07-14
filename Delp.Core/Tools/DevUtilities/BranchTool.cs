using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>
/// Options for <see cref="BranchTool.Make"/>. <paramref name="Template"/> supports the placeholders
/// <c>{type}</c>, <c>{ticket}</c>, and <c>{slug}</c>.
/// </summary>
public sealed record BranchOptions(string Type, string? Ticket, string Template, int MaxLength)
{
    public const string DefaultTemplate = "{type}/{ticket}-{slug}";
    public const int DefaultMaxLength = 60;
}

/// <summary>Generates git-safe branch names from a description, and validates existing names against git's ref rules.</summary>
public static class BranchTool
{
    public static readonly IReadOnlyList<string> KnownTypes = new[] { "feature", "bugfix", "hotfix", "chore", "release", "custom" };

    private static readonly char[] BadRefChars = { '~', '^', ':', '?', '*', '[', '\\' };

    private static readonly Regex TicketPattern =
        new(@"^[A-Za-z]+-\d+$", RegexOptions.None, TimeSpan.FromSeconds(2));

    /// <summary>Builds a branch name from <paramref name="description"/> and <paramref name="options"/>.</summary>
    /// <exception cref="FormatException">Both description and ticket are empty, or the result is empty after sanitizing.</exception>
    public static string Make(string description, BranchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(options.Ticket))
            throw new FormatException("Enter a description or a ticket number.");
        if (options.MaxLength < 1)
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxLength, "MaxLength must be positive.");

        var type = string.IsNullOrWhiteSpace(options.Type) ? "feature" : options.Type.Trim();
        var ticket = NormalizeTicket(options.Ticket);
        var slug = Slugify(description ?? "");

        var template = string.IsNullOrWhiteSpace(options.Template) ? BranchOptions.DefaultTemplate : options.Template;
        var name = template
            .Replace("{type}", type, StringComparison.OrdinalIgnoreCase)
            .Replace("{ticket}", ticket ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{slug}", slug, StringComparison.OrdinalIgnoreCase);

        name = CollapseSegments(name);
        name = TrimToMaxLength(name, options.MaxLength);
        name = SanitizeGitRef(name);

        if (name.Length == 0)
            throw new FormatException("The generated branch name is empty; provide a description or ticket.");

        return name;
    }

    /// <summary>Convenience string for the "copy as checkout command" UI action.</summary>
    public static string CheckoutCommand(string branchName) => $"git checkout -b {branchName}";

    /// <summary>Checks <paramref name="branchName"/> against git's ref-name rules; returns one message per violation (empty = valid).</summary>
    public static IReadOnlyList<string> Validate(string branchName)
    {
        var violations = new List<string>();
        if (string.IsNullOrEmpty(branchName))
        {
            violations.Add("Branch name must not be empty.");
            return violations;
        }

        if (branchName.Contains(".."))
            violations.Add("Must not contain '..'.");
        if (branchName.StartsWith('/') || branchName.EndsWith('/'))
            violations.Add("Must not start or end with '/'.");
        if (branchName.StartsWith('.') || branchName.EndsWith('.'))
            violations.Add("Must not start or end with '.'.");
        if (branchName.StartsWith('-') || branchName.EndsWith('-'))
            violations.Add("Must not start or end with '-'.");
        if (branchName.Any(char.IsWhiteSpace))
            violations.Add("Must not contain whitespace.");
        foreach (var bad in BadRefChars)
        {
            if (branchName.Contains(bad))
                violations.Add($"Must not contain '{bad}'.");
        }
        if (branchName.Contains("@{"))
            violations.Add("Must not contain '@{'.");
        if (branchName.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
            violations.Add("Must not end with '.lock'.");
        if (branchName.Contains("//"))
            violations.Add("Must not contain consecutive slashes '//'.");
        if (branchName == "@")
            violations.Add("Must not be the single character '@'.");

        return violations;
    }

    private static string? NormalizeTicket(string? ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            return null;
        var trimmed = ticket.Trim();
        return TicketPattern.IsMatch(trimmed) ? trimmed.ToUpperInvariant() : trimmed;
    }

    /// <summary>Diacritic-stripping, lowercasing slugifier shared by branch names (self-contained: git-branch does not depend on url-slug).</summary>
    private static string Slugify(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var stripped = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            stripped.Append(c);
        }
        var ascii = stripped.ToString().Normalize(NormalizationForm.FormC);

        var result = new StringBuilder(ascii.Length);
        foreach (var c in ascii)
            result.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');

        return CollapseRuns(result.ToString(), '-').Trim('-');
    }

    /// <summary>Collapses runs of the same separator per '/'-delimited segment and trims stray leading/trailing separators.</summary>
    private static string CollapseSegments(string value)
    {
        var segments = value.Split('/')
            .Select(s => CollapseRuns(s, '-').Trim('-', '.'))
            .Where(s => s.Length > 0);
        return string.Join('/', segments);
    }

    private static string CollapseRuns(string value, char separator)
    {
        var sb = new StringBuilder(value.Length);
        var lastWasSeparator = false;
        foreach (var c in value)
        {
            var isSeparator = c == separator;
            if (isSeparator && lastWasSeparator)
                continue;
            sb.Append(c);
            lastWasSeparator = isSeparator;
        }
        return sb.ToString();
    }

    private static string TrimToMaxLength(string name, int maxLength)
    {
        if (name.Length <= maxLength)
            return name;

        var truncated = name[..maxLength];
        var lastSeparator = truncated.LastIndexOfAny(new[] { '-', '/' });
        if (lastSeparator > 0)
            truncated = truncated[..lastSeparator];
        return truncated.TrimEnd('-', '/', '.');
    }

    private static string SanitizeGitRef(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (Array.IndexOf(BadRefChars, c) >= 0 || char.IsWhiteSpace(c) || c < 0x20)
                continue;
            sb.Append(c);
        }

        var result = sb.ToString();
        while (result.Contains(".."))
            result = result.Replace("..", ".");
        while (result.Contains("//"))
            result = result.Replace("//", "/");
        while (result.Contains("@{"))
            result = result.Replace("@{", "@");

        result = result.Trim('/', '.', '-');
        if (result.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
            result = result[..^5];
        return result;
    }
}
