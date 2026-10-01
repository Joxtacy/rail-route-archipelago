# Spec Delta

## MODIFIED Requirements

### Requirement: Received items last for the level session
Received items SHALL apply to the current level until the player leaves it or loads another level or save. Starting or loading a level SHALL begin with no received items. When the client connects to the server for that level, the items the server resends SHALL restore the slot's received items. The client SHALL NOT save the received items themselves. It SHALL save only the level's received-item index with the save (see `client/save-binding`): the number of the slot's items, in the server's order, that have taken effect in the level. The index SHALL only grow within a level session, and a level loaded offline SHALL keep the index it was loaded with.

#### Scenario: Load another save
- **WHEN** the player has received items in one level and then loads a different save
- **THEN** no upgrade effects from the earlier level's received items are active

#### Scenario: Reload a save while the server is reachable
- **WHEN** the player received items in a level, saved, and loads that save again with the server reachable
- **THEN** after the client connects, every item the slot has received is active again

#### Scenario: Reload a save while offline
- **WHEN** the player loads that save and the connection fails
- **THEN** no received items are active
- **AND** a save written in that session stores the same received-item index the loaded save had

### Requirement: The replay on connect is silent
When the client connects, the server resends every item the slot has received so far. The client SHALL treat the first items of that replay, up to the level's received-item index, as already in the level: it SHALL restore their effects without repeating their one-shot side effects (for example switching existing stations to auto-accept, or changing alert preferences). The items from the index onwards are new to the level: the client SHALL apply them with their full effects and side effects. The client SHALL NOT show an unlock popup for any replayed item. It SHALL show one notification with the number of restored items and, when there are any, the number of new ones. Items that arrive after the replay SHALL show the unlock popup under the normal conditions. After the replay the level's received-item index SHALL equal the number of items the slot has.

#### Scenario: Reconnect with many items
- **WHEN** the player loads a save whose state has a received-item index of twenty, the slot has received twenty items, and the client connects
- **THEN** all twenty items' effects are active
- **AND** no unlock popup is shown for them
- **AND** no one-shot side effects run for them
- **AND** one notification says twenty items were restored

#### Scenario: Station setting survives a reconnect
- **WHEN** the player received Auto-accept Trains, turned auto-accept off at one station, saved, and loads that save with the server reachable
- **THEN** after the replay auto-accept is still off at that station

#### Scenario: Items sent while the save wasn't loaded
- **WHEN** the player loads a save whose state has a received-item index of 5, and the slot has received 7 items
- **THEN** the first 5 items are restored without side effects
- **AND** the last 2 items apply with their side effects
- **AND** one notification says 5 items were restored and 2 are new
- **AND** the level's received-item index becomes 7

#### Scenario: First login on an unbound save
- **WHEN** the player loads a save without a state file and the slot has received 3 items
- **THEN** all 3 items apply with their side effects, without unlock popups

#### Scenario: Item after the replay
- **WHEN** the replay is done and another player sends the "Tunnels" item
- **THEN** the unlock popup for Tunnels is shown, or, while the system upgrades menu is open, a side notification naming Tunnels and the sending player
- **AND** the level's received-item index grows by one
