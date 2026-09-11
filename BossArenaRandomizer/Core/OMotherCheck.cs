using System;
using System.Collections.Generic;
using System.Linq;

namespace BossArenaRandomizer.Core
{
    public sealed class OMotherCheck : ISeedCheck
    {
        private readonly IReadOnlyList<string> _invalidPhrases;

        public string Id => "seed-validity";
        public string DisplayName => "O Mother";
        public string Description => "Fails if the spoiler log contains any phrase from the editable O Mother line list.";

        public static IReadOnlyList<string> DefaultInvalidPhrases { get; } = new[]
        {
            "O Mother in Jagged Peak: Dropped by Bayle the Dread. Replaces Heart of Bayle.",
            "O Mother in Jagged Peak: Dropped by Ancient Dragon Senessax. Replaces Ancient Dragon Smithing Stone.",
            "O Mother in Scadu Altus - Darklight Catacombs: Dropped by Jori, Elder Inquisitor. Replaces Barbed Staff-Spear.",
            "O Mother in Midra's Manse: Dropped by Midra, Lord of Frenzied Flame. Replaces Remembrance of the Lord of Frenzied Flame.",
            "O Mother in Specimen Storehouse: Dropped by Messmer the Impaler. Replaces Remembrance of the Impaler.",
            "O Mother in Scaduview: Dropped by Commander Gaius. Replaces Remembrance of the Wild Boar Rider.",
            "O Mother in Stone Coffin Fissure: Dropped by Putrescent Knight. Replaces Remembrance of Putrescence."
        };

        public OMotherCheck()
            : this(DefaultInvalidPhrases)
        {
        }

        public OMotherCheck(IEnumerable<string> invalidPhrases)
        {
            _invalidPhrases = invalidPhrases?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }

        public SeedCheckResult Run(string seedText)
        {
            string? match = _invalidPhrases.FirstOrDefault(
                phrase => seedText.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0);

            bool passed = match == null;
            return new SeedCheckResult
            {
                CheckId = Id,
                Passed = passed,
                Message = passed
                    ? $"Seed Valid (none of the {_invalidPhrases.Count} invalid lines were found)."
                    : $"Seed Invalid\nMatched phrase:\n{match}"
            };
        }
    }
}
