namespace BossArenaRandomizer.Core
{
    public sealed class AssignmentPair
    {
        public required ArenaId ArenaId { get; init; }
        public required string ArenaName { get; init; }
        public required int ArenaRegion { get; init; }
        public required BossId BossId { get; init; }
        public required string BossName { get; init; }
    }
}
