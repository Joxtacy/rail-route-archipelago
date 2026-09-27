# Design

## Context

See proposal.md for motivation; requirements are in `specs/client/harmony-patching` and `specs/client/upgrade-purchase-interception`.

Observed in the decompiled Rail Route 3.0.18 `RailRoute.dll`:

- The game's `Game.ModController` loads `mods/<Name>/<Name>.dll` with `Assembly.LoadFrom` and only scans that DLL for `IGameMod` types when a DLL named after the folder exists. Other DLLs in the folder (e.g. `0Harmony.dll`) are not treated as mods, and `LoadFrom` resolves dependencies from the same directory.
- **Player purchase path:** `SystemUpgradeContextPanelView` (install button) → `CommandController.SendCommand(new UnlockUpgradeCommand { ResearchItem = … })`.
  - `UnlockUpgradeCommand.Validate()` returns `!item.Researched && ResearchController.CanResearch(item)`.
  - `UnlockUpgradeCommand.Run()` deducts `PrimaryPointsNeeded` / `SecondaryPointsNeeded` / `TertiaryPointsNeeded` from `Wallet`, then calls `ResearchController.CompleteResearch(item.Research)`.
- **Every other unlock** (tutorial chapters, `LevelController`, story, `ResearchAll`) calls `ResearchController.CompleteResearch` directly and never goes through `UnlockUpgradeCommand`.
- `ResearchController.CanResearch(ResearchItem)` is called only by the upgrade panel (to show the Install button) and by `UnlockUpgradeCommand.Validate()`.
- `CompleteResearch` side effects include enabling auto-accept/auto-reverse on stations, alert preferences, the unlock popup and `EventManager.TriggerResearchCompleted`. Undoing them after the fact is not practical.
- Newtonsoft.Json 13.0.2 and `UnityEngine.JSONSerializeModule` ship in `Managed/`. `mscorlib` exposes `RuntimeInformation.ProcessArchitecture`.

## Goals / Non-Goals

**Goals:**
- A patch lifecycle (apply once, unpatch on disable, all-or-nothing) that later Archipelago features can add patches to.
- A single seam, the purchase handler, where the future Archipelago client swaps "record pending check" for "send location check".
- Clear log evidence, per platform, of whether patching works.

**Non-Goals:**
- Persisting pending checks across save/load or game restarts. They live in memory for the session.
- Showing intercepted upgrades as "checked" in the upgrade panel beyond hiding the Install button.
- Multiplayer correctness. Commands run on the host; clients are not considered.

## Decisions

### D1: Harmony = `Lib.Harmony` 2.4.x (net48 asset), shipped as `0Harmony.dll`
This is the same package and version line as the community Rail Route QOL mod (2.4.1). The net4x build is self-contained, with MonoMod merged in.
- *Alternative: HarmonyX.* It's BepInEx-oriented and adds no value without BepInEx.
- *Alternative: BepInEx.* Its current release is broken on native arm64 macOS, and the native mod loader already works.

The csproj references the package normally, copies `0Harmony.dll` into `mods/RailRouteArchipelago/`, and keeps game DLLs `Private=false`.

### D2: Patch `UnlockUpgradeCommand.Run` with a skipping prefix, not `CompleteResearch`
`Run` is the only player-purchase path, so tutorial, story and mod grants stay untouched without any caller detection.
- In intercept mode the prefix deducts the point costs itself, records the check and returns `false`, so the original is skipped and `CompleteResearch` never runs.
- With intercept off it only logs and returns `true`.
- *Alternative: patch `CompleteResearch` or `ResearchItem.Finish`.* This would catch tutorial and mod grants too, and would need call-site detection.
- *Alternative: let `Run` complete, then revert `Researched`.* This leaves side effects applied (popup, auto-accept, `ResearchCompleted` event).
- *Trade-off:* the three-wallet deduction is duplicated from game code, so a game update changing costs must be mirrored. This is mitigated by reading costs from the `ResearchItem` fields, not constants.

### D3: Block repeat purchases with a `ResearchController.CanResearch(ResearchItem)` postfix
When intercept mode is on and the item's `Id` is pending, the result is forced to `false`. This one patch both hides the Install button and makes `Validate()` reject the command, so no points are spent.
- Checks are keyed by `ResearchItem.Id` (for example `track_speed1`), which is unique per level of a multi-level upgrade. The `Research` enum is not, since levels share a value.
- *Alternative: patch `Validate()` only.* The Install button would keep showing, and every click would silently fail.

### D4: Purchase handling behind an interface
`IUpgradePurchaseHandler.OnPurchase(ResearchItem) → bool handled`. This change ships `LogOnlyHandler` (intercept off) and `PendingCheckHandler` (intercept on, in-memory `HashSet<string>` of pending IDs, plus a notification). The Archipelago client later replaces `PendingCheckHandler` without touching patches.

### D5: Settings file = `RailRouteArchipelago.settings.json` next to the DLL, parsed with Newtonsoft
The mod's folder is found from `typeof(ArchipelagoMod).Assembly.Location`. The file is optional, and missing, invalid or unknown values mean off. Newtonsoft is already in the game and will be needed by the Archipelago client anyway.
- *Alternative: `JsonUtility`.* It gives no error detail and needs an extra module reference.

### D6: All-or-nothing patch application
- `harmony.PatchAll(typeof(ArchipelagoMod).Assembly)` runs in a try/catch.
- On any exception it calls `harmony.UnpatchSelf()`, logs the error with platform and architecture, marks the mod degraded, and queues a side notification for the next level start. Notifications need an in-play `NotificationController`.
- A static guard prevents re-patching on repeated `OnEnable` or context changes.
- `OnDisable` calls `UnpatchSelf()`.

### D7: Debug grant key for verifying the "mod grants an upgrade" path
In play mode, a key polled from a small `MonoBehaviour` the mod creates (default **F9**) grants the most recently intercepted upgrade via `ResearchController.CompleteResearch(item.Research, ignoreLockedState: true)` and removes it from pending. This exercises the path Archipelago item receipt will use. It is only active when intercept mode is on.
- *Alternative: an in-game debug menu.* This is more UI work than the proof needs.

### D8: Platform reporting
The startup log line contains:
- `Application.version`, `Application.platform` and `RuntimeInformation.ProcessArchitecture`
- `SystemInfo.operatingSystem`
- the Harmony assembly version

`ProcessArchitecture` distinguishes native arm64 from Rosetta x86_64.

## Risks / Trade-offs

- **Harmony detours fail on native arm64 Mono** (MonoMod arm64 history). → Task 1 is a platform spike before building interception. If it fails, the documented fallback is running the game under Rosetta: Steam launch option `arch -x86_64 %command%`, or Get Info → "Open using Rosetta". The failure is surfaced per the patch-failure requirement.
- **Game update renames or changes `UnlockUpgradeCommand` or `CanResearch`.** → Target methods are resolved with `AccessTools` and fail loudly (D6). The startup log records the game version.
- **Duplicated cost deduction** (D2). → Costs are read from item fields. Verification compares the wallet before and after with the unmodded behavior.
- **Newtonsoft version clash** once Archipelago.MultiClient.Net lands. → Not in this change. It is noted for the client-connection change.
- **In-memory pending checks are lost on reload.** A re-bought upgrade after reload records the check again. → Accepted for this proof. Persistence belongs to the Archipelago state change.
- **Windows unverified from this machine.** → A manual verification task on the user's Windows install checks the `Player.log` lines.

## Migration Plan

No migration is needed. Rollback means deleting `mods/RailRouteArchipelago/`, or removing the settings file to return to log-only behavior.

## Open Questions

- The exact Windows `Player.log` location and mods path should be confirmed during Windows verification. This affects docs only.
