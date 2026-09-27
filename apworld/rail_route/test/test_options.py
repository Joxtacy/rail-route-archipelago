from ..options import Map
from .bases import RailRouteTestBase


class TestDefaultOptions(RailRouteTestBase):
    run_default_tests = False

    def test_defaults(self) -> None:
        self.assertEqual(self.world.options.map.current_key, "haarlem")
        self.assertEqual(self.world.options.goal.current_key, "endless_complete")
        self.assertFalse(self.world.options.expect_delays)
        self.assertFalse(self.world.options.happy_passengers)

    def test_invalid_map_is_an_option_error(self) -> None:
        with self.assertRaises(KeyError) as raised:
            Map.from_any("berlin")
        self.assertIn("berlin", str(raised.exception))


class TestPragueExpectDelaysSlotData(RailRouteTestBase):
    options = {"map": "prague", "expect_delays": True}
    run_default_tests = False

    def test_slot_data(self) -> None:
        self.assertEqual(dict(self.world.fill_slot_data()), {
            "map": "prague",
            "goal": "endless_complete",
            "expect_delays": True,
            "happy_passengers": False,
        })
