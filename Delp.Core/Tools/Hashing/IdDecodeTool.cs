namespace Delp.Core.Tools.Hashing;

/// <summary>What kind of id a line of text was auto-detected as.</summary>
public enum DetectedIdKind
{
    Ulid,
    ObjectId,
    Snowflake,
    Uuid,
}

/// <summary>One decoded id: its detected type, embedded timestamp (if any), and remaining fields.</summary>
public sealed record DecodedId(
    string Input,
    DetectedIdKind Kind,
    string TypeLabel,
    DateTimeOffset? Timestamp,
    IReadOnlyList<(string Label, string Value)> Fields);

/// <summary>
/// Auto-detects and decodes a single line of text as a ULID, MongoDB ObjectId, Snowflake, or
/// UUID (v1/v6/v7 — the versions with an embedded timestamp; other versions are still
/// identified, just without a timestamp). Detection order matters only where lengths would
/// otherwise collide; here they don't: UUIDs are 32/36 chars, ObjectIds are exactly 24 hex
/// chars, ULIDs are exactly 26 Crockford Base32 chars (so e.g. a 26-character all-hex string
/// is a ULID, never mistaken for the 24-character ObjectId shape), and Snowflakes are plain
/// non-negative integers up to 19 digits (64-bit range).
/// </summary>
public static class IdDecodeTool
{
    /// <exception cref="FormatException">No supported id format matches the input.</exception>
    public static DecodedId Decode(string input, long snowflakeEpochMs)
    {
        ArgumentNullException.ThrowIfNull(input);
        var s = input.Trim();
        if (s.Length == 0)
            throw new FormatException("Input is empty.");

        if (Guid.TryParse(s, out var guid))
            return DecodeUuid(s, guid);

        if (s.Length == 24 && s.All(Uri.IsHexDigit))
            return DecodeObjectId(s);

        if (UlidTool.IsWellFormed(s))
            return DecodeUlid(s);

        if (s.Length is >= 1 and <= 19 && s.All(char.IsAsciiDigit) && long.TryParse(s, out var num) && num >= 0)
            return DecodeSnowflake(s, num, snowflakeEpochMs);

        throw new FormatException($"Could not detect an id format for '{s}'.");
    }

    private static DecodedId DecodeUuid(string original, Guid guid)
    {
        var hex = guid.ToString("N");
        var version = HexNibble(hex[12]);

        DateTimeOffset? timestamp = version switch
        {
            1 => UuidV1.DecodeTimestamp(guid),
            6 => UuidV6.DecodeTimestamp(guid),
            7 => UuidV7.DecodeTimestamp(guid),
            _ => null,
        };

        var fields = new List<(string, string)> { ("Version", $"v{version}") };
        if (timestamp is null)
            fields.Add(("Note", "This UUID version has no embedded timestamp."));

        return new DecodedId(original, DetectedIdKind.Uuid, $"UUID v{version}", timestamp, fields);
    }

    private static DecodedId DecodeUlid(string s)
    {
        var (ms, randomness) = UlidTool.Decode(s);
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)ms);
        var fields = new List<(string, string)> { ("Randomness", Convert.ToHexStringLower(randomness)) };
        return new DecodedId(s, DetectedIdKind.Ulid, "ULID", timestamp, fields);
    }

    private static DecodedId DecodeObjectId(string s)
    {
        var decoded = ObjectIdTool.Decode(s);
        var fields = new List<(string, string)>
        {
            ("Process random", decoded.ProcessRandomHex),
            ("Counter", decoded.Counter.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        };
        return new DecodedId(s, DetectedIdKind.ObjectId, "ObjectId", decoded.Timestamp, fields);
    }

    private static DecodedId DecodeSnowflake(string original, long num, long epochMs)
    {
        var decoded = SnowflakeTool.Decode(num, epochMs);
        var fields = new List<(string, string)>
        {
            ("Worker id", decoded.WorkerId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Process id", decoded.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Sequence", decoded.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        };
        return new DecodedId(original, DetectedIdKind.Snowflake, "Snowflake", decoded.Timestamp, fields);
    }

    private static int HexNibble(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => throw new FormatException($"Invalid hex digit '{c}'."),
    };
}
