using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using BossArenaRandomizer.Services;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.ViewModels
{
    public sealed class BossFieldOption
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public sealed class BossEditableField : ViewModelBase
    {
        private string _value = string.Empty;

        public string PropertyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Type ValueType { get; set; } = typeof(string);
        public bool UseDropdown { get; set; }

        public ObservableCollection<BossFieldOption> Options { get; } = new();

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }

    public sealed class BossEditorItem : ViewModelBase
    {
        private string _name = string.Empty;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ObservableCollection<BossEditableField> Fields { get; } = new();

        public BossEditorItem Clone()
        {
            var clone = new BossEditorItem { Name = Name };

            foreach (var field in Fields)
            {
                var newField = new BossEditableField
                {
                    PropertyName = field.PropertyName,
                    DisplayName = field.DisplayName,
                    ValueType = field.ValueType,
                    Value = field.Value,
                    UseDropdown = field.UseDropdown
                };

                foreach (var option in field.Options)
                {
                    newField.Options.Add(new BossFieldOption
                    {
                        Label = option.Label,
                        Value = option.Value
                    });
                }

                clone.Fields.Add(newField);
            }

            return clone;
        }
    }

    public sealed class BossEditorViewModel : ViewModelBase
    {
        private readonly DataRepository _dataRepository;
        private readonly AppStateService _appStateService;
        private readonly Action? _notifyAppReloaded;

        private readonly List<BossEditorItem> _allItems = new();
        private Dictionary<string, BossInfo> _loadedBossSnapshot = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> BossFieldLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = "Boss ID",
            ["bossSize"] = "Boss Size",
            ["bossType"] = "Boss Type",
            ["isTwoPhase"] = "Is Two Phase",
            ["nightBoss"] = "Night Boss",
            ["isDragon"] = "Is Dragon",
            ["isNPC"] = "Is NPC",
            ["canEscape"] = "Can Escape",
            ["isMessmer"] = "Is Messmer",
            ["isMaliketh"] = "Is Maliketh",
            ["isEvergaolIncompatible"] = "Evergaol Incompatible",
            ["isOpenworldIncompatible"] = "Open World Incompatible",
            ["isGodskinDuo"] = "Is Godskin Duo",
            ["isFiregiant"] = "Is Fire Giant",
            ["isHard"] = "Is Hard",
            ["baseDifficulty"] = "Base Difficulty",
            ["spawner"] = "Spawner",
            ["region"] = "Region",
            ["scaling"] = "Scaling",
            ["dlc"] = "DLC"
        };

        private static readonly Dictionary<string, List<BossFieldOption>> BossFieldOptions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["region"] = HCData.RegionNames
                .OrderBy(x => x.Key)
                .Select(x => new BossFieldOption
                {
                    Label = x.Value,
                    Value = x.Key.ToString(CultureInfo.InvariantCulture)
                })
                .ToList(),

            ["dlc"] = YesNoOptions("Base Game", "DLC"),

            ["bossSize"] = new List<BossFieldOption>
            {
                new BossFieldOption { Label = "1 - Tiny", Value = "1" },
                new BossFieldOption { Label = "2 - Small", Value = "2" },
                new BossFieldOption { Label = "3 - Medium", Value = "3" },
                new BossFieldOption { Label = "4 - Large", Value = "4" },
                new BossFieldOption { Label = "5 - Huge", Value = "5" }
            },

            ["bossType"] = HCData.ArenaBossType
                .OrderBy(x => x.Key)
                .Select(x => new BossFieldOption
                {
                    Label = $"{x.Key} - {x.Value}",
                    Value = x.Key.ToString(CultureInfo.InvariantCulture)
                })
                .ToList(),

            ["baseDifficulty"] = new List<BossFieldOption>
            {
                new BossFieldOption { Label = "1 - Lowest", Value = "1" },
                new BossFieldOption { Label = "2 - Low", Value = "2" },
                new BossFieldOption { Label = "3 - Medium", Value = "3" },
                new BossFieldOption { Label = "4 - High", Value = "4" },
                new BossFieldOption { Label = "5 - Highest", Value = "5" }
            },

            ["isTwoPhase"] = ZeroOneOptions(),
            ["nightBoss"] = ZeroOneOptions(),
            ["isDragon"] = ZeroOneOptions(),
            ["isNPC"] = ZeroOneOptions(),
            ["canEscape"] = ZeroOneOptions(),
            ["isMessmer"] = ZeroOneOptions(),
            ["isMaliketh"] = ZeroOneOptions(),
            ["isEvergaolIncompatible"] = ZeroOneOptions(),
            ["isOpenworldIncompatible"] = ZeroOneOptions(),
            ["isGodskinDuo"] = ZeroOneOptions(),
            ["isHard"] = ZeroOneOptions(),
            ["spawner"] = ZeroOneOptions()
        };

        private static List<BossFieldOption> ZeroOneOptions() => new()
        {
            new BossFieldOption { Label = "0 - No", Value = "0" },
            new BossFieldOption { Label = "1 - Yes", Value = "1" }
        };

        private static List<BossFieldOption> YesNoOptions(string zeroLabel, string oneLabel) => new()
        {
            new BossFieldOption { Label = $"0 - {zeroLabel}", Value = "0" },
            new BossFieldOption { Label = $"1 - {oneLabel}", Value = "1" }
        };

        public ObservableCollection<BossEditorItem> Bosses { get; } = new();

        private string _title = "Boss JSON Editor";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _subtitle = "Edit bosses.json inside the app. All BossInfo fields are shown dynamically so future constraints appear automatically.";
        public string Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyFilter();
            }
        }

        public ObservableCollection<string> FilterFields { get; } = new();

        private string _selectedFilterField = "All";
        public string SelectedFilterField
        {
            get => _selectedFilterField;
            set
            {
                if (SetProperty(ref _selectedFilterField, value))
                    ApplyFilter();
            }
        }

        private string _filterValue = string.Empty;
        public string FilterValue
        {
            get => _filterValue;
            set
            {
                if (SetProperty(ref _filterValue, value))
                    ApplyFilter();
            }
        }

        private void LoadFilterFields()
        {
            FilterFields.Clear();
            FilterFields.Add("All");
            FilterFields.Add("region");
            FilterFields.Add("bossType");
            FilterFields.Add("bossSize");
            FilterFields.Add("dlc");
            FilterFields.Add("isHard");
            FilterFields.Add("baseDifficulty");
            FilterFields.Add("spawner");
        }

        private BossEditorItem? _selectedBoss;
        public BossEditorItem? SelectedBoss
        {
            get => _selectedBoss;
            set
            {
                if (SetProperty(ref _selectedBoss, value))
                    OnPropertyChanged(nameof(SelectedFieldCount));
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
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public string DirtyStatusText => HasUnsavedChanges ? "Unsaved changes" : "Saved";

        public int BossCount => Bosses.Count;
        public int SelectedFieldCount => SelectedBoss?.Fields.Count ?? 0;

        public RelayCommand ReloadCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand NewBossCommand { get; }
        public RelayCommand DuplicateBossCommand { get; }
        public RelayCommand DeleteSelectedBossCommand { get; }
        public RelayCommand ResetSelectedBossCommand { get; }
        public RelayCommand ExportSelectedBossCommand { get; }
        public RelayCommand ImportBossCommand { get; }

        public BossEditorViewModel(
            DataRepository dataRepository,
            AppStateService appStateService,
            Action? notifyAppReloaded = null)
        {
            _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
            _appStateService = appStateService ?? throw new ArgumentNullException(nameof(appStateService));
            _notifyAppReloaded = notifyAppReloaded;

            ReloadCommand = new RelayCommand(_ => Reload());
            SaveCommand = new RelayCommand(_ => Save());
            NewBossCommand = new RelayCommand(_ => CreateNewBoss());
            DuplicateBossCommand = new RelayCommand(_ => DuplicateSelectedBoss(), _ => SelectedBoss != null);
            DeleteSelectedBossCommand = new RelayCommand(_ => DeleteSelectedBoss(), _ => SelectedBoss != null);
            ResetSelectedBossCommand = new RelayCommand(_ => ResetSelectedBoss(), _ => SelectedBoss != null);
            ExportSelectedBossCommand = new RelayCommand(_ => ExportSelectedBoss(), _ => SelectedBoss != null);
            ImportBossCommand = new RelayCommand(_ => ImportBoss());

            LoadFilterFields();
            Reload(force: true);
        }

        private void MarkDirty()
        {
            if (!HasUnsavedChanges)
            {
                HasUnsavedChanges = true;
                OnPropertyChanged(nameof(DirtyStatusText));
            }
        }

        private void MarkSaved()
        {
            HasUnsavedChanges = false;
            OnPropertyChanged(nameof(DirtyStatusText));
        }

        private void HookItem(BossEditorItem item)
        {
            item.PropertyChanged += (_, __) => MarkDirty();
            foreach (var field in item.Fields)
                field.PropertyChanged += (_, __) => MarkDirty();
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

        private void Reload(bool force = false)
        {
            if (!force && !ConfirmDiscardChanges("Reload from disk"))
                return;

            _loadedBossSnapshot = _dataRepository.LoadBossDictionary();
            _allItems.Clear();

            foreach (var kvp in _loadedBossSnapshot.OrderBy(x => x.Key))
            {
                var item = CreateBossEditorItem(kvp.Key, kvp.Value);
                HookItem(item);
                _allItems.Add(item);
            }

            ApplyFilter();
            SelectedBoss = Bosses.FirstOrDefault();
            StatusText = $"Loaded {_allItems.Count} bosses from bosses.json.";
            OnPropertyChanged(nameof(BossCount));
            MarkSaved();
        }

        private void ApplyFilter()
        {
            string term = (SearchText ?? string.Empty).Trim();
            string selectedField = (SelectedFilterField ?? "All").Trim();
            string filterValue = (FilterValue ?? string.Empty).Trim();

            Bosses.Clear();

            IEnumerable<BossEditorItem> filtered = _allItems;

            if (!string.IsNullOrWhiteSpace(term))
            {
                filtered = filtered.Where(item =>
                    item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    item.Fields.Any(f =>
                        f.PropertyName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        f.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        f.Value.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        GetDisplayValue(f).Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(filterValue))
            {
                if (string.Equals(selectedField, "All", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(item =>
                        item.Name.Contains(filterValue, StringComparison.OrdinalIgnoreCase) ||
                        item.Fields.Any(f =>
                            f.PropertyName.Contains(filterValue, StringComparison.OrdinalIgnoreCase) ||
                            f.DisplayName.Contains(filterValue, StringComparison.OrdinalIgnoreCase) ||
                            f.Value.Contains(filterValue, StringComparison.OrdinalIgnoreCase) ||
                            GetDisplayValue(f).Contains(filterValue, StringComparison.OrdinalIgnoreCase)));
                }
                else
                {
                    filtered = filtered.Where(item =>
                        item.Fields.Any(f =>
                            string.Equals(f.PropertyName, selectedField, StringComparison.OrdinalIgnoreCase) &&
                            (f.Value.Contains(filterValue, StringComparison.OrdinalIgnoreCase) ||
                             GetDisplayValue(f).Contains(filterValue, StringComparison.OrdinalIgnoreCase))));
                }
            }

            foreach (var boss in filtered.OrderBy(x => x.Name))
                Bosses.Add(boss);

            if (SelectedBoss != null && !Bosses.Contains(SelectedBoss))
                SelectedBoss = Bosses.FirstOrDefault();

            OnPropertyChanged(nameof(BossCount));
            OnPropertyChanged(nameof(SelectedFieldCount));
        }

        private static string GetDisplayValue(BossEditableField field)
        {
            if (field.UseDropdown && field.Options.Count > 0)
            {
                var match = field.Options.FirstOrDefault(o => o.Value == field.Value);
                if (match != null)
                    return match.Label;
            }

            return field.Value ?? string.Empty;
        }

        private EditorValidationResult ValidateBeforeSave()
        {
            var result = new EditorValidationResult();

            if (_allItems.Count == 0)
            {
                result.Errors.Add("There are no boss entries to save.");
                return result;
            }

            var nameGroups = _allItems
                .GroupBy(x => (x.Name ?? string.Empty).Trim(), System.StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var group in nameGroups)
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                {
                    result.Errors.Add("Boss name cannot be empty.");
                    continue;
                }

                if (group.Count() > 1)
                    result.Errors.Add($"Duplicate boss name: '{group.Key}'.");
            }

            var idValues = new List<(string BossName, string IdValue)>();

            foreach (var item in _allItems)
            {
                var idField = item.Fields.FirstOrDefault(f => f.PropertyName == "id");
                var regionField = item.Fields.FirstOrDefault(f => f.PropertyName == "region");
                var typeField = item.Fields.FirstOrDefault(f => f.PropertyName == "bossType");

                if (idField == null || string.IsNullOrWhiteSpace(idField.Value))
                    result.Errors.Add($"Boss '{item.Name}' is missing required field: id.");
                else
                    idValues.Add((item.Name, idField.Value.Trim()));

                if (regionField == null || !int.TryParse(regionField.Value, out _))
                    result.Errors.Add($"Boss '{item.Name}' has an invalid region value.");

                if (typeField == null || !int.TryParse(typeField.Value, out _))
                    result.Errors.Add($"Boss '{item.Name}' has an invalid bossType value.");

                foreach (var field in item.Fields)
                {
                    if (field.ValueType == typeof(int))
                    {
                        if (!int.TryParse(field.Value, out _))
                            result.Errors.Add($"Boss '{item.Name}' field '{field.DisplayName}' must be an integer.");
                    }
                    else if (field.ValueType == typeof(double))
                    {
                        if (!double.TryParse(field.Value, out _))
                            result.Errors.Add($"Boss '{item.Name}' field '{field.DisplayName}' must be a number.");
                    }
                    else if (field.ValueType == typeof(float))
                    {
                        if (!float.TryParse(field.Value, out _))
                            result.Errors.Add($"Boss '{item.Name}' field '{field.DisplayName}' must be a number.");
                    }

                    if (field.UseDropdown && field.Options.Count > 0)
                    {
                        bool validOption = field.Options.Any(o => o.Value == field.Value);
                        if (!validOption)
                            result.Errors.Add($"Boss '{item.Name}' field '{field.DisplayName}' has an invalid option value '{field.Value}'.");
                    }
                }
            }

            foreach (var group in idValues.GroupBy(x => x.IdValue, System.StringComparer.OrdinalIgnoreCase))
            {
                if (group.Count() > 1)
                {
                    var names = string.Join(", ", group.Select(x => x.BossName));
                    result.Errors.Add($"Duplicate boss id '{group.Key}' used by: {names}.");
                }
            }

            return result;
        }

        private void Save()
        {
            var validation = ValidateBeforeSave();
            if (!validation.IsValid)
            {
                StatusText = $"Save blocked: {validation.Errors.Count} validation error(s).";

                System.Windows.MessageBox.Show(
                    validation.ToDisplayText(),
                    "Boss Save Validation",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);

                return;
            }

            var output = new Dictionary<string, BossInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in _allItems)
            {
                var boss = BuildBossInfo(item);
                output[item.Name] = boss;
            }

            _dataRepository.BackupBossJson();
            _dataRepository.SaveBossDictionary(output);

            _loadedBossSnapshot = output.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase);

            _appStateService.ReloadAll();
            _notifyAppReloaded?.Invoke();

            StatusText = $"Saved {_allItems.Count} bosses to bosses.json and reloaded app data.";
            MarkSaved();
        }

        private void CreateNewBoss()
        {
            var item = CreateBossEditorItem("New Boss", new BossInfo());
            HookItem(item);
            _allItems.Add(item);

            ApplyFilter();
            SelectedBoss = item;
            StatusText = "New boss added. Rename and edit before saving.";
            MarkDirty();
        }

        private void DuplicateSelectedBoss()
        {
            if (SelectedBoss == null)
                return;

            var copy = SelectedBoss.Clone();
            copy.Name += " Copy";
            HookItem(copy);

            _allItems.Add(copy);
            ApplyFilter();
            SelectedBoss = copy;
            StatusText = "Boss duplicated.";
            MarkDirty();
        }

        private void DeleteSelectedBoss()
        {
            if (SelectedBoss == null)
                return;

            var result = MessageBox.Show(
                $"Delete '{SelectedBoss.Name}' from the editor?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            string deletedName = SelectedBoss.Name;
            _allItems.Remove(SelectedBoss);

            ApplyFilter();
            SelectedBoss = Bosses.FirstOrDefault();

            StatusText = $"Deleted boss '{deletedName}'. Save to persist changes.";
            MarkDirty();
        }

        private void ResetSelectedBoss()
        {
            if (SelectedBoss == null)
                return;

            if (!_loadedBossSnapshot.TryGetValue(SelectedBoss.Name, out var original))
            {
                StatusText = "This boss does not exist in the last loaded file snapshot.";
                return;
            }

            string selectedName = SelectedBoss.Name;
            int index = _allItems.IndexOf(SelectedBoss);
            if (index < 0)
                return;

            var restored = CreateBossEditorItem(selectedName, original);
            HookItem(restored);
            _allItems[index] = restored;

            ApplyFilter();
            SelectedBoss = Bosses.FirstOrDefault(x => x.Name == selectedName);

            StatusText = $"Reset boss '{selectedName}' to last loaded values.";
            MarkDirty();
        }

        private void ExportSelectedBoss()
        {
            if (SelectedBoss == null)
                return;

            var dialog = new SaveFileDialog
            {
                Title = "Export Boss Entry",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json",
                FileName = $"{SelectedBoss.Name}.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            var single = new Dictionary<string, BossInfo>
            {
                [SelectedBoss.Name] = BuildBossInfo(SelectedBoss)
            };

            var json = JsonSerializer.Serialize(single, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dialog.FileName, json);

            StatusText = $"Exported '{SelectedBoss.Name}' to {Path.GetFileName(dialog.FileName)}.";
        }

        private void ImportBoss()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Boss Entry",
                Filter = "JSON files (*.json)|*.json",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            string json = File.ReadAllText(dialog.FileName);
            var imported = JsonSerializer.Deserialize<Dictionary<string, BossInfo>>(json);
            if (imported == null || imported.Count == 0)
            {
                StatusText = "Import file did not contain a valid boss entry.";
                return;
            }

            var first = imported.First();
            var existing = _allItems.FirstOrDefault(x => x.Name.Equals(first.Key, StringComparison.OrdinalIgnoreCase));
            var newItem = CreateBossEditorItem(first.Key, first.Value);
            HookItem(newItem);

            if (existing != null)
            {
                int idx = _allItems.IndexOf(existing);
                _allItems[idx] = newItem;
                StatusText = $"Imported and replaced boss '{first.Key}'.";
            }
            else
            {
                _allItems.Add(newItem);
                StatusText = $"Imported boss '{first.Key}'.";
            }

            ApplyFilter();
            SelectedBoss = Bosses.FirstOrDefault(x => x.Name == first.Key);
            MarkDirty();
        }

        private static BossEditorItem CreateBossEditorItem(string name, BossInfo boss)
        {
            var item = new BossEditorItem { Name = name };

            var properties = typeof(BossInfo)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .OrderBy(p => GetSortOrder(p.Name))
                .ThenBy(p => p.Name);

            foreach (var prop in properties)
            {
                object? raw = prop.GetValue(boss);

                var field = new BossEditableField
                {
                    PropertyName = prop.Name,
                    DisplayName = BossFieldLabels.TryGetValue(prop.Name, out var label) ? label : prop.Name,
                    ValueType = prop.PropertyType,
                    Value = raw?.ToString() ?? GetDefaultValueString(prop.PropertyType)
                };

                if (BossFieldOptions.TryGetValue(prop.Name, out var options))
                {
                    field.UseDropdown = true;
                    foreach (var option in options)
                        field.Options.Add(new BossFieldOption { Label = option.Label, Value = option.Value });
                }

                item.Fields.Add(field);
            }

            return item;
        }

        private static BossInfo BuildBossInfo(BossEditorItem item)
        {
            var boss = new BossInfo();

            foreach (var field in item.Fields)
            {
                var prop = typeof(BossInfo).GetProperty(field.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || !prop.CanWrite)
                    continue;

                object? converted = ConvertFieldValue(field.Value, prop.PropertyType);
                prop.SetValue(boss, converted);
            }

            return boss;
        }

        private static object? ConvertFieldValue(string value, Type targetType)
        {
            if (targetType == typeof(string))
                return value ?? string.Empty;
            if (targetType == typeof(int))
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : 0;
            if (targetType == typeof(bool))
                return bool.TryParse(value, out var b) && b;
            if (targetType == typeof(double))
                return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d) ? d : 0d;
            if (targetType == typeof(float))
                return float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f) ? f : 0f;

            return null;
        }

        private static string GetDefaultValueString(Type type)
        {
            if (type == typeof(string)) return string.Empty;
            if (type == typeof(int) || type == typeof(double) || type == typeof(float)) return "0";
            if (type == typeof(bool)) return "false";
            return string.Empty;
        }

        private static int GetSortOrder(string propertyName)
        {
            return propertyName switch
            {
                "id" => 0,
                "bossSize" => 1,
                "bossType" => 2,
                "region" => 3,
                "scaling" => 4,
                "dlc" => 5,
                _ => 100
            };
        }
    }
}