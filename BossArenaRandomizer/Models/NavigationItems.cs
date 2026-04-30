using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossArenaRandomizer.Models;

public sealed class NavigationItem
{
    public string Title { get; set; } = string.Empty;
    public object? ViewModel { get; set; }
}
