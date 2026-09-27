# Rail Route Archipelago client mod

A C# mod loaded by Rail Route's built-in mod loader. There's no BepInEx: the game loads
`mods/RailRouteArchipelago/RailRouteArchipelago.dll` from its user data folder at startup.

The mod patches the system-upgrade purchase and the game's upgrade-effect queries with Harmony. With intercept mode on,
in an Endless level, buying an upgrade slot and owning its upgrade are separate: a purchase records a location check,
and only a received item unlocks the upgrade. There's no Archipelago connection yet.

## Build

Requires the .NET 10 SDK (the mod targets `net48` via reference assemblies; the tests target `net10.0`). Builds on macOS, Windows and Linux.

```sh
cd client
dotnet build -c Release
```

The build compiles against the game's own DLLs and, by default, copies the mod into the game's mods folder:

```
mods/RailRouteArchipelago/
├── RailRouteArchipelago.dll
├── RailRouteArchipelago.pdb
├── 0Harmony.dll                          # Harmony 2.4.2 (not shipped with the game)
└── RailRouteArchipelago.settings.json    # optional, you create it
```

Build properties:

| Property | Purpose |
|---|---|
| `-p:RailRouteManagedDir=<path>` | The game's `Managed` folder, if Steam isn't in its default location |
| `-p:RailRouteModsDir=<path>` | Where to deploy the mod |
| `-p:DeployMod=false` | Build without copying into the game |

Unit tests for the game-independent logic (`src/Core`):

```sh
dotnet test --project client.Tests     # from the repository root
```

## Paths per OS

| | Game `Managed` folder (default Steam library) | Mods folder | Game log |
|---|---|---|---|
| macOS | `~/Library/Application Support/Steam/steamapps/common/Rail Route/Rail Route.app/Contents/Resources/Data/Managed` | `~/Library/Application Support/RailRoute/mods` | `~/Library/Logs/bitrich/Rail Route/Player.log` |
| Windows | `C:\Program Files (x86)\Steam\steamapps\common\Rail Route\Rail Route_Data\Managed` | `%USERPROFILE%\AppData\LocalLow\bitrich\Rail Route\mods` | `%USERPROFILE%\AppData\LocalLow\bitrich\Rail Route\Player.log` |
| Linux | `~/.local/share/Steam/steamapps/common/Rail Route/Rail Route_Data/Managed` | `~/.config/unity3d/bitrich/Rail Route/mods` (unverified) | `~/.config/unity3d/bitrich/Rail Route/Player.log` (unverified) |

The files must sit in their own `RailRouteArchipelago` subfolder of `mods`, e.g.
`...\bitrich\Rail Route\mods\RailRouteArchipelago\RailRouteArchipelago.dll`. The game only scans subfolders of `mods`,
and it silently ignores DLLs placed directly in `mods`: `Player.log` won't have a `Loaded MOD: RailRouteArchipelago` line.
Create the `mods` folder if it doesn't exist.

On Apple Silicon, Steam runs Rail Route under Rosetta because the game's `libsteam_api.bundle` is x86_64 only.
That is expected. The log's `Runtime:` line reports `arch X64`.

## Settings

Create `RailRouteArchipelago.settings.json` next to the DLL:

```json
{
  "interceptUpgradePurchases": true
}
```

| Key | Default | Effect |
|---|---|---|
| `interceptUpgradePurchases` | `false` | `true`: in Endless levels, buying a system upgrade spends its points and marks the slot as bought, but doesn't unlock the upgrade. Instead it's recorded as a location check ("Check sent: …" notification). The upgrade only unlocks once its item is received (see F9). `false`: purchases work normally and are only logged. |

The file is read once at game start. A missing file, or an invalid file or value, means off; invalid files are logged as a warning.

With intercept mode on, in an Endless level:
- **Slot bought** is the game's own upgrade flag. The panel shows the slot as installed, child slots become buyable, and it can't be bought again. Bought slots are saved with the game's save file.
- **Item received** is what makes the upgrade work (buildable objects, track speed, station count and so on). Levelled upgrades go up one level per received copy, whichever slots are bought. Received items last for the level: leaving it, or loading another level or save, clears them.

Tutorials, the story, Timetable and Rush Hour levels and the editor behave unmodded even with intercept mode on.

## Debug keys

| Key | When | Effect |
|---|---|---|
| F9 | In an Endless level, intercept mode on | Receives the item of the upgrade selected in the upgrade panel, as though Archipelago had sent it. For a levelled upgrade that's one more level. Needs no bought slot. Logs `Debug receive: no upgrade selected` if nothing is selected. |

## Checking that it works

Every mod line in `Player.log` starts with `[Archipelago]`:

```sh
grep "\[Archipelago\]" ~/Library/Logs/bitrich/Rail\ Route/Player.log
```

A healthy start looks like this:

```
[Archipelago] Enabled.
[Archipelago] Intercept mode: off (settings: …/RailRouteArchipelago.settings.json)
[Archipelago] Runtime: game 3.0.18, platform OSXPlayer, os '…', arch X64, Harmony 2.4.2.0
[Archipelago] Patched Game.ResearchController.HasResearched: OK
[Archipelago] Patched Game.ResearchController.get_IsAutomationEnabled: OK
[Archipelago] Patched Game.ResearchController.Reset: OK
[Archipelago] Patched Game.ResearchController.ResearchedValue: OK
[Archipelago] Patched Multiplayer.Commands.Game.UnlockUpgradeCommand.Run: OK
```

If patching fails (for example after a game update), you'll see `Patching failed on …` instead. The game stays playable
without Archipelago features, and a "running degraded" notification appears when a level starts.
