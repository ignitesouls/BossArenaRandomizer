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

No git submodules are required for the app to build.

## Required Data

The app expects these files under `BossArenaRandomizer/Data/`:

- `AllArenaBossesDatabase.json`

Boss/arena pairing presets live under `BossArenaRandomizer/Data/Pairings/`:

- `everything.json`

Additional boss/arena pairing presets can be placed in that same `Data/Pairings/` folder as `.json` files.

## Usage

1. Put Item and Enemy Randomizer option files in `BossArenaRandomizer/Options/`.
2. Open the app and choose an options preset.
3. Choose a boss/arena pairing preset such as `everything.json`.
4. Select the arenas and bosses you want active.
5. Generate the output `.randomizeopt` file.

Use the `Preset Pairings` page to edit which boss IDs are allowed in each arena ID, then overwrite the selected preset or save it as a new one.
