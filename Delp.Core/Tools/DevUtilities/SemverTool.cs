using System.Globalization;
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
        var ic = CultureInfo.InvariantCulture;

        if (a.Major != b.Major)
            return $"Differ at major: {a.Major.ToString(ic)} {op} {b.Major.ToString(ic)}.";
        if (a.Minor != b.Minor)
            return $"Differ at minor: {a.Minor.ToString(ic)} {op} {b.Minor.ToString(ic)}.";
        if (a.Patch != b.Patch)
            return $"Differ at patch: {a.Patch.ToString(ic)} {op} {b.Patch.ToString(ic)}.";
        if (a.IsPrerelease != b.IsPrerelease)
            return $"{a} {op} {b} (a pre-release version precedes its release).";

        return $"Differ in pre-release precedence: \"{a.Prerelease}\" {op} \"{b.Prerelease}\".";
    }

    /// <summary>
    /// Checks whether <paramref name="version"/> satisfies <paramref name="range"/>. Supported grammar: one or
    /// more space-separated clauses (all must match, i.e. AND), each of the form <c>^x.y.z</c>, <c>~x.y.z</c>,
    /// <c>&gt;=x.y.z</c>, <c>&gt;x.y.z</c>, <c>&lt;=x.y.z</c>, <c>&lt;x.y.z</c>, <c>=x.y.z</c>, or a bare
    /// <c>x.y.z</c> (exact match). Matches npm's pre-release exclusion rule: a pre-release version only satisfies
    /// the range if at least one clause's operand shares its major.minor.patch and is itself a pre-release
    /// (so <c>&gt;=1.0.0</c> does NOT match <c>1.0.1-alpha</c>, even though 1.0.1-alpha outranks 1.0.0 by raw
    /// SemVer 2.0 precedence).
    /// </summary>
    /// <exception cref="FormatException">Version or range operand fails to parse, or the range is empty.</exception>
    public static RangeCheckResult Satisfies(string version, string range)
    {
        var v = ParseVersion(version);
        if (string.IsNullOrWhiteSpace(range))
            throw new FormatException("Enter a range expression.");

        var clauses = range.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(ParseClause).ToList();
        if (clauses.Count == 0)
            throw new FormatException("Enter a range expression.");

        if (v.IsPrerelease && !clauses.Any(c => c.Target.IsPrerelease
                && c.Target.Major == v.Major && c.Target.Minor == v.Minor && c.Target.Patch == v.Patch))
        {
            var ic = CultureInfo.InvariantCulture;
            return new RangeCheckResult(false,
                $"{v} is a pre-release; it can only satisfy a range that has a bound on "
                + $"{v.Major.ToString(ic)}.{v.Minor.ToString(ic)}.{v.Patch.ToString(ic)} "
                + "which is itself a pre-release (npm semantics).");
        }

        foreach (var clause in clauses)
        {
            if (!ClauseSatisfied(v, clause, out var note))
                return new RangeCheckResult(false, note);
        }
        return new RangeCheckResult(true, "Satisfies all clauses.");
    }

    /// <summary>One parsed range clause: its operator and operand version.</summary>
    private readonly record struct RangeClause(string Op, SemVersion Target);

    private static RangeClause ParseClause(string clause)
    {
        if (clause.StartsWith(">=", StringComparison.Ordinal))
            return new RangeClause(">=", ParseVersion(clause[2..]));
        if (clause.StartsWith("<=", StringComparison.Ordinal))
            return new RangeClause("<=", ParseVersion(clause[2..]));
        if (clause.StartsWith('>'))
            return new RangeClause(">", ParseVersion(clause[1..]));
        if (clause.StartsWith('<'))
            return new RangeClause("<", ParseVersion(clause[1..]));
        if (clause.StartsWith('='))
            return new RangeClause("=", ParseVersion(clause[1..]));
        if (clause.StartsWith('^'))
            return new RangeClause("^", ParseVersion(clause[1..]));
        if (clause.StartsWith('~'))
            return new RangeClause("~", ParseVersion(clause[1..]));

        // Bare version: exact precedence match.
        return new RangeClause("=", ParseVersion(clause));
    }

    private static bool ClauseSatisfied(SemVersion v, RangeClause clause, out string note) => clause.Op switch
    {
        ">=" => CompareClause(v, clause.Target, o => o >= 0, ">=", out note),
        "<=" => CompareClause(v, clause.Target, o => o <= 0, "<=", out note),
        ">" => CompareClause(v, clause.Target, o => o > 0, ">", out note),
        "<" => CompareClause(v, clause.Target, o => o < 0, "<", out note),
        "=" => CompareClause(v, clause.Target, o => o == 0, "=", out note),
        "^" => CaretClause(v, clause.Target, out note),
        "~" => TildeClause(v, clause.Target, out note),
        _ => throw new InvalidOperationException($"Unreachable range operator '{clause.Op}'."),
    };

    private static bool CaretClause(SemVersion v, SemVersion target, out string note)
    {
        var ok = SatisfiesCaret(v, target);
        note = ok ? $"{v} is compatible with ^{target}" : $"{v} is not compatible with ^{target}";
        return ok;
    }

    private static bool TildeClause(SemVersion v, SemVersion target, out string note)
    {
        var ok = SatisfiesTilde(v, target);
        note = ok ? $"{v} is compatible with ~{target}" : $"{v} is not compatible with ~{target}";
        return ok;
    }

    private static bool CompareClause(SemVersion v, SemVersion target, Func<int, bool> predicate, string opLabel, out string note)
    {
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
