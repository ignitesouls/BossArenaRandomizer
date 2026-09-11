using System.Collections.Generic;

namespace BossArenaRandomizer.Core
{
    public class Modules
    {
        public FilterArenas ArenaFilter { get; set; }
        public FilterBosses BossesFilter { get; set; }

        public Modules(Dictionary<string, ArenaInfo> arenasJson, Dictionary<string, BossInfo> bossesJson)
        { 

            ArenaFilter = new FilterArenas(arenasJson);
            BossesFilter = new FilterBosses(bossesJson);
        }
    }
}
