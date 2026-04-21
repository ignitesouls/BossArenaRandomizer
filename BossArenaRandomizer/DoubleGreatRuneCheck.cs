using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossArenaRandomizer
{
    public sealed class DoubleGreatRuneCheck : ISeedCheck
    {
        public string Id => "double-gr";
        public string DisplayName => "Double Great Runes";
        public string Description => "Detects if any location grants 2+ Great Runes in the spoiler log.";

        // Great runes that count
        private static readonly HashSet<string> GreatRunes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Godrick's Great Rune",
            "Malenia's Great Rune",
            "Mohg's Great Rune",
            "Radahn's Great Rune",
            "Rykard's Great Rune",
            "Morgott's Great Rune",
            "Great Rune of the Unborn",
        };

        // A “rule” = the location token to split on + patterns that qualify a line as relevant.
        private sealed record Rule(string Name, string LocationToken, string[] Patterns);

        // Data-driven rules (easy to add more later)
        private static readonly Rule[] Rules =
        {
            new Rule(
                Name: "Mohg (Mohgwyn)",
                LocationToken: "in Mohgwyn",
                Patterns: new[]
                {
                    "in Mohgwyn: Dropped by Mohg, Lord of Blood. Replaces Mohg's Great Rune.",
                    "in Mohgwyn: Dropped by Mohg, Lord of Blood. Replaces Remembrance of the Blood Lord."
                }
            ),
            new Rule(
                Name: "Rennala (Raya Lucaria)",
                LocationToken: "in Academy of Raya Lucaria",
                Patterns: new[]
                {
                    "in Academy of Raya Lucaria: Dropped by Rennala. Replaces Great Rune of the Unborn.",
                    "in Academy of Raya Lucaria: Dropped by Rennala. Replaces Remembrance of the Full Moon Queen."
                }
            ),
            new Rule(
                Name: "Godrick (Stormveil)",
                LocationToken: "in Stormveil Castle",
                Patterns: new[]
                {
                    "in Stormveil Castle: Dropped by Godrick the Grafted. Replaces Godrick's Great Rune.",
                    "in Stormveil Castle: Dropped by Godrick the Grafted. Replaces Remembrance of the Grafted."
                }
            ),
            new Rule(
                Name: "Radahn (Caelid)",
                LocationToken: "in Caelid",
                Patterns: new[]
                {
                    "in Caelid: Dropped by Starscourge Radahn. Replaces Radahn's Great Rune.",
                    "in Caelid: Dropped by Starscourge Radahn. Replaces Remembrance of the Starscourge."
                }
            ),
            new Rule(
                Name: "Rykard (Volcano Manor)",
                LocationToken: "in Volcano Manor",
                Patterns: new[]
                {
                    "in Volcano Manor: Dropped by Rykard. Replaces Rykard's Great Rune.",
                    "in Volcano Manor: Dropped by Rykard. Replaces Remembrance of the Blasphemous."
                }
            ),
            // Note: Morgott+Rold are combined like your python logic (both are Leyndell-related)
            new Rule(
                Name: "Morgott (Leyndell)",
                LocationToken: "in Leyndell",
                Patterns: new[]
                {
                    "in Leyndell: Dropped by Morgott the Omen King. Replaces Morgott's Great Rune.",
                    "in Leyndell: Dropped by Morgott the Omen King. Replaces Remembrance of the Omen King."
                }
            ),
            new Rule(
                Name: "Rold Medallion (Leyndell)",
                LocationToken: "in Leyndell",
                Patterns: new[]
                {
                    "in Leyndell: Given by Melina after defeating Morgott. Replaces Rold Medallion."
                }
            ),
            new Rule(
                Name: "Malenia (Haligtree)",
                LocationToken: "in Haligtree",
                Patterns: new[]
                {
                    "in Haligtree: Dropped by Malenia. Replaces Malenia's Great Rune.",
                    "in Haligtree: Dropped by Malenia. Replaces Remembrance of the Rot Goddess."
                }
            )
        };

        public SeedCheckResult Run(string seedText)
        {
            // Split once (handles Windows/Unix newlines)
            var lines = seedText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToArray();

            // Count great runes per “group”
            // We want Morgott+Rold combined result like your Python:
            // if (morgott + rold > 1) => flag
            var countsByRule = new Dictionary<string, (int Count, HashSet<string> RuneNames)>(StringComparer.OrdinalIgnoreCase);

            foreach (var rule in Rules)
            {
                var (count, runes) = CountGreatRunesForRule(lines, rule);
                countsByRule[rule.Name] = (count, runes);
            }

            // Determine failures (double great rune)
            var failures = new List<string>();

            // Simple ones: Mohg, Rennala, Godrick, Radahn, Rykard, Malenia
            foreach (var ruleName in new[]
            {
                "Mohg (Mohgwyn)",
                "Rennala (Raya Lucaria)",
                "Godrick (Stormveil)",
                "Radahn (Caelid)",
                "Rykard (Volcano Manor)",
                "Malenia (Haligtree)",
            })
            {
                var entry = countsByRule[ruleName];
                if (entry.Count > 1)
                {
                    failures.Add($"{ruleName}: {entry.Count} Great Runes ({string.Join(", ", entry.RuneNames)})");
                }
            }

            // Combined: Morgott + Rold
            var morgott = countsByRule["Morgott (Leyndell)"];
            var rold = countsByRule["Rold Medallion (Leyndell)"];

            var combinedCount = morgott.Count + rold.Count;
            var combinedRunes = morgott.RuneNames.Concat(rold.RuneNames).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (combinedCount > 1)
            {
                failures.Add($"Leyndell (Morgott + Rold): {combinedCount} Great Runes ({string.Join(", ", combinedRunes)})");
            }

            bool passed = failures.Count == 0;

            return new SeedCheckResult
            {
                CheckId = Id,
                Passed = passed,
                Message = passed
                    ? "✅ No double Great Runes found."
                    : "❌ Double Great Rune detected:\n" + string.Join("\n", failures)
            };
        }

        private static (int Count, HashSet<string> RuneNames) CountGreatRunesForRule(string[] lines, Rule rule)
        {
            int count = 0;
            var runesFound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines)
            {
                // match any pattern
                bool matches = false;
                foreach (var pattern in rule.Patterns)
                {
                    if (line.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        matches = true;
                        break;
                    }
                }

                if (!matches) continue;

                // Python does: item_name = line.split(location)[0].strip()
                // We'll do the same: take substring before "in X"
                int idx = line.IndexOf(rule.LocationToken, StringComparison.OrdinalIgnoreCase);
                if (idx <= 0) continue;

                string itemName = line.Substring(0, idx).Trim();

                if (GreatRunes.Contains(itemName))
                {
                    count++;
                    runesFound.Add(itemName);
                }
            }

            return (count, runesFound);
        }
    }
}
