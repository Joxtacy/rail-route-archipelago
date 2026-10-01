# Tasks

## 1. Settings

- [x] 1.1 Add `notificationSeconds` and `notificationLimit` to `ModSettings` (D5):
  - nullable int properties
  - `DefaultNotificationSeconds = 10` and `DefaultNotificationLimit = 5`
  - `NotificationSecondsOrDefault` and `NotificationLimitOrDefault`, which map null and negative values to the default and keep 0
  - `ToString()` always appends `notifications <n>s, limit <n>`, with `off` for 0

  Add `ModSettingsTests` cases:
  - both keys absent → 10 and 5
  - custom values load
  - 0 is kept and shows as `off`
  - -1 → default
  - `"ten"` → `TryLoad` returns false with the defaults
  - `ToString` contains both values

  Document both keys in the `client/README.md` settings table: their defaults, `0` turns that behavior off, and which notifications are sticky. Verify that `dotnet test --project client.Tests` passes.

## 2. Limit logic

- [x] 2.1 Add `client/src/Core/NotificationBudget.cs` (D3): a generic `NotificationBudget<T>` built with an `isGone` predicate. `Add(item, sticky, limit)` prunes gone entries, appends the item, and while `limit > 0`, the count is over the limit and a non-sticky entry exists, removes the oldest non-sticky entry and returns it. Add `Count` and `Clear()` for tests.

  Add `client.Tests/NotificationBudgetTests.cs`:
  - under the limit → nothing evicted
  - seven routine items with limit 5 → the two oldest evicted, in order
  - two sticky then five routine with limit 5 → the two oldest routine evicted and both sticky kept
  - six sticky with limit 5 → nothing evicted
  - five sticky then one routine with limit 5 → the new routine one is evicted
  - limit 0 → nothing evicted
  - gone entries are pruned and no longer count

  Verify that `dotnet test --project client.Tests` passes.

## 3. Wiring

- [x] 3.1 Rework `client/src/Notify.cs` (D1, D2, D4). `Side(text)` makes an expiring notification and the new `Sticky(text)` a sticky one. Both create it with `.Text(text).CanBeDismissed().NotSaved()`. For an expiring one with `NotificationSecondsOrDefault > 0`, call `Dismiss(seconds, playSuccessSound: false)`. Pass the notification to a static `NotificationBudget<Notification>` (`isGone = n => n.Destroyed`; `ToDestroy` is set from the first frame of a delayed dismiss, so it must not count as gone) with `NotificationLimitOrDefault`, and set `Destroyed = true` on every evicted one. Comment the `Dismiss` call: the mod sets no `DoneStatusText`. Verify with `cd client && dotnet build -c Release -p:DeployMod=false` (0 warnings).
- [x] 3.2 Switch the sticky messages to `Notify.Sticky`:
  - `ApSession`: seed mismatch, connection failed, disconnected
  - `SlotDataCheck`: level mismatch
  - `GoalWatcher`: goal complete
  - `ArchipelagoMod.OnLevelStarted`: degraded, replacing its direct `CreateSideNotification` chain

  Every other call stays `Notify.Side`. In the `client/README.md` notifications table, mark which ones stay until dismissed. Verify the build, and that `grep -rn "CreateSideNotification" client/src` only matches `Notify.cs`.

## 4. In-game test round (one batch, macOS via Steam)

- [x] 4.1 Prepare a Haarlem seed and a local server as in earlier test rounds (a scratch `Players/player.yaml`, `Generate.py`, `MultiServer.py` on port 38281). Settings: `{"interceptUpgradePurchases": true, "server": "localhost:38281", "slot": "Player"}`. Deploy with `cd client && dotnet build -c Release`. Verify that the server console shows the room hosted and that the settings log line shows `notifications 10s, limit 5`.
- [x] 4.2 Run the test round. The user clicks through each step, and every step is then confirmed against `Player.log`:
  1. **Expiry.** Load a fresh Haarlem game and connect. "Archipelago connected" and the restore line disappear by themselves after about 10 seconds.
  2. **Paused.** Pause the game, buy a slot in the upgrades menu: "Check sent" and "Received … from Player" still disappear after about 10 seconds.
  3. **Limit.** Buy four slots quickly in the upgrades menu (eight notifications): no more than five are on screen at once, and the oldest go first.
  4. **Sticky.** Stop the server: "Archipelago disconnected" stays past 10 seconds while routine ones expire, and the X dismisses it.
  5. **Settings.** Set `"notificationSeconds": 0, "notificationLimit": 3` and restart the game. The log line shows `notifications off, limit 3`. Notifications no longer expire, and buying slots keeps at most three on screen.

  Record the results and the notification facts from design.md's Context (including the `Time.timeScale` check to repeat after game updates) in a new `FINDINGS.md` section.

## 5. Roadmap

- [x] 5.1 Update `ROADMAP.md`: add `client-notification-limits` to the M5 row's changes, add a ✅ line for notification expiry and the limit in the M5 notes, and add the follow-up "combine a burst of Received notifications into one line". Verify that `openspec validate client-notification-limits --strict` passes.
