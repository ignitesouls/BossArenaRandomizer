using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public ObservableCollection<string> OptionsPresets { get; } = new();
        public ObservableCollection<string> PairingPresets { get; } = new();
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
                    OnPropertyChanged(nameof(DashboardSelectedOptionsPreset));
            }
        }

        private string _selectedPairingPreset = string.Empty;
        public string SelectedPairingPreset
        {
            get => _selectedPairingPreset;
            set => SetProperty(ref _selectedPairingPreset, value);
        }

        private string _outputPath = string.Empty;
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (SetProperty(ref _outputPath, value))
                    OnPropertyChanged(nameof(DashboardOutputPath));
            }
        }

        private int _seedCount = 1;
        public int SeedCount
        {
            get => _seedCount;
            set => SetProperty(ref _seedCount, value < 1 ? 1 : value);
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
            set => SetProperty(ref _fileNamePattern, value);
        }

        private bool _clearArenasEnabled;
        public bool ClearArenasEnabled
        {
            get => _clearArenasEnabled;
            set => SetProperty(ref _clearArenasEnabled, value);
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
        public string DashboardOutputPath => OutputPath;
        public string DashboardLastSeedText => SeedText;
        public string DashboardLastStatusText => StatusText;

        public RelayCommand BrowseOutputPathCommand { get; }
        public RelayCommand RefreshOptionsPresetsCommand { get; }
        public RelayCommand RefreshPairingPresetsCommand { get; }
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

        private void LoadState()
        {
            LoadOptionsPresets();
            LoadPairingPresets();

            OutputPath = _settingsService.GetOutputPath();
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
                SelectedPairingPreset = PairingPresets.Contains("everything.json")
                    ? "everything.json"
                    : PairingPresets[0];
            }
        }

        private void BrowseOutputPath()
        {
            var dialog = new SaveFileDialog
            {
                FileName = "BAROptionsFile.randomizeopt",
                DefaultExt = ".randomizeopt",
                Filter = "Randomizer Options File (.randomizeopt)|*.randomizeopt"
            };

            if (dialog.ShowDialog() == true)
            {
                OutputPath = dialog.FileName;
                _settingsService.SaveOutputPath(OutputPath);
                OnPropertyChanged(nameof(DashboardOutputPath));
            }
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

        private async Task RunGenerationAsync(bool writeOutputFiles)
        {
            if (IsLoading)
                return;

            var request = BuildGenerationRequest(writeOutputFiles);
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

                SeedText = $"Last Seed Used: {result.LastSeed}";
                StatusText = writeOutputFiles
                    ? $"Complete - {successCount} succeeded, {failCount} failed"
                    : $"Dry run complete - {successCount} passed, {failCount} failed";

                if (writeOutputFiles)
                {
                    _settingsService.SaveSelectedOptionsPreset(SelectedOptionsPreset);
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

        private GenerationRequest? BuildGenerationRequest(bool writeOutputFiles)
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

            if (writeOutputFiles && string.IsNullOrWhiteSpace(OutputPath))
            {
                System.Windows.MessageBox.Show("Please select an output path first.");
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
                OutputPath = OutputPath,
                SelectedOptionsPreset = SelectedOptionsPreset,
                SelectedPairingPreset = SelectedPairingPreset,
                ClearArenasEnabled = ClearArenasEnabled,
                SeedCount = SeedCount,
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
        }
    }
}
