using System;
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

        private string _title = "Analyze Seeds";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _subtitle = "Import a generated seed file and run validation checks against it.";
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

        public RelayCommand ImportSeedCommand { get; }
        public RelayCommand RunChecksCommand { get; }
        public RelayCommand ClearCommand { get; }

        public AnalyzeViewModel(SeedAnalysisService seedAnalysisService)
        {
            _seedAnalysisService = seedAnalysisService ?? throw new ArgumentNullException(nameof(seedAnalysisService));

            ImportSeedCommand = new RelayCommand(_ => ImportSeed());
            RunChecksCommand = new RelayCommand(_ => RunChecks());
            ClearCommand = new RelayCommand(_ => Clear());

            LoadChecks();
        }

        private void LoadChecks()
        {
            CheckOptions.Clear();

            var checks = _seedAnalysisService.GetAvailableCheckOptions();
            foreach (var check in checks)
            {
                CheckOptions.Add(check);

                check.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(CheckOption.IsSelected))
                        OnPropertyChanged(nameof(SelectedCheckCount));
                };
            }

            OnPropertyChanged(nameof(SelectedCheckCount));
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
