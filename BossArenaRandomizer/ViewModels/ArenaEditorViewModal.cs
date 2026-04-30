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

namespace BossArenaRandomizer.ViewModels;

public sealed class FieldOption
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class EditableField : ViewModelBase
{
    private string _value = string.Empty;

    public string PropertyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Type ValueType { get; set; } = typeof(string);
    public bool UseDropdown { get; set; }

    public ObservableCollection<FieldOption> Options { get; } = new();

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

public sealed class ArenaEditorItem : ViewModelBase
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public ObservableCollection<EditableField> Fields { get; } = new();

    public ArenaEditorItem Clone()
    {
        var clone = new ArenaEditorItem { Name = Name };

        foreach (var field in Fields)
        {
            var newField = new EditableField
            {
                PropertyName = field.PropertyName,
                DisplayName = field.DisplayName,
                ValueType = field.ValueType,
                Value = field.Value,
                UseDropdown = field.UseDropdown
            };

            foreach (var option in field.Options)
            {
                newField.Options.Add(new FieldOption
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

public sealed class ArenaEditorViewModel : ViewModelBase
{
    private readonly DataRepository _dataRepository;
    private readonly AppStateService _appStateService;
    private readonly Action? _notifyAppReloaded;

    private readonly List<ArenaEditorItem> _allItems = new();
    private Dictionary<string, ArenaInfo> _loadedArenaSnapshot = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> ArenaFieldLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "Arena ID",
        ["arenaSize"] = "Arena Size",
        ["arenaType"] = "Arena Type",
        ["twoPhaseNotAllowed"] = "Two Phase Not Allowed",
        ["nightBoss"] = "Night Boss",
        ["dragonNotAllowed"] = "Dragon Not Allowed",
        ["npcNotAllowed"] = "NPC Not Allowed",
        ["isEscapable"] = "Escapable",
        ["messmerNotAllowed"] = "Messmer Not Allowed",
        ["malikethNotAllowed"] = "Maliketh Not Allowed",
        ["godskinduoNotAllowed"] = "Godskin Duo Not Allowed",
        ["hardNotAllowed"] = "Hard Not Allowed (For Boss Rush)",
        ["difficultyPassThrough"] = "Loose Difficulty Curve",
        ["spawner"] = "Spawner",
        ["region"] = "Region",
        ["scaling"] = "Scaling",
        ["dlc"] = "DLC"
    };

    private static readonly Dictionary<string, List<FieldOption>> ArenaFieldOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["region"] = HCData.RegionNames
            .OrderBy(x => x.Key)
            .Select(x => new FieldOption
            {
                Label = x.Value,
                Value = x.Key.ToString(CultureInfo.InvariantCulture)
            })
            .ToList(),

        ["dlc"] = YesNoOptions("Base Game", "DLC"),

        ["arenaSize"] = new List<FieldOption>
        {
            new FieldOption { Label = "1 - Tiny", Value = "1" },
            new FieldOption { Label = "2 - Small", Value = "2" },
            new FieldOption { Label = "3 - Medium", Value = "3" },
            new FieldOption { Label = "4 - Large", Value = "4" },
            new FieldOption { Label = "5 - Huge", Value = "5" }
        },

        ["arenaType"] = HCData.ArenaBossType
            .OrderBy(x => x.Key)
            .Select(x => new FieldOption
            {
                Label = $"{x.Key} - {x.Value}",
                Value = x.Key.ToString(CultureInfo.InvariantCulture)
            })
            .ToList(),

        ["difficultyPassThrough"] = new List<FieldOption>
        {
            new FieldOption { Label = "1 - Easiest Bosses Allowed", Value = "1" },
            new FieldOption { Label = "2 - Easy Bosses", Value = "2" },
            new FieldOption { Label = "3 - Moderate Bosses Allowed", Value = "3" },
            new FieldOption { Label = "4 - Hard Bosses Allowed", Value = "4" },
            new FieldOption { Label = "5 - Hardest Bosses Allowed", Value = "5" }
        },

        ["twoPhaseNotAllowed"] = ZeroOneOptions(),
        ["nightBoss"] = ZeroOneOptions(),
        ["dragonNotAllowed"] = ZeroOneOptions(),
        ["npcNotAllowed"] = ZeroOneOptions(),
        ["isEscapable"] = ZeroOneOptions(),
        ["messmerNotAllowed"] = ZeroOneOptions(),
        ["malikethNotAllowed"] = ZeroOneOptions(),
        ["godskinduoNotAllowed"] = ZeroOneOptions(),
        ["hardNotAllowed"] = ZeroOneOptions(),
        ["spawner"] = ZeroOneOptions()
    };

    private static List<FieldOption> ZeroOneOptions() => new()
    {
        new FieldOption { Label = "0 - No", Value = "0" },
        new FieldOption { Label = "1 - Yes", Value = "1" }
    };

    private static List<FieldOption> YesNoOptions(string zeroLabel, string oneLabel) => new()
    {
        new FieldOption { Label = $"0 - {zeroLabel}", Value = "0" },
        new FieldOption { Label = $"1 - {oneLabel}", Value = "1" }
    };

    public ObservableCollection<ArenaEditorItem> Arenas { get; } = new();

    private string _title = "Arena JSON Editor";
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _subtitle = "Edit arenas.json inside the app. All ArenaInfo fields are shown dynamically so future constraints appear automatically.";
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
        FilterFields.Add("arenaType");
        FilterFields.Add("arenaSize");
        FilterFields.Add("dlc");
        FilterFields.Add("hardNotAllowed");
        FilterFields.Add("difficultyPassThrough");
        FilterFields.Add("spawner");
    }

    private ArenaEditorItem? _selectedArena;
    public ArenaEditorItem? SelectedArena
    {
        get => _selectedArena;
        set
        {
            if (SetProperty(ref _selectedArena, value))
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

    public int ArenaCount => Arenas.Count;
    public int SelectedFieldCount => SelectedArena?.Fields.Count ?? 0;

    public RelayCommand ReloadCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand NewArenaCommand { get; }
    public RelayCommand DuplicateArenaCommand { get; }
    public RelayCommand DeleteSelectedArenaCommand { get; }
    public RelayCommand ResetSelectedArenaCommand { get; }
    public RelayCommand ExportSelectedArenaCommand { get; }
    public RelayCommand ImportArenaCommand { get; }

    public ArenaEditorViewModel(
        DataRepository dataRepository,
        AppStateService appStateService,
        Action? notifyAppReloaded = null)
    {
        _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
        _appStateService = appStateService ?? throw new ArgumentNullException(nameof(appStateService));
        _notifyAppReloaded = notifyAppReloaded;

        ReloadCommand = new RelayCommand(_ => Reload());
        SaveCommand = new RelayCommand(_ => Save());
        NewArenaCommand = new RelayCommand(_ => CreateNewArena());
        DuplicateArenaCommand = new RelayCommand(_ => DuplicateSelectedArena(), _ => SelectedArena != null);
        DeleteSelectedArenaCommand = new RelayCommand(_ => DeleteSelectedArena(), _ => SelectedArena != null);
        ResetSelectedArenaCommand = new RelayCommand(_ => ResetSelectedArena(), _ => SelectedArena != null);
        ExportSelectedArenaCommand = new RelayCommand(_ => ExportSelectedArena(), _ => SelectedArena != null);
        ImportArenaCommand = new RelayCommand(_ => ImportArena());

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

    private void HookItem(ArenaEditorItem item)
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

        _loadedArenaSnapshot = _dataRepository.LoadArenaDictionary();
        _allItems.Clear();

        foreach (var kvp in _loadedArenaSnapshot.OrderBy(x => x.Key))
        {
            var item = CreateArenaEditorItem(kvp.Key, kvp.Value);
            HookItem(item);
            _allItems.Add(item);
        }

        ApplyFilter();
        SelectedArena = Arenas.FirstOrDefault();
        StatusText = $"Loaded {_allItems.Count} arenas from arenas.json.";
        OnPropertyChanged(nameof(ArenaCount));
        MarkSaved();
    }

    private void ApplyFilter()
    {
        string term = (SearchText ?? string.Empty).Trim();
        string selectedField = (SelectedFilterField ?? "All").Trim();
        string filterValue = (FilterValue ?? string.Empty).Trim();

        Arenas.Clear();

        IEnumerable<ArenaEditorItem> filtered = _allItems;

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

        foreach (var arena in filtered.OrderBy(x => x.Name))
            Arenas.Add(arena);

        if (SelectedArena != null && !Arenas.Contains(SelectedArena))
            SelectedArena = Arenas.FirstOrDefault();

        OnPropertyChanged(nameof(ArenaCount));
        OnPropertyChanged(nameof(SelectedFieldCount));
    }

    private static string GetDisplayValue(EditableField field)
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
            result.Errors.Add("There are no arena entries to save.");
            return result;
        }

        var nameGroups = _allItems
            .GroupBy(x => (x.Name ?? string.Empty).Trim(), System.StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in nameGroups)
        {
            if (string.IsNullOrWhiteSpace(group.Key))
            {
                result.Errors.Add("Arena name cannot be empty.");
                continue;
            }

            if (group.Count() > 1)
                result.Errors.Add($"Duplicate arena name: '{group.Key}'.");
        }

        var idValues = new List<(string ArenaName, string IdValue)>();

        foreach (var item in _allItems)
        {
            var idField = item.Fields.FirstOrDefault(f => f.PropertyName == "id");
            var regionField = item.Fields.FirstOrDefault(f => f.PropertyName == "region");
            var typeField = item.Fields.FirstOrDefault(f => f.PropertyName == "arenaType");

            if (idField == null || string.IsNullOrWhiteSpace(idField.Value))
                result.Errors.Add($"Arena '{item.Name}' is missing required field: id.");
            else
                idValues.Add((item.Name, idField.Value.Trim()));

            if (regionField == null || !int.TryParse(regionField.Value, out _))
                result.Errors.Add($"Arena '{item.Name}' has an invalid region value.");

            if (typeField == null || !int.TryParse(typeField.Value, out _))
                result.Errors.Add($"Arena '{item.Name}' has an invalid arenaType value.");

            foreach (var field in item.Fields)
            {
                if (field.ValueType == typeof(int))
                {
                    if (!int.TryParse(field.Value, out _))
                        result.Errors.Add($"Arena '{item.Name}' field '{field.DisplayName}' must be an integer.");
                }
                else if (field.ValueType == typeof(double))
                {
                    if (!double.TryParse(field.Value, out _))
                        result.Errors.Add($"Arena '{item.Name}' field '{field.DisplayName}' must be a number.");
                }
                else if (field.ValueType == typeof(float))
                {
                    if (!float.TryParse(field.Value, out _))
                        result.Errors.Add($"Arena '{item.Name}' field '{field.DisplayName}' must be a number.");
                }

                if (field.UseDropdown && field.Options.Count > 0)
                {
                    bool validOption = field.Options.Any(o => o.Value == field.Value);
                    if (!validOption)
                        result.Errors.Add($"Arena '{item.Name}' field '{field.DisplayName}' has an invalid option value '{field.Value}'.");
                }
            }
        }

        foreach (var group in idValues.GroupBy(x => x.IdValue, System.StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() > 1)
            {
                var names = string.Join(", ", group.Select(x => x.ArenaName));
                result.Errors.Add($"Duplicate arena id '{group.Key}' used by: {names}.");
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
                "Arena Save Validation",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);

            return;
        }

        var output = new Dictionary<string, ArenaInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in _allItems)
        {
            var arena = BuildArenaInfo(item);
            output[item.Name] = arena;
        }

        _dataRepository.BackupArenaJson();
        _dataRepository.SaveArenaDictionary(output);

        _loadedArenaSnapshot = output.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase);

        _appStateService.ReloadAll();
        _notifyAppReloaded?.Invoke();

        StatusText = $"Saved {_allItems.Count} arenas to arenas.json and reloaded app data.";
        MarkSaved();
    }

    private void CreateNewArena()
    {
        var item = CreateArenaEditorItem("New Arena", new ArenaInfo());
        HookItem(item);
        _allItems.Add(item);

        ApplyFilter();
        SelectedArena = item;
        StatusText = "New arena added. Rename and edit before saving.";
        MarkDirty();
    }

    private void DuplicateSelectedArena()
    {
        if (SelectedArena == null)
            return;

        var copy = SelectedArena.Clone();
        copy.Name += " Copy";
        HookItem(copy);

        _allItems.Add(copy);
        ApplyFilter();
        SelectedArena = copy;
        StatusText = "Arena duplicated.";
        MarkDirty();
    }

    private void DeleteSelectedArena()
    {
        if (SelectedArena == null)
            return;

        var result = MessageBox.Show(
            $"Delete '{SelectedArena.Name}' from the editor?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        string deletedName = SelectedArena.Name;
        _allItems.Remove(SelectedArena);

        ApplyFilter();
        SelectedArena = Arenas.FirstOrDefault();

        StatusText = $"Deleted arena '{deletedName}'. Save to persist changes.";
        MarkDirty();
    }

    private void ResetSelectedArena()
    {
        if (SelectedArena == null)
            return;

        if (!_loadedArenaSnapshot.TryGetValue(SelectedArena.Name, out var original))
        {
            StatusText = "This arena does not exist in the last loaded file snapshot.";
            return;
        }

        string selectedName = SelectedArena.Name;
        int index = _allItems.IndexOf(SelectedArena);
        if (index < 0)
            return;

        var restored = CreateArenaEditorItem(selectedName, original);
        HookItem(restored);
        _allItems[index] = restored;

        ApplyFilter();
        SelectedArena = Arenas.FirstOrDefault(x => x.Name == selectedName);

        StatusText = $"Reset arena '{selectedName}' to last loaded values.";
        MarkDirty();
    }

    private void ExportSelectedArena()
    {
        if (SelectedArena == null)
            return;

        var dialog = new SaveFileDialog
        {
            Title = "Export Arena Entry",
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = ".json",
            FileName = $"{SelectedArena.Name}.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        var single = new Dictionary<string, ArenaInfo>
        {
            [SelectedArena.Name] = BuildArenaInfo(SelectedArena)
        };

        var json = JsonSerializer.Serialize(single, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dialog.FileName, json);

        StatusText = $"Exported '{SelectedArena.Name}' to {Path.GetFileName(dialog.FileName)}.";
    }

    private void ImportArena()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Arena Entry",
            Filter = "JSON files (*.json)|*.json",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        string json = File.ReadAllText(dialog.FileName);
        var imported = JsonSerializer.Deserialize<Dictionary<string, ArenaInfo>>(json);
        if (imported == null || imported.Count == 0)
        {
            StatusText = "Import file did not contain a valid arena entry.";
            return;
        }

        var first = imported.First();
        var existing = _allItems.FirstOrDefault(x => x.Name.Equals(first.Key, StringComparison.OrdinalIgnoreCase));
        var newItem = CreateArenaEditorItem(first.Key, first.Value);
        HookItem(newItem);

        if (existing != null)
        {
            int idx = _allItems.IndexOf(existing);
            _allItems[idx] = newItem;
            StatusText = $"Imported and replaced arena '{first.Key}'.";
        }
        else
        {
            _allItems.Add(newItem);
            StatusText = $"Imported arena '{first.Key}'.";
        }

        ApplyFilter();
        SelectedArena = Arenas.FirstOrDefault(x => x.Name == first.Key);
        MarkDirty();
    }

    private static ArenaEditorItem CreateArenaEditorItem(string name, ArenaInfo arena)
    {
        var item = new ArenaEditorItem { Name = name };

        var properties = typeof(ArenaInfo)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .OrderBy(p => GetSortOrder(p.Name))
            .ThenBy(p => p.Name);

        foreach (var prop in properties)
        {
            object? raw = prop.GetValue(arena);

            var field = new EditableField
            {
                PropertyName = prop.Name,
                DisplayName = ArenaFieldLabels.TryGetValue(prop.Name, out var label) ? label : prop.Name,
                ValueType = prop.PropertyType,
                Value = raw?.ToString() ?? GetDefaultValueString(prop.PropertyType)
            };

            if (ArenaFieldOptions.TryGetValue(prop.Name, out var options))
            {
                field.UseDropdown = true;
                foreach (var option in options)
                    field.Options.Add(new FieldOption { Label = option.Label, Value = option.Value });
            }

            item.Fields.Add(field);
        }

        return item;
    }

    private static ArenaInfo BuildArenaInfo(ArenaEditorItem item)
    {
        var arena = new ArenaInfo();

        foreach (var field in item.Fields)
        {
            var prop = typeof(ArenaInfo).GetProperty(field.PropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanWrite)
                continue;

            object? converted = ConvertFieldValue(field.Value, prop.PropertyType);
            prop.SetValue(arena, converted);
        }

        return arena;
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
            "arenaSize" => 1,
            "arenaType" => 2,
            "region" => 3,
            "scaling" => 4,
            "dlc" => 5,
            _ => 100
        };
    }
}