using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossArenaRandomizer
{
    public sealed class SeedCheckRunner
    {
        private readonly List<ISeedCheck> _checks;

        public SeedCheckRunner(IEnumerable<ISeedCheck> checks)
        {
            _checks = checks.ToList();
        }

        public IReadOnlyList<ISeedCheck> AvailableChecks => _checks;

        public List<SeedCheckResult> RunSelected(string seedText, IEnumerable<string> selectedCheckIds)
        {
            var selected = new HashSet<string>(selectedCheckIds);

            return _checks
                .Where(c => selected.Contains(c.Id))
                .Select(c => c.Run(seedText))
                .ToList();
        }
    }
}
