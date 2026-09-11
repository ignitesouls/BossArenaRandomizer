using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace BossArenaRandomizer.Core
{
    public class BossSelection : INotifyPropertyChanged
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

    public class FilterBosses : INotifyPropertyChanged
    {
        public ObservableCollection<BossSelection> BossSelections { get; } = new();

        public int SelectedCount =>
            BossSelections.Count(boss => boss.IsSelected);

        public FilterBosses(Dictionary<string, BossInfo> bossesJson)
        {
            foreach (var bossEntry in bossesJson)
            {
                var bossInfo = bossEntry.Value;
                var boss = new BossSelection
                {
                    Name = bossEntry.Key,
                    Id = bossInfo.id,
                    RegionName = HCData.RegionNames.ContainsKey(bossInfo.region)
                        ? HCData.RegionNames[bossInfo.region]
                        : $"Region {bossInfo.region}",
                    IsSelected = HCFilterIds.BaseGameBossesIds.Contains(bossInfo.id)
                        || HCFilterIds.DLCBossesIds.Contains(bossInfo.id)
                };

                boss.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(BossSelection.IsSelected))
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCount)));
                    }
                };

                BossSelections.Add(boss);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
