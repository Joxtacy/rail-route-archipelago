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
