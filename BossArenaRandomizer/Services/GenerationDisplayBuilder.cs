using System.Collections.Generic;
using System.Linq;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services
{
    public sealed class GenerationDisplayBuilder
    {
        public List<GenerationDisplayGroup> Build(IReadOnlyCollection<AssignmentPair> assignments)
        {
            return assignments
                .GroupBy(assignment => assignment.ArenaRegion)
                .OrderBy(group => group.Key)
                .Select(regionGroup =>
                {
                    string regionName = HCData.RegionNames.ContainsKey(regionGroup.Key)
                        ? HCData.RegionNames[regionGroup.Key]
                        : $"Region {regionGroup.Key}";

                    return new GenerationDisplayGroup
                    {
                        RegionName = regionName,
                        Lines = regionGroup
                            .Select(assignment => $"{assignment.ArenaName} (ID: {assignment.ArenaId}) -> {assignment.BossName} (ID: {assignment.BossId})")
                            .ToList()
                    };
                })
                .ToList();
        }
    }
}
