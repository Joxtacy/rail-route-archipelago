# Proposal

## Why

The Archipelago integration needs to turn "the player buys a system upgrade" into "the player sends a location check" — the upgrade itself must arrive later as a received item. The game's public mod API can grant and gate upgrades but has no hook to replace what a purchase does, so we need Harmony runtime patching. Harmony detours on native Apple Silicon Mono are a known risk (MonoMod arm64 issues), so this must be proven on macOS arm64 and Windows x64 before any Archipelago work is built on top of it.

## What Changes

- Bundle Harmony (`Lib.Harmony` 2.4.x, net48 build) with the client mod and deploy `0Harmony.dll` next to `RailRouteArchipelago.dll` in the mods folder.
- Apply the mod's Harmony patches when the mod is enabled and remove them when it is disabled; log patch success/failure together with the runtime platform and CPU architecture.
- Patch the player's upgrade purchase path (`Multiplayer.Commands.Game.UnlockUpgradeCommand`) so the mod observes every player purchase and can optionally take over its outcome.
- Add an opt-in **intercept mode** (setting in a JSON file next to the DLL, default off):
  - off: the purchase behaves exactly as in the unmodded game; the mod only logs it.
  - on: the purchase spends the upgrade's points but does **not** unlock the upgrade; the mod records it as a pending location check, logs it, and shows an in-game notification. The same upgrade cannot be bought again in that session.
- Upgrades granted by other code paths (tutorials, story, level scripts, future Archipelago item grants via `ResearchController.CompleteResearch`) are never intercepted.
- Document how to verify on macOS arm64 (native), macOS under Rosetta (fallback), and Windows x64.

## Capabilities

### New Capabilities
- `client/harmony-patching`: How the client mod loads Harmony, applies and removes its patches, reports patch health per platform, and what happens when patching fails.
- `client/upgrade-purchase-interception`: Observing player system-upgrade purchases and, in intercept mode, replacing the unlock with a recorded location check.

### Modified Capabilities
<!-- None: no specs exist yet in openspec/specs/. -->

## Impact

- **Code**: `client/RailRouteArchipelago.csproj` (Harmony package + deploy of `0Harmony.dll`), `client/src/ArchipelagoMod.cs` (patch lifecycle), new patch/interception/settings classes under `client/src/`.
- **Dependencies**: `Lib.Harmony` (NuGet, MIT). Shipped alongside the mod; the game does not bundle Harmony.
- **Game coupling**: Relies on the non-public-API shape of `UnlockUpgradeCommand.Run()` / `Validate()` in Rail Route 3.0.18; a game update that changes them breaks the patch (must fail loudly, not silently).
- **Platforms**: macOS arm64 native is the primary risk; Rosetta (`arch -x86_64`) is the documented fallback. Windows x64 verification is manual on the user's Windows machine.
- **Out of scope**: Archipelago connectivity, persisting pending checks across saves, UI changes to the upgrade panel, multiplayer sessions.
