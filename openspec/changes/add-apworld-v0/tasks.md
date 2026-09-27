# Tasks

## 1. Dev environment and world skeleton

- [ ] 1.1 Create `apworld/rail_route/` with:
  - an empty-pool `RailRouteWorld` (`game = "Rail Route"`) in `__init__.py`
  - `RailRouteItem` and `RailRouteLocation` classes
  - `archipelago.json` with `game: "Rail Route"`, `minimum_ap_version: "0.6.7"`, `world_version: "0.1.0"` and `authors: ["Joxtacy"]`
  - an empty `test/__init__.py`

  Verify the files exist and that `python3.13 -m py_compile` succeeds on each `.py` file.
- [ ] 1.2 Link the world into the AP 0.6.7 checkout.
  - Ask the user before writing outside the repo. Then create the symlink `~/PrivateProjects/Archipelago/worlds/rail_route → <repo>/apworld/rail_route`, and a Python 3.13 venv in that checkout with its requirements installed (`python ModuleUpdate.py -y`).
  - Verify that `python -c "import worlds; from worlds.AutoWorld import AutoWorldRegister as R; print('Rail Route' in R.world_types)"` prints `True`.

## 2. Data table, items and locations

- [ ] 2.1 Write `data.py` with the 50 upgrade rows exactly as in design D2 (game id, title, colour, tier, parent id, location ID, item name, item ID, classification, DLC condition), plus the 3 progressive items (100–102) and the filler item Green XP Bundle (500). Verify with a unit test in `test/test_data.py`:
  - 49 distinct location names and IDs, all within 1000–1099
  - 44 item names with IDs in the specified ranges
  - no duplicate IDs
  - every parent id refers to an existing row
- [ ] 2.2 Derive `item_name_to_id`, `location_name_to_id` and `item_name_groups` (`Throughput`, `Red Income`) from the table in `items.py` and `locations.py`. Add a golden test `test/test_ids.py` holding a frozen name→ID dict for all items and locations; it asserts that every frozen entry is still present with the same ID. Verify the test passes, and that it fails when one ID in `data.py` is changed temporarily (then revert).
- [ ] 2.3 Implement the per-seed location and item selection from the DLC options: `create_regions` places the active slot locations, and `create_items` adds one item per active slot, with the progressive items taking the chain slots. Verify with `test/test_pool.py` using `WorldTestBase`:
  - 43 locations and 43 items with no DLC, 49 and 49 with Expect Delays
  - exactly 3 copies of each progressive item
  - no filler item in the pool
  - Custom Contracts is in `Red T3` with Happy Passengers off and in `Green T1` with it on
  - Maintenance Depot is absent with Expect Delays off

## 3. Options and slot data

- [ ] 3.1 Implement `options.py` (design D6: `map`, `goal`, `expect_delays`, `happy_passengers` in a `PerGameCommonOptions` dataclass) and `fill_slot_data`. Verify with `test/test_options.py`:
  - the defaults are Haarlem, `endless_complete` and both DLC off
  - `map: prague` with `expect_delays: true` gives slot data `{"map": "prague", "goal": "endless_complete", "expect_delays": true, "happy_passengers": false}`
  - an invalid `map` value raises an option error

## 4. Logic

- [ ] 4.1 Implement `rules.py` with the regions and entrances from design D3, the per-map gate table from D4 (Haarlem 2/4/7, Prague 3/6/9, Amsterdam 4/8/11), and the red income rules. Place the `Endless Complete` victory event in `Goal` and set the completion condition. Verify with `test/test_rules.py`:
  - with no items collected, a parentless Green T1 location is reachable
  - on each map, a Green T2 location is unreachable with gate−1 throughput items and reachable with the gate count
  - a Red T1 location is unreachable with the G2 gate met but neither Regional Trains nor Freights, and reachable once Freights is added
  - a Red T3 location needs both income items and the G3 gate
  - completion is false at goal−1 throughput items and true at the goal count
- [ ] 4.2 Add slot parent ordering by rule composition (design D3). Verify in `test/test_rules.py`:
  - Perpetual Circuit is unreachable in a state where the Automatic Routing location is unreachable
  - Corridor Tracks requires Advanced Tracks' rule, which covers the chain
  - Service Automation (R2) requires the Maintenance Depot slot's rule with Expect Delays on
- [ ] 4.3 Add the option-matrix tests `test/test_matrix.py`: one `WorldTestBase` subclass per combination of `map` × `expect_delays` × `happy_passengers` (12 in total), running the default tests. Verify that `python -m pytest worlds/rail_route/test` passes from the AP root.

## 5. Packaging, docs and integration

- [ ] 5.1 Write `apworld/README.md` covering:
  - the dev setup from 1.2 and the test command
  - building with the Launcher's "Build APWorlds" component
  - a minimal example player YAML
  - the location-name list (the name contract with the client, design D8)

  Verify the documented test command runs as written.
- [ ] 5.2 Run the AP generic suites against the world from the AP root with `python -m pytest test/general -k "not Webhost"`, or the closest selection that includes our world. Verify they pass. If an unrelated world in the checkout fails, record which one and that the failure is outside `rail_route`.
- [ ] 5.3 Build `rail_route.apworld` through the "Build APWorlds" component and generate a seed with `Generate.py` using the example YAML from 5.1 in a scratch `Players/` folder. Verify that the build writes `build/apworlds/rail_route.apworld` and that generation writes an output zip with a spoiler log listing the `Endless Complete` goal. Keep no generated files in the repo.
- [ ] 5.4 Update the project docs:
  - `CLAUDE.md` Commands: APWorld tests and build
  - `FINDINGS.md`: the upgrade table source, DLC filtering, red income, the map UUIDs and the tier thresholds
  - `ROADMAP.md`: M2 status and the `add-apworld-v0` change link. Add to the M3 notes that received items no longer go through `CompleteResearch` (split-flag) and that the child-upgrade, tier-threshold and map-override checks belong in its test round.

  Verify that `openspec validate add-apworld-v0` passes.
