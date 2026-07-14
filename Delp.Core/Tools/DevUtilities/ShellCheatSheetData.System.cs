namespace Delp.Core.Tools.DevUtilities;

public static partial class ShellCheatSheetData
{
    internal static IReadOnlyList<ShellEntry> ProcessEntries { get; } = new List<ShellEntry>
    {
        new("List running processes", "Processes",
            "ps aux",
            "Get-Process",
            null),

        new("Show top CPU/memory consumers", "Processes",
            "top",
            "Get-Process | Sort-Object CPU -Descending | Select-Object -First 10",
            "top refreshes live; the PowerShell form is a one-shot snapshot - wrap it in a loop to poll."),

        new("Kill a process by name", "Processes",
            "pkill -f myapp",
            "Get-Process myapp | Stop-Process -Force",
            "pkill -f matches the full command line; Stop-Process -Name only matches the process name."),

        new("Kill whatever process is listening on a port", "Processes",
            "kill -9 $(lsof -ti:8080)",
            "Get-Process -Id (Get-NetTCPConnection -LocalPort 8080).OwningProcess | Stop-Process -Force",
            null),

        new("Run a command in the background", "Processes",
            "mycommand &",
            "Start-Process mycommand",
            "Use Start-Job instead if you need to capture output from a PowerShell script block."),

        new("Check whether a process is running", "Processes",
            "pgrep -x myapp >/dev/null && echo running",
            "if (Get-Process myapp -ErrorAction SilentlyContinue) { \"running\" }",
            null),

        new("Show the current shell's process ID", "Processes",
            "echo $$",
            "$PID",
            null),
    };

    internal static IReadOnlyList<ShellEntry> NetworkEntries { get; } = new List<ShellEntry>
    {
        new("List listening ports", "Network",
            "netstat -tulnp",
            "Get-NetTCPConnection -State Listen",
            "Listing the owning process for every port typically needs an elevated prompt on both platforms."),

        new("Make an HTTP GET request", "Network",
            "curl -s https://example.com",
            "Invoke-RestMethod -Uri https://example.com",
            "Invoke-RestMethod parses JSON/XML responses automatically; use Invoke-WebRequest for raw response details."),

        new("Download a file", "Network",
            "curl -O https://example.com/file.zip",
            "Invoke-WebRequest -Uri https://example.com/file.zip -OutFile file.zip",
            null),

        new("Look up a domain's DNS records", "Network",
            "nslookup example.com",
            "Resolve-DnsName example.com",
            null),

        new("Trace the network route to a host", "Network",
            "traceroute example.com",
            "Test-NetConnection example.com -TraceRoute",
            "-TraceRoute needs an elevated prompt; classic tracert.exe also works from PowerShell."),

        new("Show my public IP address", "Network",
            "curl -s ifconfig.me/ip",
            "Invoke-RestMethod -Uri https://ifconfig.me/ip",
            "Unlike curl, Invoke-RestMethod requires an explicit scheme in the URI or it throws an Invalid URI error."),

        new("Show local network interface addresses", "Network",
            "ip addr",
            "Get-NetIPAddress",
            "ifconfig is the older bash equivalent on systems without iproute2."),

        new("Test whether a TCP port is reachable", "Network",
            "nc -zv example.com 443",
            "Test-NetConnection example.com -Port 443",
            null),
    };

    internal static IReadOnlyList<ShellEntry> ArchiveEntries { get; } = new List<ShellEntry>
    {
        new("Create a .tar.gz archive", "Archives",
            "tar -czvf archive.tar.gz dir/",
            "tar -czvf archive.tar.gz dir/",
            "Windows 10 1803+ ships bsdtar as tar.exe, so the command is identical; on older systems use Compress-Archive for a .zip instead."),

        new("Extract a .tar.gz archive", "Archives",
            "tar -xzvf archive.tar.gz",
            "tar -xzvf archive.tar.gz",
            "Same bundled tar.exe as above handles extraction identically."),

        new("List the contents of an archive without extracting", "Archives",
            "tar -tzvf archive.tar.gz",
            "tar -tzvf archive.tar.gz",
            null),

        new("Create a .zip archive", "Archives",
            "zip -r archive.zip dir/",
            "Compress-Archive -Path dir -DestinationPath archive.zip",
            null),

        new("Extract a .zip archive", "Archives",
            "unzip archive.zip -d .",
            "Expand-Archive -Path archive.zip -DestinationPath .",
            null),

        new("Compress a single file with gzip", "Archives",
            "gzip -k file.txt",
            "$in = [System.IO.File]::OpenRead('file.txt'); $out = [System.IO.File]::Create('file.txt.gz'); $gzip = New-Object System.IO.Compression.GZipStream $out, ([System.IO.Compression.CompressionMode]::Compress); $in.CopyTo($gzip); $gzip.Close(); $out.Close(); $in.Close()",
            "Windows PowerShell 5.1 has no built-in gzip cmdlet; tar -czf would wrap the file in a tar container instead of producing a plain .gz, so GZipStream is used to match gzip's output format exactly."),
    };

    internal static IReadOnlyList<ShellEntry> EnvironmentEntries { get; } = new List<ShellEntry>
    {
        new("Set an environment variable for the current session", "Environment",
            "export VAR=value",
            "$env:VAR = \"value\"",
            null),

        new("Read an environment variable", "Environment",
            "echo $VAR",
            "$env:VAR",
            null),

        new("List all environment variables", "Environment",
            "env",
            "Get-ChildItem Env:",
            null),

        new("Persist an environment variable permanently", "Environment",
            "echo 'export VAR=value' >> ~/.bashrc",
            "[Environment]::SetEnvironmentVariable('VAR', 'value', 'User')",
            "The bash form only takes effect in new shells (or after sourcing ~/.bashrc); the PowerShell form writes to the registry and needs a new session too."),

        new("Show the PATH variable", "Environment",
            "echo $PATH",
            "$env:Path",
            null),

        new("Add a directory to PATH for the current session", "Environment",
            "export PATH=\"$PATH:/new/dir\"",
            "$env:Path += ';C:\\new\\dir'",
            null),

        new("Find the location of an executable", "Environment",
            "which python",
            "Get-Command python | Select-Object -ExpandProperty Source",
            null),
    };

    internal static IReadOnlyList<ShellEntry> SystemEntries { get; } = new List<ShellEntry>
    {
        new("Show free disk space", "System",
            "df -h",
            "Get-PSDrive -PSProvider FileSystem",
            null),

        new("Show memory usage", "System",
            "free -h",
            "Get-CimInstance Win32_OperatingSystem | Select-Object FreePhysicalMemory, TotalVisibleMemorySize",
            "Both values are reported in KB; divide by 1024 to convert to MB, or by 1MB (1,048,576) to convert to GB."),

        new("Show system uptime", "System",
            "uptime",
            "(Get-Date) - (Get-CimInstance Win32_OperatingSystem).LastBootUpTime",
            "Get-Uptime is a PowerShell 7+ shortcut for the same calculation."),

        new("List system services", "System",
            "systemctl list-units --type=service",
            "Get-Service",
            null),

        new("Restart a service", "System",
            "sudo systemctl restart nginx",
            "Restart-Service -Name W3SVC",
            "Both typically require administrator/root privileges."),

        new("List scheduled jobs", "System",
            "crontab -l",
            "Get-ScheduledTask",
            "Get-ScheduledTask lists Task Scheduler entries, the closest Windows analogue to cron."),

        new("Show OS and kernel version", "System",
            "uname -a",
            "Get-CimInstance Win32_OperatingSystem | Select-Object Caption, Version, OSArchitecture",
            null),

        new("Show the current user name", "System",
            "whoami",
            "$env:USERNAME",
            "whoami.exe also works unchanged from PowerShell if you want the exact bash-style output."),
    };
}
