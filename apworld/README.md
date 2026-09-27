# Rail Route APWorld

The Archipelago world for Rail Route. It targets Archipelago 0.6.7 and builds into `rail_route.apworld`.

## Dev setup

The world is developed inside an Archipelago source checkout, which is kept outside this repo. On macOS, 0.6.7 needs Python 3.11.9 up to (but not including) 3.14, so use `python3.13`.

```sh
git clone https://github.com/ArchipelagoMW/Archipelago.git ~/PrivateProjects/Archipelago
cd ~/PrivateProjects/Archipelago
git checkout 0.6.7
ln -s <this repo>/apworld/rail_route worlds/rail_route
python3.13 -m venv .venv
.venv/bin/python ModuleUpdate.py -y
.venv/bin/python -m pip install pytest
```

Check that Archipelago picks up the world:

```sh
.venv/bin/python -c "import worlds; from worlds.AutoWorld import AutoWorldRegister as R; print('Rail Route' in R.world_types)"
```

## Tests

Run these from the Archipelago root:

```sh
.venv/bin/python -m pytest worlds/rail_route/test         # this world's tests
.venv/bin/python -m pytest test/general -k "not Webhost"  # AP's generic suites, all worlds
```

## Build

Use the Launcher's "Build APWorlds" component. From the command line:

```sh
.venv/bin/python Launcher.py "Build APWorlds" -- "Rail Route"
```

This writes `build/apworlds/rail_route.apworld`, and it opens that folder when it's done.

## Generate a seed

Put a player YAML in a scratch folder and point `Generate.py` at it:

```yaml
name: Player
game: Rail Route
Rail Route:
  map: haarlem          # haarlem, prague or amsterdam
  goal: endless_complete
  expect_delays: false  # true if you own Expect Delays and enable it in the level's start settings
  happy_passengers: false
```

```sh
.venv/bin/python Generate.py --player_files_path <scratch>/Players --outputpath <scratch>/output --spoiler 2
```

## Location names (the contract with the client)

The client maps each game upgrade `Id` to its location name and resolves the IDs through the datapackage. So these names must match the client's table exactly. The source of truth is `rail_route/data.py`, and `test/test_ids.py` pins the IDs.

The two Custom Contracts slots share one location, because the game never shows both.

| Game `Id` | Location name | Location ID | Colour / tier | DLC |
|---|---|---|---|---|
| `autoblock` | Autoblocks | 1000 | Green T1 |  |
| `auto_accept` | Auto-accept Trains | 1001 | Green T1 |  |
| `automatic_routing` | Automatic Routing | 1002 | Green T1 |  |
| `perpetual_circuit` | Perpetual Circuit | 1003 | Green T1 |  |
| `auto_reverse` | Auto-reverse Trains | 1004 | Green T1 |  |
| `manual_signal_route_preview` | Manual Signal Route Preview | 1005 | Green T1 |  |
| `manual_signal_security` | Signalling Safety | 1006 | Green T1 |  |
| `platform_management` | Platform Adjustments | 1007 | Green T1 |  |
| `contract_management` | Timetable Adjustments | 1008 | Green T1 |  |
| `station_count1` | More Stations | 1009 | Green T1 |  |
| `train_alerts` | Train Alerts | 1010 | Green T1 |  |
| `relay_sensor` | Relay Sensor | 1011 | Green T1 |  |
| `custom_contracts_alt` | Custom Contracts | 1012 | Green T1 | Happy Passengers on |
| `maintenance_depot` | Maintenance Depot | 1013 | Green T1 | Expect Delays |
| `additional_service_capacity` | Additional Service Capacity | 1014 | Green T2 | Expect Delays |
| `field_efficiency` | Field Efficiency | 1015 | Green T3 | Expect Delays |
| `expanded_service_capacity` | Expanded Service Capacity | 1016 | Green T3 | Expect Delays |
| `track_speed1` | Basic Tracks | 1017 | Green T2 |  |
| `ic_contracts` | InterCities | 1018 | Green T2 |  |
| `more_offered_contracts1` | More Contract Offers | 1019 | Green T2 |  |
| `custom_contract_period` | Custom Contract Period | 1020 | Green T2 |  |
| `contract_windows` | Operating Hours | 1021 | Green T2 | Expect Delays |
| `station_count2` | Even More Stations | 1022 | Green T2 |  |
| `departure_sensor` | Departure Sensor | 1023 | Green T2 |  |
| `platform_sensor` | Arrival Sensor | 1024 | Green T2 |  |
| `track_speed2` | Advanced Tracks | 1025 | Green T2 |  |
| `waypoint` | Waypoints | 1026 | Green T2 |  |
| `routing_sensor` | Routing Sensor | 1027 | Green T3 |  |
| `auto_contract_manager_structural` | Structural Contracts Manager | 1028 | Green T3 |  |
| `auto_contract_manager_financial` | Financial Contracts Manager | 1029 | Green T3 |  |
| `auto_contract_manager_regional` | Regional Contracts Manager | 1030 | Green T3 |  |
| `more_offered_contracts2` | Even More Contract Offers | 1031 | Green T3 |  |
| `more_offered_contracts3` | Way More Contract Offers | 1032 | Green T3 |  |
| `station_count3` | Unlimited Stations | 1033 | Green T3 |  |
| `switch_speed` | Faster Switches | 1034 | Green T3 |  |
| `track_speed3` | Corridor Tracks | 1035 | Green T3 |  |
| `regional_contracts` | Regional Trains | 1036 | Red T1 |  |
| `onetime_contracts` | Freights | 1037 | Red T1 |  |
| `shunting_commands` | Shunting Commands | 1038 | Red T1 |  |
| `shunting_track` | Shunting Track | 1039 | Red T1 |  |
| `shunting_sensor` | Shunting Sensor | 1040 | Red T2 |  |
| `stabling_sensor` | Stabling Sensor | 1041 | Red T2 |  |
| `tunnels` | Tunnels | 1042 | Red T2 |  |
| `urban_contracts` | Urban Transit Contracts | 1043 | Red T2 |  |
| `loco_coupling` | Loco Coupling | 1044 | Red T2 |  |
| `advanced_arrival_sensor` | Advanced Arrival Sensor | 1045 | Red T2 |  |
| `service_automation` | Service Automation | 1046 | Red T2 | Expect Delays |
| `coach_yard` | Regional Trains Stabling | 1047 | Red T3 |  |
| `advanced_routing_sensor` | Advanced Routing Sensor | 1048 | Red T3 |  |
| `custom_contracts` | Custom Contracts | 1012 | Red T3 | Happy Passengers off |

## Item names (the contract with the client)

The client resolves each received item ID to its name through the datapackage, then maps the name to the game upgrade it grants. So these names must match the client's table (`client/src/Core/ItemNames.cs`) exactly, and `ItemNamesTests` fails when they differ. The source of truth is `rail_route/data.py`, and `test/test_ids.py` pins the IDs, which are listed here for readers and trackers.

- An upgrade item grants that one upgrade and has the same name as the upgrade's location. Custom Contracts grants whichever of its two variants the level shows; the client maps it to the first `Id` listed.
- A progressive item grants the next level of its chain, in the order listed. The client maps it to the chain's first `Id`.
- Filler items have no effect yet.

| Item name | Item ID | Game `Id`(s) | Kind |
|---|---|---|---|
| Autoblocks | 1 | `autoblock` | upgrade |
| Auto-accept Trains | 2 | `auto_accept` | upgrade |
| Automatic Routing | 3 | `automatic_routing` | upgrade |
| Perpetual Circuit | 4 | `perpetual_circuit` | upgrade |
| Auto-reverse Trains | 5 | `auto_reverse` | upgrade |
| Manual Signal Route Preview | 6 | `manual_signal_route_preview` | upgrade |
| Signalling Safety | 7 | `manual_signal_security` | upgrade |
| Platform Adjustments | 8 | `platform_management` | upgrade |
| Timetable Adjustments | 9 | `contract_management` | upgrade |
| Train Alerts | 10 | `train_alerts` | upgrade |
| Relay Sensor | 11 | `relay_sensor` | upgrade |
| Custom Contracts | 12 | `custom_contracts`, `custom_contracts_alt` | upgrade |
| Maintenance Depot | 13 | `maintenance_depot` | upgrade |
| Additional Service Capacity | 14 | `additional_service_capacity` | upgrade |
| Field Efficiency | 15 | `field_efficiency` | upgrade |
| Expanded Service Capacity | 16 | `expanded_service_capacity` | upgrade |
| InterCities | 17 | `ic_contracts` | upgrade |
| Custom Contract Period | 18 | `custom_contract_period` | upgrade |
| Operating Hours | 19 | `contract_windows` | upgrade |
| Departure Sensor | 20 | `departure_sensor` | upgrade |
| Arrival Sensor | 21 | `platform_sensor` | upgrade |
| Waypoints | 22 | `waypoint` | upgrade |
| Routing Sensor | 23 | `routing_sensor` | upgrade |
| Structural Contracts Manager | 24 | `auto_contract_manager_structural` | upgrade |
| Financial Contracts Manager | 25 | `auto_contract_manager_financial` | upgrade |
| Regional Contracts Manager | 26 | `auto_contract_manager_regional` | upgrade |
| Faster Switches | 27 | `switch_speed` | upgrade |
| Regional Trains | 28 | `regional_contracts` | upgrade |
| Freights | 29 | `onetime_contracts` | upgrade |
| Shunting Commands | 30 | `shunting_commands` | upgrade |
| Shunting Track | 31 | `shunting_track` | upgrade |
| Shunting Sensor | 32 | `shunting_sensor` | upgrade |
| Stabling Sensor | 33 | `stabling_sensor` | upgrade |
| Tunnels | 34 | `tunnels` | upgrade |
| Urban Transit Contracts | 35 | `urban_contracts` | upgrade |
| Loco Coupling | 36 | `loco_coupling` | upgrade |
| Advanced Arrival Sensor | 37 | `advanced_arrival_sensor` | upgrade |
| Service Automation | 38 | `service_automation` | upgrade |
| Regional Trains Stabling | 39 | `coach_yard` | upgrade |
| Advanced Routing Sensor | 40 | `advanced_routing_sensor` | upgrade |
| Progressive Track Speed | 100 | `track_speed1`, `track_speed2`, `track_speed3` | progressive |
| Progressive Station Count | 101 | `station_count1`, `station_count2`, `station_count3` | progressive |
| Progressive Contract Offers | 102 | `more_offered_contracts1`, `more_offered_contracts2`, `more_offered_contracts3` | progressive |
| Green XP Bundle | 500 | — | filler |
