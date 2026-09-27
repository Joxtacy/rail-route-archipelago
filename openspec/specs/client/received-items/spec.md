# client/received-items Specification

## Purpose

Tracks the upgrade items the player has received from Archipelago separately from the slots they have bought, so a received item grants its upgrade's game effect without buying a slot. Items come from the Archipelago server, mapped by item name, and the server's replay on connect restores them after a reload. It also provides an offline way to receive items for testing.

## Requirements

### Requirement: Received items grant upgrade effects without buying slots
When intercept mode is on in an Endless level, receiving an upgrade item SHALL make that upgrade's game feature available, with the same side effects as the game's own unlock (for example enabling auto-accept on existing stations). It SHALL show the game's unlock popup under the same conditions as the game's own unlock (not while the system upgrades menu is open), except for items replayed on connect, and SHALL NOT mark any slot as bought. When the unlock popup is not shown for a received item that isn't part of the replay, the client SHALL show a side notification naming the upgrade and, for an Archipelago item, the player who sent it.

#### Scenario: Receive an item whose slot is not bought
- **WHEN** intercept mode is on in an Endless level and the player receives the Autoblocks item without having bought the Autoblocks slot
- **THEN** autoblocks become buildable
- **AND** the Autoblocks slot is still purchasable in the upgrade panel, subject to its tier and point cost
- **AND** no location check is recorded

#### Scenario: Receive an item with side effects
- **WHEN** intercept mode is on in an Endless level and the player receives the Auto-accept Trains item
- **THEN** existing stations switch to auto-accept, as when the upgrade is unlocked in the unmodded game

#### Scenario: Receive an item while the system upgrades menu is open
- **WHEN** the player buys a slot in the system upgrades menu and the server sends back an item for this slot
- **THEN** no unlock popup is shown
- **AND** a side notification names the received upgrade and the player who sent it

#### Scenario: Buy a slot after receiving its item
- **WHEN** the player has received the Autoblocks item and then buys the Autoblocks slot
- **THEN** the slot's location check is recorded
- **AND** autoblocks stay buildable

### Requirement: Upgrade effects depend only on received items
When intercept mode is on in an Endless level, an upgrade's game feature SHALL be available only if its item has been received or the game itself unlocked it outside a player purchase. Bought slots SHALL NOT make any feature available.

#### Scenario: Slot bought, item not received
- **WHEN** the player has bought the Autoblocks slot and has not received the Autoblocks item
- **THEN** autoblocks are not buildable

#### Scenario: Intercept mode off
- **WHEN** intercept mode is off
- **THEN** upgrade features follow the game's own unlock state, as in the unmodded game

### Requirement: Progressive items raise the upgrade one level per copy
For levelled upgrades (track speed, station count and contract offers), each received copy of the progressive item SHALL raise the upgrade's effect by one level, starting from the game's initial value, up to the highest level the game defines. Which of that upgrade's slots are bought SHALL NOT affect the level.

#### Scenario: First track speed copy
- **WHEN** the player receives one Progressive Track Speed item, with any combination of track speed slots bought
- **THEN** the first track speed upgrade's speed is available for building, and faster ones are not

#### Scenario: Copies beyond the highest level
- **WHEN** the player has received more copies of a progressive item than the upgrade has levels
- **THEN** the upgrade stays at its highest level and nothing fails

### Requirement: Custom Contracts is one item for both game variants
Receiving the Custom Contracts item SHALL make Custom Contracts available whichever game variant the level shows (the Green one with Happy Passengers enabled, the Red one without).

#### Scenario: Receive Custom Contracts
- **WHEN** the player receives the Custom Contracts item
- **THEN** custom contracts become available in the level

### Requirement: Received items last for the level session
Received items SHALL apply to the current level until the player leaves it or loads another level or save. Starting or loading a level SHALL begin with no received items. When the client connects to the server for that level, the items the server resends SHALL restore the slot's received items. The client SHALL NOT save received items itself.

#### Scenario: Load another save
- **WHEN** the player has received items in one level and then loads a different save
- **THEN** no upgrade effects from the earlier level's received items are active

#### Scenario: Reload a save while the server is reachable
- **WHEN** the player received items in a level, saved, and loads that save again with the server reachable
- **THEN** after the client connects, every item the slot has received is active again

#### Scenario: Reload a save while offline
- **WHEN** the player loads that save and the connection fails
- **THEN** no received items are active

### Requirement: Debug key receives the selected upgrade's item
When intercept mode is on in an Endless level, pressing F9 SHALL receive the item belonging to the upgrade currently selected in the upgrade panel, as though Archipelago had sent it. For a levelled upgrade, that is one copy of its progressive item. The key SHALL need no network connection and no bought slot, and it SHALL do nothing outside that mode.

#### Scenario: Receive via F9
- **WHEN** intercept mode is on in an Endless level, the player selects Autoblocks in the upgrade panel and presses F9
- **THEN** the Autoblocks item is received and autoblocks become buildable
- **AND** the game log contains a line identifying the received item
- **AND** no location check is recorded

#### Scenario: F9 with nothing selected
- **WHEN** intercept mode is on in an Endless level, no upgrade is selected in the upgrade panel, and the player presses F9
- **THEN** nothing is received
- **AND** the game log says that no upgrade is selected

#### Scenario: F9 outside intercept mode
- **WHEN** intercept mode is off, or the player is not in an Endless level, and the player presses F9
- **THEN** nothing happens

### Requirement: Archipelago items map to upgrades by name
The client SHALL map each Rail Route item the server sends, by its datapackage item name, to the upgrade it grants, and receive it as that upgrade's item:
- Each binary upgrade's item has the same name as the upgrade's location. For example, "Autoblocks" grants Autoblocks.
- "Progressive Track Speed", "Progressive Station Count" and "Progressive Contract Offers" each receive one copy of that levelled upgrade.
- "Custom Contracts" grants Custom Contracts for whichever variant the level shows.

Filler items (for now "Green XP Bundle") SHALL be logged and SHALL have no effect. An item whose name the client doesn't know, or whose name can't be resolved, SHALL be logged as a warning with its item ID and SHALL NOT fail the other items.

#### Scenario: Receive a binary upgrade from the server
- **WHEN** the client is connected and the server sends the "Autoblocks" item
- **THEN** autoblocks become buildable
- **AND** the game log contains a line naming the item and the player it came from

#### Scenario: Receive a progressive item from the server
- **WHEN** the client is connected and the server sends "Progressive Track Speed" twice
- **THEN** the second track speed level is available for building, and the third is not

#### Scenario: Receive a filler item
- **WHEN** the server sends "Green XP Bundle"
- **THEN** the game log records it as a filler item with no effect yet
- **AND** no upgrade changes

#### Scenario: Unknown item
- **WHEN** the server sends an item whose name the client doesn't map
- **THEN** the game log warns with the item's name or ID
- **AND** items that arrive after it are still applied

### Requirement: The client item table matches the APWorld's documented item list
An automated test SHALL fail when the client's item-name → upgrade table differs from the "Item names" table in `apworld/README.md`. That includes an item present in only one of them, or the same item mapped to different upgrades.

#### Scenario: Tables agree
- **WHEN** the unit tests run and both tables list the same item names for the same upgrades
- **THEN** the sync test passes

#### Scenario: An item is missing
- **WHEN** an item appears in only one of the two tables
- **THEN** the sync test fails and names the item

### Requirement: The replay on connect is silent
When the client connects, the server resends every item the slot has received so far. The client SHALL apply those items with their full effects and side effects, but SHALL NOT show an unlock popup for each of them. It SHALL show one notification with the number of restored items instead. Items that arrive after the replay SHALL show the unlock popup under the normal conditions.

#### Scenario: Reconnect with many items
- **WHEN** the player loads a save whose slot has received twenty items and the client connects
- **THEN** all twenty items' effects are active
- **AND** no unlock popup is shown for them
- **AND** one notification says twenty items were restored

#### Scenario: Item after the replay
- **WHEN** the replay is done and another player sends the "Tunnels" item
- **THEN** the unlock popup for Tunnels is shown, or, while the system upgrades menu is open, a side notification naming Tunnels and the sending player
