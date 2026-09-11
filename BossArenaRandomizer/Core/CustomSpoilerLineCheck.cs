using System;
using System.Linq;

namespace BossArenaRandomizer.Core
{
    public sealed class CustomSpoilerLineCheck : ISeedCheck
    {
        private readonly CustomSeedCheckDefinition _definition;

        public CustomSpoilerLineCheck(CustomSeedCheckDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public string Id => $"custom:{_definition.Id}";
        public string DisplayName => _definition.Name;
        public string Description => _definition.MatchOutcome == CustomSeedCheckOutcomes.Valid
            ? $"Passes only when a spoiler-log line contains: {_definition.SearchText}"
            : $"Fails when a spoiler-log line contains: {_definition.SearchText}";

        public SeedCheckResult Run(string seedText)
        {
            string? matchedLine = seedText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.Contains(_definition.SearchText, StringComparison.OrdinalIgnoreCase));

            bool found = matchedLine != null;
            bool validWhenFound = _definition.MatchOutcome == CustomSeedCheckOutcomes.Valid;
            bool passed = validWhenFound ? found : !found;

            string message = validWhenFound
                ? found
                    ? $"Required line found:\n{matchedLine}"
                    : $"Required line was not found:\n{_definition.SearchText}"
                : found
                    ? $"Invalid line found:\n{matchedLine}"
                    : $"Invalid line was not found:\n{_definition.SearchText}";

            return new SeedCheckResult
            {
                CheckId = Id,
                Passed = passed,
                Message = message
            };
        }
    }
}
