# client/save-binding Specification

## Purpose

Binds each Endless save to the Archipelago seed and slot it was played with. The client keeps that save's Archipelago state (sent checks, goal state, received-item index) next to it, so a save from another seed can't send checks or the goal, and the state survives a game restart.

## Requirements

### Requirement: Each save file has its own Archipelago state
In intercept mode on an Endless level, the client SHALL keep an Archipelago state for the loaded level. The state SHALL hold:
- the binding: seed name, team, slot number and slot name, or none
- the locations whose checks were sent
- the goal state: none, pending, sent, or ineligible
- the received-item index: how many of the slot's received items have taken effect in the level

Whenever the game writes a save of that level (a manual save, an autosave or a forced autosave), the client SHALL store the state as it is at that moment in a file next to the save: the save's file name with the extension `.ap.json`, in the game's save folder. The game's own save file SHALL NOT be changed.

Loading a save SHALL load that save's state. A save without a state file, a new game and a restarted level SHALL start unbound, with no sent checks, goal state none and index 0. When the game renames a save, its state file SHALL be renamed with it. When the game deletes a save, including pruning old autosaves, its state file SHALL be deleted.

The game's save list SHALL NOT show state files. Outside intercept mode the client SHALL NOT read or write state files.

#### Scenario: Save a bound level
- **WHEN** the player plays a level bound to a seed and the game writes an autosave
- **THEN** a state file named after the autosave's file name with `.ap.json` exists in the save folder
- **AND** it holds the binding, the sent checks, the goal state and the received-item index of that moment

#### Scenario: Load a save with a state file
- **WHEN** the player loads a save whose state file holds a binding and a received-item index of 7
- **THEN** the level's state has that binding and index 7

#### Scenario: Autosaves are pruned
- **WHEN** the game deletes an old autosave because the level has more than five
- **THEN** that autosave's state file is deleted too

#### Scenario: Rename a save
- **WHEN** the player renames a save in the game's save menu
- **THEN** the state file is renamed to match the new save file name
- **AND** loading the renamed save loads the same state

#### Scenario: New game
- **WHEN** the player starts a new Endless game
- **THEN** the level's state is unbound with no sent checks, goal state none and index 0

### Requirement: An unbound level binds on its first successful login
After a successful login on an unbound level whose map matches the slot data, the client SHALL bind the level to the seed and slot it logged in to and log the binding. If the level was loaded from a save file, the client SHALL write the state file for that save at once, so the save stays bound even if the game quits before the next save. A level whose map doesn't match the slot data SHALL NOT be bound. If the level already has the Endless-complete star when it is bound, its goal state SHALL become ineligible (see `client/goal-completion`).

#### Scenario: First login on a new game
- **WHEN** the player starts a new Haarlem game and the client logs in to a Haarlem slot
- **THEN** the game log records the binding with the seed name and slot
- **AND** the next save's state file holds that binding

#### Scenario: First login on an old save
- **WHEN** the player loads a save without a state file and the client logs in to a slot for its map
- **THEN** the save's state file is written with the binding before any further save

#### Scenario: Map mismatch
- **WHEN** the player loads an unbound Prague save and the client logs in to a Haarlem slot
- **THEN** the level is not bound
- **AND** no state file is written for it

### Requirement: A save bound to another seed is refused
After a successful login on a level that is bound to another seed name, team or slot number than the slot it logged in to, the client SHALL send no location checks and no goal, and SHALL apply no received items. It SHALL log both bindings, notify the player that the save belongs to another Archipelago seed, and disconnect. The level's state SHALL stay unchanged and SHALL still be stored with later saves.

#### Scenario: Save from another seed
- **WHEN** the player loads a save bound to seed A and the client logs in to a slot of seed B
- **THEN** no location checks are sent, neither the resend on connect nor for slots bought afterwards
- **AND** no goal is reported
- **AND** no received items are applied
- **AND** the game log names both bindings
- **AND** the player sees a notification that the save belongs to another seed
- **AND** the client disconnects

#### Scenario: Same seed, other slot
- **WHEN** the player loads a save bound to slot 1 of a seed and the client logs in to slot 2 of the same seed
- **THEN** the save is refused as from another seed

#### Scenario: Same seed and slot
- **WHEN** the player loads a save bound to the seed and slot the client logs in to
- **THEN** the client stays connected and sends checks and the goal as usual

### Requirement: An unreadable state file blocks the save
When a loaded save's state file exists but can't be read or parsed, the client SHALL log an error naming the file and treat the level as bound to an unknown seed: after login it SHALL refuse the save as from another seed. The client SHALL NOT overwrite that file. Later saves of that level SHALL get a state file that marks the binding as unknown, so they are refused the same way.

#### Scenario: Corrupt state file
- **WHEN** the player loads a save whose state file holds invalid JSON and the client logs in
- **THEN** the game log names the file and says it can't be read
- **AND** no checks or goal are sent, and the client disconnects
- **AND** the file is unchanged
- **AND** a later autosave of that level is refused the same way when loaded
