# Spec Delta

## MODIFIED Requirements

### Requirement: Every bought slot is resent on connect
After a successful login on a level that is bound to the connected seed and slot, or that binds at this login (see `client/save-binding`), the client SHALL send as checked the location of every slot currently marked as bought in the level, in one batch. That includes slots bought while offline and slots restored from the save. Sending a location the server already has SHALL be harmless. For a save bound to another seed, the client SHALL send nothing. Every location the client hands to the server, on connect or for a purchase, SHALL be recorded in the level's sent checks, which are stored with the save. The recorded sent checks SHALL NOT stop a bought slot from being resent.

#### Scenario: Reload a save with bought slots
- **WHEN** the player loads a save bound to this seed with five bought slots and the client connects
- **THEN** those five locations are sent as checked
- **AND** the game log records how many locations were resent
- **AND** the level's sent checks hold those five locations

#### Scenario: Slots bought offline
- **WHEN** the player bought slots while the server was unreachable and then connects in a later session
- **THEN** those slots' locations are sent as checked on connect

#### Scenario: Nothing bought
- **WHEN** the player connects with no bought slots
- **THEN** no locations are sent

#### Scenario: Save from another seed
- **WHEN** the player loads a save with bought slots that is bound to another seed and the client connects
- **THEN** no locations are sent
