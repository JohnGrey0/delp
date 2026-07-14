using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace Delp.Core.Tools.Hashing;

/// <summary>Display formatting options applied uniformly to any generated UUID string.</summary>
public readonly record struct UuidStyle(bool Uppercase = false, bool Braces = false, bool NoHyphens = false);

/// <summary>Formats a <see cref="Guid"/> per <see cref="UuidStyle"/>. Shared by every UUID tool view.</summary>
public static class UuidFormat
{
    public static string Apply(Guid guid, UuidStyle style)
    {
        var text = style.NoHyphens ? guid.ToString("N") : guid.ToString("D");
        if (style.Uppercase)
            text = text.ToUpperInvariant();
        return style.Braces ? $"{{{text}}}" : text;
    }
}

/// <summary>Generates and formats a batch of UUIDs. Shared by every UUID tool view.</summary>
public static class UuidBatch
{
    public const int MaxCount = 1000;

    /// <exception cref="ArgumentException">Count is outside 1..1000.</exception>
    public static IReadOnlyList<string> Generate(Func<Guid> generator, int count, UuidStyle style)
    {
        if (count is < 1 or > MaxCount)
            throw new ArgumentException($"Count must be between 1 and {MaxCount}.", nameof(count));

        var results = new List<string>(count);
        for (var i = 0; i < count; i++)
            results.Add(UuidFormat.Apply(generator(), style));
        return results;
    }
}

/// <summary>
/// Byte-order-safe conversion between a <see cref="Guid"/> and its RFC 9562 network-order byte layout.
/// <see cref="Guid.ToByteArray()"/> and the <c>Guid(byte[])</c> constructor use a mixed-endian layout that
/// does NOT match the RFC field order (the first three fields are little-endian). Every hand-rolled UUID
/// builder in this file goes through these helpers instead, so field values written big-endian into a byte
/// array come back out exactly as intended.
/// </summary>
internal static class UuidBytes
{
    public static byte[] ToNetworkOrder(Guid guid) => Convert.FromHexString(guid.ToString("N"));

    public static Guid FromNetworkOrder(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 16)
            throw new ArgumentException("A UUID is exactly 16 bytes.", nameof(bytes));
        return Guid.Parse(Convert.ToHexString(bytes));
    }

    public static void WriteUInt32BE(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    public static void WriteUInt16BE(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value >> 8);
        buffer[offset + 1] = (byte)value;
    }

    public static uint ReadUInt32BE(byte[] buffer, int offset) =>
        ((uint)buffer[offset] << 24) | ((uint)buffer[offset + 1] << 16) | ((uint)buffer[offset + 2] << 8) | buffer[offset + 3];

    public static ushort ReadUInt16BE(byte[] buffer, int offset) =>
        (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
}

/// <summary>
/// The RFC 9562 60-bit "Gregorian" timestamp (100 ns ticks since 1582-10-15) shared by v1/v2/v6, with a
/// monotonic guard so a tight generation loop (e.g. a batch of 1000) never repeats a timestamp value even
/// when the system clock's resolution is coarser than 100 ns.
/// </summary>
internal static class GregorianTimestamp
{
    private static readonly long EpochTicks = new DateTime(1582, 10, 15, 0, 0, 0, DateTimeKind.Utc).Ticks;
    private static long _last;
    private static readonly object Gate = new();

    public const ulong Mask60 = 0x0FFF_FFFF_FFFF_FFFF;

    /// <summary>Next 60-bit timestamp value, guaranteed strictly increasing across calls in this process.</summary>
    public static ulong Next()
    {
        lock (Gate)
        {
            var now = DateTime.UtcNow.Ticks - EpochTicks;
            if (now <= _last)
                now = _last + 1;
            _last = now;
            return (ulong)now & Mask60;
        }
    }

    public static DateTimeOffset ToDateTimeOffset(ulong ts60) =>
        new(new DateTime(EpochTicks + (long)ts60, DateTimeKind.Utc));
}

/// <summary>Node (48-bit) and clock-sequence helpers shared by v1/v2/v6.</summary>
public static class UuidNode
{
    /// <summary>Random 48-bit node with the multicast bit set -- RFC 9562's privacy-preserving alternative to a real MAC.</summary>
    public static byte[] RandomNode()
    {
        var node = RandomNumberGenerator.GetBytes(6);
        node[0] |= 0x01;
        return node;
    }

    /// <summary>The first operational, non-loopback network interface's physical address, or a random node if none is found.</summary>
    public static byte[] RealMacNode()
    {
        var mac = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                          nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().GetAddressBytes())
            .FirstOrDefault(bytes => bytes.Length == 6);
        return mac ?? RandomNode();
    }

    public static bool IsMulticast(byte[] node) => node.Length > 0 && (node[0] & 0x01) != 0;

    /// <summary>Random 14-bit clock sequence.</summary>
    public static ushort RandomClockSequence() => (ushort)RandomNumberGenerator.GetInt32(0, 1 << 14);
}

/// <summary>UUID version 1 -- Gregorian time-based, RFC 9562 §5.1.</summary>
public static class UuidV1
{
    public static Guid Generate(byte[] node, ushort clockSeq) => Build(GregorianTimestamp.Next(), clockSeq, node);

    private static Guid Build(ulong ts60, ushort clockSeq14, byte[] node6)
    {
        var b = new byte[16];
        UuidBytes.WriteUInt32BE(b, 0, (uint)(ts60 & 0xFFFF_FFFF));
        UuidBytes.WriteUInt16BE(b, 4, (ushort)((ts60 >> 32) & 0xFFFF));
        UuidBytes.WriteUInt16BE(b, 6, (ushort)(((ts60 >> 48) & 0x0FFF) | (1u << 12)));
        b[8] = (byte)(((clockSeq14 >> 8) & 0x3F) | 0x80);
        b[9] = (byte)(clockSeq14 & 0xFF);
        Array.Copy(node6, 0, b, 10, 6);
        return UuidBytes.FromNetworkOrder(b);
    }

    /// <summary>Decodes the embedded Gregorian timestamp of a version 1 UUID.</summary>
    /// <exception cref="FormatException">The GUID is not a version 1 UUID.</exception>
    public static DateTimeOffset DecodeTimestamp(Guid guid)
    {
        var b = UuidBytes.ToNetworkOrder(guid);
        if (b[6] >> 4 != 1)
            throw new FormatException("Not a version 1 UUID.");
        var timeLow = UuidBytes.ReadUInt32BE(b, 0);
        var timeMid = UuidBytes.ReadUInt16BE(b, 4);
        var timeHiVer = UuidBytes.ReadUInt16BE(b, 6);
        var ts = ((ulong)(timeHiVer & 0x0FFF) << 48) | ((ulong)timeMid << 32) | timeLow;
        return GregorianTimestamp.ToDateTimeOffset(ts);
    }
}

/// <summary>DCE Security domain byte used by UUID version 2.</summary>
public enum DceDomain : byte
{
    Person = 0,
    Group = 1,
    Org = 2,
}

/// <summary>
/// UUID version 2 -- DCE Security, RFC 9562 §5.2. Same layout as v1 except <c>time_low</c> becomes a 32-bit
/// local domain identifier and <c>clock_seq_low</c> becomes the domain byte.
/// </summary>
public static class UuidV2
{
    public static Guid Generate(uint localId, DceDomain domain, byte[] node, ushort clockSeq)
    {
        var ts = GregorianTimestamp.Next();
        var b = new byte[16];
        UuidBytes.WriteUInt32BE(b, 0, localId);
        UuidBytes.WriteUInt16BE(b, 4, (ushort)((ts >> 32) & 0xFFFF));
        UuidBytes.WriteUInt16BE(b, 6, (ushort)(((ts >> 48) & 0x0FFF) | (2u << 12)));
        b[8] = (byte)(((clockSeq >> 8) & 0x3F) | 0x80);
        b[9] = (byte)domain;
        Array.Copy(node, 0, b, 10, 6);
        return UuidBytes.FromNetworkOrder(b);
    }

    /// <exception cref="FormatException">The GUID is not a version 2 UUID.</exception>
    public static (uint LocalId, DceDomain Domain) Decode(Guid guid)
    {
        var b = UuidBytes.ToNetworkOrder(guid);
        if (b[6] >> 4 != 2)
            throw new FormatException("Not a version 2 UUID.");
        return (UuidBytes.ReadUInt32BE(b, 0), (DceDomain)b[9]);
    }
}

/// <summary>Well-known namespace UUIDs from RFC 9562 Appendix A, for v3/v5 name-based generation.</summary>
public static class UuidNamespaces
{
    public static readonly Guid Dns = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
    public static readonly Guid Url = Guid.Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8");
    public static readonly Guid Oid = Guid.Parse("6ba7b812-9dad-11d1-80b4-00c04fd430c8");
    public static readonly Guid X500 = Guid.Parse("6ba7b814-9dad-11d1-80b4-00c04fd430c8");
}

/// <summary>UUID versions 3 (MD5) and 5 (SHA-1), name-based, RFC 9562 §5.3/§5.5.</summary>
public static class UuidNameBased
{
    public static Guid GenerateV3(Guid ns, string name) => Generate(ns, name, 3);

    public static Guid GenerateV5(Guid ns, string name) => Generate(ns, name, 5);

    /// <summary>Parses a namespace UUID from user-supplied text.</summary>
    /// <exception cref="FormatException">The text is not a valid UUID.</exception>
    public static Guid ParseNamespace(string text)
    {
        if (!Guid.TryParse(text.Trim(), out var ns))
            throw new FormatException($"'{text}' is not a valid namespace UUID.");
        return ns;
    }

    private static Guid Generate(Guid ns, string name, int version)
    {
        var nsBytes = UuidBytes.ToNetworkOrder(ns);
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
        var data = new byte[nsBytes.Length + nameBytes.Length];
        nsBytes.CopyTo(data, 0);
        nameBytes.CopyTo(data, nsBytes.Length);

        var hash = version == 3 ? MD5.HashData(data) : SHA1.HashData(data);
        var b = hash[..16];
        b[6] = (byte)((b[6] & 0x0F) | (version << 4));
        b[8] = (byte)((b[8] & 0x3F) | 0x80);
        return UuidBytes.FromNetworkOrder(b);
    }
}

/// <summary>UUID version 4 -- random, RFC 9562 §5.4.</summary>
public static class UuidV4
{
    public static Guid Generate() => Guid.NewGuid();
}

/// <summary>
/// UUID version 6 -- reordered Gregorian time-based, RFC 9562 §5.6. Same fields as v1 but the timestamp is
/// laid out high-to-low so the lexical string order matches time order.
/// </summary>
public static class UuidV6
{
    public static Guid Generate(byte[] node, ushort clockSeq)
    {
        var ts = GregorianTimestamp.Next();
        var b = new byte[16];
        UuidBytes.WriteUInt32BE(b, 0, (uint)(ts >> 28));
        UuidBytes.WriteUInt16BE(b, 4, (ushort)((ts >> 12) & 0xFFFF));
        UuidBytes.WriteUInt16BE(b, 6, (ushort)((ts & 0x0FFF) | (6u << 12)));
        b[8] = (byte)(((clockSeq >> 8) & 0x3F) | 0x80);
        b[9] = (byte)(clockSeq & 0xFF);
        Array.Copy(node, 0, b, 10, 6);
        return UuidBytes.FromNetworkOrder(b);
    }

    /// <exception cref="FormatException">The GUID is not a version 6 UUID.</exception>
    public static DateTimeOffset DecodeTimestamp(Guid guid)
    {
        var b = UuidBytes.ToNetworkOrder(guid);
        if (b[6] >> 4 != 6)
            throw new FormatException("Not a version 6 UUID.");
        var timeHigh = UuidBytes.ReadUInt32BE(b, 0);
        var timeMid = UuidBytes.ReadUInt16BE(b, 4);
        var timeLowVer = UuidBytes.ReadUInt16BE(b, 6);
        var ts = ((ulong)timeHigh << 28) | ((ulong)timeMid << 12) | (ulong)(uint)(timeLowVer & 0x0FFF);
        return GregorianTimestamp.ToDateTimeOffset(ts);
    }
}

/// <summary>UUID version 7 -- Unix epoch time-based, RFC 9562 §5.7. Generation delegates to the BCL.</summary>
public static class UuidV7
{
    public static Guid Generate() => Guid.CreateVersion7();

    /// <summary>Decodes the embedded Unix millisecond timestamp of a version 7 UUID.</summary>
    /// <exception cref="FormatException">The GUID is not a version 7 UUID.</exception>
    public static DateTimeOffset DecodeTimestamp(Guid guid)
    {
        var b = UuidBytes.ToNetworkOrder(guid);
        if (b[6] >> 4 != 7)
            throw new FormatException("Not a version 7 UUID.");
        long ms = ((long)b[0] << 40) | ((long)b[1] << 32) | ((long)b[2] << 24) | ((long)b[3] << 16) | ((long)b[4] << 8) | b[5];
        return DateTimeOffset.FromUnixTimeMilliseconds(ms);
    }
}

/// <summary>UUID version 8 -- vendor/custom, RFC 9562 §5.8. 122 bits are free; only the version nibble and variant bits are fixed.</summary>
public static class UuidV8
{
    /// <exception cref="FormatException">The custom data is not valid hexadecimal, or exceeds 32 digits.</exception>
    public static Guid Generate(string? customHex, bool randomFill)
    {
        byte[] data;
        if (string.IsNullOrWhiteSpace(customHex))
        {
            data = randomFill ? RandomNumberGenerator.GetBytes(16) : new byte[16];
        }
        else
        {
            var hex = customHex.Trim();
            if (hex.Length > 32)
                throw new FormatException("Custom data must be at most 32 hex digits.");
            if (!hex.All(Uri.IsHexDigit))
                throw new FormatException($"'{customHex}' is not valid hexadecimal.");
            data = Convert.FromHexString(hex.PadRight(32, '0'));
        }

        data[6] = (byte)((data[6] & 0x0F) | 0x80); // version nibble = 8
        data[8] = (byte)((data[8] & 0x3F) | 0x80); // variant bits = 10
        return UuidBytes.FromNetworkOrder(data);
    }
}
