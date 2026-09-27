from dataclasses import dataclass

from Options import Choice, PerGameCommonOptions, Toggle


class Map(Choice):
    """The Endless map you will play. The seed's logic is tuned for it."""
    display_name = "Map"
    option_haarlem = 0
    option_prague = 1
    option_amsterdam = 2
    default = option_haarlem


class Goal(Choice):
    """What completes your world.

    - Endless Complete: earn the Endless-complete star on the chosen map.
    """
    display_name = "Goal"
    option_endless_complete = 0
    default = option_endless_complete


class ExpectDelays(Toggle):
    """You own the Expect Delays DLC and will enable it in the level's start settings.
    Adds its upgrades to the seed."""
    display_name = "Expect Delays"


class HappyPassengers(Toggle):
    """You own the Happy Passengers DLC and will enable it in the level's start settings.
    Moves Custom Contracts from Red tier 3 to Green tier 1, as the game does."""
    display_name = "Happy Passengers"


# The game's level UUID for each map option, sent to the client in slot data.
LEVEL_UUIDS: dict[int, str] = {
    Map.option_haarlem: "Haarlem",
    Map.option_prague: "prague",
    Map.option_amsterdam: "Amsterdam",
}


@dataclass
class RailRouteOptions(PerGameCommonOptions):
    map: Map
    goal: Goal
    expect_delays: ExpectDelays
    happy_passengers: HappyPassengers
