from collections.abc import Callable, Mapping
from typing import TYPE_CHECKING, NamedTuple

from BaseClasses import CollectionState, ItemClassification, Region

from .data import GREEN, RED, VICTORY, Upgrade
from .items import RED_INCOME, THROUGHPUT, RailRouteItem
from .locations import RailRouteLocation
from .options import Map

if TYPE_CHECKING:
    from . import RailRouteWorld

Rule = Callable[[CollectionState], bool]


class TierGates(NamedTuple):
    """Throughput-item counts needed per map. Initial estimates, to be tuned by playtesting."""
    green_t2: int
    green_t3: int
    goal: int


TIER_GATES: dict[int, TierGates] = {
    Map.option_haarlem: TierGates(green_t2=2, green_t3=4, goal=7),
    Map.option_prague: TierGates(green_t2=3, green_t3=6, goal=9),
    Map.option_amsterdam: TierGates(green_t2=4, green_t3=8, goal=11),
}

MENU = "Menu"
GREEN_T1 = f"{GREEN} T1"
GREEN_T2 = f"{GREEN} T2"
GREEN_T3 = f"{GREEN} T3"
RED_T1 = f"{RED} T1"
RED_T2 = f"{RED} T2"
RED_T3 = f"{RED} T3"
GOAL = "Goal"


def always(_state: CollectionState) -> bool:
    return True


def entrance_rules(player: int, gates: TierGates) -> list[tuple[str, str, Rule]]:
    """The region graph as (source, target, rule). Every region has exactly one way in."""
    def throughput(count: int) -> Rule:
        return lambda state: state.count_from_list(THROUGHPUT, player) >= count

    def all_of(*rules: Rule) -> Rule:
        return lambda state: all(rule(state) for rule in rules)

    any_red_income: Rule = lambda state: state.has_any(RED_INCOME, player)
    all_red_income: Rule = lambda state: state.has_all(RED_INCOME, player)
    return [
        (MENU, GREEN_T1, always),
        (GREEN_T1, GREEN_T2, throughput(gates.green_t2)),
        (GREEN_T2, GREEN_T3, throughput(gates.green_t3)),
        (GREEN_T3, GOAL, throughput(gates.goal)),
        (GREEN_T1, RED_T1, all_of(throughput(gates.green_t2), any_red_income)),
        (RED_T1, RED_T2, always),
        (RED_T2, RED_T3, all_of(throughput(gates.green_t3), all_red_income)),
    ]


def region_reach_rules(connections: list[tuple[str, str, Rule]]) -> dict[str, Rule]:
    """The full item condition for reaching each region from Menu, composed along its entrance chain."""
    reach: dict[str, Rule] = {MENU: always}

    def chain(upstream: Rule, rule: Rule) -> Rule:
        return lambda state: upstream(state) and rule(state)

    for source, target, rule in connections:
        reach[target] = chain(reach[source], rule)
    return reach


def slot_rules(upgrades: list[Upgrade], reach: Mapping[str, Rule]) -> dict[str, Rule]:
    """The full item condition for buying each slot: its own region, plus its parent slot's full condition.

    Parent ordering is composed from item rules rather than `can_reach_location`, so it stays cheap and cache-safe.
    """
    by_id = {upgrade.game_id: upgrade for upgrade in upgrades}
    rules: dict[str, Rule] = {}

    def both(first: Rule, second: Rule) -> Rule:
        return lambda state: first(state) and second(state)

    def rule_for(upgrade: Upgrade) -> Rule:
        if upgrade.game_id not in rules:
            own = reach[upgrade.region]
            if upgrade.parent is None:
                rules[upgrade.game_id] = own
            else:
                rules[upgrade.game_id] = both(own, rule_for(by_id[upgrade.parent]))
        return rules[upgrade.game_id]

    for upgrade in upgrades:
        rule_for(upgrade)
    return rules


def parent_rule(upgrade: Upgrade, rules: Mapping[str, Rule]) -> Rule | None:
    """The location rule on top of the entrance rules: the parent slot's full condition, if there is a parent."""
    return None if upgrade.parent is None else rules[upgrade.parent]


def create_regions(world: "RailRouteWorld") -> None:
    player = world.player
    multiworld = world.multiworld
    gates = TIER_GATES[world.options.map.value]
    connections = entrance_rules(player, gates)

    regions = {MENU: Region(MENU, player, multiworld)}
    for _source, target, _rule in connections:
        regions[target] = Region(target, player, multiworld)
    for source, target, rule in connections:
        regions[source].connect(regions[target], f"{source} -> {target}", rule)
    multiworld.regions.extend(regions.values())

    for upgrade in world.upgrades:
        region = regions[upgrade.region]
        region.locations.append(RailRouteLocation(player, upgrade.title, upgrade.location_id, region))

    goal = regions[GOAL]
    victory = RailRouteLocation(player, VICTORY, None, goal)
    victory.place_locked_item(RailRouteItem(VICTORY, ItemClassification.progression, None, player))
    goal.locations.append(victory)


def set_rules(world: "RailRouteWorld") -> None:
    player = world.player
    gates = TIER_GATES[world.options.map.value]
    reach = region_reach_rules(entrance_rules(player, gates))
    rules = slot_rules(world.upgrades, reach)
    for upgrade in world.upgrades:
        rule = parent_rule(upgrade, rules)
        if rule is not None:
            world.get_location(upgrade.title).access_rule = rule

    world.multiworld.completion_condition[player] = lambda state: state.has(VICTORY, player)
