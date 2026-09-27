# Spec Delta

## ADDED Requirements

### Requirement: Interception applies only on Endless levels
Intercept mode SHALL change purchase behavior only while the player is in an Endless level. In every other mode (tutorials, the story campaign, Timetable and Rush Hour levels, the level editor), purchases and unlocks SHALL behave as in the unmodded game even when intercept mode is on.

#### Scenario: Purchase in a tutorial with intercept mode on
- **WHEN** intercept mode is on and the player buys an upgrade in a tutorial or story level
- **THEN** the upgrade unlocks and its points are spent exactly as in the unmodded game
- **AND** no location check is recorded

#### Scenario: Purchase in an Endless level with intercept mode on
- **WHEN** intercept mode is on and the player buys an upgrade in an Endless level
- **THEN** the purchase is intercepted as a slot purchase

### Requirement: Intercepted purchases buy the slot and record a check
When intercept mode is on in an Endless level, buying a system upgrade SHALL:
- spend the upgrade's point cost
- mark the upgrade's slot as bought, so the upgrade panel shows it as installed and child slots that require it become purchasable
- record a location check under the slot's Archipelago location name
- show an in-game notification naming the check

It SHALL NOT grant the upgrade's game effect. The effect depends only on received items.

#### Scenario: Purchase with intercept mode on
- **WHEN** intercept mode is on in an Endless level and the player buys an upgrade slot they can afford
- **THEN** the upgrade's point cost is deducted
- **AND** the slot shows as installed in the upgrade panel
- **AND** the upgrade's game feature stays unavailable unless its item has been received
- **AND** a notification names the slot's location as a sent check
- **AND** the game log contains a line with the slot's game identifier and its location name

#### Scenario: Levelled upgrade purchased with intercept mode on
- **WHEN** intercept mode is on in an Endless level and the player buys the first track speed slot
- **THEN** only that slot is marked as bought and only its location is recorded as a check
- **AND** the track speed available for building does not change

#### Scenario: Child slot after buying its parent slot
- **WHEN** intercept mode is on in an Endless level and the player has bought the Automatic Routing slot but has not received the Automatic Routing item
- **THEN** the Perpetual Circuit slot is purchasable, subject to its tier and point cost

#### Scenario: Child slot without its parent slot
- **WHEN** intercept mode is on in an Endless level and the player has received the Automatic Routing item but has not bought the Automatic Routing slot
- **THEN** the Perpetual Circuit slot is not purchasable

## MODIFIED Requirements

### Requirement: An intercepted upgrade cannot be bought twice
While intercept mode is on, a slot that has been bought SHALL NOT be purchasable again, and attempting to buy it SHALL NOT spend any points or record a second check. Bought slots SHALL stay bought when the game is saved and loaded again.

#### Scenario: Repeat purchase attempt
- **WHEN** the player tries to buy a slot they have already bought
- **THEN** no points are spent
- **AND** no second check is recorded for that slot

#### Scenario: Save and reload
- **WHEN** the player buys a slot, saves, and loads that save in a later game session with intercept mode on
- **THEN** the slot still shows as installed and can't be bought again
- **AND** the upgrade's game feature is still unavailable unless its item has been received in that session

### Requirement: Non-purchase unlocks are never intercepted
Upgrades unlocked by anything other than a player purchase — tutorials, the story campaign, level configuration or "unlock all upgrades" settings — SHALL unlock normally and grant their game effect regardless of intercept mode, and SHALL NOT record a location check.

#### Scenario: Tutorial grants an upgrade
- **WHEN** intercept mode is on and a tutorial or story chapter unlocks an upgrade
- **THEN** the upgrade unlocks and its game feature becomes available
- **AND** no check is recorded

#### Scenario: Mod grants an upgrade
- **WHEN** intercept mode is on in an Endless level and the mod grants an upgrade as a received item
- **THEN** the upgrade's game feature becomes available
- **AND** no check is recorded
- **AND** the upgrade's slot is not marked as bought

#### Scenario: Game grants an upgrade during an Endless level
- **WHEN** intercept mode is on in an Endless level and the game itself unlocks an upgrade (for example through level configuration)
- **THEN** the upgrade's game feature becomes available
- **AND** no check is recorded

## REMOVED Requirements

### Requirement: Intercepted purchases become pending location checks
**Reason**: Replaced by "Intercepted purchases buy the slot and record a check". The purchase now sets the slot's bought state instead of keeping an in-memory pending set that left the slot un-bought, which the APWorld's slot parent logic requires.
**Migration**: None for players. Dev testing: after a purchase the panel shows the slot as installed rather than "Not enough points", and F9 now receives the selected upgrade's item (see `client/received-items`).
