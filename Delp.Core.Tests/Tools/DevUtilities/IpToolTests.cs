using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class IpToolTests
{
    // ---------------------------------------------------------------- Analyze: parsing & basics

    [Fact]
    public void Analyze_V4Address_ReturnsVersion4()
    {
        var result = IpTool.Analyze("192.168.1.10");
        Assert.Equal(4, result.Version);
        Assert.Equal("192.168.1.10", result.Canonical);
    }

    [Fact]
    public void Analyze_V6Address_ReturnsVersion6()
    {
        var result = IpTool.Analyze("2001:db8::1");
        Assert.Equal(6, result.Version);
    }

    [Fact]
    public void Analyze_InvalidInput_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => IpTool.Analyze("not an ip"));
        Assert.Throws<FormatException>(() => IpTool.Analyze("999.999.999.999"));
        Assert.Throws<FormatException>(() => IpTool.Analyze(""));
    }

    [Fact]
    public void Analyze_V4_IntegerFormAndBinaryForm()
    {
        var result = IpTool.Analyze("192.168.1.10");
        Assert.Equal((192u * 16777216) + (168u * 65536) + (1u * 256) + 10u, uint.Parse(result.IntegerForm));
        Assert.Equal("11000000.10101000.00000001.00001010", result.BinaryForm);
    }

    [Fact]
    public void Analyze_V4_PtrName()
    {
        var result = IpTool.Analyze("192.168.1.10");
        Assert.Equal("10.1.168.192.in-addr.arpa", result.PtrName);
    }

    [Fact]
    public void Analyze_V6_PtrName()
    {
        var result = IpTool.Analyze("2001:db8::1");
        Assert.EndsWith("ip6.arpa", result.PtrName);
        Assert.StartsWith("1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0", result.PtrName);
    }

    // ---------------------------------------------------------------- Classification: v4

    [Theory]
    [InlineData("127.0.0.1", IpTool.Classification.Loopback)]
    [InlineData("127.1.2.3", IpTool.Classification.Loopback)]
    [InlineData("10.0.0.1", IpTool.Classification.PrivateRfc1918)]
    [InlineData("172.16.0.1", IpTool.Classification.PrivateRfc1918)]
    [InlineData("172.31.255.255", IpTool.Classification.PrivateRfc1918)]
    [InlineData("192.168.1.1", IpTool.Classification.PrivateRfc1918)]
    [InlineData("169.254.1.1", IpTool.Classification.LinkLocal)]
    [InlineData("100.64.0.1", IpTool.Classification.CarrierGradeNat)]
    [InlineData("100.127.255.255", IpTool.Classification.CarrierGradeNat)]
    [InlineData("224.0.0.1", IpTool.Classification.Multicast)]
    [InlineData("192.0.2.1", IpTool.Classification.Documentation)]
    [InlineData("198.51.100.1", IpTool.Classification.Documentation)]
    [InlineData("203.0.113.1", IpTool.Classification.Documentation)]
    [InlineData("0.0.0.0", IpTool.Classification.Unspecified)]
    [InlineData("255.255.255.255", IpTool.Classification.LimitedBroadcast)]
    [InlineData("8.8.8.8", IpTool.Classification.Global)]
    [InlineData("1.1.1.1", IpTool.Classification.Global)]
    public void Analyze_V4_Classification(string ip, string expectedClass)
    {
        var result = IpTool.Analyze(ip);
        Assert.Equal(expectedClass, result.Classification);
        Assert.Equal(expectedClass == IpTool.Classification.Global, result.IsGlobal);
    }

    // ---------------------------------------------------------------- Classification: v6

    [Theory]
    [InlineData("::1", IpTool.Classification.Loopback)]
    [InlineData("::", IpTool.Classification.Unspecified)]
    [InlineData("fe80::1", IpTool.Classification.LinkLocal)]
    [InlineData("fc00::1", IpTool.Classification.UniqueLocal)]
    [InlineData("fd12:3456:789a::1", IpTool.Classification.UniqueLocal)]
    [InlineData("ff02::1", IpTool.Classification.Multicast)]
    [InlineData("2001:db8::1", IpTool.Classification.Documentation)]
    [InlineData("::ffff:192.168.1.1", IpTool.Classification.Ipv4Mapped)]
    [InlineData("2606:4700:4700::1111", IpTool.Classification.Global)]
    public void Analyze_V6_Classification(string ip, string expectedClass)
    {
        var result = IpTool.Analyze(ip);
        Assert.Equal(expectedClass, result.Classification);
    }

    // ---------------------------------------------------------------- CIDR: v4

    [Fact]
    public void AnalyzeCidr_V4Slash24_StandardMath()
    {
        var result = IpTool.AnalyzeCidr("10.0.0.0/24");
        Assert.Equal("10.0.0.0", result.Network);
        Assert.Equal("10.0.0.255", result.Broadcast);
        Assert.Equal("10.0.0.1", result.FirstUsable);
        Assert.Equal("10.0.0.254", result.LastUsable);
        Assert.Equal("254", result.HostCount);
        Assert.Equal("255.255.255.0", result.PrefixMaskDotted);
    }

    [Fact]
    public void AnalyzeCidr_V4Slash31_TwoUsableHostsPerRfc3021()
    {
        var result = IpTool.AnalyzeCidr("192.168.1.0/31");
        Assert.Equal("2", result.HostCount);
        Assert.Equal("192.168.1.0", result.FirstUsable);
        Assert.Equal("192.168.1.1", result.LastUsable);
        Assert.Null(result.Broadcast);
    }

    [Fact]
    public void AnalyzeCidr_V4Slash32_SingleHost()
    {
        var result = IpTool.AnalyzeCidr("10.1.2.3/32");
        Assert.Equal("1", result.HostCount);
        Assert.Equal("10.1.2.3", result.FirstUsable);
        Assert.Equal("10.1.2.3", result.LastUsable);
    }

    [Fact]
    public void AnalyzeCidr_V4Slash0_CoversEntireSpace()
    {
        var result = IpTool.AnalyzeCidr("0.0.0.0/0");
        Assert.Equal("0.0.0.0", result.Network);
        Assert.Equal("0.0.0.0", result.PrefixMaskDotted);
    }

    // ---------------------------------------------------------------- CIDR: v6

    [Fact]
    public void AnalyzeCidr_V6Slash64_HostCount()
    {
        var result = IpTool.AnalyzeCidr("2001:db8::/64");
        Assert.Equal("18446744073709551616", result.HostCount); // 2^64
        Assert.Equal("2001:db8::", result.Network);
        Assert.Null(result.Broadcast);
        Assert.Null(result.PrefixMaskDotted);
    }

    [Fact]
    public void AnalyzeCidr_V6Slash128_SingleAddress()
    {
        var result = IpTool.AnalyzeCidr("2001:db8::1/128");
        Assert.Equal("1", result.HostCount);
        Assert.Equal(result.FirstUsable, result.LastUsable);
    }

    [Fact]
    public void AnalyzeCidr_InvalidInput_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => IpTool.AnalyzeCidr("not-a-cidr"));
        Assert.Throws<FormatException>(() => IpTool.AnalyzeCidr("10.0.0.0"));
        Assert.Throws<FormatException>(() => IpTool.AnalyzeCidr("10.0.0.0/33"));
        Assert.Throws<FormatException>(() => IpTool.AnalyzeCidr("2001:db8::/129"));
    }

    // ---------------------------------------------------------------- Local adapters

    [Fact]
    public void LocalAdapters_DoesNotThrow()
    {
        var adapters = IpTool.LocalAdapters();
        Assert.NotNull(adapters);
    }
}
