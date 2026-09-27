from ..data import FILLER_ITEM, PROGRESSIVE_ITEMS, VICTORY
from ..items import item_name_to_id
from ..locations import location_name_to_id
from .bases import RailRouteTestBase

EXPECT_DELAYS_ONLY = (
    "Maintenance Depot",
    "Additional Service Capacity",
    "Expanded Service Capacity",
    "Field Efficiency",
    "Service Automation",
    "Operating Hours",
)


class PoolTestMixin:
    expected_count: int

    def slot_locations(self):
        return [location for location in self.multiworld.get_locations(self.player) if location.name != VICTORY]

    def test_location_and_item_counts(self) -> None:
        self.assertEqual(len(self.slot_locations()), self.expected_count)
        self.assertEqual(len(self.multiworld.itempool), self.expected_count)

    def test_three_copies_per_progressive_item(self) -> None:
        for name in PROGRESSIVE_ITEMS:
            self.assertEqual(len(self.get_items_by_name(name)), 3, name)

    def test_no_levelled_single_items(self) -> None:
        pool_names = {item.name for item in self.multiworld.itempool}
        for name in ("Basic Tracks", "Advanced Tracks", "Corridor Tracks"):
            self.assertNotIn(name, pool_names)

    def test_no_filler(self) -> None:
        self.assertFalse(self.get_items_by_name(FILLER_ITEM))

    def test_id_tables_independent_of_options(self) -> None:
        self.assertEqual(self.world.item_name_to_id, item_name_to_id)
        self.assertEqual(self.world.location_name_to_id, location_name_to_id)

    def custom_contracts_region(self) -> str:
        return self.world.get_location("Custom Contracts").parent_region.name


class TestBaseGamePool(PoolTestMixin, RailRouteTestBase):
    expected_count = 43

    def test_custom_contracts_is_red_t3(self) -> None:
        self.assertEqual(self.custom_contracts_region(), "Red T3")

    def test_expect_delays_upgrades_absent(self) -> None:
        location_names = {location.name for location in self.slot_locations()}
        item_names = {item.name for item in self.multiworld.itempool}
        for name in EXPECT_DELAYS_ONLY:
            self.assertNotIn(name, location_names)
            self.assertNotIn(name, item_names)


class TestExpectDelaysPool(PoolTestMixin, RailRouteTestBase):
    options = {"expect_delays": True}
    run_default_tests = False
    expected_count = 49

    def test_expect_delays_upgrades_present(self) -> None:
        location_names = {location.name for location in self.slot_locations()}
        for name in EXPECT_DELAYS_ONLY:
            self.assertIn(name, location_names)
            self.assertTrue(self.get_items_by_name(name), name)


class TestHappyPassengersPool(PoolTestMixin, RailRouteTestBase):
    options = {"happy_passengers": True}
    run_default_tests = False
    expected_count = 43

    def test_custom_contracts_is_green_t1(self) -> None:
        self.assertEqual(self.custom_contracts_region(), "Green T1")
        self.assertEqual(len(self.get_items_by_name("Custom Contracts")), 1)
