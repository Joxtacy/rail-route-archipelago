from BaseClasses import Item, ItemClassification

from .data import (FILLER, FILLER_ITEMS, GAME, PROGRESSION, PROGRESSIVE_CONTRACT_OFFERS, PROGRESSIVE_ITEMS,
                   PROGRESSIVE_STATION_COUNT, PROGRESSIVE_TRACK_SPEED, UPGRADES)


class RailRouteItem(Item):
    game = GAME


item_name_to_id: dict[str, int] = {upgrade.item: upgrade.item_id for upgrade in UPGRADES}
item_name_to_id.update(PROGRESSIVE_ITEMS)
item_name_to_id.update(FILLER_ITEMS)

item_classification: dict[str, ItemClassification] = {upgrade.item: upgrade.classification for upgrade in UPGRADES}
item_classification.update({name: PROGRESSION for name in PROGRESSIVE_ITEMS})
item_classification.update({name: FILLER for name in FILLER_ITEMS})

# Progression items that raise points per cycle. The tier gates and the goal count these (3 copies per progressive).
THROUGHPUT: tuple[str, ...] = (
    PROGRESSIVE_TRACK_SPEED,
    PROGRESSIVE_STATION_COUNT,
    PROGRESSIVE_CONTRACT_OFFERS,
    "InterCities",
    "Automatic Routing",
    "Autoblocks",
)

# Upgrades that let the game generate contracts paying red points.
RED_INCOME: tuple[str, ...] = (
    "Regional Trains",
    "Freights",
)

item_name_groups: dict[str, set[str]] = {
    "Throughput": set(THROUGHPUT),
    "Red Income": set(RED_INCOME),
}
