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
            var allArenaBossesPath = Path.Combine(_basePath, "Data", "AllArenaBossesDatabase.json");
            if (File.Exists(allArenaBossesPath))
            {
                var loaded = InitialDataRead.LoadAllArenaBosses(allArenaBossesPath);
                Arenas = loaded.Arenas;
                Bosses = loaded.Bosses;
            }
            else
            {
                Arenas = new Dictionary<string, ArenaInfo>();
                Bosses = new Dictionary<string, BossInfo>();
            }
            Modules = new Modules(Arenas, Bosses);

            StateReloaded?.Invoke(this, EventArgs.Empty);
        }
    }
}
