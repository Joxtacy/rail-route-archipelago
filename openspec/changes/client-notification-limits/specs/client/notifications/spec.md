# Spec Delta

## Purpose

Keeps the mod's side notifications from piling up: routine messages clear themselves after a while, warnings and the goal stay until the player dismisses them, and the number shown at once is limited. Both the duration and the limit can be set in the settings file.

## ADDED Requirements

### Requirement: Routine notifications expire
Every side notification the mod shows SHALL stay dismissable. A notification that isn't sticky SHALL also clear itself once it has been on screen for the configured number of seconds. The time SHALL count real seconds, including while the game is paused. With expiry turned off, notifications SHALL stay until the player dismisses them.

#### Scenario: A check notification clears itself
- **WHEN** the player buys an upgrade slot in intercept mode with the default settings
- **THEN** the "Check sent: …" notification appears
- **AND** it disappears about 10 seconds later without being dismissed

#### Scenario: Paused game
- **WHEN** a routine notification appears and the player pauses the game
- **THEN** the notification still disappears after the configured number of seconds

#### Scenario: Expiry turned off
- **WHEN** the settings file sets `notificationSeconds` to 0
- **THEN** routine notifications stay until the player dismisses them

#### Scenario: Dismissed early
- **WHEN** the player dismisses a routine notification before it expires
- **THEN** it disappears immediately

### Requirement: Warnings and the goal are sticky
These notifications SHALL be sticky: they never expire and stay until the player dismisses them or the game clears its notifications.
- the save belongs to another seed
- the level doesn't match the seed
- the mod is running degraded
- the connection failed
- the connection was lost
- the Archipelago goal is complete

Every other mod notification SHALL expire.

#### Scenario: Connection lost
- **WHEN** the connection drops during play with the default settings
- **THEN** the "Archipelago disconnected" notification appears
- **AND** it is still shown after the expiry time has passed

#### Scenario: Goal complete
- **WHEN** the goal is reported to the server
- **THEN** the "Archipelago goal complete" notification stays until the player dismisses it

### Requirement: The number of mod notifications is limited
When a new mod notification takes the number of the mod's notifications on screen above the configured limit, the client SHALL remove the oldest notifications that aren't sticky, one at a time, until the count is within the limit or only sticky ones are left. A sticky notification SHALL NOT be removed because of the limit, so sticky notifications can take the count above it. A notification the player dismissed, or one that expired, SHALL no longer count. With the limit turned off, the client SHALL remove nothing because of the count. The limit SHALL NOT affect the game's own notifications.

#### Scenario: Burst of received items
- **WHEN** seven routine notifications appear within a few seconds with the default limit of 5
- **THEN** at most five of them are on screen at once
- **AND** the two oldest are the ones removed

#### Scenario: Sticky notifications are kept
- **WHEN** two sticky notifications are on screen and five routine notifications follow with a limit of 5
- **THEN** both sticky notifications stay
- **AND** the two oldest routine notifications are removed

#### Scenario: Only sticky notifications left
- **WHEN** six sticky notifications are on screen with a limit of 5
- **THEN** all six stay

#### Scenario: Limit turned off
- **WHEN** the settings file sets `notificationLimit` to 0
- **THEN** no notification is removed because of the count

### Requirement: The settings file controls expiry and the limit
The client SHALL read `notificationSeconds` (seconds before a routine notification expires, default 10) and `notificationLimit` (the most mod notifications on screen, default 5) from the mod's settings file. A value of 0 SHALL turn that behavior off. A missing, null or negative value SHALL mean the default. A value that isn't a number SHALL be treated like any other invalid settings file: the defaults apply and the game log has a warning. The log line that records the settings at startup SHALL include both values.

#### Scenario: No settings
- **WHEN** the settings file doesn't set either key, or there is no settings file
- **THEN** routine notifications expire after 10 seconds and the limit is 5

#### Scenario: Custom values
- **WHEN** the settings file sets `notificationSeconds` to 4 and `notificationLimit` to 3
- **THEN** routine notifications expire after 4 seconds and at most 3 mod notifications are shown, apart from sticky ones over the limit
- **AND** the settings log line shows both values

#### Scenario: Negative value
- **WHEN** the settings file sets `notificationLimit` to -1
- **THEN** the limit is 5
