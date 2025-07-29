using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

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

            // Update seed line
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
