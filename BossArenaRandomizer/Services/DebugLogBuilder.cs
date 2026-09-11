using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services
{
    public sealed class DebugLogBuilder
    {
        private readonly StringBuilder _builder = new();

        public DebugLogBuilder(GenerationRequest request)
        {
            _builder.AppendLine("Boss Arena Randomizer Debug Log");
            _builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _builder.AppendLine();
            AppendSection("Run Settings");
            _builder.AppendLine($"Options preset: {request.SelectedOptionsPreset}");
            _builder.AppendLine($"Pairing preset: {request.SelectedPairingPreset}");
            _builder.AppendLine($"Selected arenas: {request.SelectedArenaIds.Count}");
            _builder.AppendLine($"Selected bosses: {request.SelectedBossIds.Count}");
            _builder.AppendLine($"Seed count: {request.SeedCount}");
            _builder.AppendLine($"Replay seed: {(request.ReplaySeed.HasValue ? request.ReplaySeed.Value.ToString() : "None")}");
            _builder.AppendLine($"Mode: {(request.WriteOutputFiles ? "Generate files" : "Dry run")}");
            _builder.AppendLine();
        }

        public void AppendSection(string title)
        {
            _builder.AppendLine(new string('=', 72));
            _builder.AppendLine(title);
            _builder.AppendLine(new string('=', 72));
        }

        public void AppendValidation(IEnumerable<string> validationLines)
        {
            AppendSection("Pairing Validation");
            foreach (var line in validationLines)
                _builder.AppendLine(line);
            _builder.AppendLine();
        }

        public void BeginSeed(int index, int seed, int seedCount)
        {
            AppendSection($"Seed {index} of {seedCount}");
            _builder.AppendLine($"Seed: {seed}");
        }

        public void AppendLine(string line = "")
        {
            _builder.AppendLine(line);
        }

        public void AppendAssignments(IReadOnlyCollection<AssignmentPair> assignments)
        {
            _builder.AppendLine();
            _builder.AppendLine("Assignments");
            foreach (var assignment in assignments.OrderBy(x => x.ArenaName))
                _builder.AppendLine($"  {assignment.ArenaName} ({assignment.ArenaId}) -> {assignment.BossName} ({assignment.BossId})");
        }

        public void AppendUniformity(IEnumerable<string> uniformityLines)
        {
            _builder.AppendLine();
            _builder.AppendLine("Uniformity");
            foreach (var line in uniformityLines)
                _builder.AppendLine($"  {line}");
            _builder.AppendLine();
        }

        public override string ToString()
        {
            return _builder.ToString();
        }
    }
}
