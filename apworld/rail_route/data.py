"""The single source of truth for Rail Route's upgrades, items and locations.

Rows come from `ResearchController.researchItems` in the decompiled game (3.0.18), in game order.
IDs are hand-assigned and must never change once released; `test/test_ids.py` pins them.
"""
from typing import NamedTuple

from BaseClasses import ItemClassification

GAME = "Rail Route"

GREEN = "Green"
RED = "Red"

# DLC conditions: which option setting makes the game show an upgrade.
ED = "ED"  # only with Expect Delays enabled
HP_ON = "HP_ON"  # only with Happy Passengers enabled
HP_OFF = "HP_OFF"  # only with Happy Passengers disabled

PROGRESSION = ItemClassification.progression
USEFUL = ItemClassification.useful
FILLER = ItemClassification.filler

PROGRESSIVE_TRACK_SPEED = "Progressive Track Speed"
PROGRESSIVE_STATION_COUNT = "Progressive Station Count"
PROGRESSIVE_CONTRACT_OFFERS = "Progressive Contract Offers"

PROGRESSIVE_ITEMS: dict[str, int] = {
    PROGRESSIVE_TRACK_SPEED: 100,
    PROGRESSIVE_STATION_COUNT: 101,
    PROGRESSIVE_CONTRACT_OFFERS: 102,
}

FILLER_ITEM = "Green XP Bundle"
FILLER_ITEMS: dict[str, int] = {
    FILLER_ITEM: 500,
}

VICTORY = "Endless Complete"


class Upgrade(NamedTuple):
    game_id: str
    title: str
    colour: str
    tier: int
    parent: str | None
    location_id: int
    item: str
    item_id: int
    classification: ItemClassification
    dlc: str | None

    @property
    def region(self) -> str:
        return f"{self.colour} T{self.tier}"


U = Upgrade
UPGRADES: tuple[Upgrade, ...] = (
    U("autoblock", "Autoblocks", GREEN, 1, None, 1000, "Autoblocks", 1, PROGRESSION, None),
    U("auto_accept", "Auto-accept Trains", GREEN, 1, None, 1001, "Auto-accept Trains", 2, USEFUL, None),
    U("automatic_routing", "Automatic Routing", GREEN, 1, None, 1002, "Automatic Routing", 3, PROGRESSION, None),
    U("perpetual_circuit", "Perpetual Circuit", GREEN, 1, "automatic_routing", 1003,
      "Perpetual Circuit", 4, USEFUL, None),
    U("auto_reverse", "Auto-reverse Trains", GREEN, 1, None, 1004, "Auto-reverse Trains", 5, USEFUL, None),
    U("manual_signal_route_preview", "Manual Signal Route Preview", GREEN, 1, None, 1005,
      "Manual Signal Route Preview", 6, USEFUL, None),
    U("manual_signal_security", "Signalling Safety", GREEN, 1, None, 1006, "Signalling Safety", 7, USEFUL, None),
    U("platform_management", "Platform Adjustments", GREEN, 1, None, 1007, "Platform Adjustments", 8, USEFUL, None),
    U("contract_management", "Timetable Adjustments", GREEN, 1, None, 1008,
      "Timetable Adjustments", 9, USEFUL, None),
    U("station_count1", "More Stations", GREEN, 1, None, 1009, PROGRESSIVE_STATION_COUNT, 101, PROGRESSION, None),
    U("train_alerts", "Train Alerts", GREEN, 1, None, 1010, "Train Alerts", 10, USEFUL, None),
    U("relay_sensor", "Relay Sensor", GREEN, 1, "automatic_routing", 1011, "Relay Sensor", 11, USEFUL, None),
    U("custom_contracts_alt", "Custom Contracts", GREEN, 1, None, 1012, "Custom Contracts", 12, USEFUL, HP_ON),
    U("maintenance_depot", "Maintenance Depot", GREEN, 1, None, 1013, "Maintenance Depot", 13, USEFUL, ED),
    U("additional_service_capacity", "Additional Service Capacity", GREEN, 2, "maintenance_depot", 1014,
      "Additional Service Capacity", 14, USEFUL, ED),
    U("field_efficiency", "Field Efficiency", GREEN, 3, "maintenance_depot", 1015,
      "Field Efficiency", 15, USEFUL, ED),
    U("expanded_service_capacity", "Expanded Service Capacity", GREEN, 3, "additional_service_capacity", 1016,
      "Expanded Service Capacity", 16, USEFUL, ED),
    U("track_speed1", "Basic Tracks", GREEN, 2, None, 1017, PROGRESSIVE_TRACK_SPEED, 100, PROGRESSION, None),
    U("ic_contracts", "InterCities", GREEN, 2, None, 1018, "InterCities", 17, PROGRESSION, None),
    U("more_offered_contracts1", "More Contract Offers", GREEN, 2, None, 1019,
      PROGRESSIVE_CONTRACT_OFFERS, 102, PROGRESSION, None),
    U("custom_contract_period", "Custom Contract Period", GREEN, 2, None, 1020,
      "Custom Contract Period", 18, USEFUL, None),
    U("contract_windows", "Operating Hours", GREEN, 2, "custom_contract_period", 1021,
      "Operating Hours", 19, USEFUL, ED),
    U("station_count2", "Even More Stations", GREEN, 2, "station_count1", 1022,
      PROGRESSIVE_STATION_COUNT, 101, PROGRESSION, None),
    U("departure_sensor", "Departure Sensor", GREEN, 2, "automatic_routing", 1023,
      "Departure Sensor", 20, USEFUL, None),
    U("platform_sensor", "Arrival Sensor", GREEN, 2, "automatic_routing", 1024, "Arrival Sensor", 21, USEFUL, None),
    U("track_speed2", "Advanced Tracks", GREEN, 2, "track_speed1", 1025,
      PROGRESSIVE_TRACK_SPEED, 100, PROGRESSION, None),
    U("waypoint", "Waypoints", GREEN, 2, None, 1026, "Waypoints", 22, USEFUL, None),
    U("routing_sensor", "Routing Sensor", GREEN, 3, "automatic_routing", 1027, "Routing Sensor", 23, USEFUL, None),
    U("auto_contract_manager_structural", "Structural Contracts Manager", GREEN, 3, None, 1028,
      "Structural Contracts Manager", 24, USEFUL, None),
    U("auto_contract_manager_financial", "Financial Contracts Manager", GREEN, 3, None, 1029,
      "Financial Contracts Manager", 25, USEFUL, None),
    U("auto_contract_manager_regional", "Regional Contracts Manager", GREEN, 3, None, 1030,
      "Regional Contracts Manager", 26, USEFUL, None),
    U("more_offered_contracts2", "Even More Contract Offers", GREEN, 3, "more_offered_contracts1", 1031,
      PROGRESSIVE_CONTRACT_OFFERS, 102, PROGRESSION, None),
    U("more_offered_contracts3", "Way More Contract Offers", GREEN, 3, "more_offered_contracts2", 1032,
      PROGRESSIVE_CONTRACT_OFFERS, 102, PROGRESSION, None),
    U("station_count3", "Unlimited Stations", GREEN, 3, "station_count2", 1033,
      PROGRESSIVE_STATION_COUNT, 101, PROGRESSION, None),
    U("switch_speed", "Faster Switches", GREEN, 3, None, 1034, "Faster Switches", 27, USEFUL, None),
    U("track_speed3", "Corridor Tracks", GREEN, 3, "track_speed2", 1035,
      PROGRESSIVE_TRACK_SPEED, 100, PROGRESSION, None),
    U("regional_contracts", "Regional Trains", RED, 1, None, 1036, "Regional Trains", 28, PROGRESSION, None),
    U("onetime_contracts", "Freights", RED, 1, None, 1037, "Freights", 29, PROGRESSION, None),
    U("shunting_commands", "Shunting Commands", RED, 1, None, 1038, "Shunting Commands", 30, USEFUL, None),
    U("shunting_track", "Shunting Track", RED, 1, "shunting_commands", 1039, "Shunting Track", 31, USEFUL, None),
    U("shunting_sensor", "Shunting Sensor", RED, 2, "shunting_commands", 1040, "Shunting Sensor", 32, USEFUL, None),
    U("stabling_sensor", "Stabling Sensor", RED, 2, "shunting_commands", 1041, "Stabling Sensor", 33, USEFUL, None),
    U("tunnels", "Tunnels", RED, 2, None, 1042, "Tunnels", 34, USEFUL, None),
    U("urban_contracts", "Urban Transit Contracts", RED, 2, "regional_contracts", 1043,
      "Urban Transit Contracts", 35, USEFUL, None),
    U("loco_coupling", "Loco Coupling", RED, 2, "shunting_sensor", 1044, "Loco Coupling", 36, USEFUL, None),
    U("advanced_arrival_sensor", "Advanced Arrival Sensor", RED, 2, None, 1045,
      "Advanced Arrival Sensor", 37, USEFUL, None),
    U("service_automation", "Service Automation", RED, 2, "maintenance_depot", 1046,
      "Service Automation", 38, USEFUL, ED),
    U("coach_yard", "Regional Trains Stabling", RED, 3, None, 1047, "Regional Trains Stabling", 39, USEFUL, None),
    U("advanced_routing_sensor", "Advanced Routing Sensor", RED, 3, None, 1048,
      "Advanced Routing Sensor", 40, USEFUL, None),
    U("custom_contracts", "Custom Contracts", RED, 3, None, 1012, "Custom Contracts", 12, USEFUL, HP_OFF),
)
del U

UPGRADES_BY_ID: dict[str, Upgrade] = {upgrade.game_id: upgrade for upgrade in UPGRADES}


def is_active(upgrade: Upgrade, expect_delays: bool, happy_passengers: bool) -> bool:
    """Whether the game shows this upgrade's slot for the given DLC settings."""
    if upgrade.dlc == ED:
        return expect_delays
    if upgrade.dlc == HP_ON:
        return happy_passengers
    if upgrade.dlc == HP_OFF:
        return not happy_passengers
    return True


def active_upgrades(expect_delays: bool, happy_passengers: bool) -> list[Upgrade]:
    return [upgrade for upgrade in UPGRADES if is_active(upgrade, expect_delays, happy_passengers)]
