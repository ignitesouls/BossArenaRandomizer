using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossArenaRandomizer.Core
{
    public static class HCData
    {
        public static readonly Dictionary<int, string> RegionNames = new()
        {
            { 1, "Limgrave" },
            { 2, "Weeping" },
            { 3, "Liurnia" },
            { 4, "Caelid" },
            { 5, "Mt. Gelmir" },
            { 6, "Altus" },
            { 7, "Mountaintops" },
            { 8, "Farum Azula" },
            { 9, "Ashen Capital" },
            { 10, "Consecrated Snowfield" },
            { 11, "Haligtree" },
            { 12, "Siofra" },
            { 13, "Ainsel" },
            { 14, "Nokron" },
            { 15, "Deeproot Depths" },
            { 16, "Moonlight Alter" },
            { 17, "Mohgwyn Dynasty Mausoleum" },
            { 18, "Gravesite Plains" },
            { 19, "Cerulean Coast" },
            { 20, "Scadu Altus" },
            { 21, "Hinterlands" },
            { 22, "Jagged Peak" },
            { 23, "Abyssal Woods" },
            { 24, "Ancient Ruins of Rauh" },
            { 25, "Enir-Ilim" }
        };

        public static readonly Dictionary<int, string> ArenaBossType = new()
        {
            { 1, "Ruin/Mausoleums" },
            { 2, "Achievement Bosses" },
            { 3, "Open World" },
            { 4, "Cave" },
            { 5, "Catacomb" },
            { 6, "Tunnel" },
            { 7, "Evergaol" },
            { 8, "Gaol" }
        };
    }
}
