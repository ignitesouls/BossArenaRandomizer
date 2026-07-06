using System.Collections.Generic;
using System.IO;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services
{
    public interface IPairingPresetLoader
    {
        PairingPresetValidator Load(string path);
    }

    public sealed class PairingPresetFileLoader : IPairingPresetLoader
    {
        public PairingPresetValidator Load(string path)
        {
            return PairingPresetValidator.Load(path);
        }
    }

    public interface IAssignmentWriter
    {
        void Write(
            IReadOnlyCollection<AssignmentPair> assignments,
            string outputPath,
            string optionsFilePath,
            int seed,
            bool includeClearArenas);
    }

    public sealed class RandomizeOptionsAssignmentWriter : IAssignmentWriter
    {
        public void Write(
            IReadOnlyCollection<AssignmentPair> assignments,
            string outputPath,
            string optionsFilePath,
            int seed,
            bool includeClearArenas)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            FinalizeTextFile.WriteFinalAssignments(
                assignments,
                outputPath,
                optionsFilePath,
                seed,
                includeClearArenas);
        }
    }
}
