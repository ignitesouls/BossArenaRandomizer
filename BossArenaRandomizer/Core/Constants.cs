using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace BossArenaRandomizer.Core;

internal class Constants
{
    public static string ArenasJsonPath => Path.Combine("Resources", "Data", "arenas.json");
    public static string BossesJsonPath => Path.Combine("Resources", "Data", "bosses.json");
    public static string ArenaBossDataPath => Path.Combine("Resources", "Data", "ArenaBossData.csv");

    public static string ArenaPresetDirectory => Path.Combine("Resources", "Presets", "Arenas");
    public static string BossPresetDirectory => Path.Combine("Resources", "Presets", "Bosses");
    public static string OptionsPresetDirectory => Path.Combine("Resources", "Options");
}
