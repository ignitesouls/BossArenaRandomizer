using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BossArenaRandomizer.Core
{
    public sealed class SeedAnalysisLine : INotifyPropertyChanged
    {
        private string _text = string.Empty;

        public string Text
        {
            get => _text;
            set
            {
                string safeValue = value ?? string.Empty;
                if (_text == safeValue)
                    return;

                _text = safeValue;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
