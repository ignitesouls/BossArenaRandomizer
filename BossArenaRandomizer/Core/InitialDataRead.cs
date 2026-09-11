using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BossArenaRandomizer.Core
{
    public class ArenaInfo
    {
        public string id { get; set; } = string.Empty;
        public int type { get; set; }
        public int nightBoss { get; set; }
        public int region { get; set; }
        public int scaling { get; set; }
        public bool dlc { get; set; }
    }

    public class BossInfo
    {
        public string id { get; set; } = string.Empty;
        public int type { get; set; }
        public int nightBoss { get; set; }
        public int region { get; set; }
        public int scaling { get; set; }
        public bool dlc { get; set; }
    }

    internal static class InitialDataRead
    {
        public static (Dictionary<string, ArenaInfo> Arenas, Dictionary<string, BossInfo> Bosses) LoadAllArenaBosses(string filepath)
        {
            string jsonString = File.ReadAllText(filepath);
            using var document = JsonDocument.Parse(jsonString);

            var arenas = new Dictionary<string, ArenaInfo>(StringComparer.OrdinalIgnoreCase);
            var bosses = new Dictionary<string, BossInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in document.RootElement.EnumerateObject())
            {
                var element = entry.Value;
                string id = GetStringValue(element, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                int type = GetIntValue(element, "type");
                int nightBoss = GetIntValue(element, "nightBoss");
                int region = GetIntValue(element, "region");
                int scaling = GetIntValue(element, "scaling");
                bool dlc = GetBoolValue(element, "dlc");

                arenas[entry.Name] = new ArenaInfo
                {
                    id = id,
                    type = type,
                    nightBoss = nightBoss,
                    region = region,
                    scaling = scaling,
                    dlc = dlc
                };

                bosses[entry.Name] = new BossInfo
                {
                    id = id,
                    type = type,
                    nightBoss = nightBoss,
                    region = region,
                    scaling = scaling,
                    dlc = dlc
                };
            }

            return (arenas, bosses);
        }

        private static string GetStringValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                return string.Empty;

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString() ?? string.Empty,
                JsonValueKind.Number => property.GetRawText(),
                _ => string.Empty
            };
        }

        private static int GetIntValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                return 0;

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
                return number;

            if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out number))
                return number;

            return 0;
        }

        private static bool GetBoolValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                return false;

            return property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => property.TryGetInt32(out var number) && number != 0,
                JsonValueKind.String => bool.TryParse(property.GetString(), out var value) && value,
                _ => false
            };
        }
    }
}
