using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Security.Policy;
using System.Security.Cryptography.X509Certificates;



namespace BossArenaRandomizer
{
    public class ArenaInfo
    {
        public string id { get; set; } = string.Empty;
        public int arenaSize { get; set; }
        public int arenaType { get; set; }
        public int twoPhaseNotAllowed { get; set; }
        public int nightBoss { get; set; }
        public int dragonNotAllowed { get; set; }
        public int npcNotAllowed { get; set; }
        public int isEscapable { get; set; }
        public int hardNotAllowed { get; set; }
        public int spawner { get; set; }
        public int region { get; set; }
        public int scaling { get; set; }
        public int dlc { get; set; }
    }

    public class BossInfo
    {
        public string id { get; set; } = string.Empty;
        public int bossSize { get; set; }
        public int bossType { get; set; }
        public int isTwoPhase { get; set; }
        public int nightBoss { get; set; }
        public int isDragon { get; set; }
        public int isNPC { get; set; }
        public int canEscape { get; set; }

        public int isHard { get; set; }
        public int spawner { get; set; }
        public int region { get; set; }
        public int scaling { get; set; }
        public int dlc { get; set; }
    }

    class InitialDataRead
    {
        //Load up Arenas
        public static Dictionary<string, ArenaInfo> LoadArenas(string filepath)
        {
            string jsonString = File.ReadAllText(filepath);

            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<Dictionary<string, ArenaInfo>>(jsonString, options) ?? new();
        }

        //Load up Bosses
        public static Dictionary<string, BossInfo> LoadBosses(string filepath)
        {
            string jsonString = File.ReadAllText(filepath);

            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<Dictionary<string, BossInfo>>(jsonString, options) ?? new();
        }
    }
}
