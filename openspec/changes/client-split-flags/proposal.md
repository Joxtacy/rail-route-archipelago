# Proposal

## Why

The APWorld's logic (M2) assumes a split-flag client: buying an upgrade slot and owning its upgrade are separate facts. Today the game keeps a single `Researched` flag per upgrade, so the mod can't have a slot bought with its upgrade missing, or the reverse. The mod works around this with an in-memory pending set, the F9 grant goes through `CompleteResearch`, and that grant fills the slot's own location. This has to be fixed before the M3 networking change can send checks and apply received items the way the generated seeds expect.

## What Changes

- In intercept mode, on an Endless level, the upgrade panel's `Researched` flag means **"slot bought"**:
  - Buying a slot spends its points, sets the slot's `Researched` flag and records a location check under the slot's AP location name. The upgrade's effect is **not** granted.
  - Parent gating, the panel's installed state and repeat-purchase blocking come from the game's own slot flag. The in-memory `PendingChecks` set and the `CanResearch` block patch are removed.
  - Bought slots are saved with the game's save file, since the game already serializes `Researched`.
- Received items are tracked separately from slots, in a new received-items store:
  - The game's effect queries (`HasResearched`, `ResearchedValue` and `IsAutomationEnabled`) answer from the received items, not from slot flags.
  - Receiving an item applies the upgrade's side effects (station auto-accept/auto-reverse, alert and interface preferences, automation) and refreshes the UI. It never sets any slot's `Researched` flag.
  - Progressive items (track speed, station count, contract offers) raise the effect by one level per copy received, whichever slots are bought.
  - Custom Contracts is one item that covers both game variants.
- Add a client table mapping each game upgrade `Id` to its AP location name, copied from `apworld/README.md`. A unit test parses the README table and fails if the two differ.
- Replace the F9 debug grant with an **F9 debug receive**. In intercept mode on an Endless level, it receives the item for the upgrade selected in the upgrade panel. It needs no network and no bought slot.
- Outside Endless levels (tutorials, story, editor), and with intercept mode off, the game behaves unmodded.
- **BREAKING (dev-only behavior):** F9 no longer grants "the most recently intercepted upgrade". It now receives the selected upgrade's item.

## Capabilities

### New Capabilities
- `client/received-items`: Tracking items received from Archipelago separately from slots, how they drive the game's upgrade effects, and the offline debug receive key.
- `client/location-names`: The client's game `Id` → AP location-name table and the test that keeps it in sync with the APWorld's documented contract.

### Modified Capabilities
- `client/upgrade-purchase-interception`:
  - Intercepted purchases now mark the slot as bought instead of recording an in-memory pending check.
  - Repeat purchases are blocked by the slot flag, which persists with the save.
  - Interception only applies on Endless levels.
  - "The mod grants an upgrade" becomes a received item that grants effects without touching slots.

## Impact

- **Code**:
  - `client/src/Interception/` (the purchase handler rework, `DebugGrantKey` → debug receive)
  - new effect patches under `client/src/Patches/`
  - new `client/src/Core/` classes (location table, received-items counts)
  - `PendingChecks` and `CanResearchPatch` removed
  - `client/README.md` (F9, settings semantics)
- **Tests**: new `client.Tests` cases for the location table (README sync) and received-item counting. `PendingChecksTests` is removed along with `PendingChecks`.
- **Game coupling**: new Harmony patches on `ResearchController.HasResearched`, `ResearchedValue` and the `IsAutomationEnabled` getter, all in Rail Route 3.0.18. The receive path mirrors `CompleteResearch`'s side-effect switch, which has to be re-checked after game updates. `CompleteResearch` itself stays unpatched.
- **Out of scope**:
  - Archipelago networking and MultiClient.Net (the next M3 change)
  - the AP item-ID table, and persisting received items or binding state to a seed
  - a "Check sent" label in the upgrade panel
  - DeathLink and multiplayer
