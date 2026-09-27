# client/location-names Specification

## Purpose

Maps each game system-upgrade identifier to its Archipelago location name, so the client can name, and later send, the check for a bought slot. It also keeps that mapping in sync with the APWorld's documented location list.

## Requirements

### Requirement: Every upgrade slot has an Archipelago location name
The client SHALL map every system-upgrade identifier of Rail Route 3.0.18 to exactly one Archipelago location name. The two Custom Contracts variants (`custom_contracts` and `custom_contracts_alt`) SHALL both map to the single location "Custom Contracts".

#### Scenario: Look up a slot
- **WHEN** the client looks up the location name for `track_speed1`
- **THEN** the result is "Basic Tracks"

#### Scenario: Custom Contracts variants
- **WHEN** the client looks up `custom_contracts` or `custom_contracts_alt`
- **THEN** both return "Custom Contracts"

#### Scenario: Unknown identifier
- **WHEN** the client looks up an identifier that isn't in the table, or a slot is bought whose identifier isn't in it (for example after a game update)
- **THEN** the lookup reports no match instead of failing
- **AND** a purchase of that slot still marks it as bought and logs a warning that it has no location name

### Requirement: The client table matches the APWorld's documented location list
An automated test SHALL fail when the client's identifier → location-name table differs from the "Location names" table in `apworld/README.md`. That includes an identifier present in only one of them, or a different name for the same identifier.

#### Scenario: Tables agree
- **WHEN** the unit tests run and both tables list the same identifiers with the same names
- **THEN** the sync test passes

#### Scenario: A name differs
- **WHEN** one location is renamed in `apworld/README.md` but not in the client table
- **THEN** the sync test fails and names the identifier and both names

#### Scenario: An identifier is missing
- **WHEN** an identifier appears in only one of the two tables
- **THEN** the sync test fails and names the identifier
