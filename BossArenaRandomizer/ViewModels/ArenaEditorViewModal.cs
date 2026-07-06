using BossArenaRandomizer.Services;

namespace BossArenaRandomizer.ViewModels
{
    public sealed class ArenaEditorViewModel : PairingPresetEditorViewModel
    {
        public ArenaEditorViewModel(
            DataRepository dataRepository,
            PresetService presetService,
            AppStateService appStateService)
            : base(dataRepository, presetService, appStateService)
        {
            Title = "Preset Editor";
            Subtitle = "Edit boss and arena pairings in Data/Pairings preset JSON files such as everything.json.";
        }
    }
}
