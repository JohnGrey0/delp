using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>Analysis of a single IPv4 or IPv6 address.</summary>
public sealed record IpAnalysis(
    int Version,
    string Canonical,
    string IntegerForm,
    string? BinaryForm,
    string PtrName,
    string Classification,
    bool IsGlobal);

/// <summary>Analysis of an IPv4 or IPv6 CIDR block.</summary>
public sealed record CidrAnalysis(
    int Version,
    int PrefixLength,
    string Network,
    string? Broadcast,
    string FirstUsable,
    string LastUsable,
    string HostCount,
    string? PrefixMaskDotted);

/// <summary>A local network adapter and its bound addresses.</summary>
public sealed record AdapterInfo(
    string Name,
    string Description,
    IReadOnlyList<string> IPv4Addresses,
    IReadOnlyList<string> IPv6Addresses,
    string Status);

/// <summary>
/// Offline IP address, CIDR, and classification utilities. No network calls or geolocation —
/// every range check and CIDR calculation is implemented by hand against the IANA special-purpose
/// address registries (RFC 1918, 4193, 5735/6890, 6598, 3927/4291, etc.).
/// </summary>
public static class IpTool
{
    public static class Classification
    {
        public const string Unspecified = "Unspecified";
        public const string Loopback = "Loopback";
        public const string PrivateRfc1918 = "Private (RFC 1918)";
        public const string LinkLocal = "Link-Local";
        public const string CarrierGradeNat = "Carrier-Grade NAT (RFC 6598)";
        public const string Documentation = "Documentation / Reserved for Examples";
        public const string Multicast = "Multicast";
        public const string LimitedBroadcast = "Limited Broadcast";
        public const string Reserved = "Reserved";
        public const string UniqueLocal = "Unique Local (RFC 4193)";
        public const string Ipv4Mapped = "IPv4-Mapped IPv6";
        public const string Global = "Global (Public)";
    }

    public static IpAnalysis Analyze(string input)
    {
        var address = ParseAddress(input);

        if (address.AddressFamily == AddressFamily.InterNetwork)
            return AnalyzeV4(address);
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return AnalyzeV6(address);

        throw new FormatException($"'{input}' is not a supported IP address family.");
    }

    public static CidrAnalysis AnalyzeCidr(string cidr)
    {
        var (address, prefixLength) = ParseCidr(cidr);
        return address.AddressFamily == AddressFamily.InterNetwork
            ? AnalyzeCidrV4(address, prefixLength)
            : AnalyzeCidrV6(address, prefixLength);
    }

    /// <summary>
    /// Enumerates local network adapters. Never throws for a normal environment issue — a failure
    /// enumerating one adapter's addresses is skipped rather than aborting the whole list.
    /// </summary>
    public static IReadOnlyList<AdapterInfo> LocalAdapters()
    {
        var result = new List<AdapterInfo>();
        NetworkInterface[] interfaces;
        try
        {
            interfaces = NetworkInterface.GetAllNetworkInterfaces();
        }
        catch
        {
            return result;
        }

        foreach (var nic in interfaces)
        {
            try
            {
                var props = nic.GetIPProperties();
                var v4 = new List<string>();
                var v6 = new List<string>();
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                        v4.Add($"{ua.Address}/{ua.PrefixLength}");
                    else if (ua.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        v6.Add($"{ua.Address}/{ua.PrefixLength}");
                }

                result.Add(new AdapterInfo(nic.Name, nic.Description, v4, v6, nic.OperationalStatus.ToString()));
            }
            catch
            {
                // Some virtual adapters throw when queried; skip rather than fail the whole list.
            }
        }

        return result;
    }

    // ---------------------------------------------------------------- parsing

    private static IPAddress ParseAddress(string input)
    {
        var trimmed = (input ?? "").Trim();
        if (trimmed.Length == 0)
            throw new FormatException("Enter an IPv4 or IPv6 address.");
        if (!IPAddress.TryParse(trimmed, out var address))
            throw new FormatException($"'{trimmed}' is not a valid IPv4 or IPv6 address.");
        return address;
    }

    private static (IPAddress Address, int PrefixLength) ParseCidr(string cidr)
    {
        var trimmed = (cidr ?? "").Trim();
        var slash = trimmed.IndexOf('/');
        if (slash < 0)
            throw new FormatException($"'{trimmed}' is not a CIDR block — expected the form <address>/<prefix-length>.");

        var addressPart = trimmed[..slash];
        var prefixPart = trimmed[(slash + 1)..];

        if (!IPAddress.TryParse(addressPart, out var address))
            throw new FormatException($"'{addressPart}' is not a valid IP address.");

        var maxPrefix = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        if (!int.TryParse(prefixPart, NumberStyles.None, CultureInfo.InvariantCulture, out var prefixLength) ||
            prefixLength < 0 || prefixLength > maxPrefix)
            throw new FormatException($"'{prefixPart}' is not a valid prefix length for {(maxPrefix == 32 ? "IPv4" : "IPv6")} (0-{maxPrefix}).");

        return (address, prefixLength);
    }

    // ---------------------------------------------------------------- IPv4 address analysis

    private static IpAnalysis AnalyzeV4(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        var value = ToUInt32(bytes);
        var classification = ClassifyV4(value);

        return new IpAnalysis(
            Version: 4,
            Canonical: address.ToString(),
            IntegerForm: value.ToString(CultureInfo.InvariantCulture),
            BinaryForm: string.Join(".", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))),
            PtrName: $"{bytes[3]}.{bytes[2]}.{bytes[1]}.{bytes[0]}.in-addr.arpa",
            Classification: classification,
            IsGlobal: classification == Classification.Global);
    }

    private static string ClassifyV4(uint value)
    {
        if (value == 0)
            return Classification.Unspecified;
        if (value == 0xFFFFFFFFu)
            return Classification.LimitedBroadcast;
        if (InRangeV4(value, 127, 0, 0, 0, 8))
            return Classification.Loopback;
        if (InRangeV4(value, 169, 254, 0, 0, 16))
            return Classification.LinkLocal;
        if (InRangeV4(value, 10, 0, 0, 0, 8) ||
            InRangeV4(value, 172, 16, 0, 0, 12) ||
            InRangeV4(value, 192, 168, 0, 0, 16))
            return Classification.PrivateRfc1918;
        if (InRangeV4(value, 100, 64, 0, 0, 10))
            return Classification.CarrierGradeNat;
        if (InRangeV4(value, 192, 0, 2, 0, 24) ||
            InRangeV4(value, 198, 51, 100, 0, 24) ||
            InRangeV4(value, 203, 0, 113, 0, 24))
            return Classification.Documentation;
        if (InRangeV4(value, 224, 0, 0, 0, 4))
            return Classification.Multicast;
        if (InRangeV4(value, 240, 0, 0, 0, 4))
            return Classification.Reserved;

        return Classification.Global;
    }

    private static bool InRangeV4(uint value, byte a, byte b, byte c, byte d, int prefixLength)
    {
        var baseValue = ToUInt32([a, b, c, d]);
        var mask = MaskV4(prefixLength);
        return (value & mask) == (baseValue & mask);
    }

    private static uint MaskV4(int prefixLength) =>
        prefixLength == 0 ? 0u : 0xFFFFFFFFu << (32 - prefixLength);

    private static uint ToUInt32(byte[] bytes) =>
        ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];

    private static string ToDotted(uint value) =>
        $"{(value >> 24) & 0xFF}.{(value >> 16) & 0xFF}.{(value >> 8) & 0xFF}.{value & 0xFF}";

    // ---------------------------------------------------------------- IPv4 CIDR

    private static CidrAnalysis AnalyzeCidrV4(IPAddress address, int prefixLength)
    {
        var value = ToUInt32(address.GetAddressBytes());
        var mask = MaskV4(prefixLength);
        var network = value & mask;
        var broadcast = network | ~mask;

        string first, last, hostCount;
        string? broadcastText;

        if (prefixLength == 32)
        {
            first = last = ToDotted(network);
            hostCount = "1";
            broadcastText = null;
        }
        else if (prefixLength == 31)
        {
            // RFC 3021: both addresses in a /31 are usable host addresses (point-to-point links).
            first = ToDotted(network);
            last = ToDotted(broadcast);
            hostCount = "2";
            broadcastText = null;
        }
        else
        {
            first = ToDotted(network + 1);
            last = ToDotted(broadcast - 1);
            hostCount = (BigInteger.Pow(2, 32 - prefixLength) - 2).ToString(CultureInfo.InvariantCulture);
            broadcastText = ToDotted(broadcast);
        }

        return new CidrAnalysis(
            Version: 4,
            PrefixLength: prefixLength,
            Network: ToDotted(network),
            Broadcast: broadcastText,
            FirstUsable: first,
            LastUsable: last,
            HostCount: hostCount,
            PrefixMaskDotted: ToDotted(mask));
    }

    // ---------------------------------------------------------------- IPv6 address analysis

    private static IpAnalysis AnalyzeV6(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        var value = ToBigInteger(bytes);
        var classification = ClassifyV6(address, bytes, value);

        return new IpAnalysis(
            Version: 6,
            Canonical: address.ToString(),
            IntegerForm: "0x" + value.ToString("x32", CultureInfo.InvariantCulture),
            BinaryForm: null,
            PtrName: BuildPtr6(bytes),
            Classification: classification,
            IsGlobal: classification == Classification.Global);
    }

    private static string ClassifyV6(IPAddress address, byte[] bytes, BigInteger value)
    {
        if (address.Equals(IPAddress.IPv6Any))
            return Classification.Unspecified;
        if (address.Equals(IPAddress.IPv6Loopback))
            return Classification.Loopback;
        if (address.IsIPv4MappedToIPv6)
            return Classification.Ipv4Mapped;

        // fe80::/10 link-local
        if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
            return Classification.LinkLocal;

        // fc00::/7 unique local
        if ((bytes[0] & 0xFE) == 0xFC)
            return Classification.UniqueLocal;

        // ff00::/8 multicast
        if (bytes[0] == 0xFF)
            return Classification.Multicast;

        // 2001:db8::/32 documentation
        if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8)
            return Classification.Documentation;

        // 2000::/3 global unicast
        if ((bytes[0] & 0xE0) == 0x20)
            return Classification.Global;

        return Classification.Reserved;
    }

    private static string BuildPtr6(byte[] bytes)
    {
        var sb = new StringBuilder();
        for (var i = bytes.Length - 1; i >= 0; i--)
        {
            var hex = bytes[i].ToString("x2", CultureInfo.InvariantCulture);
            sb.Append(hex[1]).Append('.').Append(hex[0]).Append('.');
        }

        sb.Append("ip6.arpa");
        return sb.ToString();
    }

    private static BigInteger ToBigInteger(byte[] bytes)
    {
        // IPAddress bytes are big-endian network order; BigInteger's byte[] ctor expects little-endian,
        // and prepend a zero byte so the value is always treated as unsigned/non-negative.
        var reversed = new byte[bytes.Length + 1];
        for (var i = 0; i < bytes.Length; i++)
            reversed[i] = bytes[bytes.Length - 1 - i];
        reversed[^1] = 0;
        return new BigInteger(reversed);
    }

    private static byte[] FromBigInteger(BigInteger value, int byteCount)
    {
        var little = value.ToByteArray(isUnsigned: true, isBigEndian: false);
        var result = new byte[byteCount];
        for (var i = 0; i < little.Length && i < byteCount; i++)
            result[byteCount - 1 - i] = little[i];
        return result;
    }

    // ---------------------------------------------------------------- IPv6 CIDR

    private static CidrAnalysis AnalyzeCidrV6(IPAddress address, int prefixLength)
    {
        var value = ToBigInteger(address.GetAddressBytes());
        var hostBits = 128 - prefixLength;
        var blockSize = BigInteger.Pow(2, hostBits);
        var mask = hostBits == 0 ? BigInteger.Zero : blockSize - 1;
        var network = value & ~mask;
        var last = network + blockSize - 1;

        var networkAddress = new IPAddress(FromBigInteger(network, 16));
        var lastAddress = new IPAddress(FromBigInteger(last, 16));

        return new CidrAnalysis(
            Version: 6,
            PrefixLength: prefixLength,
            Network: networkAddress.ToString(),
            Broadcast: null,
            FirstUsable: networkAddress.ToString(),
            LastUsable: lastAddress.ToString(),
            HostCount: blockSize.ToString(CultureInfo.InvariantCulture),
            PrefixMaskDotted: null);
    }
}
