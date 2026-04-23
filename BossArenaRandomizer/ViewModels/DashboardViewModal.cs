using System;

namespace BossArenaRandomizer.ViewModels
{
    public sealed class DashboardViewModel : ViewModelBase
    {
        private readonly Func<int> _getSelectedArenaCount;
        private readonly Func<int> _getSelectedBossCount;
        private readonly Func<string> _getSelectedOptionsPreset;
        private readonly Func<string> _getOutputPath;
        private readonly Func<string> _getLastSeedText;
        private readonly Func<string> _getLastStatusText;
        private readonly Action<string>? _navigate;

        public string Title => "Dashboard";
        public string Subtitle => "Overview of your current setup, last generation run, and quick navigation.";

        public int SelectedArenaCount => _getSelectedArenaCount();
        public int SelectedBossCount => _getSelectedBossCount();

        public string SelectedOptionsPreset
        {
            get
            {
                var value = _getSelectedOptionsPreset();
                return string.IsNullOrWhiteSpace(value) ? "--" : value;
            }
        }

        public string OutputPath
        {
            get
            {
                var value = _getOutputPath();
                return string.IsNullOrWhiteSpace(value) ? "No output file selected." : value;
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

        public string LastStatusText
        {
            get
            {
                var value = _getLastStatusText();
                return string.IsNullOrWhiteSpace(value) ? "Ready" : value;
            }
        }

        public RelayCommand GoToGenerateCommand { get; }
        public RelayCommand GoToArenasCommand { get; }
        public RelayCommand GoToBossesCommand { get; }
        public RelayCommand GoToAnalyzeCommand { get; }

        public DashboardViewModel(
            Func<int> getSelectedArenaCount,
            Func<int> getSelectedBossCount,
            Func<string> getSelectedOptionsPreset,
            Func<string> getOutputPath,
            Func<string> getLastSeedText,
            Func<string> getLastStatusText,
            Action<string>? navigate = null)
        {
            _getSelectedArenaCount = getSelectedArenaCount ?? throw new ArgumentNullException(nameof(getSelectedArenaCount));
            _getSelectedBossCount = getSelectedBossCount ?? throw new ArgumentNullException(nameof(getSelectedBossCount));
            _getSelectedOptionsPreset = getSelectedOptionsPreset ?? throw new ArgumentNullException(nameof(getSelectedOptionsPreset));
            _getOutputPath = getOutputPath ?? throw new ArgumentNullException(nameof(getOutputPath));
            _getLastSeedText = getLastSeedText ?? throw new ArgumentNullException(nameof(getLastSeedText));
            _getLastStatusText = getLastStatusText ?? throw new ArgumentNullException(nameof(getLastStatusText));
            _navigate = navigate;

            GoToGenerateCommand = new RelayCommand(_ => _navigate?.Invoke("Generate"));
            GoToArenasCommand = new RelayCommand(_ => _navigate?.Invoke("Arenas"));
            GoToBossesCommand = new RelayCommand(_ => _navigate?.Invoke("Bosses"));
            GoToAnalyzeCommand = new RelayCommand(_ => _navigate?.Invoke("Analyze"));
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(SelectedArenaCount));
            OnPropertyChanged(nameof(SelectedBossCount));
            OnPropertyChanged(nameof(SelectedOptionsPreset));
            OnPropertyChanged(nameof(OutputPath));
            OnPropertyChanged(nameof(LastSeedText));
            OnPropertyChanged(nameof(LastStatusText));
        }
    }
}