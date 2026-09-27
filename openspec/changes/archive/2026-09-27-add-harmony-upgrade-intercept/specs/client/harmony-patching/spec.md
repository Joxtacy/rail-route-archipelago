# Spec Delta

## Purpose

Lets the client mod change game behavior at runtime by patching game methods, on every platform the game runs on, and makes patch health visible so platform problems are caught immediately.

## ADDED Requirements

### Requirement: Patches are applied when the mod is enabled
The client mod SHALL apply all of its runtime patches when the game enables the mod, before the player can start a level, and SHALL apply them at most once per game process.

#### Scenario: Game starts with the mod installed
- **WHEN** the game starts and loads the client mod from the mods folder
- **THEN** every patch registered by the mod is applied before the main menu is usable
- **AND** the game log contains one line per patch naming the patched game method and reporting success

#### Scenario: Game context changes repeatedly
- **WHEN** the game switches between the main menu, the editor and play mode several times in one session
- **THEN** no patch is applied a second time

### Requirement: Patches are removed when the mod is disabled
The client mod SHALL remove all of its runtime patches when the game disables the mod, restoring unmodded behavior for the patched methods.

#### Scenario: Mod is disabled
- **WHEN** the game disables the client mod
- **THEN** all patches owned by the mod are removed
- **AND** patches owned by other mods are left untouched

### Requirement: Patch health is reported with the runtime platform
The client mod SHALL write to the game log, at startup, the game version, the operating system, the CPU architecture the game process is running as (for example arm64 or x86_64), and the patching library version.

#### Scenario: Native Apple Silicon run
- **WHEN** the game runs natively on an Apple Silicon Mac
- **THEN** the log reports the architecture as arm64 alongside the patch results

#### Scenario: Rosetta run
- **WHEN** the game runs under Rosetta on an Apple Silicon Mac
- **THEN** the log reports the architecture as x86_64 alongside the patch results

### Requirement: Patch failure is loud and non-fatal
If any patch cannot be applied (for example because a game update changed the patched method, or the platform does not support runtime patching), the client mod SHALL log the failure with the error, SHALL show an in-game notification that the mod is running degraded, and SHALL NOT crash or block the game.

#### Scenario: Patched method no longer exists
- **WHEN** a game update removes or renames a method the mod patches
- **THEN** the game still reaches the main menu and levels remain playable
- **AND** the log contains an error naming the missing method
- **AND** the player sees a notification that Archipelago features are unavailable

#### Scenario: Runtime patching unsupported on the platform
- **WHEN** applying patches throws on the current platform
- **THEN** the log contains the error and the CPU architecture
- **AND** no partial set of interception behavior is active (either all interception patches are active or none are)

### Requirement: Patching library ships with the mod
The client mod's distributable SHALL include the runtime patching library alongside the mod assembly in the mod's folder, and the game SHALL NOT treat the library itself as a mod.

#### Scenario: Fresh install
- **WHEN** a user copies the mod's folder into the game's mods folder on macOS or Windows
- **THEN** the mod loads and applies its patches without any other installation step
- **AND** the game log shows exactly one loaded mod entry for the client mod
