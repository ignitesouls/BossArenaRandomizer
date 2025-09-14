using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossArenaRandomizer
{
    public class Modules
    {
        public FilterArenas ArenaFilter { get; set; }
        public FilterBosses BossesFilter { get; set; }


        /*Changed public Modules () {
            ArenaFilter = new FilterArenas();
            BossesFilter = new FilterBosses();
        }*/

        public Modules(Dictionary<string, ArenaInfo> arenasJson, Dictionary<string, BossInfo> bossesJson)
        { 

            ArenaFilter = new FilterArenas(arenasJson);
            BossesFilter = new FilterBosses(bossesJson);
        }
    }
}
