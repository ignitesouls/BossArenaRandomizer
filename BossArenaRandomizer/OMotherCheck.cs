using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace BossArenaRandomizer
{
    public sealed class OMotherCheck : ISeedCheck
    {
        public string Id => "seed-validity";
        public string DisplayName => "Seed Validity (O Mother blacklist)";
        public string Description => "Fails if the spoiler log contains any known invalid 'O Mother ... Replaces ...' phrases.";

        // Any match = invalid seed
        private static readonly string[] InvalidPhrases =
        {
            "O Mother in Jagged Peak: Dropped by Bayle the Dread. Replaces Heart of Bayle.",
            "O Mother in Jagged Peak: Dropped by Ancient Dragon Senessax. Replaces Ancient Dragon Smithing Stone.",
            "O Mother in Scadu Altus - Darklight Catacombs: Dropped by Jori, Elder Inquisitor. Replaces Barbed Staff-Spear.",
            "O Mother in Midra's Manse: Dropped by Midra, Lord of Frenzied Flame. Replaces Remembrance of the Lord of Frenzied Flame.",
            "O Mother in Specimen Storehouse: Dropped by Messmer the Impaler. Replaces Remembrance of the Impaler.",
            "O Mother in Scaduview: Dropped by Commander Gaius. Replaces Remembrance of the Wild Boar Rider.",
            "O Mother in Stone Coffin Fissure: Dropped by Putrescent Knight. Replaces Remembrance of Putrescence."
        };

        public SeedCheckResult Run(string seedText)
        {
            // Find first match
            var match = InvalidPhrases.FirstOrDefault(p =>
                seedText.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);

            bool passed = match == null;

            return new SeedCheckResult
            {
                CheckId = Id,
                Passed = passed,
                Message = passed
                    ? "Seed Valid (no invalid phrases found)."
                    : $"Seed Invalid\nMatched phrase:\n{match}"
            };
        }
    }
}
