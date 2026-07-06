using System;
using System.Collections.ObjectModel;
using System.Linq;
using BossArenaRandomizer.Models;
using BossArenaRandomizer.Services;

namespace BossArenaRandomizer.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly AppStateService _appStateService;

        public ObservableCollection<NavigationItem> NavigationItems { get; }

        private NavigationItem? _selectedNavigationItem;
        public NavigationItem? SelectedNavigationItem
        {
            get => _selectedNavigationItem;
            set
            {
                if (SetProperty(ref _selectedNavigationItem, value))
                {
                    CurrentPageViewModel = value?.ViewModel;
                }
            }
        }

        private object? _currentPageViewModel;
        public object? CurrentPageViewModel
        {
            get => _currentPageViewModel;
            set => SetProperty(ref _currentPageViewModel, value);
        }

        public MainViewModel(string basePath, AppStateService appStateService)
        {
            _appStateService = appStateService ?? throw new ArgumentNullException(nameof(appStateService));

            var settingsService = new SettingsService();
            var presetService = new PresetService(basePath);
            var seedGenerationService = new SeedGenerationService();
            var seedAnalysisService = new SeedAnalysisService();
            var dataRepository = new DataRepository(basePath);

            GenerateViewModel? generateVm = null;
            DashboardViewModel? dashboardVm = null;
            ArenaViewModel? arenaVm = null;
            BossViewModel? bossVm = null;
            DatabaseEditorViewModel? databaseEditorVm = null;

            void NavigateTo(string title)
            {
                var match = NavigationItems.FirstOrDefault(x => x.Title == title);
                if (match != null)
                    SelectedNavigationItem = match;
            }

            generateVm = new GenerateViewModel(
                basePath,
                settingsService,
                presetService,
                seedGenerationService,
                () => _appStateService.Arenas,
                () => _appStateService.Bosses,
                () => _appStateService.Modules.ArenaFilter,
                () => _appStateService.Modules.BossesFilter
            );

            dashboardVm = new DashboardViewModel(
                () => _appStateService.Modules.ArenaFilter.SelectedCount,
                () => _appStateService.Modules.BossesFilter.SelectedCount,
                () => generateVm.DashboardSelectedOptionsPreset,
                () => generateVm.DashboardOutputPath,
                () => generateVm.DashboardLastSeedText,
                () => generateVm.DashboardLastStatusText,
                NavigateTo
            );

            void RefreshSharedState()
            {
                generateVm.RefreshSelectionSummary();
                dashboardVm.Refresh();
            }

            arenaVm = new ArenaViewModel(
                _appStateService.Modules.ArenaFilter,
                presetService,
                settingsService,
                RefreshSharedState
            );

            bossVm = new BossViewModel(
                _appStateService.Modules.BossesFilter,
                presetService,
                settingsService,
                RefreshSharedState
            );

            var analyzeVm = new AnalyzeViewModel(seedAnalysisService);

            var arenaEditorVm = new ArenaEditorViewModel(
                dataRepository,
                presetService,
                _appStateService
            );

            databaseEditorVm = new DatabaseEditorViewModel(
                dataRepository,
                _appStateService
            );

            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem { Title = "Dashboard", ViewModel = dashboardVm },
                new NavigationItem { Title = "Generate", ViewModel = generateVm },
                new NavigationItem { Title = "Arenas", ViewModel = arenaVm },
                new NavigationItem { Title = "Bosses", ViewModel = bossVm },
                new NavigationItem { Title = "Analyze", ViewModel = analyzeVm },
                new NavigationItem { Title = "Preset Pairings", ViewModel = arenaEditorVm },
                new NavigationItem { Title = "Main Database", ViewModel = databaseEditorVm },
            };

            _appStateService.StateReloaded += (_, _) =>
            {
                arenaVm = new ArenaViewModel(
                    _appStateService.Modules.ArenaFilter,
                    presetService,
                    settingsService,
                    RefreshSharedState
                );

                bossVm = new BossViewModel(
                    _appStateService.Modules.BossesFilter,
                    presetService,
                    settingsService,
                    RefreshSharedState
                );

                ReplaceNavigationViewModel("Arenas", arenaVm);
                ReplaceNavigationViewModel("Bosses", bossVm);

                arenaEditorVm = new ArenaEditorViewModel(
                    dataRepository,
                    presetService,
                    _appStateService
                );

                databaseEditorVm = new DatabaseEditorViewModel(
                    dataRepository,
                    _appStateService
                );

                ReplaceNavigationViewModel("Preset Pairings", arenaEditorVm);
                ReplaceNavigationViewModel("Main Database", databaseEditorVm);

                RefreshSharedState();
            };

            RefreshSharedState();
            SelectedNavigationItem = NavigationItems[0];
        }

        private void ReplaceNavigationViewModel(string title, object newViewModel)
        {
            var item = NavigationItems.FirstOrDefault(x => x.Title == title);
            if (item == null)
                return;

            bool wasSelected = SelectedNavigationItem == item;
            item.ViewModel = newViewModel;

            if (wasSelected)
                CurrentPageViewModel = newViewModel;
        }
    }
}
