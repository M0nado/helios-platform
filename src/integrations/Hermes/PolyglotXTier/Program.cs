using System.Text.Json;
using System.Text.RegularExpressions;

namespace PolyglotXTier;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "--host-map")
        {
            var host = new IntegrationHost();
            host.WriteSystemMap(args[1]);
            Console.WriteLine($"Wrote host map: {args[1]}");
            return 0;
        }

        if (args.Length >= 3 && args[0] == "--conversation-map")
        {
            var conversationSource = args[1];
            var conversationOutput = args[2];
            var conversationPayload = BuildConversationMap(conversationSource);
            Directory.CreateDirectory(Path.GetDirectoryName(conversationOutput)!);
            File.WriteAllText(conversationOutput, JsonSerializer.Serialize(conversationPayload, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Wrote conversation map: {conversationOutput}");
            return 0;
        }

        if (args.Length >= 3 && args[0] == "--security-front-end-map")
        {
            var cppReport = args[1];
            var outputPath = args[2];
            var frontend = new SecurityFrontendMap();
            frontend.Write(cppReport, outputPath);
            Console.WriteLine($"Wrote security front-end map: {outputPath}");
            return 0;
        }

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: PolyglotXTier <source.txt> <output.json>");
            return 1;
        }

        var source = args[0];
        var output = args[1];
        var lines = File.ReadAllLines(source);
        var headings = new List<string>();
        var numberedRegex = new Regex(@"^\d+(\.\d+)*\s+[—-]\s+", RegexOptions.Compiled);
        var sectionCount = 0;
        var appendixCount = 0;
        var numberedCount = 0;
        var tableLikeCount = 0;
        var taskMentions = 0;
        var skillMentions = 0;
        var connectorMentions = 0;
        var phaseMentions = 0;
        var modeMentions = 0;
        var autoMentions = 0;
        var selfHealMentions = 0;
        var sectionSymbolCount = 0;
        var sectionSymbolMax = 0;
        var completeMarks = 0;
        var pendingMarks = 0;
        var failedMarks = 0;
        var wordCount = 0;
        var sectionSymbolRegex = new Regex(@"§\s*(\d+)", RegexOptions.Compiled);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            var lower = trimmed.ToLowerInvariant();
            wordCount += lower.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            if (trimmed.StartsWith("Section ", StringComparison.OrdinalIgnoreCase))
            {
                headings.Add(trimmed);
                sectionCount++;
            }
            else if (trimmed.StartsWith("Appendix ", StringComparison.OrdinalIgnoreCase))
            {
                headings.Add(trimmed);
                appendixCount++;
            }
            else if (numberedRegex.IsMatch(trimmed))
            {
                headings.Add(trimmed);
                numberedCount++;
            }

            if (line.Contains("  "))
            {
                tableLikeCount++;
            }

            taskMentions += Regex.Matches(lower, "task").Count;
            skillMentions += Regex.Matches(lower, "skill").Count;
            connectorMentions += Regex.Matches(lower, "connector").Count;
            phaseMentions += Regex.Matches(lower, "phase").Count;
            modeMentions += Regex.Matches(lower, "-mode").Count;
            autoMentions += Regex.Matches(lower, "auto").Count;
            selfHealMentions += Regex.Matches(lower, "self-heal").Count + Regex.Matches(lower, "self heal").Count;
            completeMarks += line.Count(c => c == '✅');
            pendingMarks += line.Count(c => c == '⏳');
            failedMarks += line.Count(c => c == '❌');

            var matches = sectionSymbolRegex.Matches(line);
            foreach (Match m in matches)
            {
                sectionSymbolCount++;
                if (int.TryParse(m.Groups[1].Value, out var n) && n > sectionSymbolMax)
                {
                    sectionSymbolMax = n;
                }
            }
        }

        var payload = new
        {
            source,
            line_count = lines.Length,
            word_count = wordCount,
            heading_count = headings.Count,
            section_count = sectionCount,
            appendix_count = appendixCount,
            numbered_heading_count = numberedCount,
            table_like_line_count = tableLikeCount,
            task_mentions = taskMentions,
            skill_mentions = skillMentions,
            connector_mentions = connectorMentions,
            phase_mentions = phaseMentions,
            mode_mentions = modeMentions,
            auto_mentions = autoMentions,
            self_heal_mentions = selfHealMentions,
            section_symbol_count = sectionSymbolCount,
            section_symbol_max = sectionSymbolMax,
            status_marks = new
            {
                complete = completeMarks,
                pending = pendingMarks,
                failed = failedMarks
            },
            headings
        };

        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(output, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Wrote: {output}");
        return 0;
    }

    private static object BuildConversationMap(string sourcePath)
    {
        var lines = File.ReadAllLines(sourcePath);
        var turns = new List<Dictionary<string, object>>();

        string? currentRole = null;
        var buffer = new List<string>();

        static string[] Tag(string text)
        {
            var tags = new List<string>();
            var lowered = text.ToLowerInvariant();
            void AddIf(string key, params string[] needles)
            {
                if (needles.Any(n => lowered.Contains(n)))
                {
                    tags.Add(key);
                }
            }

            AddIf("winre", "winre", "reagentc", "dism");
            AddIf("vhdx", "vhdx", "dev drive", "bitlocker", "refs");
            AddIf("security", "firewall", "entra", "purview", "quarantine");
            AddIf("networking", "port", "vpn", "proxy", "ethernet", "wifi");
            AddIf("ai_ml", "ai", "ml", "llm", "anomaly");
            AddIf("runtime", "docker", "gui", "api", "web app", "hermes", "aihub");
            AddIf("optimization", "cpu", "ram", "gpu", "compression", "throughput");
            return tags.Distinct().ToArray();
        }

        void Flush()
        {
            if (currentRole is null)
            {
                return;
            }

            var text = string.Join('\n', buffer).Trim();
            if (text.Length > 0)
            {
                turns.Add(
                    new Dictionary<string, object>
                    {
                        ["role"] = currentRole,
                        ["text"] = text,
                        ["tags"] = Tag(text),
                    }
                );
            }

            currentRole = null;
            buffer.Clear();
        }

        foreach (var line in lines)
        {
            if (line == "You said")
            {
                Flush();
                currentRole = "user";
                continue;
            }

            if (line == "Copilot said")
            {
                Flush();
                currentRole = "assistant";
                continue;
            }

            if (currentRole is not null)
            {
                buffer.Add(line);
            }
        }

        Flush();

        var userTurns = turns.Where(t => Equals(t["role"], "user")).ToArray();
        var assistantTurns = turns.Where(t => Equals(t["role"], "assistant")).ToArray();

        return new
        {
            source = sourcePath,
            summary = new
            {
                total_turns = turns.Count,
                user_turns = userTurns.Length,
                assistant_turns = assistantTurns.Length,
                line_count = lines.Length,
            },
            turns,
        };
    }
}
