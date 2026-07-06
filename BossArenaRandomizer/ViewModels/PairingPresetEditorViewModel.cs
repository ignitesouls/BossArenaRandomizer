using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using BossArenaRandomizer.Core;
using BossArenaRandomizer.Services;
using Microsoft.Win32;

namespace BossArenaRandomizer.ViewModels
{
    public sealed class PresetArenaItem : ViewModelBase
    {
        private int _allowedCount;

        public string Name { get; init; } = string.Empty;
        public string Id { get; init; } = string.Empty;
        public string Info { get; init; } = string.Empty;
        public int Type { get; init; }
        public int Region { get; init; }
        public int Scaling { get; init; }

        public int AllowedCount
        {
            get => _allowedCount;
            set => SetProperty(ref _allowedCount, value);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public sealed class PresetBossToggle : ViewModelBase
    {
        private bool _isAllowed;

        public string Name { get; init; } = string.Empty;
        public string Id { get; init; } = string.Empty;
        public string Info { get; init; } = string.Empty;
        public int Type { get; init; }
        public int Region { get; init; }
        public int Scaling { get; init; }

        public bool IsAllowed
        {
            get => _isAllowed;
            set => SetProperty(ref _isAllowed, value);
        }
    }

    public class PairingPresetEditorViewModel : ViewModelBase
    {
        private readonly DataRepository _dataRepository;
        private readonly PresetService _presetService;
        private readonly AppStateService _appStateService;
        private readonly Dictionary<string, ArenaInfo> _arenasByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, BossInfo> _bossesByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _arenaNamesById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _bossNamesById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _allowedBossIdsByArenaId = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<PresetArenaItem> _allArenaItems = new();
        private readonly List<PresetBossToggle> _allBossToggles = new();
        private bool _isRefreshingBosses;

        public ObservableCollection<string> PresetFiles { get; } = new();
        public ObservableCollection<PresetArenaItem> Arenas { get; } = new();
        public ObservableCollection<PresetBossToggle> Bosses { get; } = new();
        public ObservableCollection<PresetArenaItem> CopySourceArenas { get; } = new();
        public ObservableCollection<string> BossFilterModes { get; } = new()
        {
            "All",
            "Allowed",
            "Blocked"
        };
        public ObservableCollection<string> ArenaSortModes { get; } = new()
        {
            "Name",
            "Type",
            "Region",
            "Scaling"
        };
        public ObservableCollection<string> BossSortModes { get; } = new()
        {
            "Name",
            "Type",
            "Region",
            "Scaling"
        };

        private string _title = "Pairing Preset Editor";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _subtitle = "Edit which bosses are allowed in each arena for the selected preset JSON.";
        public string Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }

        private string _selectedPresetFile = string.Empty;
        public string SelectedPresetFile
        {
            get => _selectedPresetFile;
            set => SetProperty(ref _selectedPresetFile, value);
        }

        private PresetArenaItem? _selectedArena;
        public PresetArenaItem? SelectedArena
        {
            get => _selectedArena;
            set
            {
                if (SetProperty(ref _selectedArena, value))
                    RefreshBossesForSelectedArena();
            }
        }

        private PresetArenaItem? _selectedCopySourceArena;
        public PresetArenaItem? SelectedCopySourceArena
        {
            get => _selectedCopySourceArena;
            set => SetProperty(ref _selectedCopySourceArena, value);
        }

        private string _arenaSearchText = string.Empty;
        public string ArenaSearchText
        {
            get => _arenaSearchText;
            set
            {
                if (SetProperty(ref _arenaSearchText, value))
                    RefreshArenaList();
            }
        }

        private string _bossSearchText = string.Empty;
        public string BossSearchText
        {
            get => _bossSearchText;
            set
            {
                if (SetProperty(ref _bossSearchText, value))
                    RefreshBossesForSelectedArena();
            }
        }

        private string _selectedBossFilterMode = "All";
        public string SelectedBossFilterMode
        {
            get => _selectedBossFilterMode;
            set
            {
                if (SetProperty(ref _selectedBossFilterMode, value))
                    RefreshBossesForSelectedArena();
            }
        }

        private string _selectedArenaSortMode = "Name";
        public string SelectedArenaSortMode
        {
            get => _selectedArenaSortMode;
            set
            {
                if (SetProperty(ref _selectedArenaSortMode, value))
                    RefreshArenaList();
            }
        }

        private bool _showOnlyEmptyArenas;
        public bool ShowOnlyEmptyArenas
        {
            get => _showOnlyEmptyArenas;
            set
            {
                if (SetProperty(ref _showOnlyEmptyArenas, value))
                    RefreshArenaList();
            }
        }

        private string _selectedBossSortMode = "Name";
        public string SelectedBossSortMode
        {
            get => _selectedBossSortMode;
            set
            {
                if (SetProperty(ref _selectedBossSortMode, value))
                    RefreshBossesForSelectedArena();
            }
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (SetProperty(ref _hasUnsavedChanges, value))
                    OnPropertyChanged(nameof(DirtyStatusText));
            }
        }

        public string DirtyStatusText => HasUnsavedChanges ? "Unsaved changes" : "Saved";
        public int ArenaCount => Arenas.Count;
        public int VisibleBossCount => Bosses.Count;
        public int SelectedArenaAllowedCount => SelectedArena?.AllowedCount ?? 0;

        public RelayCommand RefreshPresetsCommand { get; }
        public RelayCommand LoadPresetCommand { get; }
        public RelayCommand SaveOverwriteCommand { get; }
        public RelayCommand SaveAsCommand { get; }
        public RelayCommand AllowVisibleBossesCommand { get; }
        public RelayCommand ClearVisibleBossesCommand { get; }
        public RelayCommand AllowAllBossesCommand { get; }
        public RelayCommand ClearAllBossesCommand { get; }
        public RelayCommand SelectAllFilteredCommand { get; }
        public RelayCommand ClearAllFilteredCommand { get; }
        public RelayCommand CopyPairingsFromArenaCommand { get; }
        public RelayCommand RestoreBackupCommand { get; }

        public PairingPresetEditorViewModel(
            DataRepository dataRepository,
            PresetService presetService,
            AppStateService appStateService)
        {
            _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
            _presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));
            _appStateService = appStateService ?? throw new ArgumentNullException(nameof(appStateService));

            RefreshPresetsCommand = new RelayCommand(_ => LoadPresetFiles());
            LoadPresetCommand = new RelayCommand(_ => LoadSelectedPreset());
            SaveOverwriteCommand = new RelayCommand(_ => SaveOverwrite(), _ => !string.IsNullOrWhiteSpace(SelectedPresetFile));
            SaveAsCommand = new RelayCommand(_ => SaveAs());
            AllowVisibleBossesCommand = new RelayCommand(_ => SetVisibleBossesAllowed(true), _ => SelectedArena != null);
            ClearVisibleBossesCommand = new RelayCommand(_ => SetVisibleBossesAllowed(false), _ => SelectedArena != null);
            AllowAllBossesCommand = new RelayCommand(_ => SetAllBossesAllowed(true), _ => SelectedArena != null);
            ClearAllBossesCommand = new RelayCommand(_ => SetAllBossesAllowed(false), _ => SelectedArena != null);
            SelectAllFilteredCommand = new RelayCommand(_ => SetVisibleBossesAllowed(true), _ => SelectedArena != null);
            ClearAllFilteredCommand = new RelayCommand(_ => SetVisibleBossesAllowed(false), _ => SelectedArena != null);
            CopyPairingsFromArenaCommand = new RelayCommand(
                _ => CopyPairingsFromSelectedSourceArena(),
                _ => SelectedArena != null && SelectedCopySourceArena != null);
            RestoreBackupCommand = new RelayCommand(_ => RestoreBackup(), _ => !string.IsNullOrWhiteSpace(SelectedPresetFile));

            LoadDatabase();
            LoadPresetFiles();
            if (PresetFiles.Contains("everything.json"))
                SelectedPresetFile = "everything.json";
            else if (PresetFiles.Count > 0)
                SelectedPresetFile = PresetFiles[0];

            if (!string.IsNullOrWhiteSpace(SelectedPresetFile))
                LoadSelectedPreset(force: true);
        }

        private void LoadDatabase()
        {
            _arenasByName.Clear();
            _bossesByName.Clear();
            _arenaNamesById.Clear();
            _bossNamesById.Clear();

            foreach (var arena in _appStateService.Arenas)
            {
                _arenasByName[arena.Key] = arena.Value;
                if (!_arenaNamesById.ContainsKey(arena.Value.id))
                    _arenaNamesById[arena.Value.id] = arena.Key;
            }

            foreach (var boss in _appStateService.Bosses)
            {
                _bossesByName[boss.Key] = boss.Value;
                if (!_bossNamesById.ContainsKey(boss.Value.id))
                    _bossNamesById[boss.Value.id] = boss.Key;
            }
        }

        private void LoadPresetFiles()
        {
            PresetFiles.Clear();
            foreach (var preset in _presetService.GetPairingPresetFiles())
                PresetFiles.Add(preset);
        }

        private bool ConfirmDiscardChanges(string actionText)
        {
            if (!HasUnsavedChanges)
                return true;

            var result = MessageBox.Show(
                $"You have unsaved changes. {actionText} and discard them?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        private void LoadSelectedPreset(bool force = false)
        {
            if (string.IsNullOrWhiteSpace(SelectedPresetFile))
                return;

            if (!force && !ConfirmDiscardChanges("Load another preset"))
                return;

            _allowedBossIdsByArenaId.Clear();
            var preset = _dataRepository.LoadPairingPreset(SelectedPresetFile);

            foreach (var pair in preset)
            {
                _allowedBossIdsByArenaId[pair.Key] = new HashSet<string>(
                    pair.Value ?? new List<string>(),
                    StringComparer.OrdinalIgnoreCase);
            }

            RebuildArenaItems();
            RebuildBossToggles();
            RefreshArenaList();
            SelectedArena = Arenas.FirstOrDefault();

            HasUnsavedChanges = false;
            StatusText = $"Loaded {SelectedPresetFile}.";
        }

        private void RebuildArenaItems()
        {
            _allArenaItems.Clear();

            var arenaIds = _arenaNamesById.Keys
                .Concat(_allowedBossIdsByArenaId.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetArenaName);

            foreach (var arenaId in arenaIds)
            {
                _allArenaItems.Add(new PresetArenaItem
                {
                    Id = arenaId,
                    Name = GetArenaName(arenaId),
                    Info = GetArenaInfo(arenaId),
                    Type = GetArenaType(arenaId),
                    Region = GetArenaRegion(arenaId),
                    Scaling = GetArenaScaling(arenaId),
                    AllowedCount = GetAllowedBossSet(arenaId).Count
                });
            }
        }

        private void RebuildBossToggles()
        {
            _allBossToggles.Clear();

            var bossIds = _bossNamesById.Keys
                .Concat(_allowedBossIdsByArenaId.Values.SelectMany(x => x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetBossName);

            foreach (var bossId in bossIds)
            {
                var boss = new PresetBossToggle
                {
                    Id = bossId,
                    Name = GetBossName(bossId),
                    Info = GetBossInfo(bossId),
                    Type = GetBossType(bossId),
                    Region = GetBossRegion(bossId),
                    Scaling = GetBossScaling(bossId)
                };

                boss.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(PresetBossToggle.IsAllowed))
                        UpdateSelectedArenaBoss(boss);
                };

                _allBossToggles.Add(boss);
            }
        }

        private void RefreshArenaList()
        {
            string term = (ArenaSearchText ?? string.Empty).Trim();
            var selectedId = SelectedArena?.Id;

            Arenas.Clear();
            foreach (var arena in SortArenas(_allArenaItems.Where(a => MatchesArena(a, term))))
                Arenas.Add(arena);

            SelectedArena = Arenas.FirstOrDefault(a => a.Id == selectedId) ?? Arenas.FirstOrDefault();
            OnPropertyChanged(nameof(ArenaCount));
            RefreshCopySourceArenas();
        }

        private void RefreshBossesForSelectedArena()
        {
            Bosses.Clear();

            if (SelectedArena == null)
            {
                OnPropertyChanged(nameof(VisibleBossCount));
                OnPropertyChanged(nameof(SelectedArenaAllowedCount));
                return;
            }

            string term = (BossSearchText ?? string.Empty).Trim();
            var allowed = GetAllowedBossSet(SelectedArena.Id);

            _isRefreshingBosses = true;
            foreach (var boss in SortBosses(_allBossToggles.Where(b => MatchesBoss(b, term, allowed))))
            {
                boss.IsAllowed = allowed.Contains(boss.Id);
                Bosses.Add(boss);
            }
            _isRefreshingBosses = false;

            OnPropertyChanged(nameof(VisibleBossCount));
            OnPropertyChanged(nameof(SelectedArenaAllowedCount));
        }

        private void UpdateSelectedArenaBoss(PresetBossToggle boss)
        {
            if (SelectedArena == null)
                return;

            if (_isRefreshingBosses)
                return;

            var allowed = GetAllowedBossSet(SelectedArena.Id);
            if (boss.IsAllowed)
                allowed.Add(boss.Id);
            else
                allowed.Remove(boss.Id);

            SelectedArena.AllowedCount = allowed.Count;
            HasUnsavedChanges = true;
            OnPropertyChanged(nameof(SelectedArenaAllowedCount));
        }

        private void SetVisibleBossesAllowed(bool allowed)
        {
            if (SelectedArena == null)
                return;

            var allowedBosses = GetAllowedBossSet(SelectedArena.Id);
            foreach (var boss in Bosses)
            {
                if (allowed)
                    allowedBosses.Add(boss.Id);
                else
                    allowedBosses.Remove(boss.Id);
            }

            RefreshArenaAllowedCount();
            RefreshBossesForSelectedArena();
            HasUnsavedChanges = true;
        }

        private void SetAllBossesAllowed(bool allowed)
        {
            if (SelectedArena == null)
                return;

            var allowedBosses = GetAllowedBossSet(SelectedArena.Id);

            if (allowed)
            {
                foreach (var boss in _allBossToggles)
                    allowedBosses.Add(boss.Id);
            }
            else
            {
                allowedBosses.Clear();
            }

            RefreshArenaAllowedCount();
            RefreshBossesForSelectedArena();
            HasUnsavedChanges = true;
        }

        private void RefreshArenaAllowedCount()
        {
            if (SelectedArena == null)
                return;

            SelectedArena.AllowedCount = GetAllowedBossSet(SelectedArena.Id).Count;
            OnPropertyChanged(nameof(SelectedArenaAllowedCount));
            OnPropertyChanged(nameof(ArenaCount));
            RefreshCopySourceArenas();
        }

        private void CopyPairingsFromSelectedSourceArena()
        {
            if (SelectedArena == null || SelectedCopySourceArena == null)
                return;

            var source = GetAllowedBossSet(SelectedCopySourceArena.Id);
            var target = GetAllowedBossSet(SelectedArena.Id);

            target.Clear();
            foreach (var bossId in source)
                target.Add(bossId);

            RefreshArenaAllowedCount();
            RefreshBossesForSelectedArena();
            HasUnsavedChanges = true;
            StatusText = $"Copied pairings from {SelectedCopySourceArena.Name} to {SelectedArena.Name}.";
        }

        private void RefreshCopySourceArenas()
        {
            var selectedSourceId = SelectedCopySourceArena?.Id;

            CopySourceArenas.Clear();
            foreach (var arena in _allArenaItems.OrderBy(a => a.Name))
                CopySourceArenas.Add(arena);

            SelectedCopySourceArena = CopySourceArenas.FirstOrDefault(a => a.Id == selectedSourceId)
                ?? CopySourceArenas.FirstOrDefault(a => SelectedArena == null || !string.Equals(a.Id, SelectedArena.Id, StringComparison.OrdinalIgnoreCase))
                ?? CopySourceArenas.FirstOrDefault();
        }

        private HashSet<string> GetAllowedBossSet(string arenaId)
        {
            if (!_allowedBossIdsByArenaId.TryGetValue(arenaId, out var allowed))
            {
                allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _allowedBossIdsByArenaId[arenaId] = allowed;
            }

            return allowed;
        }

        private void SaveOverwrite()
        {
            if (string.IsNullOrWhiteSpace(SelectedPresetFile))
                return;

            _dataRepository.SavePairingPreset(SelectedPresetFile, BuildPresetDictionary());
            HasUnsavedChanges = false;
            StatusText = $"Saved {SelectedPresetFile}.";
        }

        private void SaveAs()
        {
            var dialog = new SaveFileDialog
            {
                InitialDirectory = _presetService.PairingPresetDirectory,
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json",
                FileName = string.IsNullOrWhiteSpace(SelectedPresetFile)
                    ? "customPairings.json"
                    : Path.GetFileNameWithoutExtension(SelectedPresetFile) + "_copy.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            _dataRepository.SavePairingPreset(Path.GetFileName(dialog.FileName), BuildPresetDictionary());
            LoadPresetFiles();
            SelectedPresetFile = Path.GetFileName(dialog.FileName);
            HasUnsavedChanges = false;
            StatusText = $"Saved new preset {SelectedPresetFile}.";
        }

        private void RestoreBackup()
        {
            if (string.IsNullOrWhiteSpace(SelectedPresetFile))
                return;

            if (!_dataRepository.PairingPresetBackupExists(SelectedPresetFile))
            {
                MessageBox.Show("No backup exists for this pairing preset.");
                return;
            }

            var result = MessageBox.Show(
                $"Restore the backup for {SelectedPresetFile}? Current unsaved changes will be replaced.",
                "Restore Pairing Backup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            _dataRepository.RestorePairingPresetBackup(SelectedPresetFile);
            LoadSelectedPreset(force: true);
            StatusText = $"Restored backup for {SelectedPresetFile}.";
        }

        private Dictionary<string, List<string>> BuildPresetDictionary()
        {
            return _allowedBossIdsByArenaId
                .OrderBy(pair => GetArenaName(pair.Key))
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.OrderBy(GetBossName).ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }

        private bool MatchesArena(PresetArenaItem arena, string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return true;

            return arena.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || arena.Id.Contains(term, StringComparison.OrdinalIgnoreCase)
                || arena.Info.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesBoss(PresetBossToggle boss, string term, HashSet<string> allowedBossIds)
        {
            bool isAllowed = allowedBossIds.Contains(boss.Id);
            if (string.Equals(SelectedBossFilterMode, "Allowed", StringComparison.OrdinalIgnoreCase) && !isAllowed)
                return false;

            if (string.Equals(SelectedBossFilterMode, "Blocked", StringComparison.OrdinalIgnoreCase) && isAllowed)
                return false;

            if (string.IsNullOrWhiteSpace(term))
                return true;

            return boss.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || boss.Id.Contains(term, StringComparison.OrdinalIgnoreCase)
                || boss.Info.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<PresetArenaItem> SortArenas(IEnumerable<PresetArenaItem> arenas)
        {
            if (ShowOnlyEmptyArenas)
                arenas = arenas.Where(a => a.AllowedCount == 0);

            return SelectedArenaSortMode switch
            {
                "Type" => arenas.OrderBy(a => a.Type).ThenBy(a => a.Name),
                "Region" => arenas.OrderBy(a => a.Region).ThenBy(a => a.Name),
                "Scaling" => arenas.OrderBy(a => a.Scaling).ThenBy(a => a.Name),
                _ => arenas.OrderBy(a => a.Name)
            };
        }

        private IEnumerable<PresetBossToggle> SortBosses(IEnumerable<PresetBossToggle> bosses)
        {
            return SelectedBossSortMode switch
            {
                "Type" => bosses.OrderBy(b => b.Type).ThenBy(b => b.Name),
                "Region" => bosses.OrderBy(b => b.Region).ThenBy(b => b.Name),
                "Scaling" => bosses.OrderBy(b => b.Scaling).ThenBy(b => b.Name),
                _ => bosses.OrderBy(b => b.Name)
            };
        }

        private string GetArenaName(string arenaId)
        {
            return _arenaNamesById.TryGetValue(arenaId, out var name)
                ? name
                : $"Unknown Arena {arenaId}";
        }

        private string GetBossName(string bossId)
        {
            return _bossNamesById.TryGetValue(bossId, out var name)
                ? name
                : $"Unknown Boss {bossId}";
        }

        private string GetArenaInfo(string arenaId)
        {
            var arena = _arenasByName.FirstOrDefault(x => x.Value.id == arenaId).Value;
            if (arena == null)
                return $"ID: {arenaId}";

            return $"ID: {arenaId} | Type: {FormatType(arena.type)} | Region: {FormatRegion(arena.region)} | Scaling: {arena.scaling} | DLC: {arena.dlc}";
        }

        private string GetBossInfo(string bossId)
        {
            var boss = _bossesByName.FirstOrDefault(x => x.Value.id == bossId).Value;
            if (boss == null)
                return $"ID: {bossId}";

            return $"ID: {bossId} | Region: {FormatRegion(boss.region)} | Scaling: {boss.scaling} | DLC: {boss.dlc}";
        }

        private int GetArenaType(string arenaId)
        {
            return _arenasByName.FirstOrDefault(x => x.Value.id == arenaId).Value?.type ?? int.MaxValue;
        }

        private int GetArenaRegion(string arenaId)
        {
            return _arenasByName.FirstOrDefault(x => x.Value.id == arenaId).Value?.region ?? int.MaxValue;
        }

        private int GetArenaScaling(string arenaId)
        {
            return _arenasByName.FirstOrDefault(x => x.Value.id == arenaId).Value?.scaling ?? int.MaxValue;
        }

        private int GetBossType(string bossId)
        {
            return _bossesByName.FirstOrDefault(x => x.Value.id == bossId).Value?.type ?? int.MaxValue;
        }

        private int GetBossRegion(string bossId)
        {
            return _bossesByName.FirstOrDefault(x => x.Value.id == bossId).Value?.region ?? int.MaxValue;
        }

        private int GetBossScaling(string bossId)
        {
            return _bossesByName.FirstOrDefault(x => x.Value.id == bossId).Value?.scaling ?? int.MaxValue;
        }

        private static string FormatType(int typeId)
        {
            return HCData.ArenaBossType.TryGetValue(typeId, out var typeName)
                ? $"{typeId} - {typeName}"
                : typeId.ToString();
        }

        private static string FormatRegion(int regionId)
        {
            return HCData.RegionNames.TryGetValue(regionId, out var regionName)
                ? $"{regionId} - {regionName}"
                : regionId.ToString();
        }
    }
}
