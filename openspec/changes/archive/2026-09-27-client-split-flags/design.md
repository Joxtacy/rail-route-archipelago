# Design

## Context

See proposal.md for motivation. The requirements are in `specs/client/upgrade-purchase-interception`, `specs/client/received-items` and `specs/client/location-names`.

**Game facts** (decompiled Rail Route 3.0.18 `RailRoute.dll`, read 2026-09-27):

- **One flag per slot.** `ResearchItem.Researched` is a plain auto-property.
  - `ResearchController` indexes the items two ways:
    - `binaryResearchDict`: one item per `Research`, for items without a `Value`
    - `gradualResearchLookup`: the items that have a `Value` (track speed, station count and contract offers), ordered by `Value`
  - A private `InitialValue` dictionary holds the values that apply before any level is researched: 4 stations, `Connection.Speeds[0]` and 1 contract offer.
- **Effect queries.** Gameplay reads the unlock state through three members:
  - `HasResearched(Research)`, which reads `binaryResearchDict[r].Researched` and has 78 call sites
  - `ResearchedValue(Research)`, which returns the `Value` of the last researched item in the chain, or `InitialValue`, and has 12 call sites
  - `IsAutomationEnabled`, which is set by `CompleteResearch` and by save loading for items with `EnablesAutomation`: departure, arrival and routing sensors, and coach yard
- **Direct `.Researched` readers** outside `ResearchController` are all slot or UI concerns:
  - the upgrade panel and buttons (installed state and tier-complete indicators)
  - `UnlockUpgradeCommand.Validate`, which returns `!Researched && CanResearch`
  - tutorial tasks (`Unlock*Task`), story chapter S5P2 and the campaign's `UnlockingConstraint`
  - save serialization (`SavedResearchItem`, `LevelContent`)

  Parent gating (`GetUpgradeRequiredForUnlock`) also reads the parent's slot flag. So `Researched` can mean "slot bought" without touching any of these.
- **`CompleteResearch` side effects**, when the scoring model isn't `Score`:
  - Auto-accept or auto-reverse is turned on at every non-waypoint station.
  - The train-alert, route-preview and signalling-safety interface preferences are set.
  - Then: `TriggerResearchCompleted(item)`, the `UnlockPopup` (only in play, once the game has loaded, and not while the system upgrades menu is open), and `IsAutomationEnabled = true` for `EnablesAutomation` items.
- **`ResearchCompleted` subscribers** all refresh by re-querying: panels, the build menu's `UnlockedByResearch`, offices, station views, tutorial tasks and story triggers. None of them applies an effect from the event argument, so firing the event is a safe way to refresh the UI.
- **Save format.** Saves store each slot's `researched` bit. On load, `SavedResearchItem.Load` calls `Finish()` without firing an event, and sets `IsAutomationEnabled` for bought automation slots.
- **Level start.** `LevelController.Init(level, …)` fires `BeforeLevelStarted` before the level's research setup. Saved maps load through `SavedMap.Load`, and it hasn't been confirmed that this path also reaches `BeforeLevelStarted`. The string event `LevelStarted` fires once the level is running. `GameController`'s level clear calls `ResearchController.Reset()`, which clears every slot's `Researched` flag. The level editor's settings and the Routing Sensor and Arrival Sensor mini-tutorials call it too.
- **Endless** levels use `ScoringModel.Economy`. The game itself uses that check to decide whether the upgrades menu applies (`MenuButtonsController`, `UnlockedByResearch`). In Economy, `InitDefaults` returns early, so the game grants nothing at the start of an Endless level unless the level configuration or an "unlock all" start setting asks for it.
- **Selected upgrade.** `MenuController.SystemUpgradesMenu.SystemUpgradeContextPanelView.SystemUpgradeContextPanelModel?.ResearchItem`. The tutorial subtask `SelectUpgradeSubtask` uses the same path.
- **Custom Contracts.** Scheduler-office code checks `HasResearched(CustomContracts) || HasResearched(CustomContractsAlt)`.

**Current client** (M1): `PendingCheckHandler` deducts costs and records `Id` in an in-memory `PendingChecks`. `CanResearchPatch` hides the Install button for pending IDs, and F9 calls `CompleteResearch` for the most recent pending ID. So F9 sets the slot's `Researched` flag, which the APWorld's logic assumes never happens.

## Goals / Non-Goals

**Goals:**
- The effect state and the slot state are independent, and each has exactly one source: received items (plus game grants) for effects, and the game's own `Researched` flag for slots.
- Patches stay small and query-shaped, so the next change can feed received items from the network without touching patches.
- Everything that doesn't need the game is unit-tested: the location table and its README sync, and received-item counting.

**Non-Goals:**
- Persisting received items. Archipelago resends all received items on connect, so the next change rebuilds them from the server. Until then they last for one level session.
- Recording or resending checks beyond logging. The next change sends them. It can derive the checked locations from the bought slots, which the save already persists.
- Relabelling the panel's "Installed" status for bought slots as "Check sent".
- A client-side item-ID table. The debug key works in game terms (the selected upgrade), and AP item IDs arrive with networking.

## Decisions

### D1: `Researched` stays the game's slot flag; effects are redirected by patching the three queries
In split mode (defined in D2):
- Prefixes on `HasResearched(Research)` and `ResearchedValue(Research)` return the answer from the effect state and skip the original.
- A postfix on the `IsAutomationEnabled` getter replaces the result.

Outside split mode all three pass through.
- Because `Researched` keeps meaning "slot bought", parent gating, the panel, `Validate` and save/load keep working unchanged. This also removes `PendingChecks` and `CanResearchPatch`.
- *Alternative: keep `Researched` as the effect flag and track slots in the mod.* Every panel and button reader, `Validate`, parent gating and the save format would then need patching. That's roughly 20 UI call sites against 3 query methods.
- *Alternative: patch `ResearchItem.Researched`'s getter by caller.* That needs call-site detection on a hot, inlined auto-property, which is fragile.

### D2: Split mode = intercept on, patches applied, play mode, current level is Economy
`SplitFlags.Active` is computed on every query, so leaving the level or loading a tutorial immediately restores unmodded behavior.
- The purchase handler also checks it. Outside Endless it returns "not handled", and the game's own `Run` unlocks normally. This is the new "Interception applies only on Endless levels" requirement: tutorials and the story call `CompleteResearch` and read `Researched`, and they would break under split semantics.
- *Alternative: split everywhere in intercept mode.* The story's `UnlockingConstraint` and the tutorial tasks would stall.

### D3: The effect state = received counts ∪ game grants
- **Received counts.** A pure `ReceivedItems` class in `Core/` holds counts keyed by an item key string. The key is `Research.ToString()`, except that `CustomContractsAlt` is normalized to `CustomContracts` so one item covers both variants. It supports `Receive`, `Count` and `Clear`, and has unit tests.
- **Game grants.** The mod subscribes to `EventManager.ResearchCompleted`. Any event the mod didn't raise itself (a re-entrancy guard flag is set around our own `TriggerResearchCompleted` calls) is a game grant: level configuration, "unlock all", or a tutorial button inside the level. The granted item's `Id` goes into a game-grant set. This observes `CompleteResearch` through its event rather than patching it, so the rule "never intercept `CompleteResearch`" still holds.
- `HasResearched(r)` = `Count(key(r)) > 0`, or the binary item for `r` (or, for the Custom Contracts pair, either variant) is in the game-grant set.
- `ResearchedValue(r)` takes the higher of two levels:
  - the `Value` of the n-th item in the chain ordered by `Value`, where n = `min(Count, chain length)`
  - the highest `Value` among the chain's granted IDs

  If neither applies, it returns the game's `InitialValue[r]`, read once through `AccessTools`.
- `IsAutomationEnabled` = any received or granted item with `EnablesAutomation`.
- Both sets are cleared at level start (see D7).
- *Alternative: treat game grants as received items.* That's equivalent for effects, but it would muddle the log and the future sync with the server's received list.

### D4: Purchase = spend, set the slot flag, log the check, fire `ResearchCompleted` for refresh
`SlotPurchaseHandler` replaces `PendingCheckHandler`. In split mode it:
1. deducts the three costs (mirroring `UnlockUpgradeCommand.Run`, as M1 already does)
2. sets `item.Researched = true`
3. looks up the location name (D6)
4. logs `Check recorded: <id> → <location name> (<colour>, tier, cost)`
5. shows the notification "Check sent: <location name>"
6. calls `TriggerResearchCompleted(item)` under the guard flag, so the panel, buttons, tier indicators and child slots refresh exactly as after a normal purchase

It returns "handled", so `Run`'s own `CompleteResearch` is skipped. If the ID has no location name, it still buys the slot and logs a warning.
- *Alternative: refresh with `TriggerStartingResearchStateChanged`.* `SystemUpgradeButton` doesn't listen to it, so the slot button wouldn't update.
- *Trade-off:* `BottomBarController` adds the item to its "recently unlocked" build-menu highlight even though nothing became buildable. This is cosmetic and accepted.

### D5: Receive = count, mirror `CompleteResearch`'s side effects, popup, event
`UpgradeReceiver.Receive(ResearchItem selected)` resolves the effect item:
- For a binary item, it's that item.
- For a chain, it's the item at the new level: the (n+1)-th by `Value`, clamped to the last.

Then it:
1. increments `ReceivedItems`
2. runs the same `switch` as `CompleteResearch` (auto-accept, auto-reverse, train alerts, route preview, signalling safety), under the same scoring-model condition
3. shows `UnlockPopup` with the effect item's title under the game's conditions
4. calls `TriggerResearchCompleted(effectItem)` under the guard, which refreshes the build menu and panels

It never writes `Researched`.
- This is the seam the networking change calls with items resolved from AP item IDs.
- *Alternative: call `CompleteResearch` and then reset `Researched`.* `CompleteResearch` picks the first slot that isn't researched, so it does nothing once the slot is bought. It would also briefly mark the slot as bought and fire the grant observer.
- *Trade-off:* the side-effect switch is duplicated from game code, like the cost deduction. It's listed in FINDINGS.md to re-check after game updates.

### D6: The location table is a C# dictionary in `Core/`, checked against `apworld/README.md` by a test
- `LocationNames` is a static read-only `Dictionary<string, string>` with 50 rows copied from the README, and a `TryGet(id, out name)` method.
- `client.Tests` copies `../apworld/README.md` into its output with a `None` item set to `CopyToOutputDirectory`. The test parses the rows of the `## Location names` table (lines starting with `` | ` ``: first cell = backticked ID, second cell = name).
- The test compares both directions and reports each missing or differing ID with both names. A companion test checks that the parse found 50 rows, so a README reformat can't make the sync test pass vacuously.
- *Alternative: generate the C# table from `data.py` at build time.* That adds a Python step to the .NET build for 50 stable rows.
- *Alternative: embed the README as a resource and parse it at runtime.* That ships docs inside the mod, and the runtime wouldn't gain anything.

### D7: Level-session lifetime, reset with the game's own research reset
- `ReceivedItems` and the game-grant set are cleared in a postfix on `ResearchController.Reset()`. The game calls it from `GameController`'s level clear, the path every level unload and load goes through, and from the level editor's settings and two mini-tutorial chapters. That's the moment the game itself wipes slot state, so effect state and slot state always reset together. Grants made during the new level's setup are recorded after the reset.
- The grant observer subscribes once, in `OnEnable`, rather than per play context, so it never misses the first level.
- *Alternative: clear on `BeforeLevelStarted`.* Saved maps load through `SavedMap.Load`, and it isn't confirmed that this path reaches `BeforeLevelStarted`. A missed reset would leak one save's received items into another.
- *Alternative: clear on the `LevelStarted` string event.* It fires after level setup, so it would erase grants the game had just made.

### D8: F9 = receive the selected upgrade's item
`DebugGrantKey` becomes `DebugReceiveKey`. In split mode, F9 reads the selected panel item (Context) and calls `UpgradeReceiver.Receive`. With nothing selected, it logs `Debug receive: no upgrade selected`.
- This allows testing both halves independently: receive without buying, and buy without receiving.
- It also answers the M3 test-round question about child upgrades: for example, receive Departure Sensor without Automatic Routing.
- *Alternative: receive the most recently bought slot's item.* That can't test receiving an item whose slot hasn't been bought.
- *Alternative: a key per item.* That's too many bindings.

## Risks / Trade-offs

- **[`HasResearched` is on hot paths]** It has 78 call sites, some of them per frame. → The prefix does one mode check and a dictionary lookup, with no allocation. The mode check reads cached references. The item-key mapping for `Research` is precomputed once.
- **[Loading a save built with received items]** After a reload the received set is empty. Placed objects that needed an upgrade (for example 200 km/h track or autoblocks) might fail validation or get stripped during load. → This is part of the in-game test round (task 4). The networking change refills received items on connect. If load-time validation turns out to strip objects, the fix is to delay effect queries until the level has started, and that decision goes in that change.
- **[Missed reset]** If some load path skipped `ResearchController.Reset()`, received items would leak into the next level. → The test round loads a second save after F9 receives and checks that the log shows the reset and that the effect is gone.
- **[Game grants on load]** On a saved map, level-configured unlocks come back through `Finish()`, with no event, so the grant observer misses them. → None of the three AP maps has an override block (M2 findings). The test round logs any `ResearchCompleted` that isn't ours at level start, to confirm.
- **[Duplicated game logic]** The cost deduction (from M1) and the `CompleteResearch` side-effect switch are copied. → Both are listed in FINDINGS.md under "re-check after game updates".
- **[Panel says "Installed" for a bought slot whose item hasn't been received]** This is accepted for this change. The upgrade's build button stays locked, so gameplay is correct and only the label is misleading. The "Check sent" label is left for a later change.
- **[README and `data.py` can drift]** The client test only protects client ↔ README. → `data.py` is the source of truth, and its names are pinned by `test_ids.py`. A Python test comparing README ↔ `data.py` would close the chain, and it's suggested as a small follow-up in the APWorld.

## Migration Plan

This affects dev builds only. Existing saves made in intercept mode under M1 have no bought slots, since pending checks were never saved, so they load cleanly. Rollback is reverting the commit. Turning intercept mode off also restores unmodded behavior.
