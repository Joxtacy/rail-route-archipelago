from collections.abc import Mapping
from typing import Any, ClassVar

from worlds.AutoWorld import World

from . import rules
from .data import FILLER_ITEM, GAME, Upgrade, active_upgrades
from .items import RailRouteItem, item_classification, item_name_groups, item_name_to_id
from .locations import RailRouteLocation, location_name_to_id
from .options import LEVEL_UUIDS, RailRouteOptions

__all__ = ["RailRouteItem", "RailRouteLocation", "RailRouteWorld"]


class RailRouteWorld(World):
    """Rail Route is a train dispatching game about building and automating a rail network."""

    game = GAME
    options_dataclass = RailRouteOptions
    options: RailRouteOptions

    item_name_to_id: ClassVar[dict[str, int]] = item_name_to_id
    location_name_to_id: ClassVar[dict[str, int]] = location_name_to_id
    item_name_groups: ClassVar[dict[str, set[str]]] = item_name_groups

    upgrades: list[Upgrade]
    """The upgrade slots the game shows for this slot's DLC options."""

    def generate_early(self) -> None:
        self.upgrades = active_upgrades(bool(self.options.expect_delays), bool(self.options.happy_passengers))

    def create_regions(self) -> None:
        rules.create_regions(self)

    def create_items(self) -> None:
        # One item per active slot; the levelled slots each contribute one copy of their progressive item.
        self.multiworld.itempool += [self.create_item(upgrade.item) for upgrade in self.upgrades]

    def set_rules(self) -> None:
        rules.set_rules(self)

    def create_item(self, name: str) -> RailRouteItem:
        return RailRouteItem(name, item_classification[name], self.item_name_to_id[name], self.player)

    def get_filler_item_name(self) -> str:
        return FILLER_ITEM

    def fill_slot_data(self) -> Mapping[str, Any]:
        return {
            "map": LEVEL_UUIDS[self.options.map.value],
            "goal": self.options.goal.current_key,
            "expect_delays": bool(self.options.expect_delays),
            "happy_passengers": bool(self.options.happy_passengers),
        }
