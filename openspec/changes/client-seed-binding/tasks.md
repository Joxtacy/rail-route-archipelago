# Tasks

## 1. State model and decisions with unit tests

- [ ] 1.1 Add `client/src/Core/SaveApState.cs` (D2):
  - the model: `Version`; `Binding { Seed, Team, Slot, SlotName }` or null; `UnknownBinding`; `SentChecks` as a sorted set of names; `Goal` (`None`, `Pending`, `Sent`, `Ineligible`, as lowercase JSON strings); `ReceivedIndex`
  - `Serialize()` and `TryParse(text, out state, out error)` with the game's Newtonsoft.Json
  - `Binding.Matches(other)`, which compares seed, team and slot number
  - `Describe()` for log lines

  Add `client.Tests/SaveApStateTests.cs`:
  - a round trip of every field
  - a fresh state is unbound, empty, `none` and 0
  - `TryParse` errors for invalid JSON, an unknown version and a negative index
  - an unknown goal string is an error
  - `Matches` is true for the same seed, team and slot with a different slot name, and false for another seed, team or slot

  Verify that `dotnet test --project client.Tests` passes.
- [ ] 1.2 Add `BindingDecision.Decide(state, loggedIn, mapMatches)` in `Core` (D3), returning `Bind`, `Matches`, `Refuse(reason)` or `Skip`. Rules:
  - `UnknownBinding` → refuse ("state file unreadable")
  - another binding → refuse ("bound to <seed>/<slot>")
  - unbound with a map mismatch → skip
  - unbound otherwise → bind

  Add tests for each outcome, including an unknown binding with a map mismatch, which still refuses. Verify that the tests pass.
- [ ] 1.3 Rewrite `client/src/Core/GoalState.cs` as per-save pure functions (D5): `OnBind`, `OnLogin`, `OnReachedLive`, `MarkSent` and `MarkFailed` over the `SaveApState` goal value. Drop `PendingMap`, `NotPending`, `AlreadySent`'s game-run wording and `OnManual`. Add the reasons "the star predates the binding" and "the save is bound to another seed". Rewrite `client.Tests/GoalStateTests.cs`:
  - bind with the star → ineligible; without it → unchanged
  - login: bound, granted and `none` → send; `pending` → send; `sent` → refuse; `ineligible` → refuse with the predates reason; not granted → refuse; map mismatch and an unsupported goal refuse with their reasons
  - live: connected and bound → send; offline → pending
  - pending then bind with granted → ineligible
  - failed → pending

  Verify that the tests pass.
- [ ] 1.4 Add `ItemSync.Seed(int)` (D4): `Known` for the next `Replace` is `min(seeded, count)`, and `Expected` is unchanged until then. Extend `client.Tests/ItemSyncTests.cs`:
  - seed 5, replace with 7 → `Known` 5, `Expected` 7
  - seed 9, replace with 7 → `Known` 7
  - no seed behaves as today

  Verify that the tests pass.

## 2. Sidecar storage and save hooks

- [ ] 2.1 Add `client/src/Interception/SaveStateStore.cs` (D1, D2):
  - `Current`, the loaded level's state
  - `LoadFor(level, saveFile)`: a fresh state for null; otherwise it reads `<SaveDirectory>/<FileName>.ap.json`, and an unreadable file gives `UnknownBinding` plus a "don't overwrite" name and a logged error
  - `WriteFor(fileName)`: writes `Current` through a temp file and a move; it skips the protected name and logs failures without throwing
  - `Delete(fileName)` and `Move(from, to)`

  The directory comes from `Application.persistentDataPath` plus `StorageController.SaveDirectory`. Call `LoadFor` from `ArchipelagoPump.StartLevel` with `GameController.LoadedSave` before the settings check, so it also runs offline, and log `Save state: <Describe()>` (the save name or "new game"). Do nothing outside split mode. Verify with `cd client && dotnet build -c Release -p:DeployMod=false` (0 warnings).
- [ ] 2.2 Add Harmony patches in `client/src/Patches/`, each a no-op unless `SplitFlags.Active`:
  - `StorageSavePatch`: a prefix on `StorageController.Save(string, SavedGame)` that calls `WriteFor(saveName)`
  - `StorageDeleteSavePatch`: a postfix on `DeleteSave(SaveFile)` that calls `Delete(saveFile.FileName)`
  - `StorageRenamePatch`: a postfix on `Rename(SaveFile, string)` that calls `Move(oldSave.FileName, __result.FileName)`

  Verify the build, and in the next game start that the log has a `Patched … : OK` line for each.
- [ ] 2.3 Document the sidecar in `client/README.md`: its file name and location, what it holds, that it doesn't sync through Steam Cloud, and that deleting a save's `.ap.json` unbinds it. Record the game facts from design.md's Context (save naming, the write funnel, delete and rename, the Steam Cloud pattern) in `FINDINGS.md` under a new "Save files" section. Verify that the README's paths match `SaveDirectory`.

## 3. Binding on login

- [ ] 3.1 In `ApSession.OnLoginSucceeded` (D3), after `SlotDataCheck.Run` and `ReadGoal`, read `RoomState.Seed`, `ConnectionInfo.Team`, `Slot` and the slot name, then call `BindingDecision.Decide`:
  - **Refuse:** log `Save refused: <reason> (this save: <Describe()>; server: <seed>/<team>/<slot>)`, notify "This save belongs to another Archipelago seed – disconnected", and `Disconnect()`; nothing else runs.
  - **Bind:** set the binding, apply `GoalState.OnBind` with the live `Granted`, log `Save bound to <seed>, slot <name> (<n>)`, and when `LoadedSave` isn't null call `WriteFor(LoadedSave.FileName)` after the resend so that it holds the sent checks.
  - **Skip:** log that the save isn't bound because of the map mismatch.

  Expose `ChecksAllowed` (`!ChecksBlocked` and bound). Verify the build.
- [ ] 3.2 Hold items until the decision (D3): `ItemRouter` queues `(packet, replay)` pairs while no decision has been made. `OnLoginSucceeded` calls `Items.Begin(Current.ReceivedIndex)` on `Bind`, `Matches` or `Skip` (a map mismatch still applies items, as today), which seeds `ItemSync` and flushes the queue in order, or `Items.Drop()` on refusal. Verify the build.

## 4. Checks, items and goal on the per-save state

- [ ] 4.1 `CheckSender` (D6): gate on `ChecksAllowed`, and add every name handed to `CompleteLocationChecksAsync` (single and resend) to `Current.SentChecks`. The resend set stays every bought slot. Verify the build.
- [ ] 4.2 `UpgradeReceiver.Receive` takes a mode: `Restore` (counts and raises `ResearchCompleted`, no `ApplySideEffects`, no popup), `NewSilent` (side effects, no popup) or `Live` (today's non-silent path). F9 uses `Live`. `ItemRouter` picks:
  - `Restore` for items below `Known`
  - `NewSilent` for the rest of a replay
  - `Live` otherwise

  It raises `Current.ReceivedIndex` to `max(current, Expected)` after each packet. The replay notification becomes "Restored N Archipelago items", plus ", M new" when M > 0, and the log line names both counts. Update `client/README.md`'s notifications table. Verify the build.
- [ ] 4.3 `GoalWatcher` on the per-save state (D5):
  - The live handler calls `GoalState.OnReachedLive` with `connected && bound`, and stores the decision's state in `Current.Goal`.
  - `Send` marks `Sent`; the failure continuation marks `Pending`.
  - `OnLogin` (called after the binding decision, only on `Bind`/`Matches`/`Skip`) sends with ` (from the save)` on `Send`, and logs `Goal not sent: the star predates the binding` for an ineligible state with the star.
  - The "Press Shift+F10" hint is removed.

  Verify the build.
- [ ] 4.4 Remove `client/src/Interception/GoalSendKey.cs` and its install in `ArchipelagoMod`. In `client/README.md`, remove the Shift+F10 row and rewrite the "Goal" section: the star is sent live or from a bound save; a star from before the binding is never sent; a pending goal survives a restart through the save. Verify that the build has 0 warnings and `dotnet test --project client.Tests` passes.

## 5. In-game test round (one batch, macOS via Steam)

- [ ] 5.1 Prepare two Haarlem seeds (A and B), one with a room password, and a local server as in `client-goal-completion` task 4.1. Settings: `interceptUpgradePurchases`, `server`, `slot`, `debugEndlessCompleteThreshold: 3`. Deploy with `cd client && dotnet build -c Release`. Verify that the server console shows the room hosted.
- [ ] 5.2 Run the test round. The user clicks through each step, and every step is then confirmed against `Player.log`, the `saves/` folder and the server console:
  1. **Bind a new game (seed A, password room).**
     - Start a fresh Haarlem game. The log shows the password login succeeded, then `Save bound to …`.
     - Buy a slot and receive an item; turn auto-accept off at one station if Auto-accept Trains arrived.
     - Save manually. A `.ap.json` exists next to the new `.mp.lz4`, with the binding, the sent check and the index.
  2. **Reload, no repeated side effects (seed A).**
     - Reload that save. The log shows `Restored N …`, 0 new, and the station's auto-accept is still off.
     - Have the server send one more item (`/send`) while the save isn't loaded, then reload. It shows `…, 1 new`.
  3. **Goal live (seed A).** Reach the star: `Goal sent`, and the server shows the goal. The autosave's sidecar records `sent` after the next save.
  4. **Pending goal across a restart (seed A).**
     - From a save before the star, stop the server and reach the star: `Goal reached, not sent: not connected`.
     - Quit the game, start the server, load the newest autosave: `Goal sent: … (from the save)`.
  5. **Foreign save (seed B).** Host seed B and load a seed A save. The log shows `Save refused: bound to …` with both bindings, the notification appears, the client disconnects, no checks are sent (server console), and the sidecar is unchanged.
  6. **Old save with the star (seed B).** Load a pre-change save with the star and no sidecar: `Save bound …`, `Goal not sent: the star predates the binding`, and no goal on the server. Reload it: the star is still not sent.
  7. **Map mismatch, live purchase (seed B).** Load a Prague save and buy a slot live. The log shows the check blocked and the save isn't bound (no sidecar written at login).
  8. **Corrupt sidecar.** Put invalid JSON in a sidecar and load that save. The log names the file, the save is refused, and the file is unchanged; a new autosave's sidecar has `unknownBinding`.
  9. **Delete, prune and rename.**
     - Delete a save in the save menu: its `.ap.json` is gone.
     - Play long enough for autosaves to prune past five: the pruned ones' sidecars are gone.
     - If the menu offers renaming, rename a save: the sidecar follows. Otherwise record that `Rename` isn't reachable.

  Record the results in `FINDINGS.md` under "Seed binding test round".

## 6. Roadmap and wrap-up

- [ ] 6.1 Update `ROADMAP.md`:
  - Add `client-seed-binding` to the M3 row's changes.
  - Mark the follow-up "Save checked and received state per save file, bound to the seed" done, and the goal follow-up "Seed binding … retire the Shift+F10 fallback" done.
  - Mark the untested password-room and live map-mismatch purchase checks done (or keep them open if step 5.2 skipped them).
  - Update the M5 goal note (no Shift+F10).
  - Add the Steam Cloud limitation as a follow-up.

  Verify that `openspec validate client-seed-binding --strict` passes.
