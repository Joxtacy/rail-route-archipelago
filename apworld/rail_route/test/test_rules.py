from BaseClasses import CollectionState

from ..data import GREEN, RED, VICTORY, Upgrade
from ..items import THROUGHPUT
from ..rules import TIER_GATES, entrance_rules, region_reach_rules, slot_rules
from .bases import RailRouteTestBase

# One copy of each throughput item in turn, so any prefix is a valid pool subset.
THROUGHPUT_ORDER = [name for _ in range(3) for name in THROUGHPUT]


class RulesTestMixin:
    run_default_tests = False

    @property
    def gates(self):
        return TIER_GATES[self.world.options.map.value]

    def collect_throughput(self, count: int) -> None:
        pool = list(self.multiworld.itempool)
        for name in THROUGHPUT_ORDER:
            if count == 0:
                break
            item = next((item for item in pool if item.name == name), None)
            if item is not None:
                pool.remove(item)
                self.collect(item)
                count -= 1
        self.assertEqual(count, 0, "not enough throughput items in the pool")

    def test_green_t1_in_sphere_1(self) -> None:
        self.assertTrue(self.can_reach_location("Autoblocks"))

    def test_green_t2_gate(self) -> None:
        self.collect_throughput(self.gates.green_t2 - 1)
        self.assertFalse(self.can_reach_location("Waypoints"))
        self.collect_throughput(1)
        self.assertTrue(self.can_reach_location("Waypoints"))

    def test_green_t3_gate(self) -> None:
        self.collect_throughput(self.gates.green_t3 - 1)
        self.assertFalse(self.can_reach_location("Faster Switches"))
        self.collect_throughput(1)
        self.assertTrue(self.can_reach_location("Faster Switches"))

    def test_red_needs_income(self) -> None:
        self.collect_throughput(self.gates.green_t2)
        self.assertFalse(self.can_reach_location("Shunting Commands"))
        self.collect_by_name("Freights")
        self.assertTrue(self.can_reach_location("Shunting Commands"))

    def test_red_t3_needs_both_income_items_and_green_t3(self) -> None:
        self.collect_throughput(self.gates.green_t3)
        self.collect_by_name("Freights")
        self.assertFalse(self.can_reach_location("Advanced Routing Sensor"))
        self.collect_by_name("Regional Trains")
        self.assertTrue(self.can_reach_location("Advanced Routing Sensor"))

    def test_red_t3_needs_green_t3(self) -> None:
        self.collect_throughput(self.gates.green_t3 - 1)
        self.collect_by_name(["Freights", "Regional Trains"])
        self.assertFalse(self.can_reach_location("Advanced Routing Sensor"))

    def test_goal(self) -> None:
        self.collect_throughput(self.gates.goal - 1)
        self.assertFalse(self.can_reach_location(VICTORY))
        self.assertBeatable(False)
        self.collect_throughput(1)
        self.assertTrue(self.can_reach_location(VICTORY))
        self.assertBeatable(True)

    def test_gates_are_ordered(self) -> None:
        gates = self.gates
        self.assertLessEqual(gates.green_t2, gates.green_t3)
        self.assertLessEqual(gates.green_t3, gates.goal)
        self.assertLessEqual(gates.goal, len(THROUGHPUT_ORDER))


class TestRulesHaarlem(RulesTestMixin, RailRouteTestBase):
    options = {"map": "haarlem"}


class TestRulesPrague(RulesTestMixin, RailRouteTestBase):
    options = {"map": "prague"}


class TestRulesAmsterdam(RulesTestMixin, RailRouteTestBase):
    options = {"map": "amsterdam"}


class TestGateTable(RailRouteTestBase):
    run_default_tests = False

    def test_harder_maps_need_at_least_as_much(self) -> None:
        maps = [TIER_GATES[key] for key in sorted(TIER_GATES)]
        for easier, harder in zip(maps, maps[1:]):
            for field in easier._fields:
                self.assertLessEqual(getattr(easier, field), getattr(harder, field), field)


class TestParentOrdering(RailRouteTestBase):
    """A child slot is reachable only when its parent slot is, whatever items the player holds."""
    options = {"expect_delays": True, "map": "amsterdam"}
    run_default_tests = False

    PAIRS = [
        ("Perpetual Circuit", "Automatic Routing"),
        ("Corridor Tracks", "Advanced Tracks"),
        ("Advanced Tracks", "Basic Tracks"),
        ("Service Automation", "Maintenance Depot"),
        ("Urban Transit Contracts", "Regional Trains"),
    ]

    def assert_children_imply_parents(self) -> None:
        for child, parent in self.PAIRS:
            if self.can_reach_location(child):
                self.assertTrue(self.can_reach_location(parent), f"{child} reachable without {parent}")

    def test_implication_as_items_arrive(self) -> None:
        self.assert_children_imply_parents()
        for item in list(self.multiworld.itempool):
            self.collect(item)
            self.assert_children_imply_parents()

    def test_parent_rules_are_composed(self) -> None:
        # Every parent in the current table sits in a region the child's region already implies, so check the
        # composition on a synthetic child whose parent needs more than the child's own region.
        player = self.player
        reach = region_reach_rules(entrance_rules(player, self.gates))
        parent = Upgrade("p", "Parent", GREEN, 3, None, 0, "", 0, None, None)
        child = Upgrade("c", "Child", RED, 1, "p", 0, "", 0, None, None)
        grandchild = Upgrade("g", "Grandchild", RED, 1, "c", 0, "", 0, None, None)
        rules = slot_rules([parent, child, grandchild], reach)

        state = CollectionState(self.multiworld)
        for item in self.get_items_by_name(["Freights"]):
            state.collect(item)
        for name in THROUGHPUT_ORDER[:self.gates.green_t2]:
            state.collect(self.world.create_item(name))
        self.assertTrue(reach["Red T1"](state))
        self.assertFalse(rules["c"](state))
        self.assertFalse(rules["g"](state))
        for name in THROUGHPUT_ORDER[self.gates.green_t2:self.gates.green_t3]:
            state.collect(self.world.create_item(name))
        self.assertTrue(rules["c"](state))
        self.assertTrue(rules["g"](state))

    @property
    def gates(self):
        return TIER_GATES[self.world.options.map.value]
