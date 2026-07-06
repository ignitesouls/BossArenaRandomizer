using System;

namespace BossArenaRandomizer.Core
{
    public readonly record struct ArenaId(string Value)
    {
        public override string ToString() => Value;

        public static implicit operator string(ArenaId id) => id.Value;
        public static explicit operator ArenaId(string value) => new(value ?? throw new ArgumentNullException(nameof(value)));
    }

    public readonly record struct BossId(string Value)
    {
        public override string ToString() => Value;

        public static implicit operator string(BossId id) => id.Value;
        public static explicit operator BossId(string value) => new(value ?? throw new ArgumentNullException(nameof(value)));
    }
}
