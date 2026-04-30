using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using BossArenaRandomizer.Services;
using Microsoft.Win32;
using BossArenaRandomizer.Core;


namespace BossArenaRandomizer.ViewModels;

public sealed class ArenaViewModel : ViewModelBase
{
    private readonly PresetService _presetService;
    private readonly SettingsService _settingsService;
    private readonly FilterArenas _filterArenas;
    private readonly Action? _notifySelectionsChanged;

    public ObservableCollection<ArenaSelection> ArenaSelections => _filterArenas.ArenaSelections;
    public ICollectionView ArenaSelectionsView { get; }
    public ObservableCollection<string> ArenaPresets { get; } = new();

    private string _title = "Arena Editor";
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _subtitle = "Select, search, and save arena presets for generation.";
    public string Subtitle
    {
        get => _subtitle;
        set => SetProperty(ref _subtitle, value);
    }

    private string _selectedArenaPreset = string.Empty;
    public string SelectedArenaPreset
    {
        get => _selectedArenaPreset;
        set => SetProperty(ref _selectedArenaPreset, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ArenaSelectionsView.Refresh();
        }
    }

    public int SelectedCount => _filterArenas.SelectedCount;
    public int TotalCount => ArenaSelections.Count;

    public RelayCommand ResetAllCommand { get; }
    public RelayCommand ClearAllCommand { get; }
    public RelayCommand SelectBaseGameCommand { get; }
    public RelayCommand SelectDlcCommand { get; }
    public RelayCommand SavePresetCommand { get; }
    public RelayCommand LoadPresetCommand { get; }
    public RelayCommand RefreshPresetsCommand { get; }

    public ArenaViewModel(
        FilterArenas filterArenas,
        PresetService presetService,
        SettingsService settingsService,
        Action? notifySelectionsChanged = null)
    {
        _filterArenas = filterArenas ?? throw new ArgumentNullException(nameof(filterArenas));
        _presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _notifySelectionsChanged = notifySelectionsChanged;

        ArenaSelectionsView = CollectionViewSource.GetDefaultView(ArenaSelections);
        ArenaSelectionsView.Filter = FilterArena;

        foreach (var arena in ArenaSelections)
        {
            arena.PropertyChanged += Arena_PropertyChanged;
        }

        _filterArenas.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FilterArenas.SelectedCount))
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

        var lastPreset = _settingsService.GetLastUsedArenaPreset();
        if (!string.IsNullOrWhiteSpace(lastPreset) && ArenaPresets.Contains(lastPreset))
            SelectedArenaPreset = lastPreset;
    }

    private bool FilterArena(object obj)
    {
        if (obj is not ArenaSelection arena)
            return false;

        var term = (SearchText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(term))
            return true;

        return arena.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
            || arena.Id.Contains(term, StringComparison.OrdinalIgnoreCase)
            || arena.RegionName.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private void Arena_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ArenaSelection.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedCount));
            _notifySelectionsChanged?.Invoke();
        }
    }

    private void ResetAll()
    {
        foreach (var arena in ArenaSelections)
            arena.IsSelected = !HCFilterIds.UncheckArenaBossIds.Contains(arena.Id);

        RaiseSelectionChanged();
    }

    private void ClearAll()
    {
        foreach (var arena in ArenaSelections)
            arena.IsSelected = !HCFilterIds.AllBossArenas.Contains(arena.Id);

        RaiseSelectionChanged();
    }

    private void SelectBaseGame()
    {
        foreach (var arena in ArenaSelections)
            arena.IsSelected = HCFilterIds.BaseGameArenaIds.Contains(arena.Id);

        RaiseSelectionChanged();
    }

    private void SelectDlc()
    {
        foreach (var arena in ArenaSelections)
            arena.IsSelected = HCFilterIds.DLCArenaIds.Contains(arena.Id);

        RaiseSelectionChanged();
    }

    private void SavePreset()
    {
        var dialog = new SaveFileDialog
        {
            InitialDirectory = _presetService.ArenaPresetDirectory,
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = ".json",
            FileName = "MyCustomArenaPreset"
        };

        if (dialog.ShowDialog() != true)
            return;

        var selectedIds = ArenaSelections
            .Where(a => a.IsSelected)
            .Select(a => a.Id)
            .ToList();

        _presetService.SaveArenaPreset(dialog.FileName, selectedIds);
        LoadPresetList();

        var fileName = System.IO.Path.GetFileName(dialog.FileName);
        SelectedArenaPreset = fileName;
        _settingsService.SaveLastUsedArenaPreset(fileName);

        System.Windows.MessageBox.Show("Custom Arena Preset Saved!");
    }

    private void LoadPreset()
    {
        if (string.IsNullOrWhiteSpace(SelectedArenaPreset))
        {
            System.Windows.MessageBox.Show("Please select an arena preset.");
            return;
        }

        if (!_presetService.ArenaPresetExists(SelectedArenaPreset))
        {
            System.Windows.MessageBox.Show("Preset file not found.");
            return;
        }

        var loadedIds = _presetService.LoadArenaPresetIds(SelectedArenaPreset);
        HCFilterIds.CustomArenas = new System.Collections.Generic.HashSet<string>(loadedIds);

        foreach (var arena in ArenaSelections)
            arena.IsSelected = HCFilterIds.CustomArenas.Contains(arena.Id);

        _settingsService.SaveLastUsedArenaPreset(SelectedArenaPreset);
        RaiseSelectionChanged();
    }

    private void LoadPresetList()
    {
        ArenaPresets.Clear();
        foreach (var preset in _presetService.GetArenaPresetFiles())
            ArenaPresets.Add(preset);
    }

    private void RaiseSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        _notifySelectionsChanged?.Invoke();
        ArenaSelectionsView.Refresh();
    }
}