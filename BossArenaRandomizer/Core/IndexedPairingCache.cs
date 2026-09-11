using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BossArenaRandomizer.Core
{
    public sealed class IndexedPairingCache
    {
        public string[] ArenaNames { get; }
        public string[] ArenaIds { get; }
        public int[] ArenaRegions { get; }
        public string[] BossNames { get; }
        public string[] BossIds { get; }
        public IReadOnlyDictionary<string, int> ArenaIndexById { get; }
        public IReadOnlyDictionary<string, int> BossIndexById { get; }
        public BitArray[] AllowedBossesByArena { get; }

        private IndexedPairingCache(
            string[] arenaNames,
            string[] arenaIds,
            int[] arenaRegions,
            string[] bossNames,
            string[] bossIds,
            Dictionary<string, int> arenaIndexById,
            Dictionary<string, int> bossIndexById,
            BitArray[] allowedBossesByArena)
        {
            ArenaNames = arenaNames;
            ArenaIds = arenaIds;
            ArenaRegions = arenaRegions;
            BossNames = bossNames;
            BossIds = bossIds;
            ArenaIndexById = arenaIndexById;
            BossIndexById = bossIndexById;
            AllowedBossesByArena = allowedBossesByArena;
        }

        public static IndexedPairingCache Build(
            Dictionary<string, ArenaInfo> arenas,
            Dictionary<string, BossInfo> bosses,
            PairingPresetValidator validator)
        {
            if (arenas == null)
                throw new ArgumentNullException(nameof(arenas));
            if (bosses == null)
                throw new ArgumentNullException(nameof(bosses));
            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            var arenaEntries = arenas
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var bossEntries = bosses
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var arenaNames = new string[arenaEntries.Count];
            var arenaIds = new string[arenaEntries.Count];
            var arenaRegions = new int[arenaEntries.Count];
            var bossNames = new string[bossEntries.Count];
            var bossIds = new string[bossEntries.Count];
            var arenaIndexById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var bossIndexById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var allowedBossesByArena = new BitArray[arenaEntries.Count];

            for (int arenaIndex = 0; arenaIndex < arenaEntries.Count; arenaIndex++)
            {
                arenaNames[arenaIndex] = arenaEntries[arenaIndex].Key;
                arenaIds[arenaIndex] = arenaEntries[arenaIndex].Value.id;
                arenaRegions[arenaIndex] = arenaEntries[arenaIndex].Value.region;
                arenaIndexById[arenaIds[arenaIndex]] = arenaIndex;
            }

            for (int bossIndex = 0; bossIndex < bossEntries.Count; bossIndex++)
            {
                bossNames[bossIndex] = bossEntries[bossIndex].Key;
                bossIds[bossIndex] = bossEntries[bossIndex].Value.id;
                bossIndexById[bossIds[bossIndex]] = bossIndex;
            }

            for (int arenaIndex = 0; arenaIndex < arenaIds.Length; arenaIndex++)
            {
                var allowedBosses = new BitArray(bossIds.Length);
                if (validator.AllowedBossIdsByArenaId.TryGetValue(arenaIds[arenaIndex], out var allowedBossIds))
                {
                    foreach (string bossId in allowedBossIds)
                    {
                        if (bossIndexById.TryGetValue(bossId, out int bossIndex))
                            allowedBosses[bossIndex] = true;
                    }
                }

                allowedBossesByArena[arenaIndex] = allowedBosses;
            }

            return new IndexedPairingCache(
                arenaNames,
                arenaIds,
                arenaRegions,
                bossNames,
                bossIds,
                arenaIndexById,
                bossIndexById,
                allowedBossesByArena);
        }

        public bool IsAllowed(int arenaIndex, int bossIndex)
        {
            return arenaIndex >= 0
                && arenaIndex < AllowedBossesByArena.Length
                && bossIndex >= 0
                && bossIndex < BossIds.Length
                && AllowedBossesByArena[arenaIndex][bossIndex];
        }
    }
}
