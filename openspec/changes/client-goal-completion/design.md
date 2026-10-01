# Design

## Context

See proposal.md for motivation. The requirements are in `specs/client/goal-completion`.

**Game facts** (decompiled Rail Route 3.0.18, read 2026-09-27):
- `ResearchController.ThroughputRewards` holds four rewards on every Endless level, confirmed at runtime by the `LevelDiagnostics` line in Player.log (`Green 8 → 3 Red points, Green 20 → Green star, Red 20 → Red star, Green 60 →`). The endless-complete reward is the one with `RequiredThroughputType == Green`, no `RewardedStarType` and no `RewardedPointsAmount`. `GetCurrentStars`, `VictoryScreenEndless` and `CycleReport` look it up the same way.
- `ResearchController.OnExperiencePointsAwarded` (subscribed to `ExperiencePointsAwarded`) grants rewards. For the endless-complete reward it compares against `Wallet.ScoreReachedMax`, the best cycle's `PrimaryPoints + SecondaryPoints`. Every other Green reward uses `PrimaryPointsPerCycleReachedMax`. On a grant it sets `reward.Granted = true` first, then calls `StorageController.SubmitStar`, unlocks an achievement, force-autosaves, shows the star info panel, and last calls `EventManager.TriggerStarAwarded(LevelDefinition)`. That runs on the main thread inside the XP award, and the event doesn't say which star.
- `Granted` is saved with the level (`SavedResearchController.throughputRewards`) and restored on load without raising `StarAwarded`.
- `SubmitStar` also records the star in `ProgressStore`/`PlayerSettings`, for the whole profile per map (the `g`/`r`/`c` unlock order). The client ignores that record: it predates any seed.
- Endless levels never raise the `LevelCompleted` string event.

**Client facts:**
- `ArchipelagoPump` makes one `ApSession` per loaded Endless level in split mode and ends it when the level is left. There is no reconnect inside a level, so a new login only happens after a level load.
- `ApSession.OnLoginSucceeded` runs on the main thread, sets `ChecksBlocked` from `SlotDataCheck.Run`, then calls `CheckSender.ResendAll`. `ApSession.SendPacket` sends asynchronously and logs a failure through `Post`.
- `GameGrantObserver` shows how to subscribe to an `EventManager` event and re-attach when a context brings another `EventManager`. `DebugReceiveKey` shows a `DontDestroyOnLoad` key poller on `Keyboard.current`.
- The game's input actions bind F2, F3 and F9. F10 is free.
- MultiClient.Net 6.7.1 has `StatusUpdatePacket` with `ArchipelagoClientState.ClientGoal`, and `ArchipelagoSession.SetGoalAchieved()`, which is documented as irreversible.

## Goals / Non-Goals

**Goals:**
- The goal is never reported because of state that could predate this seed.
- The decisions (send now, keep pending, refuse and why) are pure logic in `client/src/Core` with unit tests, and the game-facing code stays thin.
- The goal is sent at most once per game run.

**Non-Goals:**
- Persisting a pending goal across game runs. That's the seed-binding follow-up.
- Reconnecting inside a level to flush a pending goal. The player reloads the save, as they do for checks today.
- Reading goal progress (score towards 60) for display.

## Decisions

### D1: Detect the goal by `StarAwarded`, then read the reward's `Granted` flag
- `GoalWatcher` subscribes to `EventManager.StarAwarded`, attached and re-attached the same way as `GameGrantObserver`. It ignores the event unless `SplitFlags.Active`.
- The event doesn't name the star, so the handler looks up the endless-complete reward (D2) and checks `Granted`. Green and red stars leave it false.
- The watcher records a baseline per level: `ArchipelagoPump.StartLevel` (the level is `Loaded`, so a save's rewards are restored) calls `GoalWatcher.OnLevelLoaded(level)`, which stores whether the reward is already `Granted`. That runs whether or not a server is configured. A `StarAwarded` only counts as "reached live" when the flag moved from false to true within the current level, so a second star in a level that already had the endless-complete star from its save (for example the red star awarded later) doesn't count.
- *Alternative: a Harmony postfix on `OnExperiencePointsAwarded`.* It sees the same information later than the event and adds a patch target. Rejected.
- *Alternative: read the profile-wide `ProgressStore` star.* It includes stars from before the seed. Rejected.
- *Alternative: poll `GetCurrentStars()` each frame.* It can't tell the endless-complete star from the others without the reward lookup anyway, and polling is noisier. Rejected.

### D2: One lookup for the endless-complete reward
- `GoalWatcher.EndlessCompleteGranted(IResearchController)` returns `FirstOrDefault(r => r.RequiredThroughputType == Green && !r.RewardedStarType.HasValue && !r.RewardedPointsAmount.HasValue)?.Granted ?? false`. That's the game's own predicate. `FirstOrDefault` rather than `First`, so a level without the reward means "not granted" instead of an exception.

### D3: `GoalState` in Core decides; the game code executes
`client/src/Core/GoalState.cs` is a small state machine for the game run, with no game types:

```
            ReachedLive(map)                  TrySend ok
 (none) -----------------------> Pending(map) ------------> Sent
    |                               ^   |
    |                   send failed |   | login on another map: stays Pending
    |                               +---+
    |  Manual(map) + conditions ok
    +-------------------------------------------------------> Sent
```

- Inputs are plain values: map UUID, `connected`, `mapMatches` (= `!ChecksBlocked`), `goalSupported` (slot data `goal == "endless_complete"`), `levelGranted`.
- It returns a decision: `Send`, or `Refuse(reason)`, where the reason is one of: not connected, map mismatch, unsupported goal, level not granted, already sent. The caller logs the reason text, and the tests assert it.
- Entry points: `OnReachedLive(...)` (from D1), `OnLogin(...)` (from `ApSession`, sends only if pending for this map), `OnManual(...)` (from D5), plus `MarkSent()` and `MarkFailed()`.
- Keyed by the map UUID, not the level object, so a pending goal survives the save reload that brings the next login. A pending goal for another map is left as it is.
- It's a static for the game run, like `EffectState`, and is never persisted.
- *Alternative: clear the pending goal when the level is left.* That would lose the only reconnect path we have (reload the save), so it's rejected.

### D4: Send `StatusUpdatePacket { Status = ClientGoal }` through `ApSession.SendPacket`
- It uses the existing asynchronous send, so the main thread never blocks on the socket. `SetGoalAchieved()` would build the same packet but send it synchronously.
- `GoalState.MarkSent()` is called when the packet is handed to the socket. The failure continuation posts `MarkFailed()` to the main thread, which moves the state back to `Pending` and logs `Sending the goal failed: … It's sent after the next login on this map.`
- Log `Goal sent: Endless complete on <map>` (or `… (pending since offline)` / `… (manual)`), and notify "Archipelago goal complete".
- `ApSession` reads `goal` from the slot data next to `SlotDataCheck.Run` and exposes `GoalSupported`. An unsupported or missing value is logged once at login.

### D5: Shift+F10 as a deliberate manual send
- `GoalSendKey` is a `DontDestroyOnLoad` poller like `DebugReceiveKey`, installed in `OnEnable` when patching succeeded. It fires on `f10Key.wasPressedThisFrame` while either shift key is held, only in split mode.
- It calls `GoalState.OnManual` with the reward's live `Granted` value. It bypasses only the "reached live" guard, never the connection, map, goal-type or granted checks.
- Shift is required because the goal can't be undone. The game doesn't bind F10.
- When a save with the star connects and nothing was reached live, the login path logs `Endless-complete star found in the save; the goal isn't sent automatically. Press Shift+F10 to send it.` and shows a notification with the same hint.

### D6: A debug threshold so the test round can reach the star
- Reaching a combined score of 60 in one cycle takes hours of play, which doesn't fit a test round. An optional settings key `debugEndlessCompleteThreshold` (a positive integer, absent by default) makes `GoalWatcher.OnLevelLoaded` call the game's own `ResearchController.ModifyThroughputStarReward(int)` on the endless-complete reward. It does that only when the reward isn't `Granted`, because the method replaces the reward with a new, ungranted one.
- The lowered threshold is saved with the level, so the README marks the key as test-only, for throwaway saves. Loading logs `Debug: endless-complete threshold set to <n>`.
- This isn't a spec requirement: it's a test aid, and players never need it.
- *Alternative: grant the reward from the mod.* That would bypass the game's own grant path (`SubmitStar`, the star panel, `StarAwarded`), which is exactly what the test round has to exercise. Rejected.

## Risks / Trade-offs

- [A star earned offline is lost when the game quits] → Shift+F10 recovers it, and the login hint tells the player. Seed binding removes the need later.
- [A player presses Shift+F10 on an old save and completes the seed by mistake] → It needs a held modifier, a matching map and a granted reward. It's documented as "sends the goal and can't be undone". Accepted until seed binding.
- [`StarAwarded` also fires for green and red stars] → D1 reads `Granted` and the false-to-true transition, not the event alone.
- [A future game version renames or reshapes `ThroughputRewards`] → The lookup uses `FirstOrDefault` and treats "missing" as not granted. `LevelDiagnostics` already logs the rewards on every level, so a change shows up in Player.log. The patch-target recheck note in CLAUDE.md covers it.
- [The APWorld's goal logic counts green throughput items only, while the game counts green and red] → Red points only make the goal easier, so the logic stays beatable. The goal count isn't retuned here.
- [A contest-mode level (`ContestParticipation`) keys stars differently] → The client reads the level's own reward, not the profile store, so the discriminator doesn't matter.

## Migration Plan

None. The client gains behavior and the settings file doesn't change. Rolling back means removing the mod version; a goal already sent stays sent on the server.
