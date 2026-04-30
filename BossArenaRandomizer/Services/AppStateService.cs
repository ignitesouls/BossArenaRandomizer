using System;
using System.IO;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services;

public sealed class AppStateService
{
    public Dictionary<string, ArenaInfo> Arenas { get; private set; } = new();
    public Dictionary<string, BossInfo> Bosses { get; private set; } = new();
    public Modules Modules { get; private set; } = default!;

    public event EventHandler? StateReloaded;

    public AppStateService()
    {
        ReloadAll();
    }

    public void ReloadAll()
    {
        Arenas = InitialDataRead.LoadArenas(Constants.ArenasJsonPath);
        Bosses = InitialDataRead.LoadBosses(Constants.BossesJsonPath);

        CsvTranslation.WriteArenaBossCsv(
            Arenas,
            Bosses,
            Constants.ArenaBossDataPath);

        Modules = new Modules(Arenas, Bosses);

        StateReloaded?.Invoke(this, EventArgs.Empty);
    }
}
