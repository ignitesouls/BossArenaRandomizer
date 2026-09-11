using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BossArenaRandomizer.Services;
using Microsoft.Win32;
using BossArenaRandomizer.Core;


namespace BossArenaRandomizer.ViewModels
{
    public sealed class AnalyzeViewModel : ViewModelBase
    {
        private readonly SeedAnalysisService _seedAnalysisService;

        public ObservableCollection<CheckOption> CheckOptions { get; } = new();
        public ObservableCollection<SeedAnalysisLine> AnalysisLines { get; } = new();
        public ObservableCollection<CustomSeedCheckDefinition> CustomChecks { get; } = new();
        public IReadOnlyList<string> CustomTestOutcomes { get; } = new[]
        {
            CustomSeedCheckOutcomes.Invalid,
            CustomSeedCheckOutcomes.Valid
        };

        private string _title = "Analyze Spoiler Logs";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _subtitle = "Import a generated spoiler log and run validation checks against it.";
        public string Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }

        private string _importedSeedPath = string.Empty;
        public string ImportedSeedPath
        {
            get => _importedSeedPath;
            set => SetProperty(ref _importedSeedPath, value);
        }

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _resultsText = string.Empty;
        public string ResultsText
        {
            get => _resultsText;
            set => SetProperty(ref _resultsText, value);
        }

        public int SelectedCheckCount => CheckOptions.Count(x => x.IsSelected);
        public int AnalysisLineCount => AnalysisLines.Count;
        public int CustomCheckCount => CustomChecks.Count;

        private SeedAnalysisLine? _selectedAnalysisLine;
        public SeedAnalysisLine? SelectedAnalysisLine
        {
            get => _selectedAnalysisLine;
            set => SetProperty(ref _selectedAnalysisLine, value);
        }

        private string _lineEditorStatus = "Saved";
        public string LineEditorStatus
        {
            get => _lineEditorStatus;
            set => SetProperty(ref _lineEditorStatus, value);
        }

        private CustomSeedCheckDefinition? _selectedCustomCheck;
        public CustomSeedCheckDefinition? SelectedCustomCheck
        {
            get => _selectedCustomCheck;
            set => SetProperty(ref _selectedCustomCheck, value);
        }

        private string _customCheckEditorStatus = "Saved";
        public string CustomCheckEditorStatus
        {
            get => _customCheckEditorStatus;
            set => SetProperty(ref _customCheckEditorStatus, value);
        }

        public RelayCommand ImportSeedCommand { get; }
        public RelayCommand RunChecksCommand { get; }
        public RelayCommand ClearCommand { get; }
        public RelayCommand AddAnalysisLineCommand { get; }
        public RelayCommand DeleteAnalysisLineCommand { get; }
        public RelayCommand MoveAnalysisLineUpCommand { get; }
        public RelayCommand MoveAnalysisLineDownCommand { get; }
        public RelayCommand SaveAnalysisLinesCommand { get; }
        public RelayCommand RestoreDefaultAnalysisLinesCommand { get; }
        public RelayCommand AddCustomCheckCommand { get; }
        public RelayCommand DeleteCustomCheckCommand { get; }
        public RelayCommand MoveCustomCheckUpCommand { get; }
        public RelayCommand MoveCustomCheckDownCommand { get; }
        public RelayCommand SaveCustomChecksCommand { get; }

        public AnalyzeViewModel(SeedAnalysisService seedAnalysisService)
        {
            _seedAnalysisService = seedAnalysisService ?? throw new ArgumentNullException(nameof(seedAnalysisService));

            ImportSeedCommand = new RelayCommand(_ => ImportSeed());
            RunChecksCommand = new RelayCommand(_ => RunChecks());
            ClearCommand = new RelayCommand(_ => Clear());
            AddAnalysisLineCommand = new RelayCommand(_ => AddAnalysisLine());
            DeleteAnalysisLineCommand = new RelayCommand(_ => DeleteAnalysisLine(), _ => SelectedAnalysisLine != null);
            MoveAnalysisLineUpCommand = new RelayCommand(_ => MoveAnalysisLine(-1), _ => CanMoveAnalysisLine(-1));
            MoveAnalysisLineDownCommand = new RelayCommand(_ => MoveAnalysisLine(1), _ => CanMoveAnalysisLine(1));
            SaveAnalysisLinesCommand = new RelayCommand(_ => SaveAnalysisLines());
            RestoreDefaultAnalysisLinesCommand = new RelayCommand(_ => RestoreDefaultAnalysisLines());
            AddCustomCheckCommand = new RelayCommand(_ => AddCustomCheck());
            DeleteCustomCheckCommand = new RelayCommand(_ => DeleteCustomCheck(), _ => SelectedCustomCheck != null);
            MoveCustomCheckUpCommand = new RelayCommand(_ => MoveCustomCheck(-1), _ => CanMoveCustomCheck(-1));
            MoveCustomCheckDownCommand = new RelayCommand(_ => MoveCustomCheck(1), _ => CanMoveCustomCheck(1));
            SaveCustomChecksCommand = new RelayCommand(_ => SaveCustomChecks());

            LoadChecks();
            LoadAnalysisLines(_seedAnalysisService.GetAnalysisLines());
            LoadCustomChecks(_seedAnalysisService.GetCustomChecks());
        }

        private void LoadChecks(IEnumerable<string>? selectedIds = null)
        {
            var selected = selectedIds?.ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CheckOptions.Clear();

            var checks = _seedAnalysisService.GetAvailableCheckOptions();
            foreach (var check in checks)
            {
                check.IsSelected = selected.Contains(check.Id);
                CheckOptions.Add(check);

                check.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(CheckOption.IsSelected))
                        OnPropertyChanged(nameof(SelectedCheckCount));
                };
            }

            OnPropertyChanged(nameof(SelectedCheckCount));
        }

        private void LoadCustomChecks(IEnumerable<CustomSeedCheckDefinition> checks)
        {
            CustomChecks.Clear();
            foreach (CustomSeedCheckDefinition check in checks)
                AddCustomCheckItem(check);

            SelectedCustomCheck = CustomChecks.FirstOrDefault();
            CustomCheckEditorStatus = "Saved";
            OnPropertyChanged(nameof(CustomCheckCount));
        }

        private CustomSeedCheckDefinition AddCustomCheckItem(CustomSeedCheckDefinition check)
        {
            check.PropertyChanged += (_, _) => CustomCheckEditorStatus = "Unsaved changes";
            CustomChecks.Add(check);
            return check;
        }

        private void AddCustomCheck()
        {
            SelectedCustomCheck = AddCustomCheckItem(new CustomSeedCheckDefinition
            {
                Name = $"Custom Test {CustomChecks.Count + 1}",
                MatchOutcome = CustomSeedCheckOutcomes.Invalid
            });
            CustomCheckEditorStatus = "Unsaved changes";
            OnPropertyChanged(nameof(CustomCheckCount));
        }

        private void DeleteCustomCheck()
        {
            if (SelectedCustomCheck == null)
                return;

            int index = CustomChecks.IndexOf(SelectedCustomCheck);
            CustomChecks.Remove(SelectedCustomCheck);
            SelectedCustomCheck = CustomChecks.Count == 0
                ? null
                : CustomChecks[Math.Min(index, CustomChecks.Count - 1)];
            CustomCheckEditorStatus = "Unsaved changes";
            OnPropertyChanged(nameof(CustomCheckCount));
        }

        private bool CanMoveCustomCheck(int offset)
        {
            if (SelectedCustomCheck == null)
                return false;

            int destination = CustomChecks.IndexOf(SelectedCustomCheck) + offset;
            return destination >= 0 && destination < CustomChecks.Count;
        }

        private void MoveCustomCheck(int offset)
        {
            if (!CanMoveCustomCheck(offset) || SelectedCustomCheck == null)
                return;

            int current = CustomChecks.IndexOf(SelectedCustomCheck);
            CustomChecks.Move(current, current + offset);
            CustomCheckEditorStatus = "Unsaved changes";
        }

        private void SaveCustomChecks()
        {
            if (CustomChecks.Any(check => string.IsNullOrWhiteSpace(check.Name)
                || string.IsNullOrWhiteSpace(check.SearchText)))
            {
                CustomCheckEditorStatus = "Missing required values";
                System.Windows.MessageBox.Show(
                    "Every custom test needs both a name and search line.",
                    "Analyze Seeds",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            try
            {
                var selectedIds = CheckOptions
                    .Where(check => check.IsSelected)
                    .Select(check => check.Id)
                    .ToList();
                var saved = _seedAnalysisService.SaveCustomChecks(CustomChecks);
                LoadCustomChecks(saved);
                LoadChecks(selectedIds);
                StatusText = $"Saved {saved.Count} custom tests";
            }
            catch (Exception ex)
            {
                CustomCheckEditorStatus = "Save failed";
                System.Windows.MessageBox.Show(
                    $"Custom tests could not be saved: {ex.Message}",
                    "Analyze Seeds",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadAnalysisLines(System.Collections.Generic.IEnumerable<string> lines)
        {
            AnalysisLines.Clear();
            foreach (string text in lines)
                AddAnalysisLineItem(text);

            SelectedAnalysisLine = AnalysisLines.FirstOrDefault();
            LineEditorStatus = "Saved";
            OnPropertyChanged(nameof(AnalysisLineCount));
        }

        private SeedAnalysisLine AddAnalysisLineItem(string text)
        {
            var item = new SeedAnalysisLine { Text = text };
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SeedAnalysisLine.Text))
                    LineEditorStatus = "Unsaved changes";
            };
            AnalysisLines.Add(item);
            return item;
        }

        private void AddAnalysisLine()
        {
            SelectedAnalysisLine = AddAnalysisLineItem(string.Empty);
            LineEditorStatus = "Unsaved changes";
            OnPropertyChanged(nameof(AnalysisLineCount));
        }

        private void DeleteAnalysisLine()
        {
            if (SelectedAnalysisLine == null)
                return;

            int index = AnalysisLines.IndexOf(SelectedAnalysisLine);
            AnalysisLines.Remove(SelectedAnalysisLine);
            SelectedAnalysisLine = AnalysisLines.Count == 0
                ? null
                : AnalysisLines[Math.Min(index, AnalysisLines.Count - 1)];
            LineEditorStatus = "Unsaved changes";
            OnPropertyChanged(nameof(AnalysisLineCount));
        }

        private bool CanMoveAnalysisLine(int offset)
        {
            if (SelectedAnalysisLine == null)
                return false;

            int destination = AnalysisLines.IndexOf(SelectedAnalysisLine) + offset;
            return destination >= 0 && destination < AnalysisLines.Count;
        }

        private void MoveAnalysisLine(int offset)
        {
            if (!CanMoveAnalysisLine(offset) || SelectedAnalysisLine == null)
                return;

            int current = AnalysisLines.IndexOf(SelectedAnalysisLine);
            AnalysisLines.Move(current, current + offset);
            LineEditorStatus = "Unsaved changes";
        }

        private void SaveAnalysisLines()
        {
            try
            {
                var saved = _seedAnalysisService.SaveAnalysisLines(AnalysisLines.Select(line => line.Text));
                LoadAnalysisLines(saved);
                StatusText = $"Saved {saved.Count} O Mother lines";
            }
            catch (Exception ex)
            {
                LineEditorStatus = "Save failed";
                System.Windows.MessageBox.Show(
                    $"Invalid seed lines could not be saved: {ex.Message}",
                    "Analyze Seeds",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void RestoreDefaultAnalysisLines()
        {
            var result = System.Windows.MessageBox.Show(
                "Replace the current O Mother lines with the built-in defaults?",
                "Restore Default O Mother Lines",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            var saved = _seedAnalysisService.SaveAnalysisLines(_seedAnalysisService.GetDefaultAnalysisLines());
            LoadAnalysisLines(saved);
            StatusText = "Restored default O Mother lines";
        }

        private void ImportSeed()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Seed File",
                Filter = "All Files (*.*)|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() != true)
                return;

            ImportedSeedPath = dlg.FileName;
            StatusText = string.Empty;
            ResultsText = string.Empty;
        }

        private void RunChecks()
        {
            if (string.IsNullOrWhiteSpace(ImportedSeedPath))
            {
                System.Windows.MessageBox.Show(
                    "Please import a seed file first.",
                    "Analyze Seed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            var selectedIds = CheckOptions
                .Where(o => o.IsSelected)
                .Select(o => o.Id)
                .ToList();

            if (selectedIds.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Select at least one check.",
                    "Analyze Seed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            try
            {
                var savedLines = _seedAnalysisService.SaveAnalysisLines(AnalysisLines.Select(line => line.Text));
                LoadAnalysisLines(savedLines);
                string seedText = _seedAnalysisService.ReadSeedText(ImportedSeedPath);
                var results = _seedAnalysisService.RunSelectedChecks(seedText, selectedIds);

                StatusText = _seedAnalysisService.BuildStatusText(results);
                ResultsText = _seedAnalysisService.BuildResultsText(results, CheckOptions.ToList());
            }
            catch (Exception ex)
            {
                StatusText = "Error";
                ResultsText = ex.Message;
            }
        }

        private void Clear()
        {
            ImportedSeedPath = string.Empty;
            StatusText = string.Empty;
            ResultsText = string.Empty;

            foreach (var check in CheckOptions)
                check.IsSelected = false;

            OnPropertyChanged(nameof(SelectedCheckCount));
        }
    }
}
