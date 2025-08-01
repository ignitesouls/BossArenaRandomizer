﻿using System;
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
            string filePath, string selectedOptionsFilePath, 
            int seed, 
            bool includeBetterArenas = false)
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
                        
                        if(includeBetterArenas)
                        {
                            string fixedBossId = "2801984";
                            foreach (var extraId in HCFilterIds.BetterArenasIds)
                            {
                                newEnemiesBlock.Add($"    {extraId}: {fixedBossId}");
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

            if (includeBetterArenas)
            {
                string fixedBossId = "2801984";
                foreach (var extraId in HCFilterIds.BetterArenasIds)
                {
                    enemiesBlock.AppendLine($"    {extraId}: {fixedBossId}");
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
    }
}
