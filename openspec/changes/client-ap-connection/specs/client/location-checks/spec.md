# Spec Delta

## Purpose

Sends the Archipelago location check for every upgrade slot the player buys in an Endless level, both when the slot is bought and again for all bought slots when the client connects, so no check is lost to being offline.

## ADDED Requirements

### Requirement: A bought slot is sent as a location check
When the client is connected and the player buys a slot in an Endless level, the client SHALL resolve the slot's location name to its location ID through the server's datapackage for the game "Rail Route" and SHALL send that location as checked. The game log SHALL record the sent location's name and ID.

#### Scenario: Buy a slot while connected
- **WHEN** the client is connected and the player buys the Autoblocks slot
- **THEN** the location "Autoblocks" is sent to the server as checked
- **AND** the game log contains a line naming "Autoblocks" and its location ID

#### Scenario: Buy a Custom Contracts variant
- **WHEN** the client is connected and the player buys whichever Custom Contracts slot the level shows
- **THEN** the location "Custom Contracts" is sent

#### Scenario: Buy a slot while not connected
- **WHEN** the client is not connected and the player buys a slot
- **THEN** the slot is marked as bought and its check is recorded in the log, as before
- **AND** nothing is sent until the next connection

### Requirement: Every bought slot is resent on connect
After a successful login, the client SHALL send as checked the location of every slot currently marked as bought in the level, in one batch. That includes slots bought while offline and slots restored from the save. Sending a location the server already has SHALL be harmless.

#### Scenario: Reload a save with bought slots
- **WHEN** the player loads a save with five bought slots and the client connects
- **THEN** those five locations are sent as checked
- **AND** the game log records how many locations were resent

#### Scenario: Slots bought offline
- **WHEN** the player bought slots while the server was unreachable and then connects in a later session
- **THEN** those slots' locations are sent as checked on connect

#### Scenario: Nothing bought
- **WHEN** the player connects with no bought slots
- **THEN** no locations are sent

### Requirement: Only player-bought slots with a known location are sent
The client SHALL NOT send a location for a slot the game itself unlocked (level configuration, "unlock all upgrades", tutorials). A bought slot with no location name, or whose location isn't part of the connected slot, SHALL be skipped with a warning in the game log naming the slot, and the other checks SHALL still be sent.

#### Scenario: Game-granted slot
- **WHEN** the level configuration unlocks an upgrade and the client connects
- **THEN** that upgrade's location is not sent

#### Scenario: Location not in the seed
- **WHEN** a slot is bought whose location the connected slot doesn't have (for example an Expect Delays slot in a seed generated without Expect Delays)
- **THEN** the game log warns that the location is not in the seed
- **AND** no check is sent for it
- **AND** other bought slots' checks are still sent
