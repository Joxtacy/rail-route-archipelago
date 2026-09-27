# client/goal-completion Specification

## Purpose

Reports the Archipelago goal to the server when the player earns the Endless-complete star on the seed's map, and only when the star is earned live, so a seed can be finished without an old save completing it by accident.

## Requirements

### Requirement: The Endless-complete star earned live reaches the goal
With the slot's goal `endless_complete`, the client SHALL treat the goal as reached when the game awards the Endless-complete star (the star for a combined green and red score per cycle) during play of an Endless level in intercept mode. Awarding the green star or the red star SHALL NOT reach the goal. Outside intercept mode or outside Endless levels the client SHALL NOT react to stars.

#### Scenario: Endless-complete star while connected
- **WHEN** the client is connected to a slot for the loaded map and the game awards the Endless-complete star
- **THEN** the client reports the goal as complete to the server
- **AND** the game log records that the goal was sent
- **AND** a notification says the Archipelago goal is complete

#### Scenario: Green or red star
- **WHEN** the game awards the green star or the red star
- **THEN** no goal is reported

#### Scenario: Not an Endless level
- **WHEN** a level that isn't Endless awards a star, or intercept mode is off
- **THEN** no goal is reported

### Requirement: A star found only in a save does not report the goal
The client SHALL NOT report the goal automatically because a loaded save already has the Endless-complete star. Only a star awarded during the current game run SHALL report it automatically. When a loaded level already has the star at connect time and no award was seen in this game run, the game log SHALL say the goal is not sent automatically and name the manual key.

#### Scenario: Load a save that already has the star
- **WHEN** the player loads a save that earned the Endless-complete star in an earlier game run and the client connects
- **THEN** no goal is reported
- **AND** the game log says the star was found in the save, the goal isn't sent automatically, and Shift+F10 sends it

### Requirement: The goal is reported only for a matching connected slot
The client SHALL report the goal only while connected, only when the slot data's goal is `endless_complete`, and only when the slot data's map matches the loaded level. A DLC mismatch alone SHALL NOT block the goal. When the goal is reached but can't be reported, the game log SHALL say why.

#### Scenario: Map mismatch
- **WHEN** the connected slot was generated for another map and the game awards the Endless-complete star
- **THEN** no goal is reported
- **AND** the game log says the goal isn't sent because the map doesn't match the seed

#### Scenario: Unknown goal in the slot data
- **WHEN** the slot data's goal is missing or is a value other than `endless_complete`
- **THEN** no goal is reported
- **AND** the game log names the goal value

### Requirement: A goal reached while not reported stays pending for the game run
When the Endless-complete star is awarded live but the goal can't be reported because the client isn't connected, or sending it fails, the client SHALL keep the goal pending for that map until the game quits. After the next successful login whose slot data matches that map and whose loaded level still has the Endless-complete star, the client SHALL report the pending goal. A pending goal SHALL NOT be reported for another map. A reported goal SHALL NOT be reported again in the same game run.

#### Scenario: Star earned offline, then reconnect
- **WHEN** the Endless-complete star is awarded while the server is unreachable, and the player reloads the save after the server is back, in the same game run
- **THEN** the goal is reported after login
- **AND** the game log records that a pending goal was sent

#### Scenario: Pending goal and another map
- **WHEN** a goal is pending for Haarlem and the player connects on a Prague level
- **THEN** no goal is reported

#### Scenario: Game restarted before reconnecting
- **WHEN** the Endless-complete star is awarded offline and the game quits before a successful login
- **THEN** no goal is reported on a later connect
- **AND** the manual key can still send it

### Requirement: Shift+F10 sends the goal manually
In intercept mode on an Endless level, pressing Shift+F10 SHALL report the goal when the client is connected, the slot data's goal is `endless_complete`, the map matches the seed, and the loaded level has the Endless-complete star. Otherwise it SHALL report nothing and the game log SHALL say which condition failed. F10 without Shift SHALL do nothing.

#### Scenario: Manual send for a star from an earlier game run
- **WHEN** the loaded save has the Endless-complete star, the client is connected to a matching slot, and the player presses Shift+F10
- **THEN** the goal is reported
- **AND** the game log records that the goal was sent manually

#### Scenario: Manual send without the star
- **WHEN** the loaded level doesn't have the Endless-complete star and the player presses Shift+F10
- **THEN** no goal is reported
- **AND** the game log says the level hasn't earned the Endless-complete star

#### Scenario: Manual send while offline
- **WHEN** the client isn't connected and the player presses Shift+F10
- **THEN** no goal is reported
- **AND** the game log says the client isn't connected
