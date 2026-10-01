# Design

## Context

See proposal.md for motivation. The requirements are in `specs/client/notifications`.

**Game facts** (decompiled Rail Route 3.0.18 `Game.Hud.Notification`, read 2026-10-01):
- `NotificationController.CreateSideNotification()` creates a `Notification`, queues it in `ToDisplayInSidePosition` and adds it to an unbounded list. Nothing limits that list or expires its entries. `Update` drops entries that are `ToDestroy` or `Destroyed`.
- `NotificationSidePanel.Update` takes everything off the queue each frame and creates a `NotificationItem` for each, skipping any that is already `Destroyed`.
- `NotificationItem.Update` runs every frame. It calls `Clear()` (destroys the GameObject and sets `Destroyed`) when `Notification.Destroyed` is set. When `ToDismissWithDelay` is set, it marks `ToDestroy` and calls `Invoke("Dismiss", DismissSeconds)`. The player's X button calls `Dismiss()` → `Clear()` too.
- `Notification.Dismiss(int seconds = 2, bool playSuccessSound = true)` sets `ToDismissWithDelay`, `DismissSeconds` and `PlaySuccessSound`, and replaces `StatusText` with `DoneStatusText` when one is set. The mod sets neither status text. The game uses `Dismiss(0)` for subtitles.
- `NotificationController.Clear()` (on a level change) sets `Destroyed` on every notification.
- Nothing else in `RailRoute.dll` creates side notifications. Side notifications loaded from a save are only those the game saved, and the mod's are all `NotSaved()`.
- Nothing in `RailRoute.dll` assigns `Time.timeScale`, so `MonoBehaviour.Invoke` delays run in real seconds, paused or not.

**Client facts:**
- `Notify.Side(text)` is the mod's single helper. It's called from `ApSession` (connected, seed mismatch, connection failed, disconnected), `SlotDataCheck` (level mismatch), `GoalWatcher` (goal complete), `ItemRouter` (restored), `SlotPurchaseHandler` (check sent) and `UpgradeReceiver` (received). `ArchipelagoMod.OnLevelStarted` builds the "running degraded" notification directly.
- `UpgradeInterception.Settings` holds the `ModSettings` read once at game start. `client.Tests` compiles only `client/src/Core`.

## Goals / Non-Goals

**Goals:**
- No new Harmony patch: use the game's own `Dismiss` and `Destroyed` hooks.
- The limit and eviction rules are pure logic in `client/src/Core` with unit tests.
- Each call site says whether its message is sticky, so the sticky list is visible where the messages are written.

**Non-Goals:**
- Showing a countdown or fading notifications out.
- Pausing expiry while the pointer is over a notification.
- Applying the settings again while the game runs. They're read at startup like the rest.

## Decisions

### D1: Expiry through the game's `Notification.Dismiss`
- For a notification that isn't sticky, with `notificationSeconds > 0`, `Notify` calls `Dismiss(seconds, playSuccessSound: false)` right after creating it. The delay starts when its `NotificationItem` first updates, which is when it appears on screen. A notification queued while no side panel exists doesn't count down until it's shown.
- `Dismiss` takes an `int`, so the setting is whole seconds.
- *Alternative: a mod `MonoBehaviour` that tracks creation times and destroys entries.* It duplicates the game's timer, and it would start counting before the notification is shown. Rejected.

### D2: Eviction by setting `Notification.Destroyed`
- The limit removes a notification by setting `Destroyed = true`. Its item clears itself on the next frame through the game's own path, which also releases any focus highlight. One that is still queued is skipped by the side panel.
- *Alternative: a Harmony patch on the side panel that limits its children.* That would also touch the game's own notifications and adds a patch target to recheck after game updates. Rejected.

### D3: `NotificationBudget<T>` in Core decides what to evict
`client/src/Core/NotificationBudget.cs` keeps the mod's notifications oldest first, each with its sticky flag, and knows nothing about Unity:

```
 Add(item, sticky, limit) -> items to evict
   1. drop entries where isGone(item)        (dismissed, expired, cleared by the game)
   2. append (item, sticky)
   3. if limit > 0: while count > limit and a non-sticky entry exists,
        remove the oldest non-sticky entry and return it for eviction
```

- `isGone` is passed to the constructor. `Notify` passes `n => n.Destroyed || n.ToDestroy`, and the tests pass a fake.
- It's pruned when a notification is added rather than every frame, because the count only matters at that point.
- A newly added routine notification can be evicted straight away, but only when every other entry is sticky and the count is over the limit. The rule doesn't need a special case for that, since the oldest routine entry is then the new one.
- *Alternative: evict sticky ones too once the limit is reached.* The warnings are the messages the change is meant to keep visible. Rejected, as agreed.

### D4: `Notify.Side` and `Notify.Sticky`
- `Notify.Side(text)` keeps its signature and becomes the expiring kind. A new `Notify.Sticky(text)` is the sticky kind. Both go through one private method that creates the notification, applies D1, and evicts what D3 returns.
- Sticky call sites:
  - `ApSession`: seed mismatch, connection failed and disconnected
  - `SlotDataCheck`: level mismatch
  - `GoalWatcher`: goal complete
  - `ArchipelagoMod.OnLevelStarted`: degraded, which now goes through `Notify.Sticky` instead of the controller
- The budget is static for the game run, like `EffectState`. It holds references to `Notification` objects only until they're pruned.
- *Alternative: a `sticky` bool parameter on `Side`.* Two method names read more clearly at the call site and can't be passed the wrong way. Chosen: two methods.

### D5: Settings as nullable ints with defaults applied on read
- `ModSettings` gains `[JsonProperty("notificationSeconds")] int? NotificationSeconds` and `[JsonProperty("notificationLimit")] int? NotificationLimit`, plus `NotificationSecondsOrDefault` (10) and `NotificationLimitOrDefault` (5), which map null and negative values to the default. Constants `DefaultNotificationSeconds` and `DefaultNotificationLimit` hold the defaults.
- A string such as `"ten"` makes Newtonsoft throw, so the existing `TryLoad` path already logs a warning and uses all the defaults. A fractional number is converted by Newtonsoft (`ReadAsInt32`), so the spec doesn't promise anything about it.
- `ToString()` always adds `notifications <seconds>s, limit <n>`, with `off` for 0.

## Risks / Trade-offs

- [A future game version scales time when paused] → `Invoke` delays would stop while paused. Notifications would then just last longer, which does no harm. The `timeScale` check goes into FINDINGS.md so it's rechecked after game updates.
- [`Dismiss` with a `DoneStatusText` would change the status line] → The mod never sets one. D1 is documented next to the call.
- [The game's `Update` drops `ToDestroy` entries from its own list a few seconds before the item disappears] → The budget prunes on `ToDestroy` too, so a notification on its way out doesn't count. It's gone moments later anyway.
- [With many sticky warnings the panel goes over the limit] → That's intended (see spec). Only a broken setup produces several of them.

## Migration Plan

None. A settings file without the new keys gets the defaults, so existing players start seeing routine notifications expire after 10 seconds. `notificationSeconds: 0` and `notificationLimit: 0` restore today's behavior.
