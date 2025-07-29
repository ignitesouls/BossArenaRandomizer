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
    public class ArenaSelection : INotifyPropertyChanged
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
    }
}
