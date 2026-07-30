using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BossArenaRandomizer.Core;
using BossArenaRandomizer.Services;
using Microsoft.Win32;

namespace BossArenaRandomizer.ViewModels
{
    public sealed class GenerationResultLine
    {
        public string Text { get; set; } = string.Empty;
        public bool IsHeader { get; set; }
    }

    public sealed class BatchSeedResultRow
    {
        public int Index { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Seed { get; set; }
        public string OutputPath { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class GenerateViewModel : ViewModelBase
    {
        private readonly SettingsService _settingsService;
        private readonly PresetService _presetService;
        private readonly SeedGenerationService _seedGenerationService;

        private readonly string _basePath;
        private readonly Func<Dictionary<string, ArenaInfo>> _getArenas;
        private readonly Func<Dictionary<string, BossInfo>> _getBosses;
        private readonly Func<FilterArenas> _getArenaFilter;
        private readonly Func<FilterBosses> _getBossFilter;

        private string _lastDebugLog = string.Empty;
        private bool _isApplyingConfiguration;

        public ObservableCollection<string> OptionsPresets { get; } = new();
        public ObservableCollection<string> PairingPresets { get; } = new();
        public ObservableCollection<string> ArenaPresets { get; } = new();
        public ObservableCollection<string> BossPresets { get; } = new();
        public ObservableCollection<string> Configurations { get; } = new();
        public ObservableCollection<GenerationResultLine> ResultLines { get; } = new();
        public ObservableCollection<GenerationResultLine> ValidationLines { get; } = new();
        public ObservableCollection<GenerationResultLine> UniformityLines { get; } = new();
        public ObservableCollection<GenerationResultLine> PairingFrequencyLines { get; } = new();
        public ObservableCollection<BatchSeedResultRow> BatchResults { get; } = new();

        private string _title = "Generate Seeds";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _subtitle = "Create one or many random assignments from your selected arenas, bosses, options preset, and pairing preset.";
        public string Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }

        private string _selectedOptionsPreset = string.Empty;
        public string SelectedOptionsPreset
        {
            get => _selectedOptionsPreset;
            set
            {
                if (SetProperty(ref _selectedOptionsPreset, value))
                {
                    InvalidateActiveConfiguration();
                    OnPropertyChanged(nameof(DashboardSelectedOptionsPreset));
                }
            }
        }

        private string _selectedPairingPreset = string.Empty;
        public string SelectedPairingPreset
        {
            get => _selectedPairingPreset;
            set
            {
                if (SetProperty(ref _selectedPairingPreset, value))
                    InvalidateActiveConfiguration();
            }
        }

        private string _selectedArenaPreset = string.Empty;
        public string SelectedArenaPreset
        {
            get => _selectedArenaPreset;
            set
            {
                if (SetProperty(ref _selectedArenaPreset, value))
                    InvalidateActiveConfiguration();
            }
        }

        private string _selectedBossPreset = string.Empty;
        public string SelectedBossPreset
        {
            get => _selectedBossPreset;
            set
            {
                if (SetProperty(ref _selectedBossPreset, value))
                    InvalidateActiveConfiguration();
            }
        }

        private string _selectedConfiguration = string.Empty;
        public string SelectedConfiguration
        {
            get => _selectedConfiguration;
            set
            {
                string safeValue = value ?? string.Empty;
                if (SetProperty(ref _selectedConfiguration, safeValue) && !string.IsNullOrWhiteSpace(safeValue))
                {
                    ConfigurationName = Path.GetFileNameWithoutExtension(safeValue);
                    _settingsService.SaveSelectedConfiguration(safeValue);
                }
            }
        }

        private string _activeConfiguration = string.Empty;
        public string ActiveConfiguration
        {
            get => _activeConfiguration;
            private set
            {
                if (SetProperty(ref _activeConfiguration, value ?? string.Empty))
                    OnPropertyChanged(nameof(DashboardSelectedConfiguration));
            }
        }

        private string _configurationName = string.Empty;
        public string ConfigurationName
        {
            get => _configurationName;
            set => SetProperty(ref _configurationName, value ?? string.Empty);
        }

        private string _outputFolderPath = string.Empty;
        public string OutputFolderPath
        {
            get => _outputFolderPath;
            set
            {
                if (SetProperty(ref _outputFolderPath, value))
                    OnPropertyChanged(nameof(DashboardOutputPath));
            }
        }

        private int _seedCount = 1;
        public int SeedCount
        {
            get => _seedCount;
            set
            {
                if (SetProperty(ref _seedCount, value < 1 ? 1 : value))
                    _settingsService.SaveGenerateSettings(_seedCount, FileNamePattern);
            }
        }

        private string _replaySeedText = string.Empty;
        public string ReplaySeedText
        {
            get => _replaySeedText;
            set => SetProperty(ref _replaySeedText, value);
        }

        private string _fileNamePattern = "BAR_{index}_{seed}.randomizeopt";
        public string FileNamePattern
        {
            get => _fileNamePattern;
            set
            {
                if (SetProperty(ref _fileNamePattern, value ?? string.Empty))
                    _settingsService.SaveGenerateSettings(SeedCount, _fileNamePattern);
            }
        }

        private bool _clearArenasEnabled;
        public bool ClearArenasEnabled
        {
            get => _clearArenasEnabled;
            set => SetProperty(ref _clearArenasEnabled, value);
        }

        private string _lastGeneratedOutputPath = string.Empty;
        public string LastGeneratedOutputPath
        {
            get => _lastGeneratedOutputPath;
            private set
            {
                if (SetProperty(ref _lastGeneratedOutputPath, value ?? string.Empty))
                    OnPropertyChanged(nameof(DashboardLastGeneratedOutputPath));
            }
        }

        private string _seedText = "Last Seed Used: --";
        public string SeedText
        {
            get => _seedText;
            set
            {
                if (SetProperty(ref _seedText, value))
                    OnPropertyChanged(nameof(DashboardLastSeedText));
            }
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (SetProperty(ref _statusText, value))
                    OnPropertyChanged(nameof(DashboardLastStatusText));
            }
        }

        private bool _isSpoilerRevealed;
        public bool IsSpoilerRevealed
        {
            get => _isSpoilerRevealed;
            set => SetProperty(ref _isSpoilerRevealed, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int SelectedArenaCount => _getArenaFilter().SelectedCount;
        public int SelectedBossCount => _getBossFilter().SelectedCount;

        public string DashboardSelectedOptionsPreset => SelectedOptionsPreset;
        public string DashboardSelectedConfiguration => Path.GetFileNameWithoutExtension(ActiveConfiguration);
        public string DashboardSelectedArenaPreset => SelectedArenaPreset;
        public string DashboardSelectedBossPreset => SelectedBossPreset;
        public string DashboardSelectedPairingPreset => SelectedPairingPreset;
        public string DashboardOutputPath => OutputFolderPath;
        public string DashboardLastSeedText => SeedText;
        public string DashboardLastStatusText => StatusText;
        public string DashboardLastGeneratedOutputPath => LastGeneratedOutputPath;

        public RelayCommand BrowseOutputPathCommand { get; }
        public RelayCommand RefreshOptionsPresetsCommand { get; }
        public RelayCommand RefreshPairingPresetsCommand { get; }
        public RelayCommand RefreshSelectionPresetsCommand { get; }
        public RelayCommand LoadArenaPresetCommand { get; }
        public RelayCommand LoadBossPresetCommand { get; }
        public RelayCommand LoadConfigurationCommand { get; }
        public RelayCommand SaveConfigurationCommand { get; }
        public RelayCommand RenameConfigurationCommand { get; }
        public RelayCommand DuplicateConfigurationCommand { get; }
        public RelayCommand DeleteConfigurationCommand { get; }
        public RelayCommand OpenConfigurationFolderCommand { get; }
        public RelayCommand OpenOutputFolderCommand { get; }
        public RelayCommand RefreshConfigurationsCommand { get; }
        public RelayCommand RefreshAllPresetsCommand { get; }
        public RelayCommand GenerateCommand { get; }
        public RelayCommand ValidatePairingPresetCommand { get; }
        public RelayCommand DryRunCommand { get; }
        public RelayCommand ExportDebugLogCommand { get; }
        public RelayCommand ToggleSpoilerCommand { get; }

        public GenerateViewModel(
            string basePath,
            SettingsService settingsService,
            PresetService presetService,
            SeedGenerationService seedGenerationService,
            Func<Dictionary<string, ArenaInfo>> getArenas,
            Func<Dictionary<string, BossInfo>> getBosses,
            Func<FilterArenas> getArenaFilter,
            Func<FilterBosses> getBossFilter)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));
            _seedGenerationService = seedGenerationService ?? throw new ArgumentNullException(nameof(seedGenerationService));
            _getArenas = getArenas ?? throw new ArgumentNullException(nameof(getArenas));
            _getBosses = getBosses ?? throw new ArgumentNullException(nameof(getBosses));
            _getArenaFilter = getArenaFilter ?? throw new ArgumentNullException(nameof(getArenaFilter));
            _getBossFilter = getBossFilter ?? throw new ArgumentNullException(nameof(getBossFilter));

            BrowseOutputPathCommand = new RelayCommand(_ => BrowseOutputPath());
            RefreshOptionsPresetsCommand = new RelayCommand(_ => LoadOptionsPresets());
            RefreshPairingPresetsCommand = new RelayCommand(_ => LoadPairingPresets());
            RefreshSelectionPresetsCommand = new RelayCommand(_ => LoadSelectionPresets());
            LoadArenaPresetCommand = new RelayCommand(_ => LoadArenaPreset());
            LoadBossPresetCommand = new RelayCommand(_ => LoadBossPreset());
            LoadConfigurationCommand = new RelayCommand(_ => LoadConfiguration());
            SaveConfigurationCommand = new RelayCommand(_ => SaveConfiguration());
            RenameConfigurationCommand = new RelayCommand(_ => RenameConfiguration());
            DuplicateConfigurationCommand = new RelayCommand(_ => DuplicateConfiguration());
            DeleteConfigurationCommand = new RelayCommand(_ => DeleteConfiguration());
            OpenConfigurationFolderCommand = new RelayCommand(_ => OpenConfigurationFolder());
            OpenOutputFolderCommand = new RelayCommand(_ => OpenOutputFolder());
            RefreshConfigurationsCommand = new RelayCommand(_ => LoadConfigurations());
            RefreshAllPresetsCommand = new RelayCommand(_ => RefreshAllPresets());
            GenerateCommand = new RelayCommand(async _ => await RunGenerationAsync(writeOutputFiles: true), _ => !IsLoading);
            ValidatePairingPresetCommand = new RelayCommand(async _ => await ValidatePairingPresetAsync(), _ => !IsLoading);
            DryRunCommand = new RelayCommand(async _ => await RunGenerationAsync(writeOutputFiles: false), _ => !IsLoading);
            ExportDebugLogCommand = new RelayCommand(_ => ExportDebugLog());
            ToggleSpoilerCommand = new RelayCommand(_ => IsSpoilerRevealed = !IsSpoilerRevealed);

            LoadState();
        }

        public void RefreshSelectionSummary()
        {
            OnPropertyChanged(nameof(SelectedArenaCount));
            OnPropertyChanged(nameof(SelectedBossCount));
        }

        public void NotifySelectionChanged()
        {
            InvalidateActiveConfiguration();
            RefreshSelectionSummary();
        }

        private void InvalidateActiveConfiguration()
        {
            if (!_isApplyingConfiguration)
                ActiveConfiguration = string.Empty;
        }

        private void LoadState()
        {
            LoadOptionsPresets();
            LoadPairingPresets();
            LoadSelectionPresets();
            LoadConfigurations();

            OutputFolderPath = _settingsService.GetOutputFolderPath();
            SeedCount = _settingsService.GetSeedCount();
            FileNamePattern = _settingsService.GetFileNamePattern();
            ClearArenasEnabled = _settingsService.GetUseClearArenas();

            var savedPreset = _settingsService.GetSelectedOptionsPreset();
            if (!string.IsNullOrWhiteSpace(savedPreset) && OptionsPresets.Contains(savedPreset))
                SelectedOptionsPreset = savedPreset;

            RefreshSelectionSummary();
        }

        private void LoadOptionsPresets()
        {
            OptionsPresets.Clear();

            foreach (var preset in _presetService.GetOptionsPresetNames())
                OptionsPresets.Add(preset);

            if (string.IsNullOrWhiteSpace(SelectedOptionsPreset) && OptionsPresets.Count > 0)
            {
                var savedPreset = _settingsService.GetSelectedOptionsPreset();
                SelectedOptionsPreset = !string.IsNullOrWhiteSpace(savedPreset) && OptionsPresets.Contains(savedPreset)
                    ? savedPreset
                    : OptionsPresets[0];
            }

            OnPropertyChanged(nameof(DashboardSelectedOptionsPreset));
        }

        private void LoadPairingPresets()
        {
            PairingPresets.Clear();

            foreach (var preset in _presetService.GetPairingPresetFiles())
                PairingPresets.Add(preset);

            if (string.IsNullOrWhiteSpace(SelectedPairingPreset) && PairingPresets.Count > 0)
            {
                var savedPreset = _settingsService.GetSelectedPairingPreset();
                SelectedPairingPreset = !string.IsNullOrWhiteSpace(savedPreset) && PairingPresets.Contains(savedPreset)
                    ? savedPreset
                    : PairingPresets.Contains("everything.json")
                        ? "everything.json"
                        : PairingPresets[0];
            }
        }

        private void BrowseOutputPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Choose Output Base Folder",
                InitialDirectory = Directory.Exists(OutputFolderPath) ? OutputFolderPath : null
            };

            if (dialog.ShowDialog() == true)
            {
                OutputFolderPath = dialog.FolderName;
                _settingsService.SaveOutputFolderPath(OutputFolderPath);
                OnPropertyChanged(nameof(DashboardOutputPath));
            }
        }

        private void LoadSelectionPresets()
        {
            string currentArenaPreset = SelectedArenaPreset;
            ArenaPresets.Clear();
            foreach (var preset in _presetService.GetArenaPresetFiles())
                ArenaPresets.Add(preset);

            string savedArenaPreset = _settingsService.GetLastUsedArenaPreset();
            SelectedArenaPreset = ArenaPresets.Contains(currentArenaPreset)
                ? currentArenaPreset
                : ArenaPresets.Contains(savedArenaPreset)
                    ? savedArenaPreset
                    : ArenaPresets.FirstOrDefault() ?? string.Empty;

            string currentBossPreset = SelectedBossPreset;
            BossPresets.Clear();
            foreach (var preset in _presetService.GetBossPresetFiles())
                BossPresets.Add(preset);

            string savedBossPreset = _settingsService.GetLastUsedBossPreset();
            SelectedBossPreset = BossPresets.Contains(currentBossPreset)
                ? currentBossPreset
                : BossPresets.Contains(savedBossPreset)
                    ? savedBossPreset
                    : BossPresets.FirstOrDefault() ?? string.Empty;
        }

        private void LoadConfigurations()
        {
            string currentConfiguration = SelectedConfiguration;
            Configurations.Clear();
            foreach (var configuration in _presetService.GetConfigurationFiles())
                Configurations.Add(configuration);

            string savedConfiguration = _settingsService.GetSelectedConfiguration();
            SelectedConfiguration = Configurations.Contains(currentConfiguration)
                ? currentConfiguration
                : Configurations.Contains(savedConfiguration)
                    ? savedConfiguration
                    : Configurations.FirstOrDefault() ?? string.Empty;
        }

        private void RefreshAllPresets()
        {
            LoadOptionsPresets();
            LoadPairingPresets();
            LoadSelectionPresets();
            LoadConfigurations();
            StatusText = "Preset lists refreshed";
        }

        private void SaveConfiguration()
        {
            if (string.IsNullOrWhiteSpace(ConfigurationName))
            {
                System.Windows.MessageBox.Show("Enter a configuration name first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedOptionsPreset)
                || string.IsNullOrWhiteSpace(SelectedArenaPreset)
                || string.IsNullOrWhiteSpace(SelectedBossPreset)
                || string.IsNullOrWhiteSpace(SelectedPairingPreset))
            {
                System.Windows.MessageBox.Show("Select a Rando Options, Arena, Boss, and Pairing preset before saving a configuration.");
                return;
            }

            try
            {
                if (_presetService.ConfigurationExists(ConfigurationName))
                {
                    var overwrite = System.Windows.MessageBox.Show(
                        $"Overwrite the configuration '{ConfigurationName}'?",
                        "Overwrite Configuration",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);

                    if (overwrite != System.Windows.MessageBoxResult.Yes)
                        return;
                }

                string fileName = _presetService.SaveConfiguration(ConfigurationName, new PresetConfiguration
                {
                    RandoOptionsPreset = SelectedOptionsPreset,
                    ArenaPreset = SelectedArenaPreset,
                    BossPreset = SelectedBossPreset,
                    PairingPreset = SelectedPairingPreset
                });

                LoadConfigurations();
                SelectedConfiguration = fileName;
                ActiveConfiguration = fileName;
                StatusText = $"Saved configuration: {Path.GetFileNameWithoutExtension(fileName)}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Configuration could not be saved: {ex.Message}");
            }
        }

        private void RenameConfiguration()
        {
            if (string.IsNullOrWhiteSpace(SelectedConfiguration) || string.IsNullOrWhiteSpace(ConfigurationName))
            {
                System.Windows.MessageBox.Show("Select a configuration and enter its new name first.");
                return;
            }

            string oldFileName = SelectedConfiguration;
            string oldDisplayName = Path.GetFileNameWithoutExtension(oldFileName);
            string newDisplayName = ConfigurationName.Trim();
            if (string.Equals(oldDisplayName, newDisplayName, StringComparison.OrdinalIgnoreCase))
                return;

            var confirm = System.Windows.MessageBox.Show(
                $"Rename '{oldDisplayName}' to '{newDisplayName}'?",
                "Rename Configuration",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            if (confirm != System.Windows.MessageBoxResult.Yes)
                return;

            try
            {
                string newFileName = _presetService.RenameConfiguration(oldFileName, newDisplayName);
                bool wasActive = string.Equals(ActiveConfiguration, oldFileName, StringComparison.OrdinalIgnoreCase);
                LoadConfigurations();
                SelectedConfiguration = newFileName;
                if (wasActive)
                    ActiveConfiguration = newFileName;
                _settingsService.SaveSelectedConfiguration(newFileName);
                StatusText = $"Renamed configuration to {Path.GetFileNameWithoutExtension(newFileName)}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Configuration could not be renamed: {ex.Message}");
            }
        }

        private void DuplicateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(SelectedConfiguration) || string.IsNullOrWhiteSpace(ConfigurationName))
            {
                System.Windows.MessageBox.Show("Select a configuration and enter a name for the copy first.");
                return;
            }

            string newName = ConfigurationName.Trim();
            if (_presetService.ConfigurationExists(newName))
            {
                System.Windows.MessageBox.Show("A configuration with that name already exists. Choose another name.");
                return;
            }

            try
            {
                string newFileName = _presetService.DuplicateConfiguration(SelectedConfiguration, newName);
                LoadConfigurations();
                SelectedConfiguration = newFileName;
                StatusText = $"Duplicated configuration as {Path.GetFileNameWithoutExtension(newFileName)}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Configuration could not be duplicated: {ex.Message}");
            }
        }

        private void DeleteConfiguration()
        {
            if (string.IsNullOrWhiteSpace(SelectedConfiguration))
            {
                System.Windows.MessageBox.Show("Select a configuration first.");
                return;
            }

            string fileName = SelectedConfiguration;
            string displayName = Path.GetFileNameWithoutExtension(fileName);
            var confirm = System.Windows.MessageBox.Show(
                $"Delete the configuration '{displayName}'?",
                "Delete Configuration",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (confirm != System.Windows.MessageBoxResult.Yes)
                return;

            try
            {
                _presetService.DeleteConfiguration(fileName);
                if (string.Equals(ActiveConfiguration, fileName, StringComparison.OrdinalIgnoreCase))
                    ActiveConfiguration = string.Empty;
                SelectedConfiguration = string.Empty;
                ConfigurationName = string.Empty;
                _settingsService.SaveSelectedConfiguration(string.Empty);
                LoadConfigurations();
                StatusText = $"Deleted configuration: {displayName}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Configuration could not be deleted: {ex.Message}");
            }
        }

        private void OpenConfigurationFolder()
        {
            OpenFolder(_presetService.ConfigurationDirectory, "The configuration folder is not available.");
        }

        public void OpenOutputFolder()
        {
            string folder = !string.IsNullOrWhiteSpace(LastGeneratedOutputPath)
                ? Path.GetDirectoryName(LastGeneratedOutputPath) ?? OutputFolderPath
                : OutputFolderPath;
            OpenFolder(folder, "Select an output folder first.");
        }

        private static void OpenFolder(string folder, string missingMessage)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                System.Windows.MessageBox.Show(missingMessage);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }

        private bool LoadConfiguration()
        {
            if (string.IsNullOrWhiteSpace(SelectedConfiguration))
            {
                System.Windows.MessageBox.Show("Select a configuration first.");
                return false;
            }

            try
            {
                PresetConfiguration configuration = _presetService.LoadConfiguration(SelectedConfiguration);
                var missing = new List<string>();

                if (!_presetService.OptionsPresetExists(configuration.RandoOptionsPreset))
                    missing.Add($"Rando Options: {configuration.RandoOptionsPreset}");
                if (!_presetService.ArenaPresetExists(configuration.ArenaPreset))
                    missing.Add($"Arena preset: {configuration.ArenaPreset}");
                if (!_presetService.BossPresetExists(configuration.BossPreset))
                    missing.Add($"Boss preset: {configuration.BossPreset}");
                if (!_presetService.PairingPresetExists(configuration.PairingPreset))
                    missing.Add($"Pairing preset: {configuration.PairingPreset}");

                if (missing.Count > 0)
                {
                    System.Windows.MessageBox.Show(
                        "Configuration cannot be loaded because these files are missing:\n\n" + string.Join("\n", missing));
                    return false;
                }

                _isApplyingConfiguration = true;
                try
                {
                    SelectedOptionsPreset = configuration.RandoOptionsPreset;
                    SelectedArenaPreset = configuration.ArenaPreset;
                    SelectedBossPreset = configuration.BossPreset;
                    SelectedPairingPreset = configuration.PairingPreset;

                    ApplyArenaPresetSelection(SelectedArenaPreset);
                    ApplyBossPresetSelection(SelectedBossPreset);
                }
                finally
                {
                    _isApplyingConfiguration = false;
                }
                _settingsService.SaveSelectedOptionsPreset(SelectedOptionsPreset);
                _settingsService.SaveSelectedPairingPreset(SelectedPairingPreset);
                _settingsService.SaveSelectedConfiguration(SelectedConfiguration);

                ConfigurationName = Path.GetFileNameWithoutExtension(SelectedConfiguration);
                ActiveConfiguration = SelectedConfiguration;
                StatusText = $"Loaded configuration: {ConfigurationName}";
                RefreshSelectionSummary();
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Configuration could not be loaded: {ex.Message}");
                return false;
            }
        }

        private void LoadArenaPreset()
        {
            if (string.IsNullOrWhiteSpace(SelectedArenaPreset) || !_presetService.ArenaPresetExists(SelectedArenaPreset))
            {
                System.Windows.MessageBox.Show("Please select a valid arena preset.");
                return;
            }

            ApplyArenaPresetSelection(SelectedArenaPreset);
            StatusText = $"Loaded arena preset: {SelectedArenaPreset}";
        }

        private void LoadBossPreset()
        {
            if (string.IsNullOrWhiteSpace(SelectedBossPreset) || !_presetService.BossPresetExists(SelectedBossPreset))
            {
                System.Windows.MessageBox.Show("Please select a valid boss preset.");
                return;
            }

            ApplyBossPresetSelection(SelectedBossPreset);
            StatusText = $"Loaded boss preset: {SelectedBossPreset}";
        }

        private void ApplyArenaPresetSelection(string presetName)
        {
            var selectedIds = _presetService.LoadArenaPresetIds(presetName).ToHashSet();
            HCFilterIds.CustomArenas = selectedIds;
            foreach (var arena in _getArenaFilter().ArenaSelections)
                arena.IsSelected = selectedIds.Contains(arena.Id);

            _settingsService.SaveLastUsedArenaPreset(presetName);
            RefreshSelectionSummary();
        }

        private void ApplyBossPresetSelection(string presetName)
        {
            var selectedIds = _presetService.LoadBossPresetIds(presetName).ToHashSet();
            HCFilterIds.CustomBosses = selectedIds;
            foreach (var boss in _getBossFilter().BossSelections)
                boss.IsSelected = selectedIds.Contains(boss.Id);

            _settingsService.SaveLastUsedBossPreset(presetName);
            RefreshSelectionSummary();
        }

        private async Task ValidatePairingPresetAsync()
        {
            if (IsLoading)
                return;

            var request = BuildGenerationRequest(writeOutputFiles: false);
            if (request == null)
                return;

            try
            {
                IsLoading = true;
                StatusText = "Validating pairing preset...";
                ClearRunOutput();

                var result = await Task.Run(() => _seedGenerationService.Generate(request));
                ApplyReportOutput(result);

                StatusText = result.ValidationLines.Any(x => x.StartsWith("[Error]", StringComparison.OrdinalIgnoreCase))
                    ? "Validation failed"
                    : "Validation passed";

                if (!result.Success && !string.IsNullOrWhiteSpace(result.ErrorMessage))
                    System.Windows.MessageBox.Show(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                StatusText = "Validation failed";
                System.Windows.MessageBox.Show($"Validation failed: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task QuickGenerateAsync()
        {
            if (IsLoading || !LoadConfiguration())
                return;

            await RunGenerationAsync(writeOutputFiles: true, seedCountOverride: 1);
        }

        private async Task RunGenerationAsync(bool writeOutputFiles, int? seedCountOverride = null)
        {
            if (IsLoading)
                return;

            var request = BuildGenerationRequest(writeOutputFiles, seedCountOverride);
            if (request == null)
                return;

            try
            {
                IsLoading = true;
                StatusText = writeOutputFiles ? "Generating batch..." : "Testing randomization...";
                SeedText = "Last Seed Used: --";
                IsSpoilerRevealed = false;
                ClearRunOutput();

                var result = await Task.Run(() => _seedGenerationService.Generate(request));
                ApplyGenerationOutput(result);

                if (!result.Success)
                {
                    StatusText = writeOutputFiles ? "Generation failed" : "Test failed";
                    SeedText = "Last Seed Used: --";
                    System.Windows.MessageBox.Show(result.ErrorMessage);
                    RaiseDashboardProperties();
                    return;
                }

                int successCount = result.BatchResults.Count(x => x.Success);
                int failCount = result.BatchResults.Count(x => !x.Success);
                LastGeneratedOutputPath = result.BatchResults
                    .LastOrDefault(x => x.Success && !string.IsNullOrWhiteSpace(x.OutputPath))
                    ?.OutputPath ?? string.Empty;

                SeedText = $"Last Seed Used: {result.LastSeed}";
                StatusText = writeOutputFiles
                    ? $"Complete - {successCount} succeeded, {failCount} failed"
                    : $"Dry run complete - {successCount} passed, {failCount} failed";

                _settingsService.SaveSelectedPairingPreset(SelectedPairingPreset);

                if (writeOutputFiles)
                {
                    _settingsService.SaveSelectedOptionsPreset(SelectedOptionsPreset);
                    _settingsService.SaveGenerateSettings(SeedCount, FileNamePattern);
                    _settingsService.SaveGenerationFlags(
                        ClearArenasEnabled,
                        false,
                        false
                    );
                }

                RaiseDashboardProperties();
            }
            catch (Exception ex)
            {
                StatusText = writeOutputFiles ? "Generation failed" : "Test failed";
                SeedText = "Last Seed Used: --";
                System.Windows.MessageBox.Show($"{(writeOutputFiles ? "Generation" : "Test")} failed: {ex.Message}");
                RaiseDashboardProperties();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private GenerationRequest? BuildGenerationRequest(bool writeOutputFiles, int? seedCountOverride = null)
        {
            RefreshSelectionSummary();

            if (string.IsNullOrWhiteSpace(SelectedOptionsPreset))
            {
                System.Windows.MessageBox.Show("Please load an options preset.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(SelectedPairingPreset))
            {
                System.Windows.MessageBox.Show("Please select a pairing preset.");
                return null;
            }

            if (writeOutputFiles && string.IsNullOrWhiteSpace(OutputFolderPath))
            {
                System.Windows.MessageBox.Show("Please select an output folder first.");
                return null;
            }

            var selectedArenaIds = _getArenaFilter().ArenaSelections
                .Where(a => a.IsSelected)
                .Select(a => a.Id)
                .ToList();

            var selectedBossIds = _getBossFilter().BossSelections
                .Where(b => b.IsSelected)
                .Select(b => b.Id)
                .ToList();

            if (selectedArenaIds.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select at least one arena.");
                return null;
            }

            if (selectedBossIds.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select at least one boss.");
                return null;
            }

            int? replaySeed = null;
            if (!string.IsNullOrWhiteSpace(ReplaySeedText))
            {
                if (!int.TryParse(ReplaySeedText.Trim(), out int parsedSeed) || parsedSeed < 1)
                {
                    System.Windows.MessageBox.Show("Replay seed must be a positive whole number.");
                    return null;
                }

                replaySeed = parsedSeed;
            }

            return new GenerationRequest
            {
                Arenas = _getArenas(),
                Bosses = _getBosses(),
                SelectedArenaIds = selectedArenaIds,
                SelectedBossIds = selectedBossIds,
                BasePath = _basePath,
                OutputFolderPath = OutputFolderPath,
                SelectedOptionsPreset = SelectedOptionsPreset,
                SelectedPairingPreset = SelectedPairingPreset,
                ClearArenasEnabled = ClearArenasEnabled,
                SeedCount = seedCountOverride ?? SeedCount,
                ReplaySeed = replaySeed,
                FileNamePattern = FileNamePattern,
                WriteOutputFiles = writeOutputFiles
            };
        }

        private void ApplyGenerationOutput(GenerationResult result)
        {
            ApplyReportOutput(result);

            foreach (var batch in result.BatchResults)
            {
                BatchResults.Add(new BatchSeedResultRow
                {
                    Index = batch.Index,
                    Status = batch.Success ? "Success" : "Failed",
                    Seed = batch.Seed,
                    OutputPath = batch.OutputPath,
                    Message = batch.Message
                });
            }

            foreach (var group in result.DisplayGroups)
            {
                ResultLines.Add(new GenerationResultLine
                {
                    Text = $"{group.RegionName}:",
                    IsHeader = true
                });

                foreach (var line in group.Lines)
                {
                    ResultLines.Add(new GenerationResultLine
                    {
                        Text = line,
                        IsHeader = false
                    });
                }
            }
        }

        private void ApplyReportOutput(GenerationResult result)
        {
            _lastDebugLog = result.DebugLog;

            ValidationLines.Clear();
            foreach (var line in result.ValidationLines)
                ValidationLines.Add(new GenerationResultLine { Text = line });

            UniformityLines.Clear();
            foreach (var line in result.UniformityLines)
                UniformityLines.Add(new GenerationResultLine { Text = line });

            PairingFrequencyLines.Clear();
            foreach (var line in result.PairingFrequencyLines)
                PairingFrequencyLines.Add(new GenerationResultLine { Text = line });
        }

        private void ClearRunOutput()
        {
            ResultLines.Clear();
            BatchResults.Clear();
            ValidationLines.Clear();
            UniformityLines.Clear();
            PairingFrequencyLines.Clear();
            LastGeneratedOutputPath = string.Empty;
            _lastDebugLog = string.Empty;
        }

        private void ExportDebugLog()
        {
            if (string.IsNullOrWhiteSpace(_lastDebugLog))
            {
                System.Windows.MessageBox.Show("No debug log is available yet.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                FileName = $"BAR_Debug_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                DefaultExt = ".txt",
                Filter = "Text files (*.txt)|*.txt"
            };

            if (dialog.ShowDialog() != true)
                return;

            File.WriteAllText(dialog.FileName, _lastDebugLog);
            StatusText = $"Exported debug log to {dialog.FileName}";
        }

        private void RaiseDashboardProperties()
        {
            OnPropertyChanged(nameof(DashboardSelectedOptionsPreset));
            OnPropertyChanged(nameof(DashboardOutputPath));
            OnPropertyChanged(nameof(DashboardLastSeedText));
            OnPropertyChanged(nameof(DashboardLastStatusText));
            OnPropertyChanged(nameof(DashboardLastGeneratedOutputPath));
        }
    }
}
