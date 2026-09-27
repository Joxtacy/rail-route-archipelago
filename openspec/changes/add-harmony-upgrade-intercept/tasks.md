# Tasks

## 1. Harmony platform spike (go/no-go)

- [ ] 1.1 Add `Lib.Harmony` 2.4.x to `client/RailRouteArchipelago.csproj` and extend the `DeployMod` target to copy `0Harmony.dll` into `mods/RailRouteArchipelago/`. Verify with `dotnet build -c Release` (0 errors, 0 warnings) and check that `0Harmony.dll` sits next to `RailRouteArchipelago.dll` in the mods folder.
- [ ] 1.2 Add the patch lifecycle to `ArchipelagoMod` (D6):
  - `Harmony` instance with id `railroute.archipelago`
  - static once-guard, `PatchAll` in `OnEnable`, `UnpatchSelf` on failure and in `OnDisable`
  - log one line per patched method

  Verify: `Player.log` shows exactly one `Loaded MOD: RailRouteArchipelago` entry and no second `PatchAll` after returning to the main menu and starting a level again.
- [ ] 1.3 Add the startup platform line (D8): game version, OS, process architecture, Harmony version. Verify it appears in `Player.log` on a native launch and reports `Arm64`.
- [ ] 1.4 Add a trivial logging postfix on `UnlockUpgradeCommand.Run`. Verify on **native macOS arm64** that buying any upgrade in an Endless map logs the upgrade id, colour, tier and cost. Record the result (works / fails, with the error) in `FINDINGS.md`.
- [ ] 1.5 If 1.4 fails natively: launch under Rosetta (`arch -x86_64`), confirm the platform line reports `X64` and the postfix fires, then document the Rosetta launch setup in `FINDINGS.md`. Skip this task if 1.4 passed. Either way, record which mode later groups are verified in.

## 2. Pure logic with unit tests

- [ ] 2.1 Create `client/src/Core/` containing only code with no game or Unity dependencies:
  - `ModSettings` with `interceptUpgradePurchases` (default `false`), `TryLoad(path, out settings, out error)`, using Newtonsoft
  - `PendingChecks` (add / contains / remove / most-recent, keyed by string id)

  Verify the mod still builds.
- [ ] 2.2 Create `client.Tests/` (xunit, current .NET) that compiles `client/src/Core/*.cs` as linked sources. Cover:
  - missing file → off
  - `{"interceptUpgradePurchases": true}` → on
  - malformed JSON → off plus an error message
  - unknown keys ignored
  - duplicate add ignored
  - most-recent / remove ordering

  Verify `dotnet test client.Tests` passes.

## 3. Upgrade purchase interception

- [ ] 3.1 Implement `IUpgradePurchaseHandler` with `LogOnlyHandler` and `PendingCheckHandler` (D4).
  - `PendingCheckHandler` deducts primary, secondary and tertiary costs from `Wallet` using the item's fields, records `ResearchItem.Id` in `PendingChecks`, shows a side notification "Check sent: <upgrade title>" and logs it.
  - Load settings from `RailRouteArchipelago.settings.json` beside the DLL at enable time; log a warning for a malformed file.

  Verify: build succeeds, and a malformed settings file produces the warning line in `Player.log`.
- [ ] 3.2 Replace the spike postfix with a skipping prefix on `UnlockUpgradeCommand.Run` that delegates to the active handler (D2). Verify with intercept **off** that buying `autoblock` in a fresh Endless map unlocks it and spends exactly 3 green points, the same as unmodded.
- [ ] 3.3 Add the `ResearchController.CanResearch(ResearchItem)` postfix that returns `false` for pending ids when intercept is on (D3). Verify with intercept **on** in a fresh Endless map:
  - Buying `autoblock` spends 3 green points and leaves autoblocks unavailable in the build menu.
  - The notification appears.
  - The Install button no longer shows for that upgrade.
- [ ] 3.4 Verify levelled upgrades with intercept on: buying `track_speed1` records only `track_speed1`, and 80 km/h track stays unavailable.
- [ ] 3.5 Add the F9 debug grant (D7), active only in play mode with intercept on: it grants the most recent pending upgrade via `CompleteResearch(..., ignoreLockedState: true)` and removes it from pending. Verify that after intercepting `autoblock`, pressing F9 makes autoblocks buildable, shows the game's unlock popup, and records no new check.
- [ ] 3.6 Verify that non-purchase unlocks are untouched with intercept on: play one mini-tutorial that grants an upgrade (for example the Arrival Sensor tutorial). The upgrade unlocks and no "Check sent" notification or log line appears.
- [ ] 3.7 Add `client/README.md` covering build, install paths per OS, the settings file, the F9 key and where `Player.log` lives. Verify the documented commands run as written on macOS.

## 4. Failure handling and cross-platform checks

- [ ] 4.1 Verify loud, non-fatal failure: temporarily point one patch at a non-existent method name in a local build. The game still reaches the main menu, `Player.log` shows the error and architecture, no interception is active (a purchase unlocks normally), and a "running degraded" notification shows at level start. Revert afterwards.
- [ ] 4.2 Windows x64 manual check on the user's Windows install:
  - copy `mods/RailRouteArchipelago/` (DLL, `0Harmony.dll`, settings file)
  - confirm the platform line reports `X64` and repeat 3.3
  - record the actual Windows mods and `Player.log` paths in `client/README.md` and fix the csproj defaults if they differ
- [ ] 4.3 Update `FINDINGS.md` with the final platform matrix (macOS arm64 native, macOS Rosetta, Windows x64: works / not tested / fails). Verify that `openspec validate add-harmony-upgrade-intercept` passes.
