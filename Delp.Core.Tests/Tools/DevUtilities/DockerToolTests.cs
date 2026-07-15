using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class DockerToolTests
{
    [Fact]
    public void RunToCompose_SimpleImage_DerivesServiceNameFromImage()
    {
        var yaml = DockerTool.RunToCompose("docker run nginx:latest");
        Assert.Contains("services:", yaml);
        Assert.Contains("nginx:", yaml);
        Assert.Contains("image: nginx:latest", yaml);
    }

    [Fact]
    public void RunToCompose_NameFlag_SetsContainerNameAndServiceKey()
    {
        var yaml = DockerTool.RunToCompose("docker run --name myapp -d redis:7");
        Assert.Contains("myapp:", yaml);
        Assert.Contains("container_name: myapp", yaml);
        Assert.Contains("redis:7", yaml);
    }

    [Fact]
    public void RunToCompose_PublishAndVolumeFlags_BecomeListEntries()
    {
        var yaml = DockerTool.RunToCompose("docker run -p 8080:80 -p 443:443/tcp -v /host/data:/data:ro myimage");
        Assert.Contains("ports:", yaml);
        Assert.Contains("8080:80", yaml);
        Assert.Contains("443:443/tcp", yaml);
        Assert.Contains("volumes:", yaml);
        Assert.Contains("/host/data:/data:ro", yaml);
    }

    [Fact]
    public void RunToCompose_EnvFlagWithQuotedSpacedValue_IsPreserved()
    {
        var yaml = DockerTool.RunToCompose("docker run -e 'GREETING=hello world' -e DEBUG=1 myimage");
        Assert.Contains("environment:", yaml);
        Assert.Contains("GREETING=hello world", yaml);
        Assert.Contains("DEBUG=1", yaml);
    }

    [Fact]
    public void RunToCompose_RestartFlag_MapsDirectlyToRestartKey()
    {
        var yaml = DockerTool.RunToCompose("docker run --restart on-failure:5 myimage");
        Assert.Contains("restart:", yaml);
        Assert.Contains("on-failure:5", yaml);
    }

    [Fact]
    public void RunToCompose_HealthFlags_MapToHealthcheckBlock()
    {
        var yaml = DockerTool.RunToCompose(
            "docker run --health-cmd 'curl -f http://localhost/ || exit 1' " +
            "--health-interval 30s --health-timeout 5s --health-retries 3 --health-start-period 10s myimage");

        Assert.Contains("healthcheck:", yaml);
        Assert.Contains("CMD-SHELL", yaml);
        Assert.Contains("curl -f http://localhost/ || exit 1", yaml);
        Assert.Contains("interval: 30s", yaml);
        Assert.Contains("timeout: 5s", yaml);
        Assert.Contains("retries: 3", yaml);
        Assert.Contains("start_period: 10s", yaml);
    }

    [Fact]
    public void RunToCompose_RmAndDetachFlags_AreDroppedWithWarningComments()
    {
        var yaml = DockerTool.RunToCompose("docker run --rm -d myimage");
        Assert.Contains("# --rm has no Compose equivalent", yaml);
        Assert.Contains("# -d/--detach is Compose's default run mode", yaml);
    }

    [Fact]
    public void RunToCompose_UnknownFlag_AddsWarningCommentAndNeverThrows()
    {
        var yaml = DockerTool.RunToCompose("docker run --totally-bogus myimage");
        Assert.Contains("# Unrecognized flag '--totally-bogus' ignored.", yaml);
    }

    [Fact]
    public void RunToCompose_RegistryQualifiedImage_DerivesServiceNameFromLastPathSegment()
    {
        var yaml = DockerTool.RunToCompose("docker run registry.example.com:5000/team/api:1.2.3");
        Assert.Contains("api:", yaml);
        Assert.Contains("registry.example.com:5000/team/api:1.2.3", yaml);
    }

    [Fact]
    public void RunToCompose_MemoryAndCpusFlags_MapToDeployResourceLimits()
    {
        var yaml = DockerTool.RunToCompose("docker run --memory 512m --cpus 1.5 myimage");
        Assert.Contains("deploy:", yaml);
        Assert.Contains("resources:", yaml);
        Assert.Contains("limits:", yaml);
        Assert.Contains("512m", yaml);
        Assert.Contains("1.5", yaml);
    }

    [Fact]
    public void RunToCompose_NoImage_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => DockerTool.RunToCompose("docker run --name only-flags"));
    }

    [Fact]
    public void ComposeToRun_SingleService_ProducesExpectedDockerRunLine()
    {
        const string yaml = """
            services:
              web:
                image: nginx:latest
                ports:
                  - "8080:80"
                environment:
                  - FOO=bar
            """;

        var result = DockerTool.ComposeToRun(yaml, multiline: false).Trim();
        Assert.Equal("docker run --name web -p 8080:80 -e FOO=bar nginx:latest", result);
    }

    [Fact]
    public void ComposeToRun_EnvValueWithSpace_IsShellQuoted()
    {
        const string yaml = """
            services:
              app:
                image: myimage
                environment:
                  GREETING: "hello world"
            """;

        var result = DockerTool.ComposeToRun(yaml, multiline: false);
        Assert.Contains("-e 'GREETING=hello world'", result);
    }

    [Fact]
    public void ComposeToRun_MultiService_ProducesOneRunLinePerService()
    {
        const string yaml = """
            services:
              web:
                image: nginx:latest
              db:
                image: postgres:16
            """;

        var result = DockerTool.ComposeToRun(yaml, multiline: false);
        Assert.Contains("nginx:latest", result);
        Assert.Contains("postgres:16", result);
        Assert.Equal(2, result.Split("docker run").Length - 1);
    }

    [Fact]
    public void ComposeToRun_MultilineOption_UsesBackslashContinuations()
    {
        const string yaml = """
            services:
              web:
                image: nginx:latest
                ports:
                  - "8080:80"
            """;

        var result = DockerTool.ComposeToRun(yaml, multiline: true);
        Assert.Contains(" \\\n", result);
    }

    [Fact]
    public void ComposeToRun_MalformedYaml_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => DockerTool.ComposeToRun("services: [unterminated"));
    }

    [Fact]
    public void ComposeToRun_NoServicesKey_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => DockerTool.ComposeToRun("foo: bar"));
    }

    [Fact]
    public void RoundTrip_RunToComposeToRun_PreservesKeyFlags()
    {
        const string original = "docker run --name api -p 8080:80 -e MODE=prod --restart unless-stopped myimage:1.0";

        var compose = DockerTool.RunToCompose(original);
        var runLine = DockerTool.ComposeToRun(compose, multiline: false);

        Assert.Contains("--name api", runLine);
        Assert.Contains("-p 8080:80", runLine);
        Assert.Contains("-e MODE=prod", runLine);
        Assert.Contains("--restart unless-stopped", runLine);
        Assert.Contains("myimage:1.0", runLine);
    }

    [Fact]
    public void RoundTrip_ComposeToRunToCompose_PreservesHealthcheck()
    {
        const string yaml = """
            services:
              web:
                image: nginx:latest
                healthcheck:
                  test:
                    - CMD-SHELL
                    - curl -f http://localhost/
                  interval: 30s
                  retries: 3
            """;

        var runLine = DockerTool.ComposeToRun(yaml, multiline: false);
        var reComposed = DockerTool.RunToCompose(runLine);

        Assert.Contains("--health-cmd", runLine);
        Assert.Contains("healthcheck:", reComposed);
        Assert.Contains("curl -f http://localhost/", reComposed);
        Assert.Contains("interval: 30s", reComposed);
        Assert.Contains("retries: 3", reComposed);
    }
}
