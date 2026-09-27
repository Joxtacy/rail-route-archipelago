# Rail Route Archipelago client mod

A C# mod loaded by Rail Route's built-in mod loader. There's no BepInEx: the game loads
`mods/RailRouteArchipelago/RailRouteArchipelago.dll` from its user data folder at startup.

The mod currently patches the system-upgrade purchase with Harmony. With intercept mode on, buying an
upgrade records a pending location check instead of unlocking it. There's no Archipelago connection yet.

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
| `interceptUpgradePurchases` | `false` | `true`: buying a system upgrade spends its points but doesn't unlock it. Instead it's recorded as a pending check ("Check sent: …" notification), and that upgrade can't be bought again this session. `false`: purchases work normally and are only logged. |

The file is read once at game start. A missing file, or an invalid file or value, means off; invalid files are logged as a warning.
Pending checks live in memory only and are lost when the game restarts.

## Debug keys

| Key | When | Effect |
|---|---|---|
| F9 | In a level, intercept mode on | Grants the most recently intercepted upgrade (the way a received Archipelago item will) and removes it from pending |

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
[Archipelago] Patched Game.ResearchController.CanResearch: OK
[Archipelago] Patched Multiplayer.Commands.Game.UnlockUpgradeCommand.Run: OK
```

If patching fails (for example after a game update), you'll see `Patching failed on …` instead. The game stays playable
without Archipelago features, and a "running degraded" notification appears when a level starts.
