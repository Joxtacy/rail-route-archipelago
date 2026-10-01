# Design

## Context

See proposal.md for motivation. The requirements are in `specs/client/save-binding` plus the deltas to `goal-completion`, `received-items` and `location-checks`.

**Game facts** (decompiled Rail Route 3.0.18, read 2026-10-01):
- **Save files.** Saves are `<persistentDataPath>/saves/<FileName>.mp.lz4`: MessagePack, contractless resolver, LZ4 block array. A `.jpg` thumbnail sits next to each one, and legacy saves are `.gd.gz`.
- **No room for mod data.** `SavedGame` is a fixed set of private `readonly` fields with no extension slot.
- **File names.** `StorageController.GetSaveFileName` gives `<levelUuid>[#discriminator]_<name>_<hh-mm-ss>_<5 chars of a new Guid>`. `GetAutoSaveFileName` gives `<levelUuid>[#discriminator]_Autosave <name>_<hh-mm-ss>_autosave`. So every save, even "save over" or a repeated autosave, is a new file name.
- **One write funnel.** Every write goes through `GameController.SaveGameInBackground` (manual save, autosave, `ForceAutoSave`, and the autosave on leaving a changed level before a new game), which calls `StorageController.Save(string saveName, SavedGame)`. That call happens synchronously in the coroutine's first step, right after `new SavedGame(null)` captured the game state on the main thread. Only after the async write does `LoadedSave` become `GetSaveFile(saveName)`.
- **Delete and rename.** `StorageController.DeleteSave(SaveFile)` deletes the save and both screenshot extensions. Every delete uses it: the save menus, `PruneAutoSaves` (keeps the five newest autosaves per level) and `ReturnBackToSave`. `StorageController.Rename(SaveFile, string)` moves the save and screenshots to a new `GetSaveFileName` and returns the new `SaveFile`.
- **Save listing.** `IsMatchingSave` only accepts the `.mp.lz4` and `.gd.gz` suffixes, so a `.ap.json` file never shows up as a save.
- **Loaded save.** `GameController.LoadedSave` is the `SaveFile` being loaded. `LoadSavedGameAsync` sets it before `Loaded` turns true. A new game, a restart and multiplayer set it to `null`.
- **Star order.** On the Endless-complete star, the game force-autosaves before it raises `StarAwarded` (FINDINGS.md "Goal completion"). The autosave written at the star moment therefore already has `Granted = true`, but not whatever the mod does in its `StarAwarded` handler.
- **Steam Cloud.** Locally, `remotecache.vdf` for app 1124180 lists only `saves/*.mp.lz4` files: no `.jpg`, no `.Player.log`. A sidecar won't sync.

**Client facts:**
- `ArchipelagoPump.StartLevel` runs once per loaded level (`GameController.Loaded`), and `EffectState` is cleared on every `ResearchController.Reset`.
- `ApSession.OnLoginSucceeded` runs `SlotDataCheck.Run` (map mismatch → `ChecksBlocked`), then `CheckSender.ResendAll`, `ReadGoal` and `GoalWatcher.OnLogin`.
- MultiClient's `RoomState.Seed` (the seed name from `RoomInfo`) and `ConnectionInfo.Team`/`Slot` are set by the time the login succeeds.
- `ItemRouter` uses `ItemSync`. A full list right after `Connected` is the replay. `ItemSync.Known` already models "how many of this list were applied before".
- `UpgradeReceiver.Receive` always runs `ApplySideEffects`, which sets `AutoAccept`/`AutoReverse` on every station and forces three alert preferences, the route preview and signalling safety on. Those settings are saved with the level, so reapplying them on each replay overwrites the player's choices.

## Goals / Non-Goals

**Goals:**
- The state file always describes the save file it sits next to: it is a snapshot taken at the same moment as the game's own snapshot.
- Binding and goal decisions are pure logic in `client/src/Core` with unit tests. The I/O and the Harmony glue stay thin.
- A broken or foreign state never leads to a send. When in doubt, refuse.

**Non-Goals:**
- Migrating saves from the previous client: they have no state file and bind as old saves (a star in them is ineligible).
- Rebinding or unbinding from the game. A player who wants to reuse a save for another seed deletes its `.ap.json` by hand. The README says so.
- A reconnect inside a level after a refusal.

## Decisions

### D1: A sidecar per save file, `<FileName>.ap.json`, snapshotted in a `StorageController.Save` prefix
- A Harmony prefix on `StorageController.Save(string saveName, SavedGame)` writes the level's current `SaveApState` to `saves/<saveName>.ap.json`. It runs on the main thread, in the same coroutine step as `new SavedGame(null)`, so both snapshots describe the same moment. The file is small, so it's written synchronously, to a temp file and then moved into place.
- A postfix on `DeleteSave(SaveFile)` deletes `<FileName>.ap.json` if it exists. A postfix on `Rename(SaveFile, string)` moves it to the returned `SaveFile.FileName`.
- The directory comes from the same place as the game's: `Path.Combine(Application.persistentDataPath, storageController.SaveDirectory)`. The `SaveDirectory` field is public.
- Sidecar I/O errors are logged and never thrown into the game's save path: a failed sidecar write must not fail the game's save.
- *Alternative: store the state inside the save.* No field is free. Hijacking a game field, or appending bytes after the MessagePack payload, depends on deserializer details that a game update can break, and it touches the player's actual save. Rejected; the user chose the sidecar.
- *Alternative: one state file per seed, or per level UUID.* A level has many saves at different points of progress (five autosaves, manual saves). The received index and the goal state are per point of progress, so a per-level file would be wrong for any older save. Rejected.
- *Alternative: patch `SaveGameInBackground`.* It's an iterator, so a prefix runs when the enumerator is created, and the name-to-file mapping is less direct. Rejected.

### D2: `SaveApState`, a pure model in `Core`
- Fields:
  - `version` (1)
  - `binding`: `{ seed, team, slot, slotName }`, or null
  - `unknownBinding`: bool
  - `sentChecks`: a sorted set of location names
  - `goal`: `none | pending | sent | ineligible`
  - `receivedIndex`: int
- Serialized with the game's Newtonsoft.Json, like `ModSettings`.
- `SaveApState.TryParse(text, out state, out error)`: invalid JSON, an unknown version or a negative index is an error.
- `Binding.Matches(other)` compares seed, team and slot number. `slotName` is only for the log, because the slot number identifies the slot within a seed.
- `SaveStateStore` (in `Interception`) owns `Current`, the state for the loaded level. `ArchipelagoPump.StartLevel` loads it from `LoadedSave?.FileName`, or uses a fresh state. A level's state is tied to its `Level` instance the same way the pump's `handledLevel` is.
- An unreadable sidecar (spec "An unreadable state file blocks the save") gives `Current = { unknownBinding = true }` and remembers the file name as "don't overwrite". The `Save` prefix skips only a write to that exact name. Later saves get a sidecar with `unknownBinding = true`.

### D3: The binding check runs first after login, and a refusal disconnects
- `OnLoginSucceeded` order becomes:
  1. `SlotDataCheck.Run` → `ChecksBlocked`
  2. `ReadGoal`
  3. `BindingDecision.Decide(current, loggedIn, mapMatches)`, which returns `Bind`, `Matches`, `Refuse(reason)` or `Skip` (map mismatch: unbound, nothing written)
  4. the resend and the goal.
- **Items wait for the decision.** The replay `ReceivedItems` is posted from the socket thread right after `Connected`, but `OnLoginSucceeded` is posted from the `LoginAsync` continuation on another thread, so their order on the main queue isn't guaranteed. `ItemRouter` therefore holds every packet it gets before the login decision, in arrival order, with the replay flag it was posted with. After the decision, `OnLoginSucceeded` either seeds the index (D4) and flushes the held packets, or drops them on `Refuse`. Today's code relies on that order by luck. This makes it explicit.
- On `Refuse`: log both bindings, show the notification, drop the held packets, then `Disconnect()`. Disconnecting starts a new epoch, so anything still queued is dropped by the pump.
- On `Bind`: set `Current.Binding`. If the star is already granted, set `goal = ineligible`. If `LoadedSave != null`, write the sidecar for `LoadedSave.FileName` at once. This early write describes the save file correctly: the file's game state hasn't changed, the binding is identity, and `receivedIndex` is still the loaded value. The checks resent at this login are included, and they are exactly the file's bought slots.
- *Alternative: stay connected and only block checks, like a map mismatch.* Every send path (purchase, resend, live goal, login goal) and the item router would need a new guard. A disconnect gives a single invariant: a refused save never has a session. Rejected.

### D4: The received index seeds `ItemSync`, and restores skip side effects
- `ItemSync` gets `Seed(int index)`, called once at the login decision (D3) with `Current.ReceivedIndex`, before the held packets are flushed. On the replay's `Replace`, `Known = min(seeded, count)` (today it's `min(Expected, count)` with `Expected` = 0). After that, `Expected` works as before.
- `ItemRouter.Apply` passes a mode to `UpgradeReceiver.Receive`:
  - `Restore` for items `< Known` in the replay: counts the effect, no `ApplySideEffects`, no popup, and still raises `ResearchCompleted` for the UI.
  - `NewSilent` for replay items `>= Known`: side effects, no popup.
  - `Live` for everything else.

  The non-replay full list (a resync reply) keeps today's rule: items below `Known` restore and the rest are live.
- After applying, `Current.ReceivedIndex = max(Current.ReceivedIndex, sync.Expected)`. It never shrinks, and a level loaded offline keeps the loaded value.
- Filler items count toward the index like any other item, because the index counts the server's list, not the upgrades. That's what M4's XP and money bundles will need.
- F9 (`DebugReceiveKey`) doesn't touch the index: it isn't a server item.
- *Alternative: persist the received item IDs and diff them.* The AP item list is append-only and ordered, so an index is enough, and it's what the protocol's own `index` means. Rejected.

### D5: The goal decision becomes per-save logic in `GoalState`
- `GoalState` stops being static game-run state. It becomes pure functions over (`goal` state, connected, bound to this slot, map matches, goal supported, star granted):
  - `OnBind(granted)` → `ineligible` if granted, else unchanged.
  - `OnLogin(state, …)` → send when bound and matching, goal supported, granted, and the state is `none` or `pending`.
  - `OnReachedLive(state, connected, bound, …)` → send, or `pending` (offline, or not yet bound but a session may still bind).
  - `MarkSent` → `sent`.
  - `MarkFailed` → `pending`.
- A live star on a never-bound level: while offline, the goal state becomes `pending`, but the level has no binding. At its first login `OnBind(granted: true)` turns it `ineligible`, so binding takes precedence (spec: "Star earned on a level that was never bound").
- The force-autosave at the star moment is written before the `StarAwarded` handler runs, so that autosave's sidecar says `none` with a granted star. `OnLogin` treats `none` + granted + bound as sendable, which covers it without depending on the order. The next save after the handler stores `pending` or `sent`.
- A lost `sent`, for example when the game quits before the next save, only causes a resend of `ClientGoal` on a later login. That is idempotent on the server.
- `GoalSendKey` and its install are deleted, along with `GoalRefusal.NotPending` and the "game run" wording.

### D6: Checks: record what was sent, and still resend every bought slot
- `CheckSender.Send` and `ResendAll` add every location name they hand to `CompleteLocationChecksAsync` to `Current.SentChecks`.
- The resend set is still "every bought slot". The recorded set is never a reason to skip, because a socket can accept a packet the server never processes.
- `CheckSender` reads a session-level `ChecksAllowed` (`!ChecksBlocked && bound`). After a refusal there is no session, so it's moot, but a purchase between login and decision can't slip through.

## Risks / Trade-offs

- [The sidecar doesn't sync through Steam Cloud] → On a second machine the save is unbound. It binds at first login, a star in it becomes ineligible, and all items apply with their side effects. Documented in the README and FINDINGS.md. Steam Cloud only syncs the configured pattern, and naming the sidecar `*.mp.lz4` would make the game list it as a save.
- [A game update renames `Save`/`DeleteSave`/`Rename` or adds a second write path] → The patches fail loudly and the mod degrades (all-or-nothing `PatchManager`). The FINDINGS.md note lists the funnel, so it gets re-checked after game updates.
- [A sidecar write fails (disk full, permissions)] → It's logged, the game's save still succeeds, and that save loads unbound later. The failure mode is "unbound", never "foreign", so the worst case is an ineligible star, not a wrong send.
- [An unbound old save sends its bought slots into the first seed it connects to] → This is today's behavior, kept on purpose ("bind on first login"). A map mismatch still blocks it.
- [A player loads an older save of the same seed] → Its sidecar is older too: a lower index means those items' side effects reapply, which is correct because the older save lacks them. Server-side checks it doesn't have stay checked. That is out of scope (proposal).
- [Write-at-bind targets `LoadedSave`, which is `null` while a save is in progress] → Binding happens at login, a few frames after load. If a save is in flight, the binding is carried by that save's prefix snapshot anyway.

## Migration Plan

- No data migration. Saves from earlier client versions have no sidecar, so they are unbound and bind at their next login. The game's own save files are never modified, so rolling back to the previous mod build just ignores the `.ap.json` files. Deleting them restores the old state exactly.

## Open Questions

- Whether `Rename` is reachable from the current save UI at all (it's on the interface). The patch is cheap either way, and the test round checks it if the menu offers renaming.
