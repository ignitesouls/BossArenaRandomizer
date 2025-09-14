using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BossArenaRandomizer
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

        private readonly string basePath = AppDomain.CurrentDomain.BaseDirectory;

        public FilterBosses(Dictionary<string, BossInfo> bossesJson)
        {
            BossSelections = new ObservableCollection<BossSelection>();
            RegionGroups = new ObservableCollection<RegionGroupBoss>();

            // Load base list from CSV
            if (File.Exists("ArenaBossData.csv"))
            {
                var lines = File.ReadAllLines(Path.Combine(basePath, "ArenaBossData.csv")).Skip(1); // Skip header
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        string name = parts[0];
                        string id = parts[1];

                        var boss = new BossSelection
                        {
                            Name = name,
                            Id = id,
                            IsSelected = !HCFilterIds.UncheckArenaBossIds.Contains(id)
                        };

                        // Subscribe to checkbox changes
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
            }

            // Enrich with region info from JSON + group
            foreach (var boss in BossSelections)
            {
                if (bossesJson.TryGetValue(boss.Name, out var bossInfo))
                {
                    boss.RegionId = bossInfo.region;
                    boss.RegionName = HCData.RegionNames.ContainsKey(boss.RegionId)
                        ? HCData.RegionNames[boss.RegionId]
                        : $"Region {boss.RegionId}";

                    // Find or create region group
                    var regionGroup = RegionGroups.FirstOrDefault(r => r.RegionName == boss.RegionName);
                    if (regionGroup == null)
                    {
                        regionGroup = new RegionGroupBoss(boss.RegionName);
                        RegionGroups.Add(regionGroup);
                    }

                    regionGroup.Bosses.Add(boss);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }




    /*public class BossSelection : INotifyPropertyChanged
    {
        private bool isSelected;

        public string Name { get; set; }
        public string Id { get; set; }

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

    public class FilterBosses : INotifyPropertyChanged
    {
        public ObservableCollection<BossSelection> BossSelections { get; private set; }

        public int SelectedCount => BossSelections.Count(a => a.IsSelected);
        private readonly string basePath = AppDomain.CurrentDomain.BaseDirectory;

        public FilterBosses()
        {
            BossSelections = new ObservableCollection<BossSelection>();

            if (File.Exists("ArenaBossData.csv"))
            {
                var lines = File.ReadAllLines(System.IO.Path.Combine(basePath, "ArenaBossData.csv")).Skip(1); // Skip header
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        string name = parts[0];
                        string id = parts[1];

                        var arena = new BossSelection
                        {
                            Name = name,
                            Id = id,
                            IsSelected = !HCFilterIds.UncheckArenaBossIds.Contains(id)
                        };

                        // Subscribe to checkbox changes
                        arena.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(BossSelection.IsSelected))
                            {
                                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCount)));
                            }
                        };

                        BossSelections.Add(arena);
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }*/
}
