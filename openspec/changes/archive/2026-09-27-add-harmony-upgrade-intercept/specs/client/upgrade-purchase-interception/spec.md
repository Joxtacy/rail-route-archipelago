# Spec Delta

## Purpose

Lets the client mod see every system upgrade the player buys and, when enabled, replace the unlock with a recorded location check, which is the basis for sending Archipelago checks from upgrade purchases.

## ADDED Requirements

### Requirement: Intercept mode is opt-in and configurable
The client mod SHALL read an intercept-mode setting from a settings file in the mod's folder. Intercept mode SHALL be off when the file or the setting is missing or invalid, and an invalid file SHALL be reported in the game log.

#### Scenario: No settings file
- **WHEN** the mod's folder contains no settings file
- **THEN** intercept mode is off

#### Scenario: Settings file enables intercept mode
- **WHEN** the settings file sets intercept mode to on
- **THEN** intercept mode is on for the whole game session

#### Scenario: Malformed settings file
- **WHEN** the settings file cannot be parsed
- **THEN** intercept mode is off
- **AND** the game log contains a warning naming the settings file

### Requirement: Player upgrade purchases are observed
The client mod SHALL detect every system upgrade the player buys through the in-game system upgrades panel and SHALL log the upgrade's identifier, its colour type, its tier and the points it cost.

#### Scenario: Purchase with intercept mode off
- **WHEN** intercept mode is off and the player buys an upgrade
- **THEN** the upgrade unlocks and the points are spent exactly as in the unmodded game
- **AND** the game log contains a line identifying the purchased upgrade

### Requirement: Intercepted purchases become pending location checks
When intercept mode is on, buying a system upgrade SHALL spend the upgrade's point cost, SHALL NOT unlock the upgrade, SHALL record the upgrade's identifier as a pending location check, and SHALL show the player an in-game notification naming the upgrade.

#### Scenario: Purchase with intercept mode on
- **WHEN** intercept mode is on and the player buys an upgrade they can afford
- **THEN** the upgrade's point cost is deducted
- **AND** the upgrade remains not unlocked (its game feature stays unavailable)
- **AND** the upgrade's identifier is recorded as a pending location check
- **AND** a notification names the upgrade as a sent check
- **AND** the game log contains a line identifying the check

#### Scenario: Levelled upgrade purchased with intercept mode on
- **WHEN** intercept mode is on and the player buys one level of a multi-level upgrade (for example the first track speed upgrade)
- **THEN** only that level's identifier is recorded as a pending check
- **AND** no level of that upgrade becomes unlocked

### Requirement: An intercepted upgrade cannot be bought twice
While intercept mode is on, an upgrade whose identifier is already recorded as a pending check in the current game session SHALL NOT be purchasable again, and attempting to buy it SHALL NOT spend any points.

#### Scenario: Repeat purchase attempt
- **WHEN** the player tries to buy an upgrade that was already intercepted this session
- **THEN** no points are spent
- **AND** no second check is recorded for that upgrade

### Requirement: Non-purchase unlocks are never intercepted
Upgrades unlocked by anything other than a player purchase — tutorials, the story campaign, level configuration, "unlock all upgrades" settings, or the mod itself granting an upgrade — SHALL unlock normally regardless of intercept mode.

#### Scenario: Tutorial grants an upgrade
- **WHEN** intercept mode is on and a tutorial or story chapter unlocks an upgrade
- **THEN** the upgrade unlocks
- **AND** no check is recorded

#### Scenario: Mod grants an upgrade
- **WHEN** intercept mode is on and the mod grants an upgrade to the player
- **THEN** the upgrade unlocks and its game feature becomes available
- **AND** no check is recorded
