using System.Globalization;
using Delp.Core.Tools.Common;
using Delp.Core.Tools.DataFormat;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>
/// Converts between a <c>docker run ...</c> command line and an equivalent Docker Compose
/// service definition. Reuses the shared YAML plumbing from json-yaml/yaml-format
/// (<see cref="YamlSerializing"/>, <see cref="YamlParsing"/>, <see cref="YamlGraphHelper"/>) so
/// Compose documents are emitted and read with the same conventions as the rest of the app.
/// Parsing never throws for unrecognized <c>docker run</c> flags — they're collected as warnings
/// and appended to the generated YAML as comments instead.
/// </summary>
public static class DockerTool
{
    /// <exception cref="FormatException">No image was found in the command.</exception>
    public static string RunToCompose(string command)
    {
        var tokens = ShellTokenizer.Tokenize(command ?? "");
        var warnings = new List<string>();

        var i = 0;
        if (tokens.Count > 0 && tokens[i].Equals("docker", StringComparison.OrdinalIgnoreCase))
            i++;
        if (i < tokens.Count && tokens[i].Equals("run", StringComparison.OrdinalIgnoreCase))
            i++;

        string? name = null;
        var ports = new List<string>();
        var volumes = new List<string>();
        var envs = new List<string>();
        var envFiles = new List<string>();
        string? restart = null;
        var networks = new List<string>();
        string? hostname = null;
        string? workdir = null;
        string? user = null;
        string? entrypoint = null;
        var labels = new List<string>();
        var extraHosts = new List<string>();
        var devices = new List<string>();
        var capAdd = new List<string>();
        var capDrop = new List<string>();
        var privileged = false;
        string? memory = null;
        string? cpus = null;
        string? healthCmd = null;
        string? healthInterval = null;
        string? healthTimeout = null;
        string? healthRetries = null;
        string? healthStartPeriod = null;
        var tty = false;
        var stdinOpen = false;
        string? image = null;
        var commandTail = new List<string>();
        var pastImage = false;

        string? NextArg(string flag)
        {
            if (i + 1 < tokens.Count)
                return tokens[++i];
            warnings.Add($"'{flag}' expects a value but none was given — ignored.");
            return null;
        }

        for (; i < tokens.Count; i++)
        {
            var tok = tokens[i];
            if (tok.Length == 0)
                continue;

            if (pastImage)
            {
                commandTail.Add(tok);
                continue;
            }

            switch (tok)
            {
                case "-p" or "--publish": { var v = NextArg(tok); if (v is not null) ports.Add(v); break; }
                case "-v" or "--volume": { var v = NextArg(tok); if (v is not null) volumes.Add(v); break; }
                case "--mount": { var v = NextArg(tok); if (v is not null) volumes.Add(ConvertMount(v, warnings)); break; }
                case "-e" or "--env": { var v = NextArg(tok); if (v is not null) envs.Add(v); break; }
                case "--env-file": { var v = NextArg(tok); if (v is not null) envFiles.Add(v); break; }
                case "--name": { var v = NextArg(tok); if (v is not null) name = v; break; }
                case "--restart": { var v = NextArg(tok); if (v is not null) restart = v; break; }
                case "--network": { var v = NextArg(tok); if (v is not null) networks.Add(v); break; }
                case "--hostname": { var v = NextArg(tok); if (v is not null) hostname = v; break; }
                case "-w" or "--workdir": { var v = NextArg(tok); if (v is not null) workdir = v; break; }
                case "-u" or "--user": { var v = NextArg(tok); if (v is not null) user = v; break; }
                case "--entrypoint": { var v = NextArg(tok); if (v is not null) entrypoint = v; break; }
                case "--label": { var v = NextArg(tok); if (v is not null) labels.Add(v); break; }
                case "--add-host": { var v = NextArg(tok); if (v is not null) extraHosts.Add(v); break; }
                case "--device": { var v = NextArg(tok); if (v is not null) devices.Add(v); break; }
                case "--cap-add": { var v = NextArg(tok); if (v is not null) capAdd.Add(v); break; }
                case "--cap-drop": { var v = NextArg(tok); if (v is not null) capDrop.Add(v); break; }
                case "--privileged": privileged = true; break;
                case "-m" or "--memory": { var v = NextArg(tok); if (v is not null) memory = v; break; }
                case "--cpus": { var v = NextArg(tok); if (v is not null) cpus = v; break; }
                case "--health-cmd": { var v = NextArg(tok); if (v is not null) healthCmd = v; break; }
                case "--health-interval": { var v = NextArg(tok); if (v is not null) healthInterval = v; break; }
                case "--health-timeout": { var v = NextArg(tok); if (v is not null) healthTimeout = v; break; }
                case "--health-retries": { var v = NextArg(tok); if (v is not null) healthRetries = v; break; }
                case "--health-start-period": { var v = NextArg(tok); if (v is not null) healthStartPeriod = v; break; }
                case "-it" or "-ti" or "-ti=true": tty = true; stdinOpen = true; break;
                case "-i" or "--interactive": stdinOpen = true; break;
                case "-t" or "--tty": tty = true; break;
                case "--rm":
                    warnings.Add("--rm has no Compose equivalent for a single service — omitted.");
                    break;
                case "-d" or "--detach":
                    warnings.Add("-d/--detach is Compose's default run mode (`docker compose up -d`) — omitted.");
                    break;
                default:
                    if (tok.Length > 1 && tok[0] == '-')
                        warnings.Add($"Unrecognized flag '{tok}' ignored.");
                    else
                    {
                        image = tok;
                        pastImage = true;
                    }
                    break;
            }
        }

        if (image is null)
            throw new FormatException("No image found in the command.");

        var serviceName = name ?? DeriveServiceName(image);
        var service = new Dictionary<string, object?> { ["image"] = image };

        if (name is not null) service["container_name"] = name;
        if (ports.Count > 0) service["ports"] = ports.Cast<object?>().ToList();
        if (volumes.Count > 0) service["volumes"] = volumes.Cast<object?>().ToList();
        if (envs.Count > 0) service["environment"] = envs.Cast<object?>().ToList();
        if (envFiles.Count > 0) service["env_file"] = envFiles.Cast<object?>().ToList();
        if (restart is not null) service["restart"] = restart;
        if (networks.Count > 0) service["networks"] = networks.Cast<object?>().ToList();
        if (hostname is not null) service["hostname"] = hostname;
        if (workdir is not null) service["working_dir"] = workdir;
        if (user is not null) service["user"] = user;
        if (entrypoint is not null) service["entrypoint"] = entrypoint;
        if (commandTail.Count > 0) service["command"] = commandTail.Cast<object?>().ToList();

        if (labels.Count > 0)
        {
            var labelMap = new Dictionary<string, object?>();
            foreach (var l in labels)
            {
                var eq = l.IndexOf('=');
                if (eq < 0) labelMap[l] = "";
                else labelMap[l[..eq]] = l[(eq + 1)..];
            }
            service["labels"] = labelMap;
        }

        if (extraHosts.Count > 0) service["extra_hosts"] = extraHosts.Cast<object?>().ToList();
        if (devices.Count > 0) service["devices"] = devices.Cast<object?>().ToList();
        if (capAdd.Count > 0) service["cap_add"] = capAdd.Cast<object?>().ToList();
        if (capDrop.Count > 0) service["cap_drop"] = capDrop.Cast<object?>().ToList();
        if (privileged) service["privileged"] = true;

        if (memory is not null || cpus is not null)
        {
            var limits = new Dictionary<string, object?>();
            if (memory is not null) limits["memory"] = memory;
            if (cpus is not null) limits["cpus"] = cpus;
            service["deploy"] = new Dictionary<string, object?>
            {
                ["resources"] = new Dictionary<string, object?> { ["limits"] = limits },
            };
        }

        if (healthCmd is not null || healthInterval is not null || healthTimeout is not null
            || healthRetries is not null || healthStartPeriod is not null)
        {
            var health = new Dictionary<string, object?>();
            if (healthCmd is not null) health["test"] = new List<object?> { "CMD-SHELL", healthCmd };
            if (healthInterval is not null) health["interval"] = healthInterval;
            if (healthTimeout is not null) health["timeout"] = healthTimeout;
            if (healthRetries is not null)
                health["retries"] = int.TryParse(healthRetries, NumberStyles.Integer, CultureInfo.InvariantCulture, out var retries)
                    ? retries
                    : healthRetries;
            if (healthStartPeriod is not null) health["start_period"] = healthStartPeriod;
            service["healthcheck"] = health;
        }

        if (tty) service["tty"] = true;
        if (stdinOpen) service["stdin_open"] = true;

        var root = new Dictionary<string, object?>
        {
            ["services"] = new Dictionary<string, object?> { [serviceName] = service },
        };

        var yamlText = YamlSerializing.Create().Serialize(root).TrimEnd('\r', '\n');

        if (warnings.Count > 0)
            yamlText += "\n" + string.Join("\n", warnings.Select(w => "# " + w));

        return yamlText + "\n";
    }

    /// <summary>Emits one <c>docker run</c> line per Compose service. When <paramref name="multiline"/>
    /// is true (the default), each command wraps with <c>\</c> line continuations for readability.</summary>
    /// <exception cref="FormatException">The YAML is malformed, or has no top-level 'services:' mapping.</exception>
    public static string ComposeToRun(string yaml, bool multiline = true)
    {
        var stream = YamlParsing.ParseOrThrow(yaml);
        if (stream.Documents.Count == 0)
            throw new FormatException("The YAML document is empty.");

        if (YamlGraphHelper.ToGraph(stream.Documents[0].RootNode) is not Dictionary<string, object?> graph)
            throw new FormatException("Expected a mapping at the document root (a Compose file starts with 'services:').");

        if (graph.TryGetValue("services", out var servicesObj)
            && servicesObj is Dictionary<string, object?> services && services.Count > 0)
        {
            var blocks = services.Select(kv =>
                BuildRunCommand(kv.Key, kv.Value as Dictionary<string, object?> ?? new(), multiline));
            return string.Join("\n\n", blocks) + "\n";
        }

        throw new FormatException("No services found — expected a top-level 'services:' mapping.");
    }

    private static string BuildRunCommand(string serviceName, Dictionary<string, object?> svc, bool multiline)
    {
        var flags = new List<string>();

        if (svc.TryGetValue("container_name", out var cn) && cn is string cnStr)
            flags.Add($"--name {ShellQuote(cnStr)}");
        else
            flags.Add($"--name {ShellQuote(serviceName)}");

        foreach (var v in AsStringList(svc, "ports")) flags.Add($"-p {ShellQuote(v)}");
        foreach (var v in AsStringList(svc, "volumes")) flags.Add($"-v {ShellQuote(v)}");

        if (svc.TryGetValue("environment", out var envObj))
            foreach (var (k, v) in AsEnvironmentPairs(envObj))
                flags.Add(v is null ? $"-e {ShellQuote(k)}" : $"-e {ShellQuote($"{k}={v}")}");

        foreach (var f in AsStringList(svc, "env_file")) flags.Add($"--env-file {ShellQuote(f)}");

        if (svc.TryGetValue("restart", out var restart) && restart is not null)
            flags.Add($"--restart {ShellQuote(restart.ToString() ?? "")}");

        foreach (var n in AsStringList(svc, "networks")) flags.Add($"--network {ShellQuote(n)}");

        if (svc.TryGetValue("hostname", out var hn) && hn is string hs) flags.Add($"--hostname {ShellQuote(hs)}");
        if (svc.TryGetValue("working_dir", out var wd) && wd is string ws) flags.Add($"-w {ShellQuote(ws)}");
        if (svc.TryGetValue("user", out var us) && us is string userStr) flags.Add($"-u {ShellQuote(userStr)}");
        if (svc.TryGetValue("entrypoint", out var ep) && ep is not null)
            flags.Add($"--entrypoint {ShellQuote(EntrypointToString(ep))}");

        if (svc.TryGetValue("labels", out var labelsObj))
            foreach (var (k, v) in AsLabelPairs(labelsObj))
                flags.Add($"--label {ShellQuote(v.Length == 0 ? k : $"{k}={v}")}");

        foreach (var h in AsStringList(svc, "extra_hosts")) flags.Add($"--add-host {ShellQuote(h)}");
        foreach (var d in AsStringList(svc, "devices")) flags.Add($"--device {ShellQuote(d)}");
        foreach (var c in AsStringList(svc, "cap_add")) flags.Add($"--cap-add {ShellQuote(c)}");
        foreach (var c in AsStringList(svc, "cap_drop")) flags.Add($"--cap-drop {ShellQuote(c)}");

        if (svc.TryGetValue("privileged", out var priv) && priv is true) flags.Add("--privileged");

        if (svc.TryGetValue("deploy", out var deployObj) && deployObj is Dictionary<string, object?> deploy
            && deploy.TryGetValue("resources", out var resObj) && resObj is Dictionary<string, object?> res
            && res.TryGetValue("limits", out var limObj) && limObj is Dictionary<string, object?> lim)
        {
            if (lim.TryGetValue("memory", out var mem) && mem is not null)
                flags.Add($"--memory {ShellQuote(mem.ToString() ?? "")}");
            if (lim.TryGetValue("cpus", out var cpuVal) && cpuVal is not null)
                flags.Add($"--cpus {ShellQuote(cpuVal.ToString() ?? "")}");
        }

        if (svc.TryGetValue("healthcheck", out var hcObj) && hcObj is Dictionary<string, object?> hc)
        {
            if (hc.TryGetValue("test", out var testObj) && testObj is not null)
                flags.Add($"--health-cmd {ShellQuote(HealthTestToString(testObj))}");
            if (hc.TryGetValue("interval", out var iv) && iv is not null)
                flags.Add($"--health-interval {ShellQuote(iv.ToString() ?? "")}");
            if (hc.TryGetValue("timeout", out var to) && to is not null)
                flags.Add($"--health-timeout {ShellQuote(to.ToString() ?? "")}");
            if (hc.TryGetValue("retries", out var rt) && rt is not null)
                flags.Add($"--health-retries {ShellQuote(rt.ToString() ?? "")}");
            if (hc.TryGetValue("start_period", out var sp) && sp is not null)
                flags.Add($"--health-start-period {ShellQuote(sp.ToString() ?? "")}");
        }

        var tty = svc.TryGetValue("tty", out var ttyObj) && ttyObj is true;
        var stdinOpen = svc.TryGetValue("stdin_open", out var stdinObj) && stdinObj is true;
        if (tty && stdinOpen) flags.Add("-it");
        else if (tty) flags.Add("-t");
        else if (stdinOpen) flags.Add("-i");

        var image = svc.TryGetValue("image", out var imgObj) && imgObj is string imgStr ? imgStr : serviceName;
        var tail = svc.TryGetValue("command", out var cmdObj) && cmdObj is not null
            ? " " + CommandToString(cmdObj)
            : "";

        if (!multiline)
            return $"docker run {string.Join(" ", flags)} {ShellQuote(image)}{tail}".TrimEnd();

        var lines = new List<string> { "docker run" };
        lines.AddRange(flags);
        lines.Add(ShellQuote(image) + tail);
        return string.Join(" \\\n  ", lines);
    }

    private static string ConvertMount(string raw, List<string> warnings)
    {
        string? type = null, source = null, target = null;
        var readOnly = false;
        foreach (var part in raw.Split(','))
        {
            var eq = part.IndexOf('=');
            var key = (eq < 0 ? part : part[..eq]).Trim();
            var value = eq < 0 ? "" : part[(eq + 1)..].Trim();
            switch (key)
            {
                case "type": type = value; break;
                case "source" or "src": source = value; break;
                case "target" or "dst" or "destination": target = value; break;
                case "readonly" or "ro": readOnly = value.Length == 0 || value.Equals("true", StringComparison.OrdinalIgnoreCase); break;
            }
        }
        _ = type;

        if (target is null)
        {
            warnings.Add($"--mount '{raw}' has no target — ignored.");
            return raw;
        }
        if (source is null)
            return readOnly ? $"{target}:ro" : target;
        return readOnly ? $"{source}:{target}:ro" : $"{source}:{target}";
    }

    /// <summary>Derives a Compose service name from an image reference when <c>--name</c> wasn't
    /// given: the last path segment holds "name[:tag]" regardless of any registry host/port
    /// prefix earlier in the path (e.g. "registry.example.com:5000/team/api:1.2.3" -&gt; "api"),
    /// so stripping the digest and tag from the final segment is all that's needed.</summary>
    private static string DeriveServiceName(string image)
    {
        var atIdx = image.IndexOf('@');
        var withoutDigest = atIdx >= 0 ? image[..atIdx] : image;

        var lastSegment = withoutDigest.Split('/')[^1];
        var colonIdx = lastSegment.IndexOf(':');
        var name = colonIdx >= 0 ? lastSegment[..colonIdx] : lastSegment;
        return name.Length > 0 ? name : "app";
    }

    private static IEnumerable<string> AsStringList(Dictionary<string, object?> svc, string key)
    {
        if (svc.TryGetValue(key, out var v) && v is List<object?> list)
            foreach (var item in list)
                if (item is not null)
                    yield return item.ToString() ?? "";
    }

    private static IEnumerable<(string Key, string? Value)> AsEnvironmentPairs(object? envObj)
    {
        switch (envObj)
        {
            case List<object?> list:
                foreach (var item in list)
                {
                    var s = item?.ToString() ?? "";
                    var eq = s.IndexOf('=');
                    yield return eq < 0 ? (s, null) : (s[..eq], s[(eq + 1)..]);
                }
                break;
            case Dictionary<string, object?> map:
                foreach (var kv in map)
                    yield return (kv.Key, kv.Value?.ToString());
                break;
        }
    }

    private static IEnumerable<(string Key, string Value)> AsLabelPairs(object? labelsObj)
    {
        switch (labelsObj)
        {
            case List<object?> list:
                foreach (var item in list)
                {
                    var s = item?.ToString() ?? "";
                    var eq = s.IndexOf('=');
                    yield return eq < 0 ? (s, "") : (s[..eq], s[(eq + 1)..]);
                }
                break;
            case Dictionary<string, object?> map:
                foreach (var kv in map)
                    yield return (kv.Key, kv.Value?.ToString() ?? "");
                break;
        }
    }

    private static string EntrypointToString(object ep) => ep switch
    {
        string s => s,
        List<object?> list => string.Join(" ", list.Select(x => x?.ToString() ?? "")),
        _ => ep.ToString() ?? "",
    };

    private static string HealthTestToString(object test) => test switch
    {
        string s => s,
        List<object?> list => string.Join(" ", list.Select(x => x?.ToString() ?? "")
            .Where(s => !s.Equals("CMD-SHELL", StringComparison.OrdinalIgnoreCase) && !s.Equals("CMD", StringComparison.OrdinalIgnoreCase))),
        _ => test.ToString() ?? "",
    };

    private static string CommandToString(object cmdObj)
    {
        var parts = cmdObj switch
        {
            string s => ShellTokenizer.Tokenize(s),
            List<object?> list => list.Select(x => x?.ToString() ?? "").ToList(),
            _ => [],
        };
        return string.Join(" ", parts.Select(ShellQuote));
    }

    private static string ShellQuote(string s)
    {
        if (s.Length > 0 && s.All(c => char.IsLetterOrDigit(c) || c is '.' or '/' or '-' or '_' or ':' or '=' or '@'))
            return s;
        return "'" + s.Replace("'", "'\\''") + "'";
    }
}
