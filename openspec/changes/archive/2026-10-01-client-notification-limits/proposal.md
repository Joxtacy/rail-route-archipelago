# Proposal

## Why

Every side notification the mod shows stays on screen until the player clicks its X. The game has no limit and no expiry for side notifications. The routine messages pile up quickly: each purchase in the upgrades menu adds "Check sent: …" and "Received … from …", and every item that arrives while the menu is open adds another. A long session buries the side of the screen, and the few messages that matter (a seed mismatch, a lost connection) get lost among them.

## What Changes

- **Routine messages expire.** A side notification that isn't sticky clears itself a set number of seconds after it appears. The timer counts real time, so it keeps running while the game is paused.
- **Warnings and the goal are sticky.** These stay until the player dismisses them:
  - seed mismatch ("This save belongs to another Archipelago seed – disconnected")
  - level mismatch ("This level doesn't match the Archipelago seed")
  - "running degraded"
  - connection failed
  - disconnected
  - "Archipelago goal complete"
- **A limit on the mod's notifications.** When a new notification takes the count above the limit, the oldest expiring ones are removed until it fits again. Sticky ones are never removed this way, so a stack of warnings can go over the limit.
- **Both are configurable** in `RailRouteArchipelago.settings.json`:
  - `notificationLimit`, default 5. `0` means no limit.
  - `notificationSeconds`, default 10. `0` means no expiry, the current behavior.
  - A missing, null or negative value means the default. Both values appear in the settings log line.
- The "running degraded" notification in `ArchipelagoMod` goes through the mod's notification helper like every other message, so the same rules apply to it.

## Capabilities

### New Capabilities
- `client/notifications`: how long the mod's side notifications stay on screen, which ones are sticky, the limit on how many are shown, and the two settings that control them.

### Modified Capabilities
None. The existing specs only require that a notification appears. They say nothing about how long it stays, so their requirements don't change.

## Impact

- **Code**:
  - new `client/src/Core/NotificationBudget.cs`: the pure limit and eviction logic, with no Unity types
  - `client/src/Notify.cs`: a sticky flag, expiry through the game's own `Notification.Dismiss`, and the limit
  - `client/src/Core/ModSettings.cs`: the two settings and their defaults
  - the call sites of the sticky messages: `ApSession`, `SlotDataCheck`, `GoalWatcher` and `ArchipelagoMod`
- **Tests**: `client.Tests/NotificationBudgetTests.cs`, plus new cases in `ModSettingsTests`.
- **Docs**: `client/README.md` (settings table, which notifications are sticky), `FINDINGS.md` (how the game's notification system works, and the test round), `ROADMAP.md`.
- **Out of scope**:
  - combining a burst of "Received …" messages into one line
  - the game's own central notifications and tasks
  - an in-game settings screen
