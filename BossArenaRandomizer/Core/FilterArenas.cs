using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace BossArenaRandomizer.Core
{
    public class ArenaSelection : INotifyPropertyChanged
    {
        private bool isSelected;

        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class FilterArenas : INotifyPropertyChanged
    {
        public ObservableCollection<ArenaSelection> ArenaSelections { get; } = new();

        public int SelectedCount =>
            ArenaSelections.Count(arena => arena.IsSelected);

        public FilterArenas(Dictionary<string, ArenaInfo> arenasJson)
        {
            foreach (var arenaEntry in arenasJson)
            {
                var arenaJson = arenaEntry.Value;
                var arena = new ArenaSelection
                {
                    Name = arenaEntry.Key,
                    Id = arenaJson.id,
                    RegionName = HCData.RegionNames.ContainsKey(arenaJson.region)
                        ? HCData.RegionNames[arenaJson.region]
                        : $"Region {arenaJson.region}",
                    IsSelected = HCFilterIds.BaseGameArenaIds.Contains(arenaJson.id)
                        || HCFilterIds.DLCArenaIds.Contains(arenaJson.id)
                };

                arena.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ArenaSelection.IsSelected))
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCount)));
                    }
                };

                ArenaSelections.Add(arena);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
