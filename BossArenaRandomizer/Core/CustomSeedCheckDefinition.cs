using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BossArenaRandomizer.Core
{
    public sealed class CustomSeedCheckDefinition : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _searchText = string.Empty;
        private string _matchOutcome = CustomSeedCheckOutcomes.Invalid;

        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value ?? string.Empty);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetField(ref _searchText, value ?? string.Empty);
        }

        public string MatchOutcome
        {
            get => _matchOutcome;
            set => SetField(ref _matchOutcome, CustomSeedCheckOutcomes.Normalize(value));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField(ref string field, string value, [CallerMemberName] string? propertyName = null)
        {
            if (field == value)
                return;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class CustomSeedCheckOutcomes
    {
        public const string Invalid = "Invalid if found";
        public const string Valid = "Valid only if found";

        public static string Normalize(string? value)
        {
            return string.Equals(value, Valid, StringComparison.OrdinalIgnoreCase)
                ? Valid
                : Invalid;
        }
    }
}
