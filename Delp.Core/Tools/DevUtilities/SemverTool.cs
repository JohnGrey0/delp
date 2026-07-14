using System.Numerics;
using Semver;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>Field-by-field breakdown of a parsed SemVer 2.0 version.</summary>
public sealed record SemverBreakdown(
    BigInteger Major,
    BigInteger Minor,
    BigInteger Patch,
    string? Prerelease,
    string? Metadata,
    bool IsPrerelease);

/// <summary>Result of comparing two versions: -1/0/1 plus a plain-English explanation of why.</summary>
public sealed record CompareResult(int Order, string Explanation);

/// <summary>Result of a range-satisfaction check.</summary>
public sealed record RangeCheckResult(bool Satisfies, string Note);

/// <summary>
/// SemVer 2.0 parsing, precedence comparison, and a hand-rolled range checker for the common operator subset:
/// exact match, <c>^</c> (caret, npm-compatible semantics), <c>~</c> (tilde), <c>&gt;= &gt; &lt;= &lt; =</c>,
/// and space-joined AND. Comparisons use the Semver package's <see cref="SemVersion"/> for parsing and
/// SemVer-2.0-correct precedence, but the range grammar itself is implemented here rather than via the
/// package's built-in (npm-flavored) range parser, so the supported subset stays exactly what's documented in the UI.
/// </summary>
public static class SemverTool
{
    public static SemverBreakdown Parse(string version)
    {
        var v = ParseVersion(version);
        return new SemverBreakdown(
            v.Major,
            v.Minor,
            v.Patch,
            v.IsPrerelease ? v.Prerelease : null,
            string.IsNullOrEmpty(v.Metadata) ? null : v.Metadata,
            v.IsPrerelease);
    }

    /// <summary>Compares two versions by SemVer 2.0 precedence (build metadata ignored) with an explanation of the first differing part.</summary>
    public static CompareResult Compare(string a, string b)
    {
        var va = ParseVersion(a);
        var vb = ParseVersion(b);
        var order = Math.Sign(va.ComparePrecedenceTo(vb));
        return new CompareResult(order, Explain(va, vb, order));
    }

    private static string Explain(SemVersion a, SemVersion b, int order)
    {
        if (order == 0)
        {
            return a.Equals(b)
                ? "Equal versions."
                : "Equal precedence (they differ only in build metadata, which is ignored when comparing).";
        }

        var op = order < 0 ? "<" : ">";

        if (a.Major != b.Major)
            return $"Differ at major: {a.Major} {op} {b.Major}.";
        if (a.Minor != b.Minor)
            return $"Differ at minor: {a.Minor} {op} {b.Minor}.";
        if (a.Patch != b.Patch)
            return $"Differ at patch: {a.Patch} {op} {b.Patch}.";
        if (a.IsPrerelease != b.IsPrerelease)
            return $"{a} {op} {b} (a pre-release version precedes its release).";

        return $"Differ in pre-release precedence: \"{a.Prerelease}\" {op} \"{b.Prerelease}\".";
    }

    /// <summary>
    /// Checks whether <paramref name="version"/> satisfies <paramref name="range"/>. Supported grammar: one or
    /// more space-separated clauses (all must match, i.e. AND), each of the form <c>^x.y.z</c>, <c>~x.y.z</c>,
    /// <c>&gt;=x.y.z</c>, <c>&gt;x.y.z</c>, <c>&lt;=x.y.z</c>, <c>&lt;x.y.z</c>, <c>=x.y.z</c>, or a bare
    /// <c>x.y.z</c> (exact match).
    /// </summary>
    /// <exception cref="FormatException">Version or range operand fails to parse, or the range is empty.</exception>
    public static RangeCheckResult Satisfies(string version, string range)
    {
        var v = ParseVersion(version);
        if (string.IsNullOrWhiteSpace(range))
            throw new FormatException("Enter a range expression.");

        foreach (var clause in range.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!ClauseSatisfied(v, clause, out var note))
                return new RangeCheckResult(false, note);
        }
        return new RangeCheckResult(true, "Satisfies all clauses.");
    }

    private static bool ClauseSatisfied(SemVersion v, string clause, out string note)
    {
        if (clause.StartsWith(">=", StringComparison.Ordinal))
            return CompareClause(v, clause[2..], o => o >= 0, ">=", out note);
        if (clause.StartsWith("<=", StringComparison.Ordinal))
            return CompareClause(v, clause[2..], o => o <= 0, "<=", out note);
        if (clause.StartsWith('>'))
            return CompareClause(v, clause[1..], o => o > 0, ">", out note);
        if (clause.StartsWith('<'))
            return CompareClause(v, clause[1..], o => o < 0, "<", out note);
        if (clause.StartsWith('='))
            return CompareClause(v, clause[1..], o => o == 0, "=", out note);

        if (clause.StartsWith('^'))
        {
            var target = ParseVersion(clause[1..]);
            var ok = SatisfiesCaret(v, target);
            note = ok ? $"{v} is compatible with ^{target}" : $"{v} is not compatible with ^{target}";
            return ok;
        }
        if (clause.StartsWith('~'))
        {
            var target = ParseVersion(clause[1..]);
            var ok = SatisfiesTilde(v, target);
            note = ok ? $"{v} is compatible with ~{target}" : $"{v} is not compatible with ~{target}";
            return ok;
        }

        // Bare version: exact precedence match.
        return CompareClause(v, clause, o => o == 0, "=", out note);
    }

    private static bool CompareClause(SemVersion v, string operand, Func<int, bool> predicate, string opLabel, out string note)
    {
        var target = ParseVersion(operand);
        var order = v.ComparePrecedenceTo(target);
        var ok = predicate(order);
        note = ok ? $"{v} {opLabel} {target}" : $"{v} is not {opLabel} {target}";
        return ok;
    }

    /// <summary>^x.y.z — compatible within the same leftmost non-zero component (npm ^ semantics).</summary>
    private static bool SatisfiesCaret(SemVersion v, SemVersion target)
    {
        if (v.ComparePrecedenceTo(target) < 0)
            return false;

        if (target.Major > 0)
            return v.Major == target.Major;
        if (target.Minor > 0)
            return v.Major == 0 && v.Minor == target.Minor;
        return v.Major == 0 && v.Minor == 0 && v.Patch == target.Patch;
    }

    /// <summary>~x.y.z — patch-level changes only (minor stays fixed).</summary>
    private static bool SatisfiesTilde(SemVersion v, SemVersion target)
    {
        if (v.ComparePrecedenceTo(target) < 0)
            return false;
        return v.Major == target.Major && v.Minor == target.Minor;
    }

    private static SemVersion ParseVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new FormatException("Enter a version string.");
        try
        {
            return SemVersion.Parse(version.Trim());
        }
        catch (FormatException ex)
        {
            throw new FormatException($"'{version.Trim()}' is not a valid SemVer 2.0 version: {ex.Message}", ex);
        }
    }
}
