using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using BossArenaRandomizer.Core;
using BossArenaRandomizer.Services;
using Microsoft.Win32;

namespace BossArenaRandomizer.ViewModels
{
    public sealed class DatabaseOption
    {
        public int Value { get; init; }
        public string DisplayName { get; init; } = string.Empty;

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public sealed class DatabaseEntryRow : ViewModelBase
    {
        private string _name = string.Empty;
        private string _id = string.Empty;
        private int _type;
        private int _nightBoss;
        private int _region;
        private int _scaling;
        private bool _dlc;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                    OnPropertyChanged(nameof(TypeDisplayName));
            }
        }

        public int NightBoss
        {
            get => _nightBoss;
            set
            {
                if (SetProperty(ref _nightBoss, value))
                    OnPropertyChanged(nameof(NightBossDisplayName));
            }
        }

        public int Region
        {
            get => _region;
            set
            {
                if (SetProperty(ref _region, value))
                    OnPropertyChanged(nameof(RegionDisplayName));
            }
        }

        public int Scaling
        {
            get => _scaling;
            set => SetProperty(ref _scaling, value);
        }

        public bool Dlc
        {
            get => _dlc;
            set => SetProperty(ref _dlc, value);
        }

        public string TypeDisplayName => HCData.ArenaBossType.TryGetValue(Type, out var name)
            ? name
            : Type.ToString();

        public string NightBossDisplayName => NightBoss == 1 ? "Yes" : "No";

        public string RegionDisplayName => HCData.RegionNames.TryGetValue(Region, out var name)
            ? name
            : Region.ToString();
    }

    public sealed class DatabaseEditorViewModel : ViewModelBase
    {
        private readonly DataRepository _dataRepository;
        private readonly AppStateService _appStateService;

        public ObservableCollection<DatabaseEntryRow> Entries { get; } = new();
        public ObservableCollection<DatabaseOption> TypeOptions { get; } = new();
        public ObservableCollection<DatabaseOption> NightOptions { get; } = new();
        public ObservableCollection<DatabaseOption> RegionOptions { get; } = new();
        public ICollectionView EntriesView { get; }

        private DatabaseEntryRow? _selectedEntry;
        public DatabaseEntryRow? SelectedEntry
        {
            get => _selectedEntry;
            set => SetProperty(ref _selectedEntry, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    EntriesView.Refresh();
            }
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public int TotalCount => Entries.Count;
        public string DatabasePath => _dataRepository.AllArenaBossesJsonPath;

        public RelayCommand AddEntryCommand { get; }
        public RelayCommand DeleteEntryCommand { get; }
        public RelayCommand SaveDatabaseCommand { get; }
        public RelayCommand ReloadDatabaseCommand { get; }
        public RelayCommand ImportDatabaseCommand { get; }
        public RelayCommand RestoreDatabaseBackupCommand { get; }

        public DatabaseEditorViewModel(
            DataRepository dataRepository,
            AppStateService appStateService)
        {
            _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
            _appStateService = appStateService ?? throw new ArgumentNullException(nameof(appStateService));

            LoadOptions();

            EntriesView = CollectionViewSource.GetDefaultView(Entries);
            EntriesView.Filter = FilterEntry;

            AddEntryCommand = new RelayCommand(_ => AddEntry());
            DeleteEntryCommand = new RelayCommand(_ => DeleteEntry(), _ => SelectedEntry != null);
            SaveDatabaseCommand = new RelayCommand(_ => SaveDatabase());
            ReloadDatabaseCommand = new RelayCommand(_ => ReloadFromDisk());
            ImportDatabaseCommand = new RelayCommand(_ => ImportDatabase());
            RestoreDatabaseBackupCommand = new RelayCommand(_ => RestoreDatabaseBackup());

            LoadFromAppState();
        }

        private void LoadOptions()
        {
            TypeOptions.Clear();
            foreach (var option in HCData.ArenaBossType.OrderBy(x => x.Key))
            {
                TypeOptions.Add(new DatabaseOption
                {
                    Value = option.Key,
                    DisplayName = option.Value
                });
            }

            NightOptions.Clear();
            NightOptions.Add(new DatabaseOption { Value = 0, DisplayName = "No" });
            NightOptions.Add(new DatabaseOption { Value = 1, DisplayName = "Yes" });

            RegionOptions.Clear();
            foreach (var option in HCData.RegionNames.OrderBy(x => x.Key))
            {
                RegionOptions.Add(new DatabaseOption
                {
                    Value = option.Key,
                    DisplayName = option.Value
                });
            }
        }

        public void LoadFromAppState()
        {
            Entries.Clear();

            foreach (var entry in _appStateService.Arenas.OrderBy(x => x.Key))
            {
                Entries.Add(new DatabaseEntryRow
                {
                    Name = entry.Key,
                    Id = entry.Value.id,
                    Type = entry.Value.type,
                    NightBoss = entry.Value.nightBoss,
                    Region = entry.Value.region,
                    Scaling = entry.Value.scaling,
                    Dlc = entry.Value.dlc
                });
            }

            SelectedEntry = Entries.FirstOrDefault();
            EntriesView.Refresh();
            OnPropertyChanged(nameof(TotalCount));
            StatusText = $"Loaded {Entries.Count} database entries.";
        }

        private bool FilterEntry(object obj)
        {
            if (obj is not DatabaseEntryRow entry)
                return false;

            var term = (SearchText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(term))
                return true;

            return entry.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || entry.Id.Contains(term, StringComparison.OrdinalIgnoreCase)
                || entry.Type.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || entry.Region.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || entry.Scaling.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || GetTypeName(entry.Type).Contains(term, StringComparison.OrdinalIgnoreCase)
                || GetRegionName(entry.Region).Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private void AddEntry()
        {
            var row = new DatabaseEntryRow
            {
                Name = BuildUniqueName("New Boss"),
                Id = string.Empty,
                Type = 3,
                NightBoss = 0,
                Region = 1,
                Scaling = 0,
                Dlc = false
            };

            Entries.Add(row);
            SelectedEntry = row;
            EntriesView.Refresh();
            OnPropertyChanged(nameof(TotalCount));
            StatusText = "Added a new database entry.";
        }

        private void DeleteEntry()
        {
            if (SelectedEntry == null)
                return;

            var result = MessageBox.Show(
                $"Delete {SelectedEntry.Name} from the database?",
                "Delete Database Entry",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            Entries.Remove(SelectedEntry);
            SelectedEntry = Entries.FirstOrDefault();
            EntriesView.Refresh();
            OnPropertyChanged(nameof(TotalCount));
            StatusText = "Entry deleted. Save the database to keep this change.";
        }

        private void SaveDatabase()
        {
            if (!TryBuildDatabase(out var entries, out var error))
            {
                MessageBox.Show(error, "Database Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _dataRepository.SaveAllArenaBossDatabase(entries);
                _appStateService.ReloadAll();
                StatusText = $"Saved {entries.Count} database entries.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database could not be saved: {ex.Message}", "Database Editor");
            }
        }

        private void ReloadFromDisk()
        {
            _appStateService.ReloadAll();
            LoadFromAppState();
        }

        private void ImportDatabase()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Main Database JSON",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                _dataRepository.ReplaceAllArenaBossDatabase(dialog.FileName);
                _appStateService.ReloadAll();
                LoadFromAppState();
                StatusText = $"Imported database from {dialog.FileName}.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database could not be imported: {ex.Message}", "Database Editor");
            }
        }

        private void RestoreDatabaseBackup()
        {
            if (!_dataRepository.MainDatabaseBackupExists())
            {
                MessageBox.Show("No main database backup exists.", "Database Editor");
                return;
            }

            var result = MessageBox.Show(
                "Restore the main database backup? Current unsaved changes will be replaced.",
                "Restore Database Backup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _dataRepository.RestoreMainDatabaseBackup();
                _appStateService.ReloadAll();
                LoadFromAppState();
                StatusText = "Restored main database backup.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database backup could not be restored: {ex.Message}", "Database Editor");
            }
        }

        private bool TryBuildDatabase(
            out Dictionary<string, ArenaInfo> entries,
            out string error)
        {
            entries = new Dictionary<string, ArenaInfo>(StringComparer.OrdinalIgnoreCase);
            error = string.Empty;

            foreach (var row in Entries)
            {
                string name = (row.Name ?? string.Empty).Trim();
                string id = (row.Id ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    error = "Every database entry needs a name.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(id))
                {
                    error = $"{name} needs an ID.";
                    return false;
                }

                if (entries.ContainsKey(name))
                {
                    error = $"Duplicate name found: {name}.";
                    return false;
                }

                entries[name] = new ArenaInfo
                {
                    id = id,
                    type = row.Type,
                    nightBoss = row.NightBoss,
                    region = row.Region,
                    scaling = row.Scaling,
                    dlc = row.Dlc
                };
            }

            var duplicateId = entries
                .GroupBy(x => x.Value.id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateId != null)
            {
                error = $"Duplicate ID found: {duplicateId.Key}.";
                return false;
            }

            return true;
        }

        private string BuildUniqueName(string baseName)
        {
            string candidate = baseName;
            int index = 1;

            while (Entries.Any(x => string.Equals(x.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                index++;
                candidate = $"{baseName} {index}";
            }

            return candidate;
        }

        private static string GetTypeName(int type)
        {
            return HCData.ArenaBossType.TryGetValue(type, out var name)
                ? name
                : string.Empty;
        }

        private static string GetRegionName(int region)
        {
            return HCData.RegionNames.TryGetValue(region, out var name)
                ? name
                : string.Empty;
        }
    }
}
