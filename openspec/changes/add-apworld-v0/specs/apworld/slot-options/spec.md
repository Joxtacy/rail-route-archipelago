# Spec Delta

## Purpose

Defines the options a player sets in their Rail Route YAML and the slot data the world hands to the client mod, so the client can set up the matching game.

## ADDED Requirements

### Requirement: Map option
The world SHALL offer a `map` option with the values `haarlem`, `prague` and `amsterdam`, defaulting to `haarlem`. The chosen map SHALL be the Endless map the seed's logic is tuned for.

#### Scenario: Default map
- **WHEN** a player's YAML sets no `map`
- **THEN** the seed is generated for Haarlem

#### Scenario: Unsupported map
- **WHEN** a player's YAML sets `map` to a value outside the three supported maps
- **THEN** generation fails with an option error naming the invalid value

### Requirement: Goal option
The world SHALL offer a `goal` option whose only value in this version is `endless_complete`, which is also the default. With that goal, the player completes their world by earning the Endless-complete star on the chosen map. Later versions MAY add values without changing the meaning of existing ones.

#### Scenario: Default goal
- **WHEN** a player's YAML sets no `goal`
- **THEN** the seed's goal is the Endless-complete star

### Requirement: DLC options
The world SHALL offer the boolean options `expect_delays` and `happy_passengers`, both defaulting to off. Each SHALL mean that the player owns that DLC and will play with it enabled in the level's start settings.

#### Scenario: Defaults assume no DLC
- **WHEN** a player's YAML sets neither DLC option
- **THEN** the seed is generated for the base game without DLC upgrades

### Requirement: Slot data for the client
The world SHALL send slot data containing:
- the chosen map as the game's level UUID (`Haarlem`, `prague` or `Amsterdam`)
- the goal name
- the `expect_delays` value
- the `happy_passengers` value

#### Scenario: Slot data content
- **WHEN** a seed is generated with `map: prague`, `expect_delays: true` and `happy_passengers: false`
- **THEN** the slot data contains `map = "prague"`, `goal = "endless_complete"`, `expect_delays = true` and `happy_passengers = false`
