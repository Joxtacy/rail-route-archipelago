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
