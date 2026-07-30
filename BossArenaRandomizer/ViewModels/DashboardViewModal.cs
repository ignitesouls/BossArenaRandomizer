using System;
using System.Threading.Tasks;

namespace BossArenaRandomizer.ViewModels
{
    public sealed class DashboardViewModel : ViewModelBase
    {
        private readonly Func<int> _getSelectedArenaCount;
        private readonly Func<int> _getSelectedBossCount;
        private readonly Func<string> _getSelectedOptionsPreset;
        private readonly Func<string> _getSelectedConfiguration;
        private readonly Func<string> _getSelectedArenaPreset;
        private readonly Func<string> _getSelectedBossPreset;
        private readonly Func<string> _getSelectedPairingPreset;
        private readonly Func<string> _getOutputPath;
        private readonly Func<string> _getLastSeedText;
        private readonly Func<string> _getLastStatusText;
        private readonly Func<string> _getLastGeneratedOutputPath;
        private readonly Func<Task> _quickGenerate;
        private readonly Action _openOutputFolder;
        private readonly Action<string>? _navigate;

        public string Title => "Dashboard";
        public string Subtitle => "Overview of your current setup, last generation run, and quick navigation.";

        public int SelectedArenaCount => _getSelectedArenaCount();
        public int SelectedBossCount => _getSelectedBossCount();
        public string SelectedOptionsPreset => DisplayOrPlaceholder(_getSelectedOptionsPreset());
        public string SelectedConfiguration => DisplayOrPlaceholder(_getSelectedConfiguration());
        public string SelectedArenaPreset => DisplayOrPlaceholder(_getSelectedArenaPreset());
        public string SelectedBossPreset => DisplayOrPlaceholder(_getSelectedBossPreset());
        public string SelectedPairingPreset => DisplayOrPlaceholder(_getSelectedPairingPreset());

        public string OutputPath
        {
            get
            {
                var value = _getOutputPath();
                return string.IsNullOrWhiteSpace(value) ? "No output folder selected." : value;
            }
        }

        public string LastSeedText
        {
            get
            {
                var value = _getLastSeedText();
                return string.IsNullOrWhiteSpace(value) ? "Last Seed Used: --" : value;
            }
        }

        public string LastGeneratedOutputPath => DisplayOrPlaceholder(_getLastGeneratedOutputPath());

        public string LastStatusText
        {
            get
            {
                var value = _getLastStatusText();
                return string.IsNullOrWhiteSpace(value) ? "Ready" : value;
            }
        }

        public RelayCommand QuickGenerateCommand { get; }
        public RelayCommand OpenOutputFolderCommand { get; }
        public RelayCommand GoToArenasCommand { get; }
        public RelayCommand GoToBossesCommand { get; }
        public RelayCommand GoToAnalyzeCommand { get; }
        public RelayCommand GoToPresetPairingsCommand { get; }

        public DashboardViewModel(
            Func<int> getSelectedArenaCount,
            Func<int> getSelectedBossCount,
            Func<string> getSelectedOptionsPreset,
            Func<string> getSelectedConfiguration,
            Func<string> getSelectedArenaPreset,
            Func<string> getSelectedBossPreset,
            Func<string> getSelectedPairingPreset,
            Func<string> getOutputPath,
            Func<string> getLastSeedText,
            Func<string> getLastStatusText,
            Func<string> getLastGeneratedOutputPath,
            Func<Task> quickGenerate,
            Action openOutputFolder,
            Action<string>? navigate = null)
        {
            _getSelectedArenaCount = getSelectedArenaCount ?? throw new ArgumentNullException(nameof(getSelectedArenaCount));
            _getSelectedBossCount = getSelectedBossCount ?? throw new ArgumentNullException(nameof(getSelectedBossCount));
            _getSelectedOptionsPreset = getSelectedOptionsPreset ?? throw new ArgumentNullException(nameof(getSelectedOptionsPreset));
            _getSelectedConfiguration = getSelectedConfiguration ?? throw new ArgumentNullException(nameof(getSelectedConfiguration));
            _getSelectedArenaPreset = getSelectedArenaPreset ?? throw new ArgumentNullException(nameof(getSelectedArenaPreset));
            _getSelectedBossPreset = getSelectedBossPreset ?? throw new ArgumentNullException(nameof(getSelectedBossPreset));
            _getSelectedPairingPreset = getSelectedPairingPreset ?? throw new ArgumentNullException(nameof(getSelectedPairingPreset));
            _getOutputPath = getOutputPath ?? throw new ArgumentNullException(nameof(getOutputPath));
            _getLastSeedText = getLastSeedText ?? throw new ArgumentNullException(nameof(getLastSeedText));
            _getLastStatusText = getLastStatusText ?? throw new ArgumentNullException(nameof(getLastStatusText));
            _getLastGeneratedOutputPath = getLastGeneratedOutputPath ?? throw new ArgumentNullException(nameof(getLastGeneratedOutputPath));
            _quickGenerate = quickGenerate ?? throw new ArgumentNullException(nameof(quickGenerate));
            _openOutputFolder = openOutputFolder ?? throw new ArgumentNullException(nameof(openOutputFolder));
            _navigate = navigate;

            QuickGenerateCommand = new RelayCommand(async _ => await _quickGenerate());
            OpenOutputFolderCommand = new RelayCommand(_ => _openOutputFolder());
            GoToArenasCommand = new RelayCommand(_ => _navigate?.Invoke("Arenas"));
            GoToBossesCommand = new RelayCommand(_ => _navigate?.Invoke("Bosses"));
            GoToAnalyzeCommand = new RelayCommand(_ => _navigate?.Invoke("Analyze"));
            GoToPresetPairingsCommand = new RelayCommand(_ => _navigate?.Invoke("Preset Pairings"));
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(SelectedArenaCount));
            OnPropertyChanged(nameof(SelectedBossCount));
            OnPropertyChanged(nameof(SelectedOptionsPreset));
            OnPropertyChanged(nameof(SelectedConfiguration));
            OnPropertyChanged(nameof(SelectedArenaPreset));
            OnPropertyChanged(nameof(SelectedBossPreset));
            OnPropertyChanged(nameof(SelectedPairingPreset));
            OnPropertyChanged(nameof(OutputPath));
            OnPropertyChanged(nameof(LastSeedText));
            OnPropertyChanged(nameof(LastStatusText));
            OnPropertyChanged(nameof(LastGeneratedOutputPath));
        }

        private static string DisplayOrPlaceholder(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "--" : value;
        }
    }
}