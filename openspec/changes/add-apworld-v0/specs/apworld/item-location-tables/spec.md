# Spec Delta

## Purpose

Defines which items and locations the Rail Route world has, their stable names and IDs that the client relies on, and which of them exist in a seed for each DLC option combination.

## ADDED Requirements

### Requirement: Every system-upgrade slot is a location
The world SHALL define one location per Rail Route system-upgrade slot, named after the upgrade's in-game English title, and checked when the player buys that slot in the upgrade panel. The two Custom Contracts slots (the Green tier 1 variant shown with Happy Passengers and the Red tier 3 variant shown without it) SHALL share a single location, because the game never shows both. The world SHALL define exactly 49 upgrade-slot locations.

#### Scenario: Location for a single upgrade
- **WHEN** the location table is loaded
- **THEN** it contains a location for the Autoblocks slot
- **AND** it contains 49 upgrade-slot locations in total

#### Scenario: Levelled upgrade slots
- **WHEN** the location table is loaded
- **THEN** each level of track speed, station count and offered contracts has its own location, for example one location each for Basic Tracks, Advanced Tracks and Corridor Tracks

### Requirement: Every system upgrade is an item
The world SHALL define an item for every system upgrade, named after its in-game English title, except for the levelled upgrades. Track speed, station count and offered contracts SHALL each be a single progressive item that grants the next level each time it is received, with three copies in every seed. The two Custom Contracts variants SHALL be one item. The world SHALL also define one filler item.

#### Scenario: Progressive item
- **WHEN** a seed is generated with any options
- **THEN** the item pool contains exactly three copies of Progressive Track Speed, three of Progressive Station Count and three of Progressive Contract Offers
- **AND** it contains no separate Basic Tracks, Advanced Tracks or Corridor Tracks items

#### Scenario: Item name count
- **WHEN** the item table is loaded
- **THEN** it contains 40 single-upgrade items, 3 progressive items and 1 filler item

### Requirement: DLC options decide which slots and upgrades exist
A seed SHALL contain exactly the upgrade-slot locations and upgrade items that the game shows for the chosen DLC options:
- Without Expect Delays, it SHALL NOT contain Maintenance Depot, Additional Service Capacity, Expanded Service Capacity, Field Efficiency, Service Automation or Operating Hours.
- The Custom Contracts location SHALL follow the Green tier 1 slot with Happy Passengers, and the Red tier 3 slot without it.
- The number of upgrade items in a seed SHALL equal the number of upgrade-slot locations in it.

#### Scenario: Base game options
- **WHEN** a seed is generated with Expect Delays off and Happy Passengers off
- **THEN** it contains 43 upgrade-slot locations and 43 upgrade items
- **AND** Custom Contracts is a Red tier 3 location

#### Scenario: Expect Delays on
- **WHEN** a seed is generated with Expect Delays on
- **THEN** it contains 49 upgrade-slot locations and 49 upgrade items, including Maintenance Depot

#### Scenario: Happy Passengers on
- **WHEN** a seed is generated with Happy Passengers on
- **THEN** Custom Contracts is a Green tier 1 location

#### Scenario: Filler item not placed in v0
- **WHEN** a seed is generated with any supported options
- **THEN** the item pool contains no filler items

### Requirement: Item and location IDs are stable
Every item and location SHALL have a fixed numeric ID that is the same regardless of options and never changes after release. Location IDs for upgrade slots SHALL lie in 1000–1099. Single-upgrade item IDs SHALL lie in 1–99, progressive item IDs in 100–199 and filler item IDs in 500–599. No two items and no two locations SHALL share an ID.

#### Scenario: IDs independent of options
- **WHEN** the world is loaded with any options
- **THEN** its item-name-to-ID and location-name-to-ID tables are identical

#### Scenario: Renumbering is caught
- **WHEN** a change alters the ID of an existing item or location, or removes one
- **THEN** the world's test suite fails

### Requirement: Items are classified for fill
Items that the logic relies on SHALL be classified as progression. These are the three progressive items, InterCities, Automatic Routing, Autoblocks, Regional Trains and Freights. Every other upgrade item SHALL be classified as useful, and the filler item as filler.

#### Scenario: Red-income item
- **WHEN** a Regional Trains item is created
- **THEN** its classification is progression

#### Scenario: Quality-of-life item
- **WHEN** a Train Alerts item is created
- **THEN** its classification is useful
