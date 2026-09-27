# Spec Delta

## Purpose

Defines when each Rail Route location is reachable in logic and what completes the goal, so that every generated seed can be beaten by a player on the chosen map.

## ADDED Requirements

### Requirement: Slot parent ordering
A location whose upgrade slot has a parent slot in the game (for example Perpetual Circuit under Automatic Routing, or Advanced Tracks under Basic Tracks) SHALL be reachable in logic only when its parent slot's location is reachable. This models the game's rule that a slot can't be bought before its parent slot is bought. It SHALL NOT depend on having received the parent's item.

#### Scenario: Child slot needs parent slot
- **WHEN** the logic evaluates the Perpetual Circuit location
- **THEN** it is reachable only in states where the Automatic Routing location is reachable

#### Scenario: Progressive slot chain
- **WHEN** the logic evaluates the Corridor Tracks location
- **THEN** it is reachable only in states where the Advanced Tracks location is reachable

### Requirement: Tier gates
Green tier 1 slot locations SHALL be reachable from the start. Green tier 2 and tier 3 slot locations SHALL require a minimum count of throughput items, which are the progression items that raise points per cycle: the three progressive items, InterCities, Automatic Routing and Autoblocks. The minimum SHALL come from a per-map table, and a harder map SHALL NOT require fewer items than an easier one for the same tier.

#### Scenario: Green tier 1 in sphere 1
- **WHEN** the logic evaluates a Green tier 1 location that has no parent, with no items collected
- **THEN** the location is reachable

#### Scenario: Green tier 2 gate
- **WHEN** the logic evaluates a Green tier 2 location on a map whose tier 2 gate is N throughput items
- **THEN** it is unreachable with N−1 throughput items and reachable with N, as long as its parent slot is reachable

### Requirement: Red XP income
Every red slot location SHALL require the green tier 2 gate and at least one of Regional Trains or Freights, the upgrades that let the game generate contracts paying red points. Red tier 3 slot locations SHALL also require the green tier 3 gate and both of those items.

#### Scenario: No red income
- **WHEN** the logic evaluates a red tier 1 location with the green tier 2 gate met but neither Regional Trains nor Freights collected
- **THEN** the location is unreachable

#### Scenario: One red income item
- **WHEN** the logic evaluates a red tier 1 location with the green tier 2 gate met and Freights collected
- **THEN** the location is reachable, as long as its parent slot is reachable

### Requirement: Goal condition
With the `endless_complete` goal, the world SHALL be complete when the player has the goal count of throughput items for the chosen map. The goal count SHALL be at least the map's green tier 3 gate and at most the number of throughput items in the pool.

#### Scenario: Goal reachable only with enough throughput items
- **WHEN** the logic evaluates completion on a map whose goal count is G
- **THEN** the world is not complete with G−1 throughput items and complete with G

### Requirement: Every supported option combination generates a beatable seed
For every combination of `map`, `expect_delays` and `happy_passengers`, a single-player seed SHALL generate without errors, SHALL be beatable, and SHALL be able to reach every one of its locations with all items collected.

#### Scenario: All combinations
- **WHEN** the world's tests generate a solo seed for each of the 12 combinations of the three maps and the two DLC options
- **THEN** every seed generates, every location is reachable with all items collected, and the goal is reachable
