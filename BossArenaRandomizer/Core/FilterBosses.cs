using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BossArenaRandomizer.Core
{
    public class BossSelection : INotifyPropertyChanged
    {
        private bool isSelected;

        public string Name { get; set; }
        public string Id { get; set; }
        public int RegionId { get; set; }    
        public string RegionName { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;
    }


    public class RegionGroupBoss
    {
        public string RegionName { get; set; }
        public ObservableCollection<BossSelection> Bosses { get; set; }

        public RegionGroupBoss(string name)
        {
            RegionName = name;
            Bosses = new ObservableCollection<BossSelection>();
        }
    }

    public class FilterBosses : INotifyPropertyChanged
    {
        public ObservableCollection<BossSelection> BossSelections { get; private set; }
        public ObservableCollection<RegionGroupBoss> RegionGroups { get; private set; }

        public int SelectedCount =>
            RegionGroups?.Sum(r => r.Bosses.Count(a => a.IsSelected)) ?? 0;

        public FilterBosses(Dictionary<string, BossInfo> bossesJson)
        {
            BossSelections = new ObservableCollection<BossSelection>();
            RegionGroups = new ObservableCollection<RegionGroupBoss>();

            foreach (var bossEntry in bossesJson)
            {
                var bossInfo = bossEntry.Value;
                var boss = new BossSelection
                {
                    Name = bossEntry.Key,
                    Id = bossInfo.id,
                    RegionId = bossInfo.region,
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

                var regionGroup = RegionGroups.FirstOrDefault(r => r.RegionName == boss.RegionName);
                if (regionGroup == null)
                {
                    regionGroup = new RegionGroupBoss(boss.RegionName);
                    RegionGroups.Add(regionGroup);
                }

                regionGroup.Bosses.Add(boss);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
