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

        public Modules()
        { 
            ArenaFilter = new FilterArenas();
            BossesFilter = new FilterBosses();
        }
    }
}
