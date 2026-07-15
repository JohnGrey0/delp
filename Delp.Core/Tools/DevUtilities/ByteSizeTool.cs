using System.Globalization;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>A unit a data size can be expressed in — decimal (SI), binary (IEC), or bit-based.</summary>
public enum ByteUnit
{
    B, KB, MB, GB, TB,
    KiB, MiB, GiB, TiB,
    Bit, Kbit, Mbit, Gbit,
}

/// <summary>One equivalent size in a specific unit.</summary>
public sealed record ByteEquivalent(ByteUnit Unit, string Label, decimal Value);

/// <summary>How long a size would take to transfer at one reference bandwidth.</summary>
public sealed record TransferTime(string RateLabel, string Duration);

/// <summary>The full conversion of one size: every unit equivalent plus transfer times at common bandwidths.</summary>
public sealed record ByteSizeResult(IReadOnlyList<ByteEquivalent> Equivalents, IReadOnlyList<TransferTime> TransferTimes);

/// <summary>
/// Pure conversions between byte/bit size units (SI decimal, IEC binary, and bit-based) plus
/// derived transfer-time estimates at a handful of common bandwidths. All arithmetic uses
/// <see cref="decimal"/> so SI/IEC multipliers stay exact; formatting is left to the caller
/// (view layer applies <see cref="CultureInfo.InvariantCulture"/> and thousands separators).
/// </summary>
public static class ByteSizeTool
{
    /// <summary>Units in display order, paired with their conventional label.</summary>
    public static readonly IReadOnlyList<(ByteUnit Unit, string Label)> UnitList = new (ByteUnit, string)[]
    {
        (ByteUnit.B, "B"),
        (ByteUnit.KB, "KB"),
        (ByteUnit.MB, "MB"),
        (ByteUnit.GB, "GB"),
        (ByteUnit.TB, "TB"),
        (ByteUnit.KiB, "KiB"),
        (ByteUnit.MiB, "MiB"),
        (ByteUnit.GiB, "GiB"),
        (ByteUnit.TiB, "TiB"),
        (ByteUnit.Bit, "bit"),
        (ByteUnit.Kbit, "Kbit"),
        (ByteUnit.Mbit, "Mbit"),
        (ByteUnit.Gbit, "Gbit"),
    };

    // Every unit's size expressed in bits, so every conversion is a single division regardless
    // of whether the source/target unit is byte-based or bit-based.
    private static readonly IReadOnlyDictionary<ByteUnit, decimal> BitsPerUnit = new Dictionary<ByteUnit, decimal>
    {
        [ByteUnit.B] = 8m,
        [ByteUnit.KB] = 8_000m,
        [ByteUnit.MB] = 8_000_000m,
        [ByteUnit.GB] = 8_000_000_000m,
        [ByteUnit.TB] = 8_000_000_000_000m,
        [ByteUnit.KiB] = 8m * 1024m,
        [ByteUnit.MiB] = 8m * 1024m * 1024m,
        [ByteUnit.GiB] = 8m * 1024m * 1024m * 1024m,
        [ByteUnit.TiB] = 8m * 1024m * 1024m * 1024m * 1024m,
        [ByteUnit.Bit] = 1m,
        [ByteUnit.Kbit] = 1_000m,
        [ByteUnit.Mbit] = 1_000_000m,
        [ByteUnit.Gbit] = 1_000_000_000m,
    };

    // Reference bandwidths for the transfer-time rows, in bits per second.
    private static readonly (string Label, decimal BitsPerSecond)[] Bandwidths =
    {
        ("10 Mbps", 10_000_000m),
        ("100 Mbps", 100_000_000m),
        ("1 Gbps", 1_000_000_000m),
        ("10 Gbps", 10_000_000_000m),
    };

    /// <summary>
    /// Converts <paramref name="value"/> (in <paramref name="fromUnit"/>) to every unit in
    /// <see cref="UnitList"/> plus transfer times at four common bandwidths. Throws
    /// <see cref="FormatException"/> for a negative value or one large enough to overflow.
    /// </summary>
    public static ByteSizeResult Convert(decimal value, ByteUnit fromUnit)
    {
        if (value < 0)
            throw new FormatException("Size cannot be negative.");

        decimal totalBits;
        try
        {
            totalBits = checked(value * BitsPerUnit[fromUnit]);
        }
        catch (OverflowException)
        {
            throw new FormatException("That value is too large to convert.");
        }

        var equivalents = new List<ByteEquivalent>(UnitList.Count);
        foreach (var (unit, label) in UnitList)
            equivalents.Add(new ByteEquivalent(unit, label, totalBits / BitsPerUnit[unit]));

        var transferTimes = new List<TransferTime>(Bandwidths.Length);
        foreach (var (label, bps) in Bandwidths)
            transferTimes.Add(new TransferTime(label, FormatDuration(totalBits / bps)));

        return new ByteSizeResult(equivalents, transferTimes);
    }

    /// <summary>Formats a duration in seconds as a short human string ("80 s" -> "1 m 20 s", sub-second -> ms/µs).</summary>
    internal static string FormatDuration(decimal totalSeconds)
    {
        if (totalSeconds < 0)
            totalSeconds = 0;

        if (totalSeconds < 1m)
        {
            var ms = totalSeconds * 1000m;
            return ms < 1m
                ? $"{(totalSeconds * 1_000_000m).ToString("0.#", CultureInfo.InvariantCulture)} µs"
                : $"{ms.ToString("0.#", CultureInfo.InvariantCulture)} ms";
        }

        var whole = (long)Math.Floor(totalSeconds);
        var s = whole % 60;
        var totalMinutes = whole / 60;
        var m = totalMinutes % 60;
        var totalHours = totalMinutes / 60;
        var h = totalHours % 24;
        var d = totalHours / 24;

        if (d > 0) return $"{d} d {h} h";
        if (h > 0) return $"{h} h {m} m";
        if (m > 0) return $"{m} m {s} s";
        return $"{totalSeconds.ToString("0.#", CultureInfo.InvariantCulture)} s";
    }
}
