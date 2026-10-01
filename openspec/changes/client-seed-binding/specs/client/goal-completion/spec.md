# Spec Delta

## MODIFIED Requirements

### Requirement: The Endless-complete star earned live reaches the goal
With the slot's goal `endless_complete`, the client SHALL treat the goal as reached when the game awards the Endless-complete star (the star for a combined green and red score per cycle) during play of an Endless level in intercept mode. Awarding the green star or the red star SHALL NOT reach the goal. Outside intercept mode or outside Endless levels the client SHALL NOT react to stars. When the goal is reported, the level's Archipelago state SHALL record it as sent (see `client/save-binding`).

#### Scenario: Endless-complete star while connected
- **WHEN** the client is connected to a slot for the loaded map, the level is bound to that slot, and the game awards the Endless-complete star
- **THEN** the client reports the goal as complete to the server
- **AND** the game log records that the goal was sent
- **AND** a notification says the Archipelago goal is complete
- **AND** the next save's state records the goal as sent

#### Scenario: Green or red star
- **WHEN** the game awards the green star or the red star
- **THEN** no goal is reported

#### Scenario: Not an Endless level
- **WHEN** a level that isn't Endless awards a star, or intercept mode is off
- **THEN** no goal is reported

### Requirement: The goal is reported only for a matching connected slot
The client SHALL report the goal only while connected, only when the slot data's goal is `endless_complete`, only when the slot data's map matches the loaded level, and only when the level is bound to the connected seed and slot. A DLC mismatch alone SHALL NOT block the goal. When the goal is reached but can't be reported, the game log SHALL say why.

#### Scenario: Map mismatch
- **WHEN** the connected slot was generated for another map and the game awards the Endless-complete star
- **THEN** no goal is reported
- **AND** the game log says the goal isn't sent because the map doesn't match the seed

#### Scenario: Unknown goal in the slot data
- **WHEN** the slot data's goal is missing or is a value other than `endless_complete`
- **THEN** no goal is reported
- **AND** the game log names the goal value

#### Scenario: Save from another seed
- **WHEN** the loaded save is bound to another seed and has the Endless-complete star
- **THEN** no goal is reported

## ADDED Requirements

### Requirement: A star in a save bound to this seed reports the goal
After a successful login on a level bound to the connected seed and slot, with a matching map and the goal `endless_complete`, the client SHALL report the goal if the loaded level has the Endless-complete star and the level's goal state is neither sent nor ineligible. The game log SHALL say that the goal was sent from the save.

A star that the level already had when it was bound SHALL make the goal state ineligible. The client SHALL NOT report an ineligible goal, ever. After login on such a level, the game log SHALL say that the star predates the binding and the goal isn't sent.

#### Scenario: Save bound to this seed with the star
- **WHEN** the player loads a save bound to this seed whose level earned the Endless-complete star after it was bound, and the goal wasn't sent, and the client logs in
- **THEN** the goal is reported
- **AND** the game log says the goal was sent from the save

#### Scenario: Goal already sent from this save
- **WHEN** the player loads a save bound to this seed whose state records the goal as sent, and the client logs in
- **THEN** no goal is reported again

#### Scenario: Old save with the star binds
- **WHEN** the player loads a save without a state file that already has the Endless-complete star, and the client logs in to a slot for its map
- **THEN** the save is bound and its goal state is ineligible
- **AND** no goal is reported
- **AND** the game log says the star predates the binding

#### Scenario: Ineligible save on a later login
- **WHEN** the player loads a save bound to this seed whose goal state is ineligible, and the client logs in
- **THEN** no goal is reported

### Requirement: A goal not reported stays pending in the save
When the Endless-complete star is awarded live but the goal can't be reported because the client isn't connected, or sending it fails, the level's goal state SHALL become pending, and later saves SHALL store it. A pending goal SHALL survive a game restart: after the next successful login on a save of that level bound to this seed, the client SHALL report it (see "A star in a save bound to this seed reports the goal"). A star awarded live on a level that isn't bound yet SHALL be treated like any star the level has when it binds: the goal becomes ineligible.

#### Scenario: Star earned offline on a bound save, game restarted
- **WHEN** the player loads a save bound to this seed, the server is unreachable, the Endless-complete star is awarded, the game autosaves and quits, and later the player loads that autosave with the server reachable
- **THEN** the goal is reported after login

#### Scenario: Sending fails
- **WHEN** the goal is sent while connected but the send fails
- **THEN** the game log says the goal is sent after the next login
- **AND** the level's goal state is pending

#### Scenario: Star earned on a level that was never bound
- **WHEN** the player starts a new game while the server is unreachable, earns the Endless-complete star, and later loads that save with the server reachable
- **THEN** the save is bound with an ineligible goal state
- **AND** no goal is reported

## REMOVED Requirements

### Requirement: A star found only in a save does not report the goal
**Reason**: The seed binding tells whether a save's star was earned with this seed. A star in a save bound to this seed is now reported automatically, and a star from before the binding is refused for good.
**Migration**: See "A star in a save bound to this seed reports the goal".

### Requirement: A goal reached while not reported stays pending for the game run
**Reason**: The pending goal is stored with the save instead of in memory for the game run, so it survives a restart.
**Migration**: See "A goal not reported stays pending in the save".

### Requirement: Shift+F10 sends the goal manually
**Reason**: It was the fallback for a star the client couldn't tie to the seed. The binding now covers that case automatically. A manual key would bypass the ineligible guard, and the goal can't be undone.
**Migration**: None needed. A save bound to this seed sends its star on login. An unbound save's star can't be sent.
