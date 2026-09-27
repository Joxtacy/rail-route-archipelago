import unittest

from .. import RailRouteWorld

# Frozen copies of the released IDs. The client resolves IDs by these names, so an entry here may only be removed
# or changed deliberately. New entries can be added to the world freely; add them here once released.
FROZEN_ITEMS: dict[str, int] = {
    "Autoblocks": 1,
    "Auto-accept Trains": 2,
    "Automatic Routing": 3,
    "Perpetual Circuit": 4,
    "Auto-reverse Trains": 5,
    "Manual Signal Route Preview": 6,
    "Signalling Safety": 7,
    "Platform Adjustments": 8,
    "Timetable Adjustments": 9,
    "Train Alerts": 10,
    "Relay Sensor": 11,
    "Custom Contracts": 12,
    "Maintenance Depot": 13,
    "Additional Service Capacity": 14,
    "Field Efficiency": 15,
    "Expanded Service Capacity": 16,
    "InterCities": 17,
    "Custom Contract Period": 18,
    "Operating Hours": 19,
    "Departure Sensor": 20,
    "Arrival Sensor": 21,
    "Waypoints": 22,
    "Routing Sensor": 23,
    "Structural Contracts Manager": 24,
    "Financial Contracts Manager": 25,
    "Regional Contracts Manager": 26,
    "Faster Switches": 27,
    "Regional Trains": 28,
    "Freights": 29,
    "Shunting Commands": 30,
    "Shunting Track": 31,
    "Shunting Sensor": 32,
    "Stabling Sensor": 33,
    "Tunnels": 34,
    "Urban Transit Contracts": 35,
    "Loco Coupling": 36,
    "Advanced Arrival Sensor": 37,
    "Service Automation": 38,
    "Regional Trains Stabling": 39,
    "Advanced Routing Sensor": 40,
    "Progressive Track Speed": 100,
    "Progressive Station Count": 101,
    "Progressive Contract Offers": 102,
    "Green XP Bundle": 500,
}

FROZEN_LOCATIONS: dict[str, int] = {
    "Autoblocks": 1000,
    "Auto-accept Trains": 1001,
    "Automatic Routing": 1002,
    "Perpetual Circuit": 1003,
    "Auto-reverse Trains": 1004,
    "Manual Signal Route Preview": 1005,
    "Signalling Safety": 1006,
    "Platform Adjustments": 1007,
    "Timetable Adjustments": 1008,
    "More Stations": 1009,
    "Train Alerts": 1010,
    "Relay Sensor": 1011,
    "Custom Contracts": 1012,
    "Maintenance Depot": 1013,
    "Additional Service Capacity": 1014,
    "Field Efficiency": 1015,
    "Expanded Service Capacity": 1016,
    "Basic Tracks": 1017,
    "InterCities": 1018,
    "More Contract Offers": 1019,
    "Custom Contract Period": 1020,
    "Operating Hours": 1021,
    "Even More Stations": 1022,
    "Departure Sensor": 1023,
    "Arrival Sensor": 1024,
    "Advanced Tracks": 1025,
    "Waypoints": 1026,
    "Routing Sensor": 1027,
    "Structural Contracts Manager": 1028,
    "Financial Contracts Manager": 1029,
    "Regional Contracts Manager": 1030,
    "Even More Contract Offers": 1031,
    "Way More Contract Offers": 1032,
    "Unlimited Stations": 1033,
    "Faster Switches": 1034,
    "Corridor Tracks": 1035,
    "Regional Trains": 1036,
    "Freights": 1037,
    "Shunting Commands": 1038,
    "Shunting Track": 1039,
    "Shunting Sensor": 1040,
    "Stabling Sensor": 1041,
    "Tunnels": 1042,
    "Urban Transit Contracts": 1043,
    "Loco Coupling": 1044,
    "Advanced Arrival Sensor": 1045,
    "Service Automation": 1046,
    "Regional Trains Stabling": 1047,
    "Advanced Routing Sensor": 1048,
}


class TestStableIds(unittest.TestCase):
    def test_item_ids_unchanged(self) -> None:
        for name, item_id in FROZEN_ITEMS.items():
            with self.subTest(item=name):
                self.assertEqual(RailRouteWorld.item_name_to_id.get(name), item_id)

    def test_location_ids_unchanged(self) -> None:
        for name, location_id in FROZEN_LOCATIONS.items():
            with self.subTest(location=name):
                self.assertEqual(RailRouteWorld.location_name_to_id.get(name), location_id)
