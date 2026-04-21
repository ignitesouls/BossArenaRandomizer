using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossArenaRandomizer
{
    public interface ISeedCheck
    {
        string Id { get; }
        string DisplayName { get; }
        string Description { get; }

        SeedCheckResult Run(string seedText);
    }

    public sealed class SeedCheckResult
    {
        public string CheckId { get; init; } = "";
        public bool Passed { get; init; }
        public string Message { get; init; } = "";
    }
}

