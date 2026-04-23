using System;
using System.IO;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services
{
    public sealed class AppStateService
    {
        private readonly string _basePath;

        public Dictionary<string, ArenaInfo> Arenas { get; private set; } = new();
        public Dictionary<string, BossInfo> Bosses { get; private set; } = new();
        public Modules Modules { get; private set; } = default!;

        public event EventHandler? StateReloaded;

        public AppStateService(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            ReloadAll();
        }

        public void ReloadAll()
        {
            Arenas = InitialDataRead.LoadArenas(Path.Combine(_basePath, "Data", "arenas.json"));
            Bosses = InitialDataRead.LoadBosses(Path.Combine(_basePath, "Data", "bosses.json"));

            CsvTranslation.WriteArenaBossCsv(
                Arenas,
                Bosses,
                Path.Combine(_basePath, "ArenaBossData.csv"));

            Modules = new Modules(Arenas, Bosses);

            StateReloaded?.Invoke(this, EventArgs.Empty);
        }
    }
}