using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace BossArenaRandomizer.Core;

internal class Constants
{
    public const string BARPrefix = "BossArenaRandomizer_v0.1";

    public static string ArenasJsonPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Data", "arenas.json");
    public static string BossesJsonPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Data", "bosses.json");
    public static string ArenaBossDataPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Data", "ArenaBossData.csv");
    public static string ArenaPresetDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Presets", "Arenas");
    public static string BossPresetDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Presets", "Bosses");
    public static string OptionsPresetDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Options");

}
