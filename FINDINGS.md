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
Archipelago.MultiClient.Net 6.7.1, net45 build, bound to the game's Newtonsoft.Json. See "Client library" under
"AP connection" below for the result.

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
  - Endless-complete is a combined green + red score of 60 in one cycle (`Wallet.ScoreReachedMax`), not green alone. See "Goal completion" below.
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

## AP connection (2026-09-27, change client-ap-connection)

### Client library

| Assembly | Identity | Newtonsoft.Json it references |
|---|---|---|
| `net45/Archipelago.MultiClient.Net.dll` | 6.7.1.0, unsigned | `Newtonsoft.Json 11.0.0.0, PublicKeyToken=null` |
| `net45/Newtonsoft.Json.dll` (bundled) | 11.0.0.0 (file 11.0.1), unsigned | |
| the game's `Managed/Newtonsoft.Json.dll` | 13.0.0.0 (file 13.0.2), signed | |

- **The binding works.** On game 3.0.18 (macOS via Steam, Rosetta, `arch X64`) the mod logs `MultiClient 6.7.1.0, Newtonsoft.Json bound to Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed (1 Newtonsoft.Json assembly(ies) loaded)`. There was no `FileNotFoundException`/`TypeLoadException`, and exactly one `Loaded MOD: RailRouteArchipelago`, so the game doesn't treat the MultiClient DLL as a mod. The `AssemblyResolve` handler logged no redirect. Mono bound the unsigned 11.0.0.0 reference to the loaded 13.0.0.0 by simple name on its own, and the handler stays as a fallback. The bundled copy isn't shipped.
- `ClientWebSocket` works for `ws://` on the game's Mono. `wss://` (TLS, archipelago.gg) is untested.
- For a `host:port` address, MultiClient tries `wss://` first. A plain `ws://` server logs `connection rejected (400 Bad Request)` and then accepts the `ws://` retry. This is harmless.
- The 0.6.7 server warns: `your client does not support compressed websocket connections! It may stop working in the future.` That's a MultiClient/`ClientWebSocket` limitation, and it isn't ours to fix.
- **`SocketClosed` fires only for a close frame.** When the server process dies, MultiClient raises only `ErrorReceived` (`The remote party closed the WebSocket connection without completing the close handshake.`). So the session treats a socket error that leaves the socket not `Connected` as the disconnect.
- **The item list on connect is conditional.** `MultiServer.py` appends `ReceivedItems(index 0, all items)` to the `Connected` reply only when the slot has items. Otherwise the slot's first item also arrives at index 0. MultiClient raises one message's packets in order on one thread, so "the index-0 packet right after `Connected`" is how the client identifies the replay.
- A failed `ConnectAsync` to an unreachable server surfaces as `A task was canceled`. The real reason (`Unable to connect to the remote server`) arrives just before it as a socket error.

### AP connection test round (2026-09-27, macOS via Steam)

A local 0.6.7 server, a Haarlem seed without DLCs (43 locations). Every step was confirmed against `Player.log` and the server console.

- **Connect.** A fresh Haarlem game (both DLCs off) connected with no slot-data warning. Notifications: "Archipelago connected".
- **Wrong-map save polluted the seed (fixed).** At startup the game auto-loaded a Prague save with 6 bought slots. As first specified, the mod stayed connected on the mismatch and resent those 6 checks into the Haarlem seed. Now a map mismatch blocks every check for the connection (`Checks not sent: the level's map doesn't match the seed (resend skipped)`). This was confirmed on Prague and Amsterdam. A DLC-only mismatch (Haarlem with Expect Delays on) warns and still sends.
- **Send a check.** Buying Autoblocks logged `Check sent: Autoblocks (1000)`, and its item (Signalling Safety, the slot's own) came straight back.
- **First item misread as the replay (fixed).** The slot had no items, so Signalling Safety arrived at index 0 and was applied silently. After the fix, `Received item list replaced: 1 item(s), 1 new` is applied with its popup.
- **No popup inside the upgrades menu (added).** The game suppresses the unlock popup while the system upgrades menu is open, and the item from one's own purchase always arrives then. Now a side notification "Received <upgrade> from <player>" replaces it. Confirmed with Waypoints.
- **Receive.** `/send Player Tunnels` gave Tunnels with the popup (menu closed). `Progressive Track Speed` made 80 km/h buildable, and 120 km/h not. `Green XP Bundle` logged `Filler item ignored` and changed nothing.
- **Save/reload replay.** The reload logged `Effect state reset`, a reconnect, `Resent 1 checks` and `Received item list replaced: 2 item(s), 0 new (replay on connect)`. The user saw "Restored 2 Archipelago items" and no unlock popups, and Tunnels and 80 km/h still worked.
- **Offline purchase.** With the server down, loading the save logged the failure and "Archipelago connection failed – see Player.log". The level stayed playable. Auto-accept Trains was bought and saved (`Check recorded`, nothing sent). After a server restart and a reload: `Resent 2 checks: Autoblocks, Auto-accept Trains`. The server then sent that location's item (Shunting Sensor) as a new item.
- **Connection lost.** Killing the server during play logged `Archipelago disconnected from localhost:38281 as Player: …`, and the notification appeared (after the fix above).
- **Wrong slot.** The log listed the server's reason, `The slot name did not match any slot on the server.`, and the level stayed playable.
- **Not Endless.** The Arrival Sensor mini-tutorial made no connection attempt.
- **No settings.** `Archipelago offline: no server/slot configured` was logged, and F9 still received the selected upgrade.
- **Not tested:** a hosted `wss://` room, and a live purchase in a map-mismatched level (the resend path uses the same block and was confirmed).

### Answers to the open M3 questions (level-load diagnostics, fresh Endless games)

| Map | Storage | Upgrade set at start | Tier thresholds (tier 1/2/3) |
|---|---|---|---|
| Haarlem | **CuratedMapPack** | default: nothing bought or locked, no game grants | Green 0/10/25, Red 0/8/30, Blue 0/2/5 |
| Prague (`prague`) | Main | default | same |
| Amsterdam | Main | default | same |

- None of the three maps overrides the upgrade set.
- The runtime thresholds match the C# defaults (Green 10/25, Red 8/30), so the serialized `SystemUpgradeTierDefaults` doesn't override them.
- **Haarlem's `Storage` is `CuratedMapPack`.** Whether players without the Curated Map Pack DLC can start it is unverified. If they can't, the APWorld's default map (`haarlem`) should change.
- Throughput rewards at start (four in total): Green 8 → 3 Red points, Green 20 → Green star, Red 20 → Red star, and Green 60 with neither points nor a star. That last one is the Endless-complete star (see "Goal completion" below).

## Goal completion (2026-09-27, change client-goal-completion)

Read from the decompiled 3.0.18 `RailRoute.dll`.

- **The Endless-complete reward** is the `ThroughputRewards` entry with `RequiredThroughputType == Green`, no `RewardedStarType` and no `RewardedPointsAmount`. `GetCurrentStars`, `VictoryScreenEndless` and `CycleReport` look it up the same way.
- **It counts green + red.** `ResearchController.OnExperiencePointsAwarded` compares that reward against `Wallet.ScoreReachedMax`, the best cycle's `PrimaryPoints + SecondaryPoints`. Every other Green reward uses `PrimaryPointsPerCycleReachedMax`. The APWorld's goal logic counts green items only, which is conservative.
- **Grant order.** It sets `reward.Granted = true` first, then calls `StorageController.SubmitStar`, unlocks an achievement, force-autosaves, shows the star panel, and last calls `EventManager.TriggerStarAwarded(LevelDefinition)`. All of that runs on the main thread. The event doesn't say which star, so the client reads the reward's `Granted` flag and counts only its false-to-true move within the level.
- **Saved with the level.** `Granted` is restored from `SavedResearchController.throughputRewards` without raising `StarAwarded`. `SubmitStar` also records the star profile-wide per map in `ProgressStore`; the client ignores that, because it predates any seed.
- Endless levels never raise the `LevelCompleted` string event.
- `ModifyThroughputStarReward(int)` replaces the reward with a new, ungranted one. The test setting `debugEndlessCompleteThreshold` uses it, and the lowered threshold is saved with the level (it shows as e.g. `Green 3 → (granted)` in the rewards line after a reload).

### Goal test round (2026-10-01, macOS via Steam, Haarlem Endless)

Local 0.6.7 server, three fresh Haarlem seeds (A, B, C), `debugEndlessCompleteThreshold` 3. Every step was confirmed against `Player.log` and the server console.

- **Live goal (seed A).** A fresh Haarlem game logged `Debug: endless-complete threshold set to 3`. At a cycle score of 3 the star panel appeared, then `Goal sent: Endless complete on Haarlem` and the notification. The server logged `Player (Team #1) has completed their goal.` At startup the game had auto-loaded an Amsterdam save first: checks were blocked by the map mismatch, but the debug threshold was applied and saved into that save too.
- **Save with the star (seed B).** Loading step 1's save logged `Endless-complete star found in the save; the goal isn't sent automatically. Press Shift+F10 to send it.` with its notification, and the server recorded no goal. Plain F10 logged nothing. Shift+F10 logged `Goal sent: Endless complete on Haarlem (manual)`, and the server recorded the goal. A second Shift+F10 logged `Goal not sent manually: the goal was already sent in this game run`.
- **Manual refusal without the star (seed C).** Shift+F10 on a fresh Haarlem game: `Goal not sent manually: the level hasn't earned the Endless-complete star`.
- **Map mismatch (seed C).** A fresh Prague game reached the star: `Goal reached, not sent: the level's map doesn't match the seed`, and Shift+F10 refused with the same reason. No goal on the server.
- **Offline, then reconnect (seed C).** With the server stopped, a fresh Haarlem game failed to connect, Shift+F10 logged `Goal not sent manually: not connected`, and the star logged `Goal reached, not sent: not connected`. After a server restart, reloading the save in the same game run logged `Goal sent: Endless complete on Haarlem (pending since offline)`, and the server recorded the goal.
- **Not tested:** the green star alone (20 green points in a cycle without the debug threshold) was skipped because it takes too long. The code reads only the Endless-complete reward's `Granted` flag, which a green or red star leaves unchanged.

## Save files (2026-10-01, change client-seed-binding)

Read from the decompiled 3.0.18 `RailRoute.dll`. The client keeps its per-save Archipelago state in a sidecar, `saves/<FileName>.ap.json` (see `client/README.md` "Save state").

- **Location and format.** Saves are `<persistentDataPath>/<StorageController.SaveDirectory>/<FileName>.mp.lz4` (`SaveDirectory` is the public field `"saves"`): MessagePack with the contractless resolver and LZ4 block array. A `.jpg` thumbnail sits next to each one, and legacy saves are `.gd.gz`. On macOS that's `~/Library/Application Support/RailRoute/saves`.
- **No room for mod data.** `SavedGame` is a fixed set of private `readonly` fields with no extension slot.
- **Every save is a new file name.** `GetSaveFileName` gives `<levelUuid>[#discriminator]_<name>_<hh-mm-ss>_<5 chars of a new Guid>`, and `GetAutoSaveFileName` gives `<levelUuid>[#discriminator]_Autosave <name>_<hh-mm-ss>_autosave`, with the in-game time. So even "save over" or a repeated autosave writes a new name.
- **One write funnel.** Every write (manual save, autosave, `ForceAutoSave`, and the autosave on leaving a changed level) goes through `GameController.SaveGameInBackground`, which sets `LoadedSave = null`, calls `StorageController.Save(string saveName, SavedGame)` in its first step on the main thread, right after `new SavedGame(null)` captured the state, and sets `LoadedSave = GetSaveFile(saveName)` after the async write. `Save` is `async`, so a Harmony prefix runs synchronously at the call. Autosaves are then pruned.
- **Delete.** `StorageController.DeleteSave(SaveFile)` deletes the save and both screenshot extensions, then raises `SaveDeleted`. Every delete goes through it: the in-game `SaveGameModal`, the main menu's `MapItemSave`, `PruneAutoSaves` (keeps the five newest autosaves per level) and `GameController`'s `ReturnBackToSave`. The main-menu delete runs outside Play mode, so the sidecar patch is gated on intercept mode, not split mode.
- **Rename.** `StorageController.Rename(SaveFile, string)` moves the save and screenshots to a new `GetSaveFileName` (new suffix, same in-game time) and returns the new `SaveFile`. Only the main menu's `MapItemSave` calls it.
- **Save listing.** `IsMatchingSave` only accepts the `.mp.lz4` and `.gd.gz` suffixes, so a `.ap.json` file never shows up as a save.
- **Loaded save.** `GameController.LoadedSave` is the `SaveFile` being loaded, set before `Loaded` turns true. A new game, a restart and multiplayer set it to `null`.
- **Steam Cloud.** Locally, `remotecache.vdf` for app 1124180 lists only `saves/*.mp.lz4` files, so sidecars don't sync.
- **Re-check after game updates:** the three patch targets (`Save`, `DeleteSave`, `Rename`) and that no second write path to `saves/` has appeared.

### Seed binding test round (2026-10-01, macOS via Steam, Haarlem Endless)

Local 0.6.7 server with two Haarlem seeds: A (`59949805810900081020`, room password) and B (`69207091424873024127`), hosted one at a time on port 38281, with `debugEndlessCompleteThreshold` 3. Every step was confirmed against `Player.log`, the `saves/` folder and the server console. The three `StorageController` patches logged `Patched … : OK`.

- **Bind a new game (A).** The password login succeeded and logged `Save bound to 59949805810900081020, slot Player (1)`. A purchase logged `Check sent: Signalling Safety (1006)`, and the first item arrived live with its popup. The manual save `herpderpA` got a sidecar with the binding, the sent check and index 1. At startup the game auto-loaded a Prague autosave first: it logged `Save not bound` and skipped the resend.
- **No repeated side effects (A).** `/send Player Auto-accept Trains` arrived live. With auto-accept turned off at one station, the save `herpderpA2` (index 2) reloaded with `2 restored, 0 new`, and the station stayed off. After a `/send Player Autoblocks` while the save wasn't loaded, the next reload logged `3 item(s), 2 restored, 1 new`.
- **Live goal (A).** `Goal sent: Endless complete on Haarlem`, and the server logged the goal. The star-moment autosave's sidecar holds `none`, because the game force-autosaves before `StarAwarded`; the next manual save holds `sent`. Completing the goal made the server auto-release the rest of the seed: 42 more items arrived live, giving index 45.
- **Pending goal across a restart (A).** With the server stopped, `herpderpA2` reached the star: `Goal reached, not sent: not connected`. The manual save's sidecar held `pending` with the loaded index 2. After a game restart and a server restart, loading it logged `Goal sent: … (from the save)` and `45 item(s), 2 restored, 43 new`. The server had the goal already from step 3, so it logged nothing new.
- **Foreign save (B).** Loading a seed A save logged `Save refused: bound to 59949805810900081020/1 (this save: …; server: 69207091424873024127/0/1)` and disconnected. No checks and no items, and the sidecar's checksum was unchanged.
- **Old save with the star (B).** After deleting the sidecar of a save with the star, its load logged `Save bound to 69207091424873024127`, wrote the sidecar with `goal: ineligible` and logged `Goal not sent: the star predates the binding`; a second load logged the same. No goal on the server. The bind sent the save's bought slot into seed B, as designed for unbound saves.
- **Map mismatch, live purchase (B).** On the Prague autosave: `Save not bound`, the resend skipped, and a live purchase logged `Checks not sent: the level's map doesn't match the seed (Manual Signal Route Preview)`. No Prague sidecar was written.
- **Corrupt sidecar (B).** Invalid JSON logged the file path with `can't be read: invalid JSON …`, then `Save refused: state file unreadable` and a disconnect. The file stayed unchanged, and the next manual save's sidecar has `unknownBinding: true`.
- **Delete, prune and rename.** Deleting a save in the main menu deleted its sidecar. Pruning an autosave past five deleted its sidecar (`Save state deleted with …`). Renaming is reachable from the main menu's save list: the sidecar moved to the new file name and loaded with the same state.
- Every login logged a `connection rejected (400 Bad Request)` on the server just before the successful connection, as in earlier rounds. It didn't affect the login.
