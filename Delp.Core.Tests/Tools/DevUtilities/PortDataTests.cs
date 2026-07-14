using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class PortDataTests
{
    [Fact]
    public void All_HasAtLeast130Entries()
    {
        Assert.True(PortData.All.Count >= 130, $"Expected >= 130 entries, got {PortData.All.Count}");
    }

    [Theory]
    [InlineData(22, "SSH")]
    [InlineData(80, "HTTP")]
    [InlineData(443, "HTTPS")]
    [InlineData(3306, "MySQL")]
    [InlineData(5432, "PostgreSQL")]
    [InlineData(6379, "Redis")]
    [InlineData(27017, "MongoDB")]
    [InlineData(3389, "RDP")]
    [InlineData(9092, "Kafka")]
    [InlineData(6443, "Kubernetes")]
    public void All_KeyPortsPresentWithRightService(int port, string serviceContains)
    {
        Assert.Contains(PortData.All, e => e.Port == port && e.Service.Contains(serviceContains));
    }

    [Fact]
    public void Search_ByPostgres_FindsPort5432()
    {
        var results = PortData.Search("postgres");
        Assert.Contains(results, e => e.Port == 5432);
    }

    [Fact]
    public void Search_By54_FindsPortsStartingWith54()
    {
        var results = PortData.Search("54");
        Assert.Contains(results, e => e.Port == 5432);
        Assert.All(results, e => Assert.StartsWith("54", e.Port.ToString()));
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAll()
    {
        Assert.Equal(PortData.All.Count, PortData.Search("").Count);
    }

    [Fact]
    public void All_PortProtocolServiceTuplesAreUnique()
    {
        var tuples = PortData.All.Select(e => (e.Port, e.Protocol, e.Service)).ToList();
        Assert.Equal(tuples.Count, tuples.Distinct().Count());
    }

    [Fact]
    public void All_DescriptionsAreNonEmpty()
    {
        foreach (var entry in PortData.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Description), $"Port {entry.Port} has no description");
            Assert.False(string.IsNullOrWhiteSpace(entry.Service), $"Port {entry.Port} has no service name");
        }
    }

    [Fact]
    public void All_PortsAreInValidRange()
    {
        Assert.All(PortData.All, e => Assert.InRange(e.Port, 1, 65535));
    }
}
