# Tasks

## 1. Goal decision logic with unit tests

- [x] 1.1 Add `client/src/Core/GoalState.cs` (D3): the state for the game run (none, pending for a map UUID, sent) and a decision result (`Send`, or `Refuse` with a reason: not connected, map mismatch, unsupported goal, level not granted, already sent), each reason with its log text. Entry points:
  - `OnReachedLive(map, connected, mapMatches, goalSupported)`: returns `Send` when every condition holds, and otherwise records pending for `map` and refuses.
  - `OnLogin(map, mapMatches, goalSupported, levelGranted)`: returns `Send` only when a goal is pending for this map and every condition holds, and leaves another map's pending goal alone.
  - `OnManual(map, connected, mapMatches, goalSupported, levelGranted)`: ignores "reached live" but applies every other check.
  - `MarkSent()` and `MarkFailed(map)`, where failed goes back to pending.
  - `IsSupportedGoal(string)`, true only for `endless_complete`.
  - `Reset()` for tests.

  Add `client.Tests/GoalStateTests.cs`: live while connected → send; live while offline → pending, then login on the same map with granted → send; login on another map → refuse and still pending; login with the level not granted → refuse; sent → a second live, login or manual refuses "already sent"; failed → pending again; manual needs connected, a matching map, a supported goal and granted, each refused with its own reason; `IsSupportedGoal` for `endless_complete`, null, empty and another value. Verify that `dotnet test --project client.Tests` passes.

## 2. Detecting and sending the goal

- [x] 2.1 Add `client/src/Interception/GoalWatcher.cs` (D1, D2):
  - `EndlessCompleteGranted(IResearchController)`, using the game's predicate with `FirstOrDefault`.
  - `OnLevelLoaded(level)`, which records the level's baseline `Granted`.
  - `Attach`/`Detach` on `EventManager.StarAwarded`, re-attaching like `GameGrantObserver`.
  - The `StarAwarded` handler: ignore unless `SplitFlags.Active`; count as reached live only when `Granted` went from the baseline false to true; update the baseline; then call `GoalState.OnReachedLive` with the current session's state and act on the decision.

  Wire it in `ArchipelagoMod` next to `GameGrantObserver` (`OnEnable`, `OnContextChanged`, `OnDisable`). Call `OnLevelLoaded` from `ArchipelagoPump.StartLevel` before the settings check, so it also runs offline. Verify with `cd client && dotnet build -c Release -p:DeployMod=false` (0 warnings).
- [x] 2.2 Send the goal (D4): add `GoalWatcher.Send(session, how)`, which sends `StatusUpdatePacket { Status = ArchipelagoClientState.ClientGoal }` through `ApSession.SendPacket`, calls `GoalState.MarkSent()`, logs `Goal sent: Endless complete on <map>` plus ` (pending since offline)` or ` (manual)`, and notifies "Archipelago goal complete". The failure continuation posts `GoalState.MarkFailed(map)` and logs `Sending the goal failed: … It's sent after the next login on this map.` Log every refusal from D3 as `Goal reached, not sent: <reason>`. Verify the build.
- [x] 2.3 Login path (D4, D5): in `ApSession.OnLoginSucceeded`, after `SlotDataCheck.Run` and `CheckSender.ResendAll`:
  - Read the slot data's `goal` into `GoalSupported`, and log `Slot goal <value> isn't supported; the goal is never sent` once when it's unsupported or missing.
  - Call `GoalState.OnLogin` with the level's live `Granted` value and send on `Send`.
  - When the level is `Granted`, nothing is pending, and nothing was sent in this game run, log `Endless-complete star found in the save; the goal isn't sent automatically. Press Shift+F10 to send it.` and show that as a side notification.

  Verify the build.
- [x] 2.4 Add `client/src/Interception/GoalSendKey.cs` (D5): a `DontDestroyOnLoad` poller like `DebugReceiveKey`, installed in `OnEnable` when patching succeeded. On `f10Key.wasPressedThisFrame` with `leftShiftKey` or `rightShiftKey` held, in split mode only, it calls `GoalState.OnManual` with the current session's state and the level's `Granted` value, then sends or logs `Goal not sent manually: <reason>`. Plain F10 does nothing. Verify the build.
- [x] 2.5 Update `client/README.md`: a "Goal" section (what the Endless-complete star is: a combined green and red score of 60 in one cycle; when the goal is sent; why a star from a save isn't sent automatically; pending until the game quits), the new notifications in the notifications table, and a Shift+F10 row in the keys table that warns the goal can't be undone. Verify that the README's commands still run as written.

## 3. Test aid for the star

- [x] 3.1 Add `debugEndlessCompleteThreshold` (a nullable int) to `ModSettings` (D6). It's included in `ToString` only when set. `GoalWatcher.OnLevelLoaded` calls `ResearchController.ModifyThroughputStarReward(n)` when it's set, positive, and the reward isn't `Granted`, and logs `Debug: endless-complete threshold set to <n>`. Add `ModSettingsTests` cases: absent → null; a value loads; `ToString` mentions it only when set. Document the key in the README settings table as test-only, noting that the threshold is saved with the level. Verify the build and that `dotnet test --project client.Tests` passes.

## 4. In-game test round (one batch, macOS via Steam)

- [x] 4.1 Prepare a Haarlem seed and a local server as in `client-ap-connection` task 5.1 (a scratch `Players/player.yaml`, `Generate.py`, `MultiServer.py` on port 38281). Settings: `{"interceptUpgradePurchases": true, "server": "localhost:38281", "slot": "Player", "debugEndlessCompleteThreshold": 3}`. Deploy with `cd client && dotnet build -c Release`. Verify that the server console shows the room hosted.
- [x] 4.2 Run the test round. The user clicks through each step, and every step is then confirmed against `Player.log` and the server console:
  1. **Live goal.** Start a fresh Haarlem Endless game. The log shows `Debug: endless-complete threshold set to 3`. Play until the first cycle reaches a score of 3: the game shows its star panel, the log shows `Goal sent: Endless complete on Haarlem`, the notification appears, and the server console shows the player's goal completed.
  2. **Green star doesn't count.** In a fresh game with the threshold setting removed, reach 20 green points in a cycle (or skip this step if that takes too long, and record that it was skipped): no goal line in the log.
  3. **Save with the star.** Generate and host a new Haarlem seed, so the server hasn't seen the goal. Load step 1's save. The log shows the "found in the save" line and its notification, and the server shows no goal. Press F10: nothing happens. Press Shift+F10: `Goal sent: … (manual)`, and the server shows the goal.
  4. **Offline, then reconnect.** Generate and host another new Haarlem seed, then stop the server. Start a fresh Haarlem game, reach the star: `Goal reached, not sent: not connected`. Start the server, reload the save in the same game run: `Goal sent: … (pending since offline)`.
  5. **Map mismatch.** With the Haarlem seed, start a fresh Prague game and reach the star: `Goal reached, not sent: the level's map doesn't match the seed`, and no goal on the server. Shift+F10 refuses with the same reason.
  6. **Manual send refusals.** On a Haarlem level without the star, Shift+F10 logs "hasn't earned the Endless-complete star". With the server stopped, it logs "not connected".

  Record the results, the reward mechanics from design.md's Context, and the correction (the endless-complete star is a combined green + red score of 60 per cycle, `ScoreReachedMax`) in `FINDINGS.md`. Fix the existing "Endless-complete is a Green throughput of 60" line.

## 5. Roadmap and wrap-up

- [x] 5.1 Update `ROADMAP.md`: add `client-goal-completion` to the M5 row's changes (M5 stays ⬜), mark "Goal completion reported to the server" done in the M5 notes, remove "goal completion (M5)" from the M3 follow-ups and add follow-ups: seed binding would allow resending a goal from a save automatically; retire the Shift+F10 fallback once it does. Verify that `openspec validate client-goal-completion --strict` passes.
