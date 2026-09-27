# Proposal

## Why

The client mod can already turn upgrade purchases into location checks (M1), but it has nothing to connect to yet. Archipelago needs a `rail_route` world that generates a seed and defines the item and location ID tables, and the client (M3) will be built against both. This is milestone M2 on the roadmap.

## What Changes

- Add a Python APWorld for the game **Rail Route** under `apworld/rail_route/`, targeting Archipelago 0.6.7. It builds into `rail_route.apworld`.
- **Locations:**
  - One location per system-upgrade slot. Buying the slot in the upgrade panel sends the check.
  - The two Custom Contracts slots (Green T1 with Happy Passengers, Red T3 without it) are mutually exclusive in game, so they share one location.
  - That makes 49 location names in total. A seed contains 43 of them, or 49 with Expect Delays.
- **Items:**
  - Every system upgrade.
  - Track speed, station count and offered contracts are progressive items with 3 copies each.
  - One filler item, which is registered but not placed in v0 seeds.
- **Slot options:**
  - `map`: Haarlem (default), Prague or Amsterdam
  - `goal`: `endless_complete` only for now
  - `expect_delays` and `happy_passengers`: both default off. They decide which upgrades exist, as the game does.
- **Logic:**
  - Slot parent/child ordering: a child slot is buyable only after its parent slot is bought.
  - Green and red tier gates, as progression-item counts from a per-map table.
  - Red XP income requires Regional Trains or Freights.
  - The goal is the Endless-complete star on the chosen map.
- **Stable IDs:** hand-assigned in data tables, the same regardless of options, and pinned by a test so they are never renumbered.
- **Slot data** for the client: map, goal and DLC options.
- **Dev workflow and docs:** run the world's tests from an Archipelago source checkout, build the `.apworld`, and document both in `CLAUDE.md`, `apworld/README.md` and `FINDINGS.md`.

## Capabilities

### New Capabilities
- `apworld/item-location-tables`: The world's item and location names, their stable IDs, item classifications, and which ones exist in a seed for each DLC option combination.
- `apworld/slot-options`: The player-facing options (map, goal, Expect Delays, Happy Passengers) and the slot data sent to the client.
- `apworld/generation-logic`: Access rules and regions: slot parent ordering, tier gates, red XP income, the goal condition, and the guarantee that every supported option combination generates a beatable seed.

### Modified Capabilities
<!-- None. The client's split-flag rework (the upgrade panel's "researched" means "slot bought", and received items are tracked separately) is a separate client change and doesn't change this change's specs. -->

## Impact

- **New code:** `apworld/rail_route/` (`__init__.py`, `items.py`, `locations.py`, `options.py`, `rules.py`, `archipelago.json`, `test/`). There are no changes under `client/`.
- **Dependencies:** a local Archipelago 0.6.7 source checkout is needed to run tests and build (outside the repo). The world itself adds no third-party Python packages.
- **Contracts with later work:**
  - The client (M3) resolves location and item IDs by name through the datapackage, so these names are a contract. Renaming one breaks the mod.
  - Tier-gate numbers are initial estimates, to be tuned by playtesting.
  - The logic assumes the client's split-flag design, where receiving an item doesn't consume its slot. That design is implemented in a later client change.
- **Out of scope:**
  - Client connection
  - Star, throughput-milestone, achievement, Timetable and Rush Hour locations (M4)
  - Filler and trap placement (M4)
  - Maps outside the curated three
  - DeathLink
  - Publishing the world
