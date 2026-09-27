# client/ap-connection Specification

## Purpose

Connects the client mod to an Archipelago server for the Endless level being played, delivers network events safely to the game, and checks that the connected slot was generated for the level and DLC settings in use.

## Requirements

### Requirement: Connection settings come from the settings file
The client SHALL read the Archipelago server address (host and port), the slot name and an optional password from the mod's settings file. It SHALL try to connect only when intercept mode is on and both the server address and the slot name are set. A missing or empty password SHALL mean no password. The client SHALL NOT write the password to the game log.

#### Scenario: Settings complete
- **WHEN** the settings file turns intercept mode on and sets a server address and a slot name
- **THEN** the client connects when an Endless level has loaded

#### Scenario: No server configured
- **WHEN** intercept mode is on and the settings file sets no server address or no slot name
- **THEN** the client makes no connection attempt
- **AND** the game log says that Archipelago is offline because no server or slot is configured
- **AND** purchases and the F9 debug key behave as without a connection

#### Scenario: Intercept mode off
- **WHEN** intercept mode is off and a server address and slot name are set
- **THEN** the client makes no connection attempt

#### Scenario: Password is never logged
- **WHEN** the settings file sets a password and the client connects
- **THEN** the game log contains no line with the password

### Requirement: The client connects per Endless level
When an Endless level has finished loading (including a saved map, after its bought slots are restored) and the connection settings are complete, the client SHALL connect to the configured server and log in to the configured slot for the game "Rail Route", requesting every item for the slot, including its starting inventory. When the player leaves that level, or loads another level or save, the client SHALL disconnect. It SHALL connect at most once per loaded level and SHALL NOT retry automatically after a failure.

#### Scenario: Successful connection
- **WHEN** the player loads an Endless level with complete connection settings and the server accepts the login
- **THEN** the game log contains a line naming the server and the slot
- **AND** the player sees a notification that Archipelago is connected

#### Scenario: Server unreachable or login refused
- **WHEN** the server can't be reached, or it refuses the login (wrong slot, wrong password, wrong game)
- **THEN** the game log contains the reasons the server or the connection gave
- **AND** the player sees a notification that the connection failed
- **AND** the level stays playable, with purchases recorded as checks and effects only from items already received

#### Scenario: Leave the level
- **WHEN** the player is connected and leaves the level or loads another save
- **THEN** the client disconnects
- **AND** the next Endless level that loads starts a new connection

#### Scenario: Connection lost during play
- **WHEN** the connection drops while the player is in the level
- **THEN** the game log records the disconnect
- **AND** the player sees a notification that Archipelago is disconnected
- **AND** already received items keep their effects until the player leaves the level

#### Scenario: Not an Endless level
- **WHEN** the player loads a tutorial, story, Timetable or Rush Hour level, or the editor
- **THEN** the client makes no connection attempt

### Requirement: Network events reach the game on its main thread
Every effect of a network event on the game (applying a received item, sending the resend of checks after connecting, logging a disconnect, showing a notification) SHALL happen on the game's main thread, in the order the events arrived. Network events that arrive after the client disconnected from a level SHALL NOT affect a later level.

#### Scenario: Items arrive on a network thread
- **WHEN** the server sends items while the game is running a frame
- **THEN** the items take effect on a later main-thread frame, in the order they arrived
- **AND** the game does not throw a threading error

#### Scenario: Late event after leaving the level
- **WHEN** an item from the old connection arrives after the player has left the level
- **THEN** it is not applied to the next level

### Requirement: Slot data is checked against the loaded level
After connecting, the client SHALL compare the slot data's map, Expect Delays and Happy Passengers values with the loaded level's identifier and with whether each DLC is owned and enabled in the level's start settings. On any mismatch it SHALL log each differing value (expected and actual) and show a notification, and it SHALL stay connected. When the map differs, the client SHALL send no location checks for the rest of the connection (see `client/location-checks`), and the notification SHALL say so. A differing DLC setting SHALL NOT stop checks. Missing or unreadable slot data SHALL be logged as a warning and SHALL NOT fail the connection.

#### Scenario: Slot matches the level
- **WHEN** the slot was generated for Haarlem without DLCs and the player connects from Haarlem with neither DLC enabled
- **THEN** no mismatch warning is logged or shown

#### Scenario: Wrong map
- **WHEN** the slot was generated for Prague and the player connects from Haarlem
- **THEN** the game log contains a warning naming the expected map "prague" and the loaded map "Haarlem"
- **AND** the player sees a notification that the level does not match the seed and that checks are not sent
- **AND** the client stays connected and still applies received items
- **AND** no location checks are sent, neither the resend on connect nor for slots bought afterwards

#### Scenario: DLC setting differs
- **WHEN** the slot was generated with Expect Delays and the level was started with Expect Delays disabled or not owned
- **THEN** the game log contains a warning naming the Expect Delays setting and both values
- **AND** location checks are still sent

### Requirement: The client library ships without a second JSON library
The mod's folder SHALL contain the Archipelago client library next to the mod assembly and SHALL NOT contain a Newtonsoft.Json assembly. At runtime the client library SHALL use the game's own Newtonsoft.Json. The game SHALL NOT treat the client library as a mod.

#### Scenario: Fresh install
- **WHEN** a user copies the mod's folder into the game's mods folder
- **THEN** the folder holds the mod assembly, the patching library and the Archipelago client library, and no Newtonsoft.Json assembly
- **AND** the game log shows exactly one loaded mod entry for the client mod

#### Scenario: Library loads on the game's runtime
- **WHEN** the client connects for the first time in a game session
- **THEN** the game log names the Newtonsoft.Json assembly version the client library is bound to
- **AND** it is the game's own version
