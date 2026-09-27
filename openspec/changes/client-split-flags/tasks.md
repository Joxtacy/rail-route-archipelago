# Tasks

## 1. Game-independent logic with unit tests

- [ ] 1.1 Add `client/src/Core/LocationNames.cs` (D6): a static read-only `Id → location name` dictionary with the 50 rows from `apworld/README.md` (both `custom_contracts` and `custom_contracts_alt` → "Custom Contracts"), plus `TryGet(id, out name)`. Verify the mod builds with `cd client && dotnet build -c Release -p:DeployMod=false` (0 warnings).
- [ ] 1.2 Add the README sync test (D6):
  - Copy `../apworld/README.md` into the `client.Tests` output with `CopyToOutputDirectory`.
  - Parse the `## Location names` table.
  - Assert both directions: every README ID is in `LocationNames` with the same name, and every `LocationNames` ID is in the README. Failure messages name the ID and both names.
  - A second test asserts that the parse found exactly 50 rows.
  - Add lookup tests: `track_speed1` → "Basic Tracks", both Custom Contracts IDs, and an unknown ID returns false.

  Verify that `dotnet test --project client.Tests` passes. Then temporarily rename one README location and confirm that the test fails naming that ID, and revert the rename.
- [ ] 1.3 Add `client/src/Core/ReceivedItems.cs` (D3): counts keyed by item-key string, with `Receive(key)`, `Count(key)` (0 for unknown keys) and `Clear()`. Add `ReceivedItemsTests`:
  - an unknown key → 0
  - repeated receives accumulate (progressive copies)
  - keys are independent
  - `Clear` resets everything

  Verify that `dotnet test --project client.Tests` passes.
## 2. Split mode and effect-query patches

- [ ] 2.1 Add `SplitFlags` (D2) under `client/src/Interception/`. `Active` = intercept on, `PatchManager.Applied`, `Ctx.Deps.CurrentMode == GameMode.Play`, and the current level's `ScoringModel == Economy`, all null-safe and allocation-free. Verify the build.
- [ ] 2.2 Add the effect state (D3): a `ReceivedItems` instance, a game-grant ID set, and the precomputed `Research` → item-key map (`CustomContractsAlt` → `CustomContracts`).
  - Provide the three answers: `HasResearched(r)`, `ResearchedValue(r)` (the n-th chain item's `Value` or the highest granted `Value`, else `InitialValue[r]` read once via `AccessTools`) and `IsAutomationEnabled`.
  - Clamp the n-th level to the chain length.

  Verify the build.
- [ ] 2.3 Add Harmony patches under `client/src/Patches/` (D1):
  - prefixes on `ResearchController.HasResearched(Research)` and `ResearchedValue(Research)` that return the effect-state answer and skip the original when `SplitFlags.Active`, and pass through otherwise
  - a postfix on the `IsAutomationEnabled` getter
  - a postfix on `ResearchController.Reset()` that clears the effect state and logs `Effect state reset` (D7)

  Verify the build and that `PatchManager` would list each target. The class-by-class loop picks them up automatically, so check that each has a `[HarmonyPatch]` target that resolves; `Player.log` confirmation happens in group 4.
- [ ] 2.4 Add the game-grant observer (D3): subscribe to `EventManager.ResearchCompleted` once in `OnEnable`, and unsubscribe in `OnDisable`. Record the item `Id` as a game grant unless the mod's own re-entrancy guard is set, and log `Game grant: <id>`. Verify the build.
- [ ] 2.5 Add a "Split flags" section to `FINDINGS.md` with the game facts behind D1/D3/D7:
  - the effect-query members and their call-site counts
  - direct `.Researched` readers are slot/UI/tutorial/save only
  - the `ResearchCompleted` subscribers only re-query
  - `ResearchController.Reset()` callers
  - the list of duplicated game logic to re-check after game updates: the `UnlockUpgradeCommand.Run` cost deduction and the `CompleteResearch` side-effect switch

  Verify the file renders and the section names the 3.0.18 methods.

## 3. Slot purchase, item receive and debug key

- [ ] 3.1 Replace `PendingCheckHandler` with `SlotPurchaseHandler` (D4):
  - In split mode: deduct costs, set `item.Researched = true`, look up `LocationNames`, log `Check recorded: <id> → <location name> (...)` (or a warning when the name is missing), show "Check sent: <location name>", and call `TriggerResearchCompleted(item)` under the guard. Return handled.
  - Outside split mode: return not handled.
  - Remove `IsBlocked` from `IUpgradePurchaseHandler` and from `LogOnlyHandler`, and delete `CanResearchPatch`, `client/src/Core/PendingChecks.cs` and `client.Tests/PendingChecksTests.cs`. Update `UpgradeInterception.InterceptEnabled` to the new handler type.

  Verify the build and that `dotnet test --project client.Tests` passes.
- [ ] 3.2 Add `UpgradeReceiver.Receive(ResearchItem)` (D5):
  1. Resolve the effect item: the item itself, or the next chain level, clamped.
  2. Increment `ReceivedItems`.
  3. Run the mirrored `CompleteResearch` side-effect switch under the same scoring-model condition.
  4. Show `UnlockPopup` under the game's conditions.
  5. Call `TriggerResearchCompleted(effectItem)` under the guard.
  6. Log `Item received: <effect id> (<title>), count <n>`.

  It never writes `Researched`. Remove `GrantMostRecentPending`. Verify the build.
- [ ] 3.3 Replace `DebugGrantKey` with `DebugReceiveKey` (D8): in split mode only, F9 receives the upgrade selected in `SystemUpgradeContextPanelView`, and logs `Debug receive: no upgrade selected` when there is none. Keep the one-time install in `ArchipelagoMod.OnEnable`. Verify the build with deploy (`cd client && dotnet build -c Release`) and check that the deployed DLL is updated in `mods/RailRouteArchipelago/`.
- [ ] 3.4 Update the docs:
  - `client/README.md`: the intercept-mode description (Endless only, slot bought vs item received, bought slots persist with the save, received items last for the level) and the F9 row (receive the selected upgrade's item)
  - `CLAUDE.md` "Game and mod facts": one line saying that in intercept mode `Researched` means "slot bought" and effects come from the patched `HasResearched`/`ResearchedValue`

  Verify that the README's documented build and test commands run as written.

## 4. In-game test round (one batch, macOS via Steam)

- [ ] 4.1 Run the test round. The user clicks through each step, and every step is then confirmed against `Player.log`. Settings: `interceptUpgradePurchases: true`. Start a fresh Haarlem Endless game and check that the log shows `Patched … OK` for every patch, including `HasResearched`, `ResearchedValue`, `get_IsAutomationEnabled` and `Reset`.
  1. **Buy without receiving.** Buy Autoblocks. Points are spent, the slot shows Installed, the notification says "Check sent: Autoblocks", and autoblocks stay locked in the build menu. Log: `Check recorded: autoblock → Autoblocks`.
  2. **Repeat purchase.** The Autoblocks slot has no Install button, and no second check appears in the log.
  3. **Receive after buying.** Select Autoblocks and press F9. Autoblocks become buildable. Log: `Item received: autoblock`, and no check line.
  4. **Receive without buying.** Select Auto-accept Trains (not bought) and press F9. Existing stations switch to auto-accept, and the slot is still purchasable. Then buy it: the check is recorded and auto-accept stays on.
  5. **Parent gating uses slots.** With Automatic Routing neither bought nor received, receive Automatic Routing via F9: Perpetual Circuit stays unbuyable. Then buy the Automatic Routing slot without it having been received: Perpetual Circuit becomes buyable.
  6. **Progressive.** Once Green tier 2 is reached (or on a save that has it), buy Basic Tracks: 80 km/h stays unavailable. Select Corridor Tracks and press F9 once: 80 km/h becomes available, and 120 and 200 don't.
  7. **Child without parent's item** (M3 roadmap question). Receive Departure Sensor via F9 without the Automatic Routing item, and record whether it can be placed and whether it works. Do the same for Shunting Sensor without Shunting Commands.
  8. **Save/reload.** Save, return to the main menu and load the save. Log: `Effect state reset`. Bought slots still show Installed, autoblocks are locked again (not received this session), and placed objects survive the load. Record whether they do.
  9. **Leak check.** Receive something, then load a different save. The log shows the reset and the effect isn't active.
  10. **Outside Endless.** Play the Arrival Sensor mini-tutorial. The upgrade unlocks normally, and there's no check line and no "Check sent" notification. F9 does nothing.
  11. **Intercept off.** Remove the settings file and buy Autoblocks on a fresh Endless map. It unlocks and spends 3 green points, as in the unmodded game.
  12. **No unexpected game grants.** In steps 1–9 the log has no `Game grant:` line on Haarlem.

  Record the results, including those for steps 7 and 8, in `FINDINGS.md`.
- [ ] 4.2 Update `ROADMAP.md`: add `client-split-flags` to the M3 row's changes, mark the split-flags and location-table notes as done, and move the child-upgrade answer from step 7 into the M3 test-round list. Verify that `openspec validate client-split-flags --strict` passes.
