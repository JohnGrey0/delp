using System.Text.RegularExpressions;

namespace Delp.Core.Tools.TextProcessing;

/// <summary>One capture group inside a match (named or numbered).</summary>
public sealed record GroupInfo(string Name, string Value, bool Success);

/// <summary>One overall regex match.</summary>
public sealed record MatchInfo(int Index, int Length, string Value, IReadOnlyList<GroupInfo> Groups);

/// <summary>Result of running a pattern against input text.</summary>
public sealed record RegexRunResult(IReadOnlyList<MatchInfo> Matches, string? Error);

/// <summary>Result of a find/replace run.</summary>
public sealed record RegexReplaceResult(string? Result, string? Error);

/// <summary>Flag toggles for the regex tester (.NET flavor).</summary>
public sealed record RegexToolOptions(
    bool IgnoreCase = false,
    bool Multiline = false,
    bool Singleline = false,
    bool IgnoreWhitespace = false);

/// <summary>
/// Pure wrapper around <see cref="Regex"/> that never lets a bad pattern or a
/// catastrophically backtracking one escape as an exception: both surface as
/// <c>Error</c> strings instead. Every regex built here has a 2 second match
/// timeout.
/// </summary>
public static class RegexTool
{
    /// <summary>Every <see cref="Regex"/> constructed by this tool gets this timeout.</summary>
    public static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(2);

    private static RegexOptions BuildOptions(RegexToolOptions options)
    {
        var opts = RegexOptions.None;
        if (options.IgnoreCase) opts |= RegexOptions.IgnoreCase;
        if (options.Multiline) opts |= RegexOptions.Multiline;
        if (options.Singleline) opts |= RegexOptions.Singleline;
        if (options.IgnoreWhitespace) opts |= RegexOptions.IgnorePatternWhitespace;
        return opts;
    }

    private static bool TryBuildRegex(string pattern, RegexToolOptions options, out Regex? regex, out string? error)
    {
        try
        {
            regex = new Regex(pattern, BuildOptions(options), MatchTimeout);
            error = null;
            return true;
        }
        catch (ArgumentException ex)
        {
            regex = null;
            error = $"Invalid pattern: {ex.Message}";
            return false;
        }
    }

    public static RegexRunResult Run(string pattern, string input, RegexToolOptions options)
    {
        if (!TryBuildRegex(pattern, options, out var regex, out var buildError))
            return new RegexRunResult(Array.Empty<MatchInfo>(), buildError);

        try
        {
            var matches = new List<MatchInfo>();
            foreach (Match m in regex!.Matches(input))
            {
                var groups = new List<GroupInfo>();
                foreach (var name in regex.GetGroupNames())
                {
                    if (name == "0")
                        continue; // group 0 is the whole match, already MatchInfo.Value
                    var g = m.Groups[name];
                    groups.Add(new GroupInfo(name, g.Success ? g.Value : "", g.Success));
                }
                matches.Add(new MatchInfo(m.Index, m.Length, m.Value, groups));
            }
            return new RegexRunResult(matches, null);
        }
        catch (RegexMatchTimeoutException)
        {
            return new RegexRunResult(Array.Empty<MatchInfo>(),
                "Regex evaluation timed out after 2s (possible catastrophic backtracking).");
        }
    }

    public static RegexReplaceResult Replace(string pattern, string input, string replacement, RegexToolOptions options)
    {
        if (!TryBuildRegex(pattern, options, out var regex, out var buildError))
            return new RegexReplaceResult(null, buildError);

        try
        {
            return new RegexReplaceResult(regex!.Replace(input, replacement ?? ""), null);
        }
        catch (RegexMatchTimeoutException)
        {
            return new RegexReplaceResult(null,
                "Regex evaluation timed out after 2s (possible catastrophic backtracking).");
        }
    }
}
