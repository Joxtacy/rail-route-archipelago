# Proposal

## Why

The APWorld's only goal is `endless_complete`, but the client never tells the server when the player reaches it, so no seed can be finished. The game already decides "Endless complete" itself: the third Endless star, granted once one cycle's combined green and red points (`Wallet.ScoreReachedMax`) reach 60. The client only has to notice that star and report it. The report can't be undone, so it has to be safe against saves that already had the star before this seed.

## What Changes

- **Detect the goal live.** In split mode on an Endless level, when the game raises `StarAwarded` and the level's endless-complete throughput reward (Green, no star type, no points) has just become `Granted`, the goal counts as reached in this game run.
- **Report it.** When connected, the slot data's `goal` is `endless_complete` and the map matches the seed (`ChecksBlocked` is false), send the Archipelago goal status (`StatusUpdate` with `ClientGoal`). Log it and show an "Archipelago goal complete" notification.
- **Live-event guard.** A star that is only found in a loaded save (`Granted` already true, no `StarAwarded` seen in this game run) is **not** reported automatically. This protects a seed from a pre-Archipelago save of the same map. Full seed binding of saves stays a separate follow-up.
- **Pending goal.** A goal reached while offline, or while the send failed, stays pending in memory for the rest of the game run, keyed by the map. The next successful login on the same map sends it, provided the loaded level's reward is still `Granted`. It's lost when the game quits.
- **Manual fallback.** Shift+F10 in split mode sends the goal on purpose. It only works when connected, on a matching map, with a supported goal, and when the loaded level's endless-complete reward is `Granted`. This covers a star earned offline in an earlier game run. Every refusal is logged with its reason.
- Fix FINDINGS.md: the endless-complete star is a combined green + red score of 60 per cycle, not green alone. Record the reward mechanics and the goal test round there.
- Move "goal completion reported to the server" from M5 to this change in ROADMAP.md.

## Capabilities

### New Capabilities
- `client/goal-completion`: detecting the Endless-complete star live, reporting the Archipelago goal (gated by connection, map and goal type), the in-memory pending goal, and the manual Shift+F10 fallback.

### Modified Capabilities
None. `client/ap-connection` already requires every network effect to run on the main thread and the map mismatch to block checks. The goal follows the same rules without changing those requirements.

## Impact

- **Code**:
  - new `client/src/Core/GoalState.cs`: the pure pending/sent decision logic
  - new `client/src/Interception/GoalWatcher.cs`: subscribes to `StarAwarded`, reads the endless-complete reward and sends the status
  - new `client/src/Interception/GoalSendKey.cs`: the Shift+F10 fallback
  - `ApSession`: reads `goal` from the slot data and asks the watcher to send a pending goal after login
  - `ArchipelagoMod`: attaches the watcher and installs the key
- **Tests**: `client.Tests/GoalStateTests.cs` for the decision logic.
- **Docs**: `client/README.md` (goal reporting, Shift+F10), `FINDINGS.md` (star mechanics, correction, test round), `ROADMAP.md` (status and follow-ups).
- **Out of scope**:
  - binding saves to a seed; that's the existing M3 follow-up, and it would allow resending a goal from a save automatically
  - other goal types (N stars, all three stars)
  - any APWorld change; the goal count in the logic stays green-only, which is conservative because red points also count
