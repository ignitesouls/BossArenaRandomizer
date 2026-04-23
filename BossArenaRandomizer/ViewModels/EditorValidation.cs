using System.Collections.Generic;
using System.Linq;

namespace BossArenaRandomizer.ViewModels
{
    public sealed class EditorValidationResult
    {
        public List<string> Errors { get; } = new();

        public bool IsValid => Errors.Count == 0;

        public string ToDisplayText()
        {
            return string.Join("\n", Errors);
        }
    }
}