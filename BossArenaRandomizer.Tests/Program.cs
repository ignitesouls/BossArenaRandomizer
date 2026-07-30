using BossArenaRandomizer.Core;
using BossArenaRandomizer.Services;

var tests = new (string Name, Action Run)[]
{
    ("Unique assignment when enough bosses are available", UniqueAssignmentWhenEnoughBossesAreAvailable),
    ("Unique assignment can solve required swap", UniqueAssignmentCanSolveRequiredSwap),
    ("Cached pairing assignment can solve required swap", CachedPairingAssignmentCanSolveRequiredSwap),
    ("Same seed replays same assignment", SameSeedReplaysSameAssignment),
    ("Duplicates are balanced when required", DuplicatesAreBalancedWhenRequired),
    ("Preset configuration saves and loads all references", PresetConfigurationRoundTrips),
    ("Output failure does not stop the batch", OutputFailureDoesNotStopBatch)
};

var failures = new List<string>();

foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS: {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{test.Name}: {ex.Message}");
        Console.WriteLine($"FAIL: {test.Name}");
        Console.WriteLine(ex.Message);
    }
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Failures:");
    foreach (var failure in failures)
        Console.WriteLine($"- {failure}");

    return 1;
}

return 0;

static void PresetConfigurationRoundTrips()
{
    string root = Path.Combine(Path.GetTempPath(), "BossArenaRandomizer.Tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);

    try
    {
        var service = new PresetService(root);
        string fileName = service.SaveConfiguration("DLC Bananza", new PresetConfiguration
        {
            RandoOptionsPreset = "DLC Options",
            ArenaPreset = "DLC Arenas.json",
            BossPreset = "DLC Bosses.json",
            PairingPreset = "DLC Pairings.json"
        });

        PresetConfiguration loaded = service.LoadConfiguration(fileName);
        Assert(fileName == "DLC Bananza.json", "Configuration should use the requested name.");
        Assert(loaded.RandoOptionsPreset == "DLC Options", "Rando Options reference should round-trip.");
        Assert(loaded.ArenaPreset == "DLC Arenas.json", "Arena reference should round-trip.");
        Assert(loaded.BossPreset == "DLC Bosses.json", "Boss reference should round-trip.");
        Assert(loaded.PairingPreset == "DLC Pairings.json", "Pairing reference should round-trip.");

        string duplicate = service.DuplicateConfiguration(fileName, "DLC Bananza Copy");
        Assert(service.ConfigurationExists(duplicate), "Duplicated configuration should exist.");

        string renamed = service.RenameConfiguration(duplicate, "DLC Bananza Renamed");
        Assert(!service.ConfigurationExists(duplicate), "Original duplicate name should be removed after rename.");
        Assert(service.ConfigurationExists(renamed), "Renamed configuration should exist.");

        service.DeleteConfiguration(renamed);
        Assert(!service.ConfigurationExists(renamed), "Deleted configuration should no longer exist.");
    }
    finally
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}

static void OutputFailureDoesNotStopBatch()
{
    string root = Path.Combine(Path.GetTempPath(), "BossArenaRandomizer.Tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);

    try
    {
        string optionsPath = Path.Combine(root, "options.randomizeopt");
        File.WriteAllText(optionsPath, string.Empty);

        var data = BuildData(arenaCount: 2, bossCount: 2);
        var validator = BuildEverythingValidator(data.Arenas, data.Bosses);
        var writer = new FailFirstAssignmentWriter();
        var service = new GenerationService(
            _ => new TestProjectPaths(optionsPath),
            new TestPairingPresetLoader(validator),
            writer,
            new UniformityReporter(),
            new GenerationDisplayBuilder());

        GenerationResult result = service.Generate(new GenerationRequest
        {
            Arenas = data.Arenas,
            Bosses = data.Bosses,
            SelectedArenaIds = data.Arenas.Values.Select(x => x.id).ToList(),
            SelectedBossIds = data.Bosses.Values.Select(x => x.id).ToList(),
            BasePath = root,
            OutputFolderPath = root,
            SelectedOptionsPreset = "options",
            SelectedPairingPreset = "everything.json",
            SeedCount = 2,
            ReplaySeed = 100,
            WriteOutputFiles = true
        });

        Assert(result.Success, "Batch should succeed when at least one output is written.");
        Assert(result.BatchResults.Count == 2, "Both seeds should have a batch result.");
        Assert(!result.BatchResults[0].Success, "First output should report its write failure.");
        Assert(result.BatchResults[1].Success, "Second output should still be generated.");
        Assert(writer.CallCount == 2, "Writer should be called for both seeds.");
    }
    finally
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}

static void UniqueAssignmentWhenEnoughBossesAreAvailable()
{
    var data = BuildData(arenaCount: 3, bossCount: 3);
    var validator = BuildEverythingValidator(data.Arenas, data.Bosses);

    bool ok = ArenaBossAssigner.TryAssign(
        data.Arenas,
        data.Bosses,
        data.Arenas.Values.Select(a => a.id).ToList(),
        data.Bosses.Values.Select(b => b.id).ToList(),
        validator,
        maxAttempts: 50,
        rng: new Random(12345),
        result: out var result);

    Assert(ok, "Assignment should succeed.");
    Assert(result != null, "Result should be returned.");
    Assert(result!.Assignments.Count == 3, "All arenas should be assigned.");
    Assert(result.Assignments.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() == 3, "Bosses should not duplicate.");
}

static void SameSeedReplaysSameAssignment()
{
    var data = BuildData(arenaCount: 4, bossCount: 4);
    var validator = BuildEverythingValidator(data.Arenas, data.Bosses);
    var selectedArenaIds = data.Arenas.Values.Select(a => a.id).ToList();
    var selectedBossIds = data.Bosses.Values.Select(b => b.id).ToList();

    bool firstOk = ArenaBossAssigner.TryAssign(
        data.Arenas,
        data.Bosses,
        selectedArenaIds,
        selectedBossIds,
        validator,
        maxAttempts: 50,
        rng: new Random(777),
        result: out var first);

    bool secondOk = ArenaBossAssigner.TryAssign(
        data.Arenas,
        data.Bosses,
        selectedArenaIds,
        selectedBossIds,
        validator,
        maxAttempts: 50,
        rng: new Random(777),
        result: out var second);

    Assert(firstOk && secondOk, "Both assignments should succeed.");
    Assert(first != null && second != null, "Both results should be returned.");
    Assert(DictionaryEquals(first!.Assignments, second!.Assignments), "Same seed should produce the same assignment.");
}

static void UniqueAssignmentCanSolveRequiredSwap()
{
    var data = BuildData(arenaCount: 2, bossCount: 2);
    var allowed = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["A1"] = new HashSet<string>(new[] { "B1", "B2" }, StringComparer.OrdinalIgnoreCase),
        ["A2"] = new HashSet<string>(new[] { "B1" }, StringComparer.OrdinalIgnoreCase)
    };
    var validator = new PairingPresetValidator(allowed);

    bool ok = ArenaBossAssigner.TryAssign(
        data.Arenas,
        data.Bosses,
        data.Arenas.Values.Select(a => a.id).ToList(),
        data.Bosses.Values.Select(b => b.id).ToList(),
        validator,
        maxAttempts: 50,
        rng: new Random(1),
        result: out var result);

    Assert(ok, "Assignment should succeed by rematching Arena 1 away from Boss 1.");
    Assert(result != null, "Result should be returned.");
    Assert(result!.Assignments["Arena 2"] == "Boss 1", "Arena 2 must get its only valid boss.");
    Assert(result.Assignments["Arena 1"] == "Boss 2", "Arena 1 should be shifted to Boss 2.");
}

static void CachedPairingAssignmentCanSolveRequiredSwap()
{
    var data = BuildData(arenaCount: 2, bossCount: 2);
    var allowed = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["A1"] = new HashSet<string>(new[] { "B1", "B2" }, StringComparer.OrdinalIgnoreCase),
        ["A2"] = new HashSet<string>(new[] { "B1" }, StringComparer.OrdinalIgnoreCase)
    };
    var validator = new PairingPresetValidator(allowed);
    var cache = IndexedPairingCache.Build(data.Arenas, data.Bosses, validator);

    bool ok = ArenaBossAssigner.TryAssign(
        cache,
        data.Arenas.Values.Select(a => a.id).ToList(),
        data.Bosses.Values.Select(b => b.id).ToList(),
        maxAttempts: 50,
        rng: new Random(1),
        result: out var result);

    Assert(ok, "Cached assignment should succeed by rematching Arena 1 away from Boss 1.");
    Assert(result != null, "Result should be returned.");
    Assert(result!.Assignments["Arena 2"] == "Boss 1", "Arena 2 must get its only valid boss.");
    Assert(result.Assignments["Arena 1"] == "Boss 2", "Arena 1 should be shifted to Boss 2.");
}

static void DuplicatesAreBalancedWhenRequired()
{
    var data = BuildData(arenaCount: 5, bossCount: 2);
    var validator = BuildEverythingValidator(data.Arenas, data.Bosses);

    bool ok = ArenaBossAssigner.TryAssign(
        data.Arenas,
        data.Bosses,
        data.Arenas.Values.Select(a => a.id).ToList(),
        data.Bosses.Values.Select(b => b.id).ToList(),
        validator,
        maxAttempts: 50,
        rng: new Random(2468),
        result: out var result);

    Assert(ok, "Assignment should succeed.");
    Assert(result != null, "Result should be returned.");

    var counts = result!.Assignments.Values
        .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
        .Select(g => g.Count())
        .OrderBy(x => x)
        .ToList();

    Assert(counts.Count == 2, "Both bosses should be used.");
    Assert(counts[^1] - counts[0] <= 1, "Duplicate use should be balanced.");
}

static (Dictionary<string, ArenaInfo> Arenas, Dictionary<string, BossInfo> Bosses) BuildData(int arenaCount, int bossCount)
{
    var arenas = Enumerable.Range(1, arenaCount)
        .ToDictionary(
            i => $"Arena {i}",
            i => new ArenaInfo
            {
                id = $"A{i}",
                type = 3,
                nightBoss = 0,
                region = 1,
                scaling = i,
                dlc = false
            },
            StringComparer.OrdinalIgnoreCase);

    var bosses = Enumerable.Range(1, bossCount)
        .ToDictionary(
            i => $"Boss {i}",
            i => new BossInfo
            {
                id = $"B{i}",
                type = 3,
                nightBoss = 0,
                region = 1,
                scaling = i,
                dlc = false
            },
            StringComparer.OrdinalIgnoreCase);

    return (arenas, bosses);
}

static PairingPresetValidator BuildEverythingValidator(
    Dictionary<string, ArenaInfo> arenas,
    Dictionary<string, BossInfo> bosses)
{
    var allowed = arenas.Values.ToDictionary(
        arena => arena.id,
        _ => bosses.Values.Select(boss => boss.id).ToHashSet(StringComparer.OrdinalIgnoreCase),
        StringComparer.OrdinalIgnoreCase);

    return new PairingPresetValidator(allowed);
}

static bool DictionaryEquals(Dictionary<string, string> first, Dictionary<string, string> second)
{
    return first.Count == second.Count
        && first.All(pair => second.TryGetValue(pair.Key, out var value)
            && string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase));
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

sealed class TestProjectPaths : IProjectPaths
{
    private readonly string _optionsPath;

    public TestProjectPaths(string optionsPath)
    {
        _optionsPath = optionsPath;
    }

    public string OptionsPresetPath(string presetName) => _optionsPath;
    public string PairingPresetPath(string presetFileName) => presetFileName;

    public string BuildBatchOutputPath(
        string outputFolderPath,
        string fileNamePattern,
        string selectedOptionsPreset,
        int index,
        int seed) => Path.Combine(outputFolderPath, $"output_{index}_{seed}.randomizeopt");
}

sealed class TestPairingPresetLoader : IPairingPresetLoader
{
    private readonly PairingPresetValidator _validator;

    public TestPairingPresetLoader(PairingPresetValidator validator)
    {
        _validator = validator;
    }

    public PairingPresetValidator Load(string path) => _validator;
}

sealed class FailFirstAssignmentWriter : IAssignmentWriter
{
    public int CallCount { get; private set; }

    public void Write(
        IReadOnlyCollection<AssignmentPair> assignments,
        string outputPath,
        string optionsFilePath,
        int seed,
        bool includeClearArenas)
    {
        CallCount++;
        if (CallCount == 1)
            throw new IOException("Simulated write failure.");
    }
}
