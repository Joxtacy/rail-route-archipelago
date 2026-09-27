# Rail Route × Archipelago — feasibility findings (2026-09-27)

## Verdict: feasible, and a good fit

No existing AP world or client found (archipelago.gg games list, GitHub, AP wiki). Discord #apworld-index not checked (login needed).

## Game tech (local install, v3.0.18)
- Unity 2021.3.45f2, **Mono** (not IL2CPP), universal binary (x86_64 + arm64)
- Game logic in `Managed/RailRoute.dll`, not obfuscated; decompiles cleanly with `ilspycmd`
  - Decompile: `DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec DOTNET_ROLL_FORWARD=Major ilspycmd -p -o out -r <Managed> <Managed>/RailRoute.dll`
- Newtonsoft.Json 13.0.2 already in `Managed/`

## Official mod loader (no BepInEx needed)
`Game.ModController.Start()` loads `<persistentDataPath>/mods/<Name>/<Name>.dll` and instantiates every
exported class implementing `Game.Mod.IGameMod` (use `AbstractMod`):
- `OnEnable()`, `OnDisable()`, `OnContextChanged(IControllers deps)` — check `deps.CurrentMode == GameMode.Play`
- macOS persistentDataPath here: `~/Library/Application Support/RailRoute/` → mods go in `.../RailRoute/mods/<Name>/`
- Log: `~/Library/Logs/bitrich/Rail Route/Player.log`
- Built-in example: `Managed/EndlessHints.dll`
- Template: https://github.com/bitrich-info/railroute-mod-template
- Harmony not bundled; mods ship their own `0Harmony.dll` (see asdfCYBER/Heatmap, Turtle7307/Rail-Route-QOL-Mod)
- Risk: Harmony/MonoMod detours on native arm64 Mono — test early. Fallback: run under Rosetta (`arch -x86_64`).

## Useful hooks
- `ResearchController` (`Ctx.Deps.ResearchController`)
  - `enum Research` (~65 ids), ~50 `ResearchItem`s with `Id`, `Parent`, `Tier`, `Type` (Green/Red/Blue), point costs
  - `CompleteResearch(Research, ignoreLockedState)` — grant an upgrade (received AP item)
  - `UnlockingConstraint : Func<ResearchItem,bool>` — built-in gate used by the campaign; can block buying
  - `OverrideInitialResearchState(item, locked, unlockedOnStart)`
  - `GetCurrentTier(ResearchType)`, `GetCurrentStars()`, `ThroughputRewards`
- `EventManager` (`Ctx.Deps.EventManager`): `ResearchCompleted`, `TimeCycleFinished`, `CycleReportReady`,
  `ExperiencePointsAwarded`, `TrainDepartedFromStation`, `ContractAccepted`, `OnWaveFinished`, `TaskFulfilled`,
  `ChapterFinished`, `TrainCrashed`, `BeforeLevelStarted`, string events `LevelCompleted`/`LevelFailed`, ...
- `Game.Achievements.AchievementController` — 76 achievements with count ladders
- `Wallet` — money + research points (primary/secondary/tertiary) → filler/trap items

## Proposed AP design (first draft)
- **Items**: each system upgrade (progressive for Track Speed, Station Count, Offered Contracts);
  filler = XP/money bundles; traps = e.g. train delay/breakdown (Expect Delays), switch lock.
- **Locations**: "System Upgrade Slot N" purchases (spend XP → send check instead of unlock),
  tier unlocks, endless stars (green/red/total), cycle count milestones, achievement ladders
  (on-time dispatches, per-train-type dispatches, ongoing contracts), timetable/rush hour stars.
- **Goal**: earn all 3 stars on a chosen endless map / N stars total.
- Scope v1 to Endless mode on one map (e.g. Prague) — upgrades there are the core progression.

## Client library
Archipelago.MultiClient.Net 6.7.1 — use the net45 or netstandard2.0 build in a net472/net48 mod. Watch for a
Newtonsoft duplicate clash. Marshal callbacks to the Unity main thread.

## Next steps
1. Hello-world mod via the native loader on macOS (confirm mods path + arm64 load)
2. Add Harmony, patch the research purchase → confirm detours on arm64 (else Rosetta)
3. Add MultiClient.Net, connect to a local AP server
4. Write the `rail_route` APWorld (Python) with items/locations/regions from the research tree

## Platform results: Harmony spike (2026-09-27, change add-harmony-upgrade-intercept)

| Mode | Result |
|---|---|
| macOS via Steam (default) | **Works.** Steam launches the game under Rosetta (`vmmap`: `X86-64 (translated)`, although `steam_osx` itself is ARM64). Harmony 2.4.2 patched `UnlockUpgradeCommand.Run`. A real purchase logged `Upgrade purchased: manual_signal_security (Green, tier 1, cost 1/0/0)`. |
| macOS native arm64 (`arch -arm64`) | **Not applicable to the Steam build.** `PlugIns/libsteam_api.bundle` is x86_64-only (every other plugin is universal), so startup throws `DllNotFoundException: libsteam_api` in `SceneController.Awake`. The mod loader never runs. |
| Windows 11 x64 via Steam | **Works.** Loaded from `%USERPROFILE%\AppData\LocalLow\bitrich\Rail Route\mods\RailRouteArchipelago\`. `Runtime: … platform WindowsPlayer, arch X64, Harmony 2.4.2.0`. Both patches OK, and a real purchase was intercepted: `Check recorded: auto_accept (Green, tier 1, cost 3/0/0)`. |

Consequences:
- The same DLL build (compiled on macOS) runs unchanged on macOS and Windows.
- Mods must be in their own subfolder (`mods/RailRouteArchipelago/`). DLLs placed directly in `mods/` are silently ignored; the first Windows attempt failed this way.
- There is no Rosetta setup step for players: Steam already runs Rail Route under Rosetta on Apple Silicon.
- The MonoMod arm64 detour risk doesn't affect the Steam build. It would only matter for a DRM-free/GOG build (which ships universal `libGalaxy64.dylib`), which is untested.

## Upgrade data for the APWorld (2026-09-27, change add-apworld-v0)

Read from the decompiled 3.0.18 `RailRoute.dll` and the game assets. `apworld/rail_route/data.py` encodes all of it.

- **Upgrade table.** `ResearchController.researchItems` holds 50 upgrades: 36 Green (14/11/11 in tiers 1/2/3) and 14 Red (4/7/3). There are no Blue upgrades, although the enum has the type.
  - Each upgrade has an `Id`, an optional `Parent`, a colour, a tier and point costs.
  - English titles come from the `Research Strings Table` localization table, key `research_item_<id>_title`. The world normalizes them to Title Case ("Unlimited stations" → "Unlimited Stations").
  - Parent gating (`GetUpgradeRequiredForUnlock`): a slot can't be bought until its parent's `Researched` flag is set.
- **DLC filtering** (`SystemUpgradesPage.InstantiateUpgrades`):
  - Without Expect Delays, 6 upgrades are hidden: Maintenance Depot, Additional Service Capacity, Expanded Service Capacity, Field Efficiency, Service Automation and Operating Hours.
  - With Happy Passengers, `custom_contracts_alt` (Green T1) is shown and `custom_contracts` (Red T3) is hidden. Without it, the reverse. The world treats them as one location and one item.
- **Red income.** Red points only come from red contracts. `ContractGenerator.TryRandomRed` only generates Freight, Regional or Urban contracts when the matching upgrade is researched, and Urban contracts also need a station with a coach yard.
- **Tier thresholds** (`GetCurrentTier`). Tiers unlock on best points per cycle, not on the number of upgrades bought.
  - C# defaults: Green T2 at 10 and T3 at 25; Red T2 at 8 and T3 at 30.
  - Red opens once Green reaches 8 points per cycle (which grants 3 red points) or Green tier 2.
  - Endless-complete is a Green throughput of 60.
  - Unverified: the serialized `SystemUpgradeTierDefaults` asset may override these. It's on the M3 test-round list.
- **Endless maps** used by the world, as level UUID and difficulty: Haarlem (`Haarlem`, 0), Prague (`prague`, 3) and Amsterdam (`Amsterdam`, 5). None of them stores an upgrade-override block in the formats found so far. The M3 test round confirms this at runtime.

## Split flags (2026-09-27, change client-split-flags)

Read from the decompiled 3.0.18 `RailRoute.dll`. In intercept mode, on an Endless (`ScoringModel.Economy`) level, the client keeps `ResearchItem.Researched` as the game's **slot bought** flag and redirects the upgrade **effects**.

- **Effect queries.** Gameplay reads unlock state only through three `ResearchController` members, which the client patches:
  - `HasResearched(Research)` (78 call sites): reads `binaryResearchDict[r].Researched`. It returns false for levelled research.
  - `ResearchedValue(Research)` (10 call sites): the `Value` of the last bought item in the `gradualResearchLookup` chain (ordered by `Value`), else the private `InitialValue[r]` (4 stations, `Connection.Speeds[0]`, 1 contract offer). It throws for non-levelled research.
  - The `IsAutomationEnabled` auto-property (1 reader, `TrainBottomBarPlaceable`, through `IResearchController`). `CompleteResearch` and `SavedResearchItem.Load` set it for `EnablesAutomation` items (departure, arrival and routing sensors, coach yard).
- **Direct `.Researched` readers** outside `ResearchController` are all slot, UI, tutorial or save concerns, so they keep working with "slot bought" semantics:
  - the upgrade panel and buttons (installed state, tier-complete indicators) and `SystemUpgradeContextPanelView`'s cost rows
  - `UnlockUpgradeCommand.Validate` (`!Researched && CanResearch`)
  - parent gating in `GetUpgradeRequiredForUnlock`
  - tutorial tasks (`Unlock*Task`), story chapter S5P2 and the campaign's `UnlockingConstraint`
  - save serialization (`SavedResearchItem`), which stores each slot's `researched` bit and on load calls `Finish()` with no event
- **`ResearchCompleted` subscribers only re-query.** None of them applies an effect from the event argument. The subscribers are the upgrade page, items, buttons and context panel, `UnlockedByResearch`, the bottom bars, offices, station and scheduler configuration views, contract panels, `InterfaceController` (fires `InterfaceConfigurationChanged` for the three interface upgrades), story triggers and tutorial tasks. `BottomBarController` adds the item to its "recently unlocked" highlight. So the client fires the event to refresh the UI after a slot purchase or a received item.
- **`ResearchController.Reset()`** clears every slot's `Researched` flag and `IsAutomationEnabled`. Its callers:
  - `GameController`'s level clear, on every level unload and load
  - the level editor's `SettingsPanel`
  - the `ArrivalSensorTutorialChapter` and `RoutingSensorTutorialChapter` mini-tutorials

  The client clears received items and game grants in a postfix on it.
- **Selected upgrade.** `Ctx.Deps.MenuController.SystemUpgradesMenu.SystemUpgradeContextPanelView.SystemUpgradeContextPanelModel?.ResearchItem`, with the view's `gameObject.activeInHierarchy` as "shown" (as in `SelectUpgradeSubtask`).

### Duplicated game logic: re-check after game updates

- `UnlockUpgradeCommand.Run` cost deduction: `Wallet.AddResearchPointsPrimary/Secondary/Tertiary(-cost)`, mirrored in `SlotPurchaseHandler`.
- `CompleteResearch`'s side-effect switch (when the scoring model isn't `Score`), mirrored in `UpgradeReceiver`:
  - auto-accept or auto-reverse on every non-waypoint station
  - train-alert preferences (braking, stopped, arrived)
  - manual signal route preview
  - signalling safety
  - then `TriggerResearchCompleted`, `UnlockPopup` (play mode, `GameController.Loaded`, upgrades menu not shown) and `IsAutomationEnabled` for `EnablesAutomation` items
- The `binaryResearchDict` / `gradualResearchLookup` split from `ResearchController.Awake` (items with or without a `Value`), and the private `InitialValue` field, mirrored in `EffectState`.

### In-game test round (2026-09-27, macOS via Steam, Haarlem Endless)

Every step was confirmed against `Player.log`, except where a step says it depends on what the user saw.

- All five patches applied (`HasResearched`, `get_IsAutomationEnabled`, `ResearchedValue`, `Reset`, `UnlockUpgradeCommand.Run`). The effect state indexed 41 binary, 3 levelled and 4 automation upgrades.
- **Buying and receiving are independent.** A bought slot shows Installed and records `Check recorded: <id> → <location>`, and the upgrade stays locked until received. It works in both orders: buy then F9, and F9 then buy. Auto-accept switched stations on when received, before its slot was bought. No repeat purchase was possible.
- **Parent gating follows slots, not items.** Receiving Automatic Routing didn't make Perpetual Circuit buyable. Buying the Automatic Routing slot did.
- **Progressive.** Buying Basic Tracks didn't unlock 80 km/h. One F9 on the chain received `track_speed1` (80 km/h only).
- **Children without their parent's item.** Departure Sensor (without Automatic Routing) and Shunting Sensor (without Shunting Commands) could both be placed once received. This answers the M3 roadmap question for those two sensors.
- **Save and reload.** Every load logs `Effect state reset` (the `GameController` level clear path covers saved maps). Bought slots stayed Installed after the reload. The log can't show that, so it rests on what the user saw. Received effects were gone after the reload, and placed objects survived it.
- **Leak check.** Loading another save cleared received items.
- **Outside Endless.** The Arrival Sensor mini-tutorial isn't an Economy level: the game unlocks everything (`InitDefaults`), `Reset()`s, then grants `platform_sensor`. It logged no check and F9 did nothing. The mod still logs those unlocks as `Game grant:`, because intercept mode is on, but they don't affect anything outside split mode.
- **Intercept off.** Purchases log `Upgrade purchased:` and unlock as in the unmodded game.
- **No game grants on Haarlem.** This confirms that the map has no upgrade overrides.
