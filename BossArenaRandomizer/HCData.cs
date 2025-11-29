using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossArenaRandomizer
{
    public static class HCData
    {
        public static readonly Dictionary<int, string> RegionNames = new()
        {
            { 1, "Limgrave/Weeping" },
            { 2, "Liurnia" },
            { 3, "Caelid/Dragonbarrow" },
            { 4, "Mt. Gelmir" },
            { 5, "Altus" },
            { 6, "Mountaintops" },
            { 7, "Farum Azula" },
            { 8, "Ashen Captial" },
            { 9, "Siofra" },
            { 10, "Nokron" },
            { 11, "Ainsel" },
            { 12, "Nokstella" },
            { 13, "Deeproot Depths" },
            { 14, "Moonlight Alter" },
            { 15, "Mohgwyn Dynasty" },
            { 16, "Consecrated Snowfields" },
            { 17, "Haligtree" },
            { 18, "Gravesite Plains" },
            { 19, "Cerulean Coast" },
            { 20, "Scadu Altus" },
            { 21, "Hinterlands" },
            { 22, "Jagged Peak" },
            { 23, "Abyssal Woods" },
            { 24, "Ancient Ruins of Rauh" },
            { 25, "Enir Ilim" },
            { 26, "Extras"}
        };

        public static readonly Dictionary<int, string> ArenaBossType = new()
        {
            { 1, "Ruin/Mausoleum" },
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
