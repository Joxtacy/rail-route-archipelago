# Design

## Context

See proposal.md for motivation. The requirements are in `specs/apworld/item-location-tables`, `specs/apworld/slot-options` and `specs/apworld/generation-logic`.

**Game facts** (decompiled Rail Route 3.0.18 `RailRoute.dll` and game assets, read 2026-09-27):

- **Upgrade table.** `ResearchController.researchItems` holds 50 upgrades: 36 Green (14/11/11 in tiers 1/2/3) and 14 Red (4/7/3). There are no Blue upgrades.
  - Each upgrade has an `Id`, an optional `Parent` id, a colour, a tier and point costs.
  - English titles come from the `Research Strings Table` localization table, under the key `research_item_<id>_title`.
- **Tiers** unlock when the best points-per-cycle reaches a threshold (`GetCurrentTier`). They don't depend on the number of upgrades bought.
  - C# defaults: Green T2 at 10 and T3 at 25; Red T2 at 8 and T3 at 30.
  - Red opens once Green reaches 8 points per cycle, which grants 3 red points, or once Green reaches tier 2.
  - Endless-complete is a Green throughput of 60.
  - The serialized `SystemUpgradeTierDefaults` asset may override these values. That hasn't been checked yet.
- **Parent gating** (`GetUpgradeRequiredForUnlock`). A slot can't be bought until its parent's `Researched` flag is set.
- **Red points** only come from red contracts. `ContractGenerator.TryRandomRed` only generates Freight, Regional or Urban contracts if the matching upgrade is researched. Urban contracts also need a station with a coach yard.
- **DLC filtering** (`SystemUpgradesPage.InstantiateUpgrades`):
  - Without Expect Delays enabled, 6 upgrades are hidden.
  - With Happy Passengers enabled, `custom_contracts_alt` (G1) is shown and `custom_contracts` (R3) is hidden. Without it, the reverse.
- **Maps.** Haarlem (UUID `Haarlem`, difficulty 0), Prague (`prague`, difficulty 3) and Amsterdam (`Amsterdam`, difficulty 5) are Endless maps. None of the three stores an upgrade-override block in the formats found so far.

**Archipelago:** the target is release 0.6.7, whose world API includes `archipelago.json` manifests and `WorldTestBase`. A 0.6.7 source checkout already exists at `~/PrivateProjects/Archipelago`. On macOS, 0.6.7 requires Python 3.11.9 up to (but not including) 3.14. The system `python3` is 3.14, and `python3.13` is installed through Homebrew.

**Client assumption:** the logic assumes the split-flag client design from the M2 exploration. The upgrade panel's `Researched` flag means "slot bought", and received items are tracked separately. So receiving an item never consumes its slot's location, and slot parent ordering only involves slots. That client work is a separate change.

## Goals / Non-Goals

**Goals:**
- Keep the data table the single source of truth, so items, locations, IDs, DLC filtering and parents are all derived from it. Adding M4 locations should mean adding rows and rules, not restructuring.
- Make logic numbers easy to tune after playtesting. They live in one per-map table.
- Have tests that fail when an ID is renumbered or a name changes.

**Non-Goals:**
- Balancing the tier-gate numbers. These are initial estimates.
- Modeling XP costs or cumulative spending in logic.
- Modeling Urban Transit Contracts as a red income source, since that depends on the map having coach yards.
- Using the 0.6.7 rule builder. We use plain access-rule lambdas instead (see D5).

## Decisions

### D1: Layout: the world lives in `apworld/rail_route/`, and tests run from the local AP checkout through a symlink
- The world package goes in `apworld/rail_route/`, containing `__init__.py`, `items.py`, `locations.py`, `data.py`, `options.py`, `rules.py`, `archipelago.json` and `test/`.
- A symlink `~/PrivateProjects/Archipelago/worlds/rail_route → <repo>/apworld/rail_route` makes the AP checkout load it.
- Tests run from the AP root with a Python 3.13 venv: `python -m pytest worlds/rail_route/test` for our tests, plus the generic suites under `test/general`.
- The `.apworld` is built with the Launcher's "Build APWorlds" component. That reads `archipelago.json` and writes `build/apworlds/rail_route.apworld`.
- *Alternative: vendor AP as a git submodule.* That would add a large checkout to the repo for no gain.
- *Alternative: a standalone test harness without AP.* AP's world tests depend on `BaseClasses` and `worlds.AutoWorld`, so this isn't practical.

### D2: One data table drives everything
`data.py` holds one row per game upgrade, in game order. Each row has:
- the game `Id` and English title
- colour and tier
- parent `Id`
- location ID
- item key and item ID
- classification
- a DLC condition (`None`, `ED`, `HP_ON` or `HP_OFF`)

`item_name_to_id`, `location_name_to_id`, the per-seed location list, the item pool and the parent rules are all derived from it.

The table below is generated from the decompiled game data and localization. Colour and tier are the slot's. For the progressive chains, the item column shows the progressive item that the slot's level belongs to.

| Game id | Slot / location name | Loc ID | Col | T | Parent slot | Item (ID) | Class | DLC |
|---|---|---|---|---|---|---|---|---|
| `autoblock` | Autoblocks | 1000 | G | 1 |  | Autoblocks (1) | progression |  |
| `auto_accept` | Auto-accept Trains | 1001 | G | 1 |  | Auto-accept Trains (2) | useful |  |
| `automatic_routing` | Automatic Routing | 1002 | G | 1 |  | Automatic Routing (3) | progression |  |
| `perpetual_circuit` | Perpetual Circuit | 1003 | G | 1 | Automatic Routing | Perpetual Circuit (4) | useful |  |
| `auto_reverse` | Auto-reverse Trains | 1004 | G | 1 |  | Auto-reverse Trains (5) | useful |  |
| `manual_signal_route_preview` | Manual Signal Route Preview | 1005 | G | 1 |  | Manual Signal Route Preview (6) | useful |  |
| `manual_signal_security` | Signalling Safety | 1006 | G | 1 |  | Signalling Safety (7) | useful |  |
| `platform_management` | Platform Adjustments | 1007 | G | 1 |  | Platform Adjustments (8) | useful |  |
| `contract_management` | Timetable Adjustments | 1008 | G | 1 |  | Timetable Adjustments (9) | useful |  |
| `station_count1` | More Stations | 1009 | G | 1 |  | Progressive Station Count (101) | progression |  |
| `train_alerts` | Train Alerts | 1010 | G | 1 |  | Train Alerts (10) | useful |  |
| `relay_sensor` | Relay Sensor | 1011 | G | 1 | Automatic Routing | Relay Sensor (11) | useful |  |
| `custom_contracts_alt` | Custom Contracts | 1012 | G | 1 |  | Custom Contracts (12) | useful | HP on |
| `maintenance_depot` | Maintenance Depot | 1013 | G | 1 |  | Maintenance Depot (13) | useful | ED |
| `additional_service_capacity` | Additional Service Capacity | 1014 | G | 2 | Maintenance Depot | Additional Service Capacity (14) | useful | ED |
| `field_efficiency` | Field Efficiency | 1015 | G | 3 | Maintenance Depot | Field Efficiency (15) | useful | ED |
| `expanded_service_capacity` | Expanded Service Capacity | 1016 | G | 3 | Additional Service Capacity | Expanded Service Capacity (16) | useful | ED |
| `track_speed1` | Basic Tracks | 1017 | G | 2 |  | Progressive Track Speed (100) | progression |  |
| `ic_contracts` | InterCities | 1018 | G | 2 |  | InterCities (17) | progression |  |
| `more_offered_contracts1` | More Contract Offers | 1019 | G | 2 |  | Progressive Contract Offers (102) | progression |  |
| `custom_contract_period` | Custom Contract Period | 1020 | G | 2 |  | Custom Contract Period (18) | useful |  |
| `contract_windows` | Operating Hours | 1021 | G | 2 | Custom Contract Period | Operating Hours (19) | useful | ED |
| `station_count2` | Even More Stations | 1022 | G | 2 | More Stations | Progressive Station Count (101) | progression |  |
| `departure_sensor` | Departure Sensor | 1023 | G | 2 | Automatic Routing | Departure Sensor (20) | useful |  |
| `platform_sensor` | Arrival Sensor | 1024 | G | 2 | Automatic Routing | Arrival Sensor (21) | useful |  |
| `track_speed2` | Advanced Tracks | 1025 | G | 2 | Basic Tracks | Progressive Track Speed (100) | progression |  |
| `waypoint` | Waypoints | 1026 | G | 2 |  | Waypoints (22) | useful |  |
| `routing_sensor` | Routing Sensor | 1027 | G | 3 | Automatic Routing | Routing Sensor (23) | useful |  |
| `auto_contract_manager_structural` | Structural Contracts Manager | 1028 | G | 3 |  | Structural Contracts Manager (24) | useful |  |
| `auto_contract_manager_financial` | Financial Contracts Manager | 1029 | G | 3 |  | Financial Contracts Manager (25) | useful |  |
| `auto_contract_manager_regional` | Regional Contracts Manager | 1030 | G | 3 |  | Regional Contracts Manager (26) | useful |  |
| `more_offered_contracts2` | Even More Contract Offers | 1031 | G | 3 | More Contract Offers | Progressive Contract Offers (102) | progression |  |
| `more_offered_contracts3` | Way More Contract Offers | 1032 | G | 3 | Even More Contract Offers | Progressive Contract Offers (102) | progression |  |
| `station_count3` | Unlimited Stations | 1033 | G | 3 | Even More Stations | Progressive Station Count (101) | progression |  |
| `switch_speed` | Faster Switches | 1034 | G | 3 |  | Faster Switches (27) | useful |  |
| `track_speed3` | Corridor Tracks | 1035 | G | 3 | Advanced Tracks | Progressive Track Speed (100) | progression |  |
| `regional_contracts` | Regional Trains | 1036 | R | 1 |  | Regional Trains (28) | progression |  |
| `onetime_contracts` | Freights | 1037 | R | 1 |  | Freights (29) | progression |  |
| `shunting_commands` | Shunting Commands | 1038 | R | 1 |  | Shunting Commands (30) | useful |  |
| `shunting_track` | Shunting Track | 1039 | R | 1 | Shunting Commands | Shunting Track (31) | useful |  |
| `shunting_sensor` | Shunting Sensor | 1040 | R | 2 | Shunting Commands | Shunting Sensor (32) | useful |  |
| `stabling_sensor` | Stabling Sensor | 1041 | R | 2 | Shunting Commands | Stabling Sensor (33) | useful |  |
| `tunnels` | Tunnels | 1042 | R | 2 |  | Tunnels (34) | useful |  |
| `urban_contracts` | Urban Transit Contracts | 1043 | R | 2 | Regional Trains | Urban Transit Contracts (35) | useful |  |
| `loco_coupling` | Loco Coupling | 1044 | R | 2 | Shunting Sensor | Loco Coupling (36) | useful |  |
| `advanced_arrival_sensor` | Advanced Arrival Sensor | 1045 | R | 2 |  | Advanced Arrival Sensor (37) | useful |  |
| `service_automation` | Service Automation | 1046 | R | 2 | Maintenance Depot | Service Automation (38) | useful | ED |
| `coach_yard` | Regional Trains Stabling | 1047 | R | 3 |  | Regional Trains Stabling (39) | useful |  |
| `advanced_routing_sensor` | Advanced Routing Sensor | 1048 | R | 3 |  | Advanced Routing Sensor (40) | useful |  |
| `custom_contracts` | Custom Contracts | 1012 | R | 3 |  | Custom Contracts (12) | useful | HP off |

- The filler item is **Green XP Bundle (500)**. It's registered but not placed in v0. `get_filler_item_name` returns it.
- Titles are normalized to Title Case where the game's casing is inconsistent, for example "Unlimited stations" becomes "Unlimited Stations".
- The two Custom Contracts rows share location 1012 and item 12. Only one of the rows is active per seed, and its colour and tier drive the logic.
- The victory event is `Endless Complete`. It has no ID.
- *Alternative: IDs generated by `enumerate(...)` over the table.* Reordering or inserting a row would silently renumber everything. Hand-assigned IDs in the row, pinned by a golden test (D7), prevent that.

### D3: Regions follow colour and tier; gates sit on entrances, parent ordering on locations
```
Menu --> Green T1 --[G2 gate]--> Green T2 --[G3 gate]--> Green T3 --[goal count]--> Goal (event)
            |
            +--[G2 gate + (Regional Trains | Freights)]--> Red T1 --> Red T2
                                                               |
                                    [G3 gate + Regional Trains + Freights]--> Red T3
```
- Each slot location is placed in its colour/tier region, using the active row for Custom Contracts.
- **Parent ordering is composed, not looked up.** A child location's rule is its parent location's full rule ANDed with its own region reachability. The parent's region is implied by entrance chaining, so the composition only has to AND the parent's region reachability and the parent's own rule, recursively. This avoids `can_reach_location` inside location rules, which is slower and easy to get wrong with sweep caching.
- Cross-colour parents work the same way, for example Service Automation (R2) under Maintenance Depot (G1).
- *Alternative: one flat region with every rule on the locations.* That's simpler, but the spoiler's playthrough would be less readable, and every tier rule would be repeated on each location.

### D4: Tier gates count "throughput items" from a per-map table
The throughput items are the 3 progressive items (3 copies each), InterCities, Automatic Routing and Autoblocks, which makes 12 copies. A gate is `state.count_from_list(THROUGHPUT, player) >= n`, or an equivalent group count.

The initial numbers are guesses to be tuned by playtesting. They stay monotone in difficulty, as the spec requires:

| Map | Green T2 | Green T3 | Goal (60/cycle) |
|---|---|---|---|
| Haarlem | 2 | 4 | 7 |
| Prague | 3 | 6 | 9 |
| Amsterdam | 4 | 8 | 11 |

- The table is a dict keyed by map option value, in `rules.py`.
- "Any map" later becomes a new row with the strictest values.
- *Alternative: specific-item gates* (for example "T3 needs Advanced Tracks"). That's more game-accurate, but we don't have playtest evidence for which items matter, so counts are easier to tune without restructuring.

### D5: Plain access-rule lambdas instead of the rule builder
The rules are few and simple (counts, `has_any`, `has_all`, composition). Lambdas are the documented baseline and every example world uses them. The rule builder would add human-readable logic explanations, which is worth reconsidering in M4 when rules grow.

### D6: Options and slot data
- `map`: a `Choice` with `haarlem` = 0 (default), `prague` = 1 and `amsterdam` = 2.
- `goal`: a `Choice` with `endless_complete` = 0.
- `expect_delays` and `happy_passengers`: both `Toggle`.
- They're grouped in a `PerGameCommonOptions` dataclass.
- `fill_slot_data` returns `{"map": <level UUID>, "goal": "endless_complete", "expect_delays": bool, "happy_passengers": bool}`. The UUID mapping lives beside the map table in `rules.py`/`options.py`, so the client can check the loaded level's UUID directly.

### D7: Tests
- **Golden ID test.** A frozen copy of `{name: id}` for items and locations lives in the test. The test asserts the world's tables are a superset of it, with the same IDs. That allows additions but not changes or removals.
- **Option matrix.** `WorldTestBase` subclasses are generated for all 12 combinations of `map` × `expect_delays` × `happy_passengers`. Each runs the default tests: all-state can reach everything, empty state can reach something, and the goal is reachable. The generation cost is small, since the world has only about 50 locations.
- **Rule tests** on single seeds, using `collect_by_name` and `can_reach_location` (see the scenarios in `generation-logic`):
  - Perpetual Circuit needs the Automatic Routing slot.
  - Green T2 is blocked at gate−1 and open at the gate.
  - Red is blocked without Regional Trains and Freights.
  - Completion is blocked at goal−1 and happens at the goal count.
- **Pool tests.** Location and item counts are checked per DLC combination (43/49), along with three copies of each progressive item and the Custom Contracts tier swap.
- The generic `test/general` suites from AP also run against the world.

### D8: Name contract with the client
The client (M3) keeps a table mapping game `Id` to AP location name and resolves IDs through the datapackage. Location names equal the English titles in D2's table. The game `Id` isn't sent through AP. It only lives in `data.py` and in the client, so the two tables have to agree. A test in the M3 client change will compare them against a shared list. This change documents the list in `apworld/README.md`.

## Risks / Trade-offs

- **[Tier-gate numbers are guesses]** A seed could be logically beatable but very grindy, or the logic could be too strict. → All numbers are in one table (D4), and the first playtest round tunes them. They don't affect IDs or names.
- **[Serialized tier thresholds may differ from the C# defaults]** The throughput mapping would shift. → The gates are item counts, not throughput numbers, so only the tuning changes. Verifying at runtime is on the client test-round list.
- **[Child upgrades might not work without their parent's item]** For example, sensors without Automatic Routing. This affects classification, not reachability. → Children stay "useful" and aren't counted in any gate. If the test round shows a child is useless without its parent, the change to make is logic-side (item-parent rules or progressive trees), and it doesn't need new IDs.
- **[Red logic is conservative]** The 3 free red points could buy one 3-cost red slot without any income item, but the logic doesn't count it. → This is accepted, since it only makes seeds easier than the logic says.
- **[Base-game item and location counts are exactly equal (43/43)]** There's no room for filler or traps. → That's fine for v0. M4 adds throughput-milestone and star locations.
- **[Renaming a location breaks the client]** → Names come from the in-game titles and are pinned by the golden test (D7). A rename would have to be a deliberate test update.
- **[Symlink into the user's AP checkout]** That checkout also contains another world in progress (`worlds/what_the_golf`). → Only `worlds/rail_route` is added, and nothing else in that checkout is touched. Setting it up is an explicit task that the user confirms.

## Migration Plan

There is no migration, since this is a new, unpublished world. Rollback means removing the `worlds/rail_route` symlink from the AP checkout.

## Open Questions

- The final tier-gate numbers per map, which come from playtesting after M3 makes seeds playable.
- `archipelago.json` uses `authors: ["Joxtacy"]` (the git user name). Change it if you prefer another handle.
