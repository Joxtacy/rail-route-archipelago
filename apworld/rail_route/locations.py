from BaseClasses import Location

from .data import GAME, UPGRADES


class RailRouteLocation(Location):
    game = GAME


location_name_to_id: dict[str, int] = {upgrade.title: upgrade.location_id for upgrade in UPGRADES}
