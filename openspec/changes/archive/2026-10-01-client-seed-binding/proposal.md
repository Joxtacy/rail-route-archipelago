# Proposal

## Why

The client keeps no Archipelago state of its own across loads, so it can't tell which seed a save was played with. Three problems follow from that:
- A save from another seed of the same map sends its bought slots into the connected seed.
- A goal earned offline is lost when the game quits, and a star already in a save can only be sent with the Shift+F10 fallback.
- Every connect replays every received item with its full side effects. That turns Auto-accept and Auto-reverse back on at every station and resets the alert preferences the player chose. In M4 it would also hand out filler XP and money again on each connect.

Binding each Endless save to the seed and slot it was played with fixes all three. It covers the M3 follow-up "Save checked and received state per save file, bound to the seed" and the goal follow-ups in ROADMAP.md.

## What Changes

- **Per-save state in a sidecar file.** The game has no room for mod data in `SavedGame`, and every save writes a new file name: manual saves get a fresh 5-character suffix, and autosaves embed the in-game time and are pruned to five. So the client keeps one JSON sidecar per save file, `<save file name>.ap.json` in the game's `saves/` folder, holding:
  - the binding (seed name, team, slot number and slot name)
  - the sent checks
  - the goal state: pending, sent, or ineligible because the star predates the binding
  - the received-item index
  
  The sidecar is written whenever the game writes a save, moved when the game renames one, and deleted when it deletes one. Pruned autosaves go through the same delete.
- **Bind on first login.** After a successful login on an unbound save (or a new game) whose map matches the seed, the client binds the level to the seed and slot. When the level was loaded from a save file, the binding is written to that file's sidecar at once.
- **Refuse another seed's save.** When the loaded save is bound to another seed or slot, the client sends no checks and no goal, applies no items, logs both bindings, notifies the player and disconnects. A sidecar it can't read is treated the same way, and the file is left untouched.
- **Goal from the save.** After login on a save bound to this seed, the client sends the goal when the level has the Endless-complete star and the save's goal isn't recorded as sent. That covers a goal reached offline in an earlier game run, and the force-autosave the game writes just before `StarAwarded`. A star that was already in the save when it was bound is ineligible and is never sent.
- **Item replay without repeated side effects.** On the replay at login, items below the save's received index are restored silently: their effect counts, without one-shot side effects. Items from the index onwards apply with their side effects, also silently, and one notification says how many items are new.
- **BREAKING (player-facing):** Shift+F10 and the in-memory, per-game-run pending goal are removed. A save that was never bound with the star in it, including a game played offline from the start, can no longer send the goal.
- Sidecars live only on the local machine: Steam Cloud syncs only the `*.mp.lz4` files in `saves/`. On another machine a cloud-synced save shows up unbound.

## Capabilities

### New Capabilities
- `client/save-binding`: the per-save sidecar (contents, when it's written, moved and deleted), binding on the first login, and refusing a save bound to another seed or with an unreadable sidecar.

### Modified Capabilities
- `client/goal-completion`:
  - A star in a save is sent automatically when the save is bound to this seed and the star came after the binding.
  - The pending goal persists per save instead of per game run.
  - The Shift+F10 requirement is removed.
- `client/received-items`:
  - The replay restores items below the save's received index without their side effects.
  - Items from the index onwards get their side effects and are counted in one "new items" notification.
  - Received items still aren't saved; only the index is.
- `client/location-checks`:
  - The resend on connect only happens for a save bound to this seed, or one that binds at this login.
  - Sent checks are recorded in the save's state.

## Impact

- **Code** (all in `client/`):
  - new `src/Core/SaveApState.cs`: the sidecar model, JSON (de)serialization and binding comparison (pure, unit-tested)
  - `src/Core/GoalState.cs` rewritten as per-save decisions
  - `src/Core/ItemSync.cs`: seeded with the persisted index
  - new `src/Interception/SaveStateStore.cs`: sidecar paths and file I/O, plus the level's current state
  - new Harmony patches on `StorageController.Save`, `DeleteSave` and `Rename`
  - `ArchipelagoPump`: loads the sidecar per level
  - `ApSession`: binding check after login, before the resend
  - `CheckSender` and `ItemRouter`: record into the state
  - `UpgradeReceiver`: a restore mode without side effects
  - `GoalWatcher`: per-save state
  - `GoalSendKey.cs` and its install in `ArchipelagoMod` are removed
- **Tests**: new `client.Tests/SaveApStateTests.cs`; `GoalStateTests` and `ItemSyncTests` updated.
- **Docs**:
  - `client/README.md`: the sidecar, refusal and Shift+F10 removal
  - `FINDINGS.md`: save naming, save, rename and delete paths, Steam Cloud patterns, test round
  - `ROADMAP.md`: M3 follow-up and goal follow-ups done
- **Out of scope**:
  - restoring received items offline from the sidecar (the server's replay still restores them)
  - syncing sidecars through Steam Cloud
  - multiplayer saves
  - reconciling a save that is older than checks the server already has
  - the upgrade panel's "Check sent" label (the recorded sent checks make it possible later)
