import unittest
from collections import Counter

from ..data import FILLER_ITEMS, PROGRESSIVE_ITEMS, UPGRADES, UPGRADES_BY_ID
from ..items import item_name_to_id
from ..locations import location_name_to_id


class TestDataTable(unittest.TestCase):
    def test_upgrade_rows(self) -> None:
        self.assertEqual(len(UPGRADES), 50)
        self.assertEqual(len(UPGRADES_BY_ID), 50, "duplicate game ids")

    def test_locations(self) -> None:
        self.assertEqual(len(location_name_to_id), 49)
        self.assertEqual(len(set(location_name_to_id.values())), 49, "two locations share an ID")
        for name, location_id in location_name_to_id.items():
            self.assertIn(location_id, range(1000, 1100), name)

    def test_each_title_has_one_location_id(self) -> None:
        ids_per_title: dict[str, set[int]] = {}
        for upgrade in UPGRADES:
            ids_per_title.setdefault(upgrade.title, set()).add(upgrade.location_id)
        for title, ids in ids_per_title.items():
            self.assertEqual(len(ids), 1, title)

    def test_items(self) -> None:
        self.assertEqual(len(item_name_to_id), 44)
        self.assertEqual(len(set(item_name_to_id.values())), 44, "two items share an ID")
        singles = {name: item_id for name, item_id in item_name_to_id.items()
                   if name not in PROGRESSIVE_ITEMS and name not in FILLER_ITEMS}
        self.assertEqual(len(singles), 40)
        for name, item_id in singles.items():
            self.assertIn(item_id, range(1, 100), name)
        for name, item_id in PROGRESSIVE_ITEMS.items():
            self.assertIn(item_id, range(100, 200), name)
        for name, item_id in FILLER_ITEMS.items():
            self.assertIn(item_id, range(500, 600), name)

    def test_each_item_name_has_one_id(self) -> None:
        for upgrade in UPGRADES:
            self.assertEqual(item_name_to_id[upgrade.item], upgrade.item_id, upgrade.game_id)

    def test_progressive_items_have_three_slots(self) -> None:
        counts = Counter(upgrade.item for upgrade in UPGRADES)
        for name in PROGRESSIVE_ITEMS:
            self.assertEqual(counts[name], 3, name)

    def test_parents_exist(self) -> None:
        for upgrade in UPGRADES:
            if upgrade.parent is not None:
                self.assertIn(upgrade.parent, UPGRADES_BY_ID, upgrade.game_id)
