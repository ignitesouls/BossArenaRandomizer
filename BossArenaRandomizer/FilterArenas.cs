using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Data;

namespace BossArenaRandomizer
{
    public class ArenaSelection : INotifyPropertyChanged
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

    public class RegionGroup
    {
        public string RegionName { get; set; }
        public ObservableCollection<ArenaSelection> Arenas { get; set; }

        public RegionGroup(string name)
        {
            RegionName = name;
            Arenas = new ObservableCollection<ArenaSelection>();
        }
    }

    public class FilterArenas : INotifyPropertyChanged
    {
        public ObservableCollection<ArenaSelection> ArenaSelections { get; private set; }
        public ObservableCollection<RegionGroup> RegionGroups { get; private set; }

        public int SelectedCount =>
            RegionGroups?.Sum(r => r.Arenas.Count(a => a.IsSelected)) ?? 0;

        private readonly string basePath = AppDomain.CurrentDomain.BaseDirectory;

        public FilterArenas(Dictionary<string, ArenaInfo> arenasJson)
        {
            ArenaSelections = new ObservableCollection<ArenaSelection>();
            RegionGroups = new ObservableCollection<RegionGroup>();

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

                        var arena = new ArenaSelection
                        {
                            Name = name,
                            Id = id,
                            IsSelected = !HCFilterIds.UncheckArenaBossIds.Contains(id)
                        };

                        // Subscribe to checkbox changes
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
            }

            // Enrich with region info from JSON + group
            foreach (var arena in ArenaSelections)
            {
                if (arenasJson.TryGetValue(arena.Name, out var arenaJson))
                {
                    arena.RegionId = arenaJson.region;
                    arena.RegionName = HCData.RegionNames.ContainsKey(arena.RegionId)
                        ? HCData.RegionNames[arena.RegionId]
                        : $"Region {arena.RegionId}";

                    // Find or create region group
                    var regionGroup = RegionGroups.FirstOrDefault(r => r.RegionName == arena.RegionName);
                    if (regionGroup == null)
                    {
                        regionGroup = new RegionGroup(arena.RegionName);
                        RegionGroups.Add(regionGroup);
                    }

                    regionGroup.Arenas.Add(arena);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    /*public class ArenaSelection : INotifyPropertyChanged
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

    public class FilterArenas : INotifyPropertyChanged
    {
        public ObservableCollection<ArenaSelection> ArenaSelections { get; private set; }

        public int SelectedCount => ArenaSelections.Count(a => a.IsSelected);
        private readonly string basePath = AppDomain.CurrentDomain.BaseDirectory;

        public FilterArenas()
        {
            ArenaSelections = new ObservableCollection<ArenaSelection>();

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

                        var arena = new ArenaSelection
                        {
                            Name = name,
                            Id = id,
                            IsSelected = !HCFilterIds.UncheckArenaBossIds.Contains(id)
                        };

                        // Subscribe to checkbox changes
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
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }*/
}
