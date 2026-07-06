using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows;

namespace BossArenaRandomizer.Core
{
    public static class FinalizeTextFile
    {
        public static void WriteFinalAssignments(
            IReadOnlyCollection<AssignmentPair> finalAssignments,
            string filePath,
            string selectedOptionsFilePath,
            int seed,
            bool includeClearArenas = false)
        {
            WriteFinalAssignments(
                finalAssignments.Select(assignment => (assignment.ArenaId.Value, assignment.BossId.Value)),
                filePath,
                selectedOptionsFilePath,
                seed,
                includeClearArenas);
        }

        public static void WriteFinalAssignments(
            Dictionary<string, string> finalAssignments,
            Dictionary<string, ArenaInfo> arenas,
            Dictionary<string, BossInfo> bosses,
            string filePath,
            string selectedOptionsFilePath,
            int seed,
            bool includeClearArenas = false)
        {
            var assignmentIds = finalAssignments.Select(kvp => (arenas[kvp.Key].id, bosses[kvp.Value].id));
            WriteFinalAssignments(
                assignmentIds,
                filePath,
                selectedOptionsFilePath,
                seed,
                includeClearArenas);
        }

        private static void WriteFinalAssignments(
            IEnumerable<(string ArenaId, string BossId)> finalAssignments,
            string filePath,
            string selectedOptionsFilePath,
            int seed,
            bool includeClearArenas = false)
        {
            if (!File.Exists(selectedOptionsFilePath))
                throw new FileNotFoundException("Options file not found", selectedOptionsFilePath);

            var optionsLines = File.ReadAllLines(selectedOptionsFilePath).ToList();

            // Update the seed line and check for preset 
            bool seedFound = false;
            for (int i = 0; i < optionsLines.Count; i++)
            {
                if (optionsLines[i].Contains("seed:"))
                {
                    optionsLines[i] = Regex.Replace(
                        optionsLines[i],
                        @"seed:\s*\d+",
                        $"seed:{seed}"
                    );

                    // Add --preset BAR if missing
                    if (!optionsLines[i].Contains("--preset"))
                        optionsLines[i] += " --preset BAR";

                    seedFound = true;
                    break;
                }
            }

            // If no seed found, append it with preset
            if (!seedFound)
                optionsLines.Add($"seed:{seed} --preset BAR");

            // Build Enemies block 
            var enemiesBlock = new StringBuilder();
            enemiesBlock.AppendLine("  Enemies:");

            if (includeClearArenas)
            {
                string clearArenaAnimal = "2822374"; // Springhare
                foreach (var extraId in HCFilterIds.ClearArenasIds)
                    enemiesBlock.AppendLine($"    {extraId}: {clearArenaAnimal}");
            }

            foreach (var kvp in finalAssignments)
            {
                enemiesBlock.AppendLine($"    {kvp.ArenaId}: {kvp.BossId}");
            }

            //  Locate key blocks 
            int enemyPresetIndex = optionsLines.FindIndex(l => l.TrimStart().StartsWith("EnemyPreset: >+"));
            int emptyEnemyPresetIndex = optionsLines.FindIndex(l => Regex.IsMatch(l.Trim(), @"^EnemyPreset:\s*$"));
            int enemiesIndex = optionsLines.FindIndex(l => l.TrimStart().StartsWith("Enemies:"));

            //  Replace existing Enemies block if found 
            if (enemiesIndex != -1)
            {
                int start = enemiesIndex;
                int end = start + 1;

                // Remove all indented lines following "Enemies:"
                while (end < optionsLines.Count && (optionsLines[end].StartsWith("  ") || optionsLines[end].StartsWith("    ")))
                    end++;

                // Remove the old block
                optionsLines.RemoveRange(start, end - start);

                // Insert new block in the same place
                optionsLines.InsertRange(start, enemiesBlock.ToString().TrimEnd().Split('\n'));
            }
            else if (enemyPresetIndex != -1)
            {
                // EnemyPreset: >+ exists but no Enemies block — insert after it 
                optionsLines.InsertRange(enemyPresetIndex + 1, enemiesBlock.ToString().TrimEnd().Split('\n'));
            }
            else if (emptyEnemyPresetIndex != -1)
            {
                // EnemyPreset: (empty) exists — replace with >+ and insert full section 
                optionsLines[emptyEnemyPresetIndex] = "EnemyPreset: >+";

                var insertion = new List<string>();
                insertion.AddRange(enemiesBlock.ToString().TrimEnd().Split('\n'));
                insertion.AddRange(new[]
                {
                    "  Classes:",
                    "    Basic: {}",
                    "    Boss: {}",
                    "    MinorBoss:",
                    "      InheritParent: true",
                    "    Miniboss:",
                    "      InheritParent: true",
                    "    NightMiniboss:",
                    "      InheritParent: true",
                    "    DragonMiniboss:",
                    "      InheritParent: true",
                    "    Evergaol:",
                    "      InheritParent: true",
                    "    Wildlife:",
                    "      InheritParent: true",
                    "    HostileNPC: {}",
                    "    Scarab: {}",
                    "  Options: bosshp regularhp v4",
                    ""
                });

                optionsLines.InsertRange(emptyEnemyPresetIndex + 1, insertion);
            }
            else
            {
                // No EnemyPreset at all — insert full preset after seed line 
                int seedIndex = optionsLines.FindIndex(l => l.Contains("seed:"));
                if (seedIndex == -1) seedIndex = optionsLines.Count - 1;

                var insertion = new List<string>
                {
                    "EnemyPreset: >+"
                };
                insertion.AddRange(enemiesBlock.ToString().TrimEnd().Split('\n'));
                insertion.AddRange(new[]
                {
                    "  Classes:",
                    "    Basic: {}",
                    "    Boss: {}",
                    "    MinorBoss:",
                    "      InheritParent: true",
                    "    Miniboss:",
                    "      InheritParent: true",
                    "    NightMiniboss:",
                    "      InheritParent: true",
                    "    DragonMiniboss:",
                    "      InheritParent: true",
                    "    Evergaol:",
                    "      InheritParent: true",
                    "    Wildlife:",
                    "      InheritParent: true",
                    "    HostileNPC: {}",
                    "    Scarab: {}",
                    "  Options: bosshp regularhp v4",
                    ""
                });

                optionsLines.InsertRange(seedIndex + 1, insertion);
            }

            // Write final file 
            File.WriteAllText(filePath, string.Join(Environment.NewLine, optionsLines));
        }
    }
}
