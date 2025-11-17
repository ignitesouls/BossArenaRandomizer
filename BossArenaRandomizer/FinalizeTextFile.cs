using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows;

namespace BossArenaRandomizer
{
    public static class FinalizeTextFile
    {
        public static void WriteFinalAssignments(
            Dictionary<string, string> finalAssignments,
            Dictionary<string, ArenaInfo> arenas,
            Dictionary<string, BossInfo> bosses,
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
                string clearArenaAnimal = "2832488"; // Springhare
                foreach (var extraId in HCFilterIds.ClearArenasIds)
                    enemiesBlock.AppendLine($"    {extraId}: {clearArenaAnimal}");
            }

            foreach (var kvp in finalAssignments)
            {
                string arenaId = arenas[kvp.Key].id;
                string bossId = bosses[kvp.Value].id;
                enemiesBlock.AppendLine($"    {arenaId}: {bossId}");
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
    /*public static class FinalizeTextFile
    {
        public static void WriteFinalAssignments(
            Dictionary<string, string> finalAssignments, 
            Dictionary<string, ArenaInfo> arenas, 
            Dictionary<string, BossInfo> bosses, 
            string filePath, string selectedOptionsFilePath, 
            int seed, 
            bool includeClearArenas = false)
        {

            if (!File.Exists(selectedOptionsFilePath))
            {
                throw new FileNotFoundException("Options file not found", selectedOptionsFilePath);
            }

            var optionsLines = File.ReadAllLines(selectedOptionsFilePath).ToList();

            // Update seed line and Create Enemies Block Correctly if Preset in not selected
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

                    if (!optionsLines[i].Contains("--preset"))
                    {
                        optionsLines[i] += " --preset BAR";
                    }

                    // Change the next line to "EnemyPreset: >+" if applicable
                    if (i + 1 < optionsLines.Count && optionsLines[i + 1].TrimStart().StartsWith("EnemyPreset:"))
                    {
                        optionsLines[i + 1] = "EnemyPreset: >+";

                        var classBlock = new List<string>
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
                            "  Options: bosshp regularhp v4"
                        };

                        optionsLines.InsertRange(i + 2, classBlock);

                        //Build Enemies Block
                        var newEnemiesBlock = new List<string> { "  Enemies:" };
                        
                        if(includeClearArenas)
                        {
                            string clearArenaAnimal = "2832488"; //Spinghare
                            foreach (var extraId in HCFilterIds.ClearArenasIds)
                            {
                                newEnemiesBlock.Add($"    {extraId}: {clearArenaAnimal}");
                            }
                        }

                        foreach (var kvp in finalAssignments)
                        {
                            string arenaId = arenas[kvp.Key].id;
                            string bossId = bosses[kvp.Value].id;

                            newEnemiesBlock.Add($"    {arenaId}: {bossId}");
                        }

                        // Find the last inserted line ("  Options: bosshp regularhp v4")
                        int insertEnemiesAfter = i + 2 + classBlock.Count - 1;
                        optionsLines.InsertRange(insertEnemiesAfter + 1, new[] { "", }.Concat(newEnemiesBlock));
                    }

                    seedFound = true;
                    break;
                }
            }

            if (!seedFound)
            {
                optionsLines.Add($"seed: {seed}");
            }

            // Build new Enemies block
            var enemiesBlock = new StringBuilder();
            enemiesBlock.AppendLine("  Enemies:");

            if (includeClearArenas)
            {
                string clearArenaAnimal = "2832488"; //Springhare
                foreach (var extraId in HCFilterIds.ClearArenasIds)
                {
                    enemiesBlock.AppendLine($"    {extraId}: {clearArenaAnimal}");
                }
            }

            foreach (var kvp in finalAssignments)
            {
                string arenaId = arenas[kvp.Key].id;
                string bossId = bosses[kvp.Value].id;
                enemiesBlock.AppendLine($"    {arenaId}: {bossId}");
            }

            // Rebuild final output with updated seed and Enemies block
            var finalOutput = new StringBuilder();
            bool insideEnemiesBlock = false;
            bool enemiesInserted = false;

            foreach (var line in optionsLines)
            {
                if (line.TrimStart().StartsWith("Enemies:"))
                {
                    // Insert new enemies block
                    finalOutput.AppendLine(enemiesBlock.ToString().TrimEnd());
                    insideEnemiesBlock = true;
                    enemiesInserted = true;
                    continue;
                }

                if (insideEnemiesBlock)
                {
                    if (line.StartsWith("  ") || line.StartsWith("    "))
                    {
                        // Skip old enemies block lines
                        continue;
                    }
                    else
                    {
                        insideEnemiesBlock = false;
                    }
                }

                finalOutput.AppendLine(line);
            }

            // If no enemies block existed, add it at the end
            if (!enemiesInserted)
            {
                finalOutput.AppendLine();
                finalOutput.AppendLine(enemiesBlock.ToString().TrimEnd());
            }

            File.WriteAllText(filePath, finalOutput.ToString());
        }
    }*/
}
