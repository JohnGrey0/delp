using System.Text;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>Composes selected .gitignore templates into a single merged file.</summary>
public static class GitignoreTool
{
    /// <summary>
    /// Merges the named templates (in the given order) into one .gitignore file. Each template gets a
    /// <c># --- Name ---</c> section header; a non-comment pattern line already emitted by an earlier
    /// section is dropped from later sections (first occurrence wins), and the result never has
    /// leading/trailing/duplicate blank lines.
    /// </summary>
    /// <exception cref="ArgumentException">A name doesn't match any known template.</exception>
    public static string Compose(IReadOnlyList<string> names)
    {
        if (names is null || names.Count == 0)
            return "";

        var byName = GitignoreData.All.ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);
        var seenPatterns = new HashSet<string>(StringComparer.Ordinal);
        var sections = new List<string>();

        foreach (var name in names)
        {
            if (!byName.TryGetValue(name, out var template))
                throw new ArgumentException($"Unknown gitignore template '{name}'.", nameof(names));

            var body = new List<string>();
            foreach (var rawLine in template.Content.Replace("\r\n", "\n").Split('\n'))
            {
                var line = rawLine.TrimEnd();
                var trimmed = line.TrimStart();
                var isPattern = trimmed.Length > 0 && !trimmed.StartsWith('#');

                // Duplicate non-comment pattern already contributed by an earlier section: drop it here
                // so the merged file doesn't repeat the same rule (comments are left alone — they're
                // documentation, not rules, and different sections' comments can legitimately repeat
                // words like "# Logs" without meaning the same thing).
                if (isPattern && !seenPatterns.Add(trimmed))
                    continue;

                body.Add(line);
            }

            TrimBlankEdges(body);

            var section = new StringBuilder();
            section.Append("# --- ").Append(name).Append(" ---");
            foreach (var line in body)
                section.Append('\n').Append(line);

            sections.Add(section.ToString());
        }

        return string.Join("\n\n", sections) + "\n";
    }

    private static void TrimBlankEdges(List<string> lines)
    {
        while (lines.Count > 0 && lines[0].Length == 0)
            lines.RemoveAt(0);
        while (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);
    }
}
