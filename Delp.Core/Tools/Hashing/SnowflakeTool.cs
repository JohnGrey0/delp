namespace Delp.Core.Tools.Hashing;

/// <summary>
/// Twitter/Discord-style "Snowflake" IDs: a 64-bit integer packing a millisecond timestamp
/// (relative to a chosen epoch), a 5-bit worker id, a 5-bit process id, and a 12-bit
/// per-millisecond sequence — bit layout <c>(timestamp &lt;&lt; 22) | (worker &lt;&lt; 17) |
/// (process &lt;&lt; 12) | sequence</c>, shared by both Twitter's and Discord's implementations
/// (they differ only in epoch).
/// </summary>
public static class SnowflakeTool
{
    /// <summary>Twitter epoch: 2010-11-04T01:42:54.657Z, in Unix milliseconds.</summary>
    public const long TwitterEpochMs = 1288834974657L;

    /// <summary>Discord epoch: 2015-01-01T00:00:00.000Z, in Unix milliseconds.</summary>
    public const long DiscordEpochMs = 1420070400000L;

    public const int MaxBatch = 1000;

    private static long _lastMs = -1;
    private static int _sequence;
    private static readonly object Gate = new();

    /// <summary>Generates one snowflake id for the current instant.</summary>
    /// <exception cref="ArgumentException">Worker or process id is outside 0..31.</exception>
    public static long Generate(long epochMs, int workerId, int processId)
    {
        ValidateField(workerId, nameof(workerId));
        ValidateField(processId, nameof(processId));

        long ts;
        int seq;
        lock (Gate)
        {
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (ts == _lastMs)
            {
                _sequence = (_sequence + 1) & 0xFFF;
                if (_sequence == 0)
                {
                    // Sequence exhausted within this millisecond — spin to the next one.
                    do
                    {
                        ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    } while (ts == _lastMs);
                }
            }
            else
            {
                _sequence = 0;
            }
            _lastMs = ts;
            seq = _sequence;
        }

        var delta = ts - epochMs;
        if (delta < 0)
            throw new ArgumentException("Epoch is later than the current time.", nameof(epochMs));
        if (delta >= (1L << 41))
            throw new ArgumentException("Epoch is too far in the past to fit the 41-bit timestamp field.", nameof(epochMs));

        return (delta << 22) | ((long)workerId << 17) | ((long)processId << 12) | (uint)seq;
    }

    /// <exception cref="ArgumentException">Count is outside 1..1000, or worker/process id is outside 0..31.</exception>
    public static IReadOnlyList<long> GenerateBatch(int count, long epochMs, int workerId, int processId)
    {
        if (count is < 1 or > MaxBatch)
            throw new ArgumentException($"Count must be between 1 and {MaxBatch}.", nameof(count));

        var results = new List<long>(count);
        for (var i = 0; i < count; i++)
            results.Add(Generate(epochMs, workerId, processId));
        return results;
    }

    public sealed record Decoded(DateTimeOffset Timestamp, int WorkerId, int ProcessId, int Sequence);

    /// <exception cref="FormatException">The id is negative (snowflakes are non-negative 64-bit integers).</exception>
    public static Decoded Decode(long id, long epochMs)
    {
        if (id < 0)
            throw new FormatException("Snowflake ids are non-negative 64-bit integers.");

        var delta = id >> 22;
        var workerId = (int)((id >> 17) & 0x1F);
        var processId = (int)((id >> 12) & 0x1F);
        var sequence = (int)(id & 0xFFF);
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(epochMs + delta);
        return new Decoded(timestamp, workerId, processId, sequence);
    }

    private static void ValidateField(int value, string name)
    {
        if (value is < 0 or > 31)
            throw new ArgumentException($"{name} must be between 0 and 31 (5 bits).", name);
    }
}
