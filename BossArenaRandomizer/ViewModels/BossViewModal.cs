using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using BossArenaRandomizer.Services;
using Microsoft.Win32;
using BossArenaRandomizer.Core;


namespace BossArenaRandomizer.ViewModels
{
    public sealed class BossViewModel : ViewModelBase
    {
        private readonly PresetService _presetService;
        private readonly SettingsService _settingsService;
        private readonly FilterBosses _filterBosses;
        private readonly Action? _notifySelectionsChanged;

        public ObservableCollection<BossSelection> BossSelections => _filterBosses.BossSelections;
        public ICollectionView BossSelectionsView { get; }
        public ObservableCollection<string> BossPresets { get; } = new();

        private string _title = "Boss Editor";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _subtitle = "Select, search, and save boss presets for generation.";
        public string Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }

        private string _selectedBossPreset = string.Empty;
        public string SelectedBossPreset
        {
            get => _selectedBossPreset;
            set => SetProperty(ref _selectedBossPreset, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    BossSelectionsView.Refresh();
            }
        }

        public int SelectedCount => _filterBosses.SelectedCount;
        public int TotalCount => BossSelections.Count;

        public RelayCommand ResetAllCommand { get; }
        public RelayCommand ClearAllCommand { get; }
        public RelayCommand SelectBaseGameCommand { get; }
        public RelayCommand SelectDlcCommand { get; }
        public RelayCommand SavePresetCommand { get; }
        public RelayCommand LoadPresetCommand { get; }
        public RelayCommand RefreshPresetsCommand { get; }

        public BossViewModel(
            FilterBosses filterBosses,
            PresetService presetService,
            SettingsService settingsService,
            Action? notifySelectionsChanged = null)
        {
            _filterBosses = filterBosses ?? throw new ArgumentNullException(nameof(filterBosses));
            _presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _notifySelectionsChanged = notifySelectionsChanged;

            BossSelectionsView = CollectionViewSource.GetDefaultView(BossSelections);
            BossSelectionsView.Filter = FilterBoss;

            foreach (var boss in BossSelections)
            {
                boss.PropertyChanged += Boss_PropertyChanged;
            }

            _filterBosses.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(FilterBosses.SelectedCount))
                    OnPropertyChanged(nameof(SelectedCount));
            };

            ResetAllCommand = new RelayCommand(_ => ResetAll());
            ClearAllCommand = new RelayCommand(_ => ClearAll());
            SelectBaseGameCommand = new RelayCommand(_ => SelectBaseGame());
            SelectDlcCommand = new RelayCommand(_ => SelectDlc());
            SavePresetCommand = new RelayCommand(_ => SavePreset());
            LoadPresetCommand = new RelayCommand(_ => LoadPreset());
            RefreshPresetsCommand = new RelayCommand(_ => LoadPresetList());

            LoadPresetList();

            var lastPreset = _settingsService.GetLastUsedBossPreset();
            if (!string.IsNullOrWhiteSpace(lastPreset) && BossPresets.Contains(lastPreset))
                SelectedBossPreset = lastPreset;
        }

        private bool FilterBoss(object obj)
        {
            if (obj is not BossSelection boss)
                return false;

            var term = (SearchText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(term))
                return true;

            return boss.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || boss.Id.Contains(term, StringComparison.OrdinalIgnoreCase)
                || boss.RegionName.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private void Boss_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BossSelection.IsSelected))
            {
                OnPropertyChanged(nameof(SelectedCount));
                _notifySelectionsChanged?.Invoke();
            }
        }

        private void ResetAll()
        {
            foreach (var boss in BossSelections)
                boss.IsSelected = !HCFilterIds.UncheckArenaBossIds.Contains(boss.Id);

            RaiseSelectionChanged();
        }

        private void ClearAll()
        {
            foreach (var boss in BossSelections)
                boss.IsSelected = !HCFilterIds.AllBossArenas.Contains(boss.Id);

            RaiseSelectionChanged();
        }

        private void SelectBaseGame()
        {
            foreach (var boss in BossSelections)
                boss.IsSelected = HCFilterIds.BaseGameBossesIds.Contains(boss.Id);

            RaiseSelectionChanged();
        }

        private void SelectDlc()
        {
            foreach (var boss in BossSelections)
                boss.IsSelected = HCFilterIds.DLCBossesIds.Contains(boss.Id);

            RaiseSelectionChanged();
        }

        private void SavePreset()
        {
            var dialog = new SaveFileDialog
            {
                InitialDirectory = _presetService.BossPresetDirectory,
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json",
                FileName = "MyCustomBossPreset"
            };

            if (dialog.ShowDialog() != true)
                return;

            var selectedIds = BossSelections
                .Where(b => b.IsSelected)
                .Select(b => b.Id)
                .ToList();

            _presetService.SaveBossPreset(dialog.FileName, selectedIds);
            LoadPresetList();

            var fileName = System.IO.Path.GetFileName(dialog.FileName);
            SelectedBossPreset = fileName;
            _settingsService.SaveLastUsedBossPreset(fileName);

            System.Windows.MessageBox.Show("Custom Boss Preset Saved!");
        }

        private void LoadPreset()
        {
            if (string.IsNullOrWhiteSpace(SelectedBossPreset))
            {
                System.Windows.MessageBox.Show("Please select a boss preset.");
                return;
            }

            if (!_presetService.BossPresetExists(SelectedBossPreset))
            {
                System.Windows.MessageBox.Show("Preset file not found.");
                return;
            }

            var loadedIds = _presetService.LoadBossPresetIds(SelectedBossPreset);
            HCFilterIds.CustomBosses = new System.Collections.Generic.HashSet<string>(loadedIds);

            foreach (var boss in BossSelections)
                boss.IsSelected = HCFilterIds.CustomBosses.Contains(boss.Id);

            _settingsService.SaveLastUsedBossPreset(SelectedBossPreset);
            RaiseSelectionChanged();
        }

        private void LoadPresetList()
        {
            BossPresets.Clear();
            foreach (var preset in _presetService.GetBossPresetFiles())
                BossPresets.Add(preset);
        }

        private void RaiseSelectionChanged()
        {
            OnPropertyChanged(nameof(SelectedCount));
            _notifySelectionsChanged?.Invoke();
            BossSelectionsView.Refresh();
        }
    }
}