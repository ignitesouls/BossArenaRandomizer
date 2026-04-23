using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BossArenaRandomizer.Services;
using Microsoft.Win32;
using BossArenaRandomizer.Core;

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

        public ObservableCollection<string> OptionsPresets { get; } = new();
        public ObservableCollection<GenerationResultLine> ResultLines { get; } = new();
        public ObservableCollection<BatchSeedResultRow> BatchResults { get; } = new();

        private string _title = "Generate Seeds";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _subtitle = "Create one or many random assignments from your selected arenas, bosses, and options preset.";
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
                    OnPropertyChanged(nameof(DashboardSelectedOptionsPreset));
                }
            }
        }

        private string _outputPath = string.Empty;
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (SetProperty(ref _outputPath, value))
                {
                    OnPropertyChanged(nameof(DashboardOutputPath));
                }
            }
        }

        private int _seedCount = 1;
        public int SeedCount
        {
            get => _seedCount;
            set => SetProperty(ref _seedCount, value < 1 ? 1 : value);
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

        private bool _arenaSizeRestrictionEnabled;
        public bool ArenaSizeRestrictionEnabled
        {
            get => _arenaSizeRestrictionEnabled;
            set => SetProperty(ref _arenaSizeRestrictionEnabled, value);
        }

        private bool _bossRushDifficultyCurveEnabled;
        public bool BossRushDifficultyCurveEnabled
        {
            get => _bossRushDifficultyCurveEnabled;
            set => SetProperty(ref _bossRushDifficultyCurveEnabled, value);
        }

        private bool _looseDifficultyEnabled;
        public bool LooseDifficultyEnabled
        {
            get => _looseDifficultyEnabled;
            set => SetProperty(ref _looseDifficultyEnabled, value);
        }

        private string _seedText = "Last Seed Used: --";
        public string SeedText
        {
            get => _seedText;
            set
            {
                if (SetProperty(ref _seedText, value))
                {
                    OnPropertyChanged(nameof(DashboardLastSeedText));
                }
            }
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (SetProperty(ref _statusText, value))
                {
                    OnPropertyChanged(nameof(DashboardLastStatusText));
                }
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
        public RelayCommand GenerateCommand { get; }
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
            GenerateCommand = new RelayCommand(async _ => await GenerateAsync(), _ => !IsLoading);
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

            OutputPath = _settingsService.GetOutputPath();
            ClearArenasEnabled = _settingsService.GetUseClearArenas();
            ArenaSizeRestrictionEnabled = _settingsService.GetUseArenaSizeRestriction();
            BossRushDifficultyCurveEnabled = _settingsService.GetUseBossRushDifficultyCurve();
            LooseDifficultyEnabled = _settingsService.GetUseLooseDifficulty();

            var savedPreset = _settingsService.GetSelectedOptionsPreset();
            if (!string.IsNullOrWhiteSpace(savedPreset) && OptionsPresets.Contains(savedPreset))
            {
                SelectedOptionsPreset = savedPreset;
            }

            RefreshSelectionSummary();
        }

        private void LoadOptionsPresets()
        {
            OptionsPresets.Clear();

            foreach (var preset in _presetService.GetOptionsPresetNames())
            {
                OptionsPresets.Add(preset);
            }

            if (string.IsNullOrWhiteSpace(SelectedOptionsPreset) && OptionsPresets.Count > 0)
            {
                var savedPreset = _settingsService.GetSelectedOptionsPreset();
                if (!string.IsNullOrWhiteSpace(savedPreset) && OptionsPresets.Contains(savedPreset))
                    SelectedOptionsPreset = savedPreset;
                else
                    SelectedOptionsPreset = OptionsPresets[0];
            }

            OnPropertyChanged(nameof(DashboardSelectedOptionsPreset));
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

        private async Task GenerateAsync()
        {
            if (IsLoading)
                return;

            RefreshSelectionSummary();

            if (string.IsNullOrWhiteSpace(SelectedOptionsPreset))
            {
                System.Windows.MessageBox.Show("Please load an options preset.");
                return;
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
                return;
            }

            if (selectedBossIds.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select at least one boss.");
                return;
            }

            var request = new GenerationRequest
            {
                Arenas = _getArenas(),
                Bosses = _getBosses(),
                SelectedArenaIds = selectedArenaIds,
                SelectedBossIds = selectedBossIds,
                BasePath = _basePath,
                OutputPath = OutputPath,
                SelectedOptionsPreset = SelectedOptionsPreset,
                ClearArenasEnabled = ClearArenasEnabled,
                ArenaSizeRestrictionEnabled = ArenaSizeRestrictionEnabled,
                BossRushDifficultyCurveEnabled = BossRushDifficultyCurveEnabled,
                LooseDifficultyEnabled = LooseDifficultyEnabled,
                SeedCount = SeedCount,
                FileNamePattern = FileNamePattern
            };

            try
            {
                IsLoading = true;
                StatusText = "Generating batch...";
                SeedText = "Last Seed Used: --";
                IsSpoilerRevealed = false;

                ResultLines.Clear();
                BatchResults.Clear();

                var result = await Task.Run(() => _seedGenerationService.Generate(request));

                if (!result.Success)
                {
                    StatusText = "Generation failed";
                    SeedText = "Last Seed Used: --";
                    System.Windows.MessageBox.Show(result.ErrorMessage);

                    OnPropertyChanged(nameof(DashboardLastSeedText));
                    OnPropertyChanged(nameof(DashboardLastStatusText));
                    return;
                }

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

                int successCount = result.BatchResults.Count(x => x.Success);
                int failCount = result.BatchResults.Count(x => !x.Success);

                SeedText = $"Last Seed Used: {result.LastSeed}";
                StatusText = $"Complete - {successCount} succeeded, {failCount} failed";

                _settingsService.SaveSelectedOptionsPreset(SelectedOptionsPreset);
                _settingsService.SaveGenerationFlags(
                    ClearArenasEnabled,
                    ArenaSizeRestrictionEnabled,
                    BossRushDifficultyCurveEnabled,
                    LooseDifficultyEnabled
                );

                OnPropertyChanged(nameof(DashboardSelectedOptionsPreset));
                OnPropertyChanged(nameof(DashboardOutputPath));
                OnPropertyChanged(nameof(DashboardLastSeedText));
                OnPropertyChanged(nameof(DashboardLastStatusText));
            }
            catch (Exception ex)
            {
                StatusText = "Generation failed";
                SeedText = "Last Seed Used: --";
                System.Windows.MessageBox.Show($"Generation failed: {ex.Message}");

                OnPropertyChanged(nameof(DashboardLastSeedText));
                OnPropertyChanged(nameof(DashboardLastStatusText));
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}