Boss Arena Randomizer

Boss Arena Randomizer (BAR) creates Elden Ring boss and arena replacement presets for use with TheFifthMatt's Item and Enemy Randomizer.

## Requirements

- .NET 8 SDK
- Windows is required to run the WPF app.
- The project can be restored/built on non-Windows machines because Windows targeting is enabled, but the app itself is Windows-only.

## Build

From the repository root:

```powershell
dotnet restore BossArenaRandomizer.sln
dotnet build BossArenaRandomizer.sln
```

# Boss Arena Randomizer

Boss Arena Randomizer, or **BAR**, is a Windows application for creating randomized Elden Ring arena-to-boss assignments.

BAR lets you build reusable arena pools, boss pools, pairing restrictions, and complete configurations through a visual interface. It writes the generated assignments into `.randomizeopt` files for use with TheFifthMatt's Item and Enemy Randomizer.

## Features

- Select which arenas participate in randomization
- Select the available boss pool
- Create reusable arena and boss presets
- Control which bosses are permitted in each arena
- Save complete configurations for quick reuse
- Generate one seed or an entire batch
- Replay a specific seed
- Clear configured arena entities before applying assignments
- Validate pairing presets before generating
- Review assignment uniformity and pairing frequency
- Analyze spoiler logs using Great Rune, O Mother, and custom checks
- Edit arena, boss, pairing, and Clear Arena ID data from inside BAR

## Getting Started

1. Download and extract the complete release archive.
2. Keep the executable, included DLLs, and data folders together.
3. Place your source `.randomizeopt` files in the `Rando Options` folder.
4. Launch `BossArenaRandomizer.exe`.
5. Select or create your arena, boss, and pairing presets.
6. Choose an output folder and generate your seeds.

BAR is published as a self-contained Windows x64 application, so a separate .NET installation should not be required.

## Recommended Workflow

### Prepare Your Presets

Place compatible `.randomizeopt` templates in `Rando Options`.

Arena and boss selection presets are stored under `BAR Presets`, while arena-to-boss pairing rules are stored under `Data\Pairings`.

Use **Refresh Lists** on the Generate page after adding files while BAR is open.

### Select Arenas and Bosses

Open the **Arenas** and **Bosses** pages to choose which entries participate in generation.

You can search the lists, select Base Game or DLC entries in bulk, and save your selections as reusable presets.

### Choose Pairing Rules

The **Preset Pairings** editor controls which bosses are allowed in each arena.

Pairing presets can be validated before generation to identify missing arenas, unavailable bosses, or arenas with no valid assignments.

### Save a Configuration

A configuration combines:

- Rando Options preset
- Arena preset
- Boss preset
- Pairing preset
- Clear Arenas setting
- Clear Boss ID

Once saved, the entire setup can be restored without selecting every preset manually.

### Generate Seeds

On the **Generate** page, choose your output folder, seed count, optional replay seed, filename pattern, and presets.

Select **Generate Batch** to create the output files. **Quick Generate** on the Dashboard loads the selected configuration and generates one seed.

## Filename Tokens

The output filename pattern supports:

- `{index}`: position within the current batch
- `{seed}`: generated or replayed seed
- `{preset}`: selected Rando Options preset

Example: `BAR_{preset}_{index}_{seed}.randomizeopt`

## Clear Arenas

When enabled, BAR replaces every entity listed in `Data\ClearArenaIds.json` with the configured Clear Boss ID before adding the generated arena assignments.

The Clear Arena ID list can be edited through **Main Database > Clear Arena IDs**.

## Seed Analysis

The **Analyze** page can inspect a spoiler log using:

- Great Rune checks
- O Mother checks
- Custom text searches
- User-defined valid or invalid results

O Mother search phrases are stored in `Data\AnalyzeSeedLines.json`, while custom checks are stored in `Data\CustomSeedChecks.json`.

## Folder Reference

| Folder or file | Purpose |
|---|---|
| `BAR Configurations` | Complete saved setups referencing all selected presets and Clear Arenas settings |
| `BAR Presets\Arenas` | Reusable lists of selected arena entity IDs |
| `BAR Presets\Bosses` | Reusable lists of selected boss entity IDs |
| `Rando Options` | Source `.randomizeopt` templates used during generation |
| `Data\Pairings` | Rules defining which bosses are allowed in each arena |
| `Data\AllArenaBossesDatabase.json` | Arena and boss names, IDs, regions, types, scaling, and DLC information |
| `Data\ClearArenaIds.json` | Entity IDs replaced when Clear Arenas is enabled |
| `Data\AnalyzeSeedLines.json` | Editable spoiler-log phrases used by O Mother analysis |
| `Data\CustomSeedChecks.json` | Custom spoiler-log searches and expected outcomes |

## Important Release Files

Keep `BossArenaRandomizer.exe`, its companion native DLLs, and all included data and preset folders together.

The `.pdb` file is optional and is only useful for debugging. It is not required to run BAR.

## Credits

**Shura**  
Primary developer of Boss Arena Randomizer  
[Support Shura on Ko-fi](https://ko-fi.com/shura125)

Special thanks to:

- **Psiphicode**
- **Helios**

Additional thanks to everyone who contributed testing, randomizer knowledge, feedback, and support.
