# Rail Route Archipelago client mod

A C# mod loaded by Rail Route's built-in mod loader. There's no BepInEx: the game loads
`mods/RailRouteArchipelago/RailRouteArchipelago.dll` from its user data folder at startup.

The mod patches the system-upgrade purchase and the game's upgrade-effect queries with Harmony. With intercept mode on,
in an Endless level, buying an upgrade slot and owning its upgrade are separate: a purchase records a location check,
and only a received item unlocks the upgrade. With a server and slot configured, the mod connects to Archipelago for the
level, sends each bought slot as a location check, applies the items the server sends and reports the goal.

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
├── Archipelago.MultiClient.Net.dll       # Archipelago client library 6.7.1 (net45 build)
├── Archipelago.MultiClient.Net.pdb
└── RailRouteArchipelago.settings.json    # optional, you create it
```

There's deliberately no `Newtonsoft.Json.dll`. The client library's package bundles its own unsigned Newtonsoft.Json 11,
but the mod binds the library to the game's Newtonsoft.Json 13 instead, so only one JSON library is loaded.

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
  "interceptUpgradePurchases": true,
  "server": "localhost:38281",
  "slot": "Player"
}
```

For a room on archipelago.gg, use the address and port the room page shows, and the room password if it has one:

```json
{
  "interceptUpgradePurchases": true,
  "server": "wss://archipelago.gg:54321",
  "slot": "Player",
  "password": "…"
}
```

| Key | Default | Effect |
|---|---|---|
| `interceptUpgradePurchases` | `false` | `true`: in Endless levels, buying a system upgrade spends its points and marks the slot as bought, but doesn't unlock the upgrade. Instead it's recorded as a location check ("Check sent: …" notification). The upgrade only unlocks once its item is received (see F9). `false`: purchases work normally and are only logged. |
| `server` | none | The Archipelago server: `host:port`, a bare `host` (port 38281), or a `ws://`/`wss://` URI. An invalid address is logged and treated as none. |
| `slot` | none | Your slot (player) name in the seed. |
| `password` | none | The room password, if it has one. It's only sent to the server and never written to the log. |
| `debugEndlessCompleteThreshold` | none | **Test only.** A positive number lowers the Endless-complete star's threshold on a level that hasn't earned the star yet (`Debug: endless-complete threshold set to <n>` in the log), so a test round can reach the goal quickly. The game saves the lowered threshold with the level, so use it only on throwaway saves. |

The file is read once at game start. A missing file, or an invalid file or value, means off; invalid files are logged as a warning.
Without `server` and `slot` the mod stays offline: purchases are recorded in the log only, and F9 still works.

With intercept mode on, in an Endless level:
- **Slot bought** is the game's own upgrade flag. The panel shows the slot as installed, child slots become buyable, and it can't be bought again. Bought slots are saved with the game's save file.
- **Item received** is what makes the upgrade work (buildable objects, track speed, station count and so on). Levelled upgrades go up one level per received copy, whichever slots are bought. Received items last for the level: leaving it, or loading another level or save, clears them, and the next connection restores them from the server.

Tutorials, the story, Timetable and Rush Hour levels and the editor behave unmodded even with intercept mode on.

## Archipelago connection

With intercept mode on and `server` and `slot` set, the mod connects when an Endless level has finished loading (a new
game, a saved map or a restart) and disconnects when you leave the level or load another one. It connects once per
loaded level and doesn't retry: after a failure or a lost connection, reload the save to try again. The level stays
playable offline, with purchases recorded and only the items already received in effect.

Notifications:

| Notification | Meaning |
|---|---|
| Archipelago connected | Logged in to the slot. |
| Archipelago connection failed – see Player.log | The server couldn't be reached or refused the login. The log lists the reasons. |
| Archipelago disconnected | The connection dropped during play. Received items keep working until you leave the level. |
| This level doesn't match the Archipelago seed | The slot was generated for another map, or with other Expect Delays / Happy Passengers settings. The log names each difference. The mod stays connected and still applies items. On another map it adds "– checks are not sent" and sends no checks for that connection, so a save from another map can't check this seed's locations. |
| This save belongs to another Archipelago seed – disconnected | The loaded save is bound to another seed or slot, or its state file can't be read (see "Save state" below). The mod sent nothing, applied no items and disconnected. |
| Restored *n* Archipelago items, *m* new | The server resent the slot's items after connecting. *n* were already in the save, *m* arrived since it was written (", *m* new" is left out when there are none). |
| Archipelago goal complete | The goal was sent to the server (see "Goal" below). |

**Sending checks.** Buying a slot while connected sends its location to the server at once (`Check sent: <name> (<id>)`
in the log). After every login the mod sends every bought slot again in one batch (`Resent <n> checks`), which covers
slots bought offline and slots restored from the save. Slots the game unlocked itself (level configuration, "unlock
all") aren't sent, and a location that isn't in the seed is skipped with a warning. Checks are only sent for a save
bound to the connected seed and slot (see "Save state").

**Receiving items.** Each item the server sends is mapped by name to its upgrade (see "Item names" in
`apworld/README.md`) and received like F9 does, with its side effects and unlock popup. While the system upgrades menu is open the game shows no popup, which is always
the case for the item your own purchase sends back, so a "Received <upgrade> from <player>" notification appears instead. Progressive items add one level.
On connect the server resends every item the slot has received so far. The mod applies that replay without a popup per
item and shows one "Restored *n* Archipelago items, *m* new" notification instead. The items the save already had (its
received-item index) are restored without their one-shot side effects, so the auto-accept and auto-reverse settings at
each station and the alert preferences stay as you left them. Items that arrived since the save was written apply with
their side effects. Filler items (Green XP Bundle) are logged
as `Filler item ignored` and have no effect yet.

## Save state

In intercept mode, each save of an Endless level gets a small state file next to it in the game's `saves` folder:
the save's file name plus `.ap.json`, for example `haarlem_My game_09-12-30_3f2a1.ap.json` next to
`haarlem_My game_09-12-30_3f2a1.mp.lz4`. The game's own save file is never changed.

| OS | Save folder |
|---|---|
| macOS | `~/Library/Application Support/RailRoute/saves` |
| Windows | `%USERPROFILE%\AppData\LocalLow\bitrich\Rail Route\saves` |
| Linux | `~/.config/unity3d/bitrich/Rail Route/saves` (unverified) |

It holds:
- the **binding**: the seed name, team, slot number and slot name the save was played with
- the **sent checks**: every location the mod handed to the server
- the **goal state**: `none`, `pending`, `sent`, or `ineligible` (the star predates the binding)
- the **received-item index**: how many of the slot's items have taken effect in the level

The mod writes it whenever the game writes a save (manual saves, autosaves), deletes it when the game deletes the save
(including pruned autosaves), and renames it with the save. The game's save list ignores these files.

- **Binding.** A new game, or a save without a state file, binds to the seed and slot at its first successful login on
  the seed's map (`Save bound to <seed>, slot <name> (<n>)`). A loaded save gets its state file at once. On another
  map the save stays unbound.
- **Another seed's save.** A save bound to another seed or slot is refused after login: no checks, no goal, no items.
  The log names both bindings (`Save refused: bound to …`), a notification appears and the mod disconnects.
- **Unreadable state file.** A state file that can't be read is logged with its path and treated as another seed's:
  the save is refused. The mod leaves that file as it is, and later saves of the level are refused the same way.
- **Unbinding.** To use a save with another seed, delete its `.ap.json` by hand. It binds again at the next login.
- **Steam Cloud** syncs only the `.mp.lz4` save files, not the state files. On another machine a synced save shows up
  unbound, binds at its first login, and a star already in it can't be sent.

## Goal

The seed's goal (`endless_complete`) is the level's Endless-complete star: the third Endless star, which the game
awards once one cycle's combined green and red score reaches 60.

- **Sent live.** When the game awards that star while you play, on the seed's map, connected, and with the save bound
  to that seed, the mod reports the goal to the server (`Goal sent: Endless complete on <map>` and an "Archipelago goal
  complete" notification). The green and red stars don't count.
- **Sent from a bound save.** After login on a save bound to the seed that has the star and whose goal state isn't
  `sent`, the mod reports the goal (`… (from the save)`). That covers a star earned offline: it's `pending` in the save
  (`Goal reached, not sent: not connected`), survives a restart with the save, and is sent at the next login.
- **Never from before the binding.** A star the save already had when it was bound, such as an old save or a game
  played offline from the start, makes the goal state `ineligible`. It's never sent (`Goal not sent: the star predates
  the binding`).
- **Refused.** On another map than the seed's, or with a goal the mod doesn't know, the goal isn't sent and the log
  says why. A send that fails leaves the goal `pending` for the next login.

## Keys

| Key | When | Effect |
|---|---|---|
| F9 | In an Endless level, intercept mode on (debug) | Receives the item of the upgrade selected in the upgrade panel, as though Archipelago had sent it. For a levelled upgrade that's one more level. Needs no bought slot. Logs `Debug receive: no upgrade selected` if nothing is selected. |

## Checking that it works

Every mod line in `Player.log` starts with `[Archipelago]`:

```sh
grep "\[Archipelago\]" ~/Library/Logs/bitrich/Rail\ Route/Player.log
```

A healthy start looks like this:

```
[Archipelago] Enabled.
[Archipelago] MultiClient 6.7.1.0, Newtonsoft.Json bound to Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed (1 Newtonsoft.Json assembly(ies) loaded)
[Archipelago] Intercept mode: on (settings: …/RailRouteArchipelago.settings.json)
[Archipelago] Settings: intercept on, server localhost:38281, slot Player, password none
[Archipelago] Runtime: game 3.0.18, platform OSXPlayer, os '…', arch X64, Harmony 2.4.2.0
[Archipelago] Patched Game.ResearchController.HasResearched: OK
[Archipelago] Patched Game.ResearchController.get_IsAutomationEnabled: OK
[Archipelago] Patched Game.ResearchController.Reset: OK
[Archipelago] Patched Game.ResearchController.ResearchedValue: OK
[Archipelago] Patched Game.Level.StorageController.DeleteSave: OK
[Archipelago] Patched Game.Level.StorageController.Rename: OK
[Archipelago] Patched Game.Level.StorageController.Save: OK
[Archipelago] Patched Multiplayer.Commands.Game.UnlockUpgradeCommand.Run: OK
```

When an Endless level loads, the mod logs the level (`Level loaded: <map>, storage …`), one `Upgrade …` line per
upgrade, the tier thresholds and the throughput rewards, the save state (`Save state (new game): unbound, …`), then
either `Archipelago offline: no server/slot configured` or:

```
[Archipelago] Connecting to localhost:38281 as Player
[Archipelago] Connected to localhost:38281 as Player (team 0, slot 1)
[Archipelago] Save bound to 12345678901234567890, slot Player (1)
[Archipelago] Resent 0 checks
[Archipelago] Received item list replaced: 0 item(s), 0 restored, 0 new
```

The item list is applied after the binding decision, even when it arrives before the `Connected` line.

If patching fails (for example after a game update), you'll see `Patching failed on …` instead. The game stays playable
without Archipelago features, and a "running degraded" notification appears when a level starts.
