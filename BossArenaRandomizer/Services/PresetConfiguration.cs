namespace BossArenaRandomizer.Services
{
    public sealed class PresetConfiguration
    {
        public string RandoOptionsPreset { get; set; } = string.Empty;
        public string ArenaPreset { get; set; } = string.Empty;
        public string BossPreset { get; set; } = string.Empty;
        public string PairingPreset { get; set; } = string.Empty;
        public bool? ClearArenasEnabled { get; set; }
        public string ClearArenaReplacementId { get; set; } = "2822374";
    }
}
