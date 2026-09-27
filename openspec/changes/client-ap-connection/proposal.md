# Proposal

## Why

The client can intercept slot purchases and apply received items, but only offline: a bought slot just logs `Check recorded: …`, and items only arrive through the F9 debug key. M3's goal is that the mod connects to an Archipelago server, sends a check for each bought slot and applies the items the server sends. Split flags (`client-split-flags`) and APWorld v0 are done, so the item and location contract is fixed and this change can build on it.

## What Changes

- Add **Archipelago.MultiClient.Net 6.7.1** (the `net45` build) and ship `Archipelago.MultiClient.Net.dll` in `mods/RailRouteArchipelago/`.
  - The package bundles its own **unsigned Newtonsoft.Json 11.0.0.0**, and its assembly references that identity (`PublicKeyToken=null`). The game ships the signed Newtonsoft.Json 13.0.2 (assembly version 13.0.0.0) in `Managed/`.
  - The mod does **not** ship the bundled copy. It binds MultiClient.Net to the game's already loaded Newtonsoft.Json, with an `AppDomain.AssemblyResolve` fallback. A runtime check at the start of implementation confirms this binding works on the game's Mono.
- Add connection settings to `RailRouteArchipelago.settings.json`: `server` (host and port), `slot` and an optional `password`.
- Connect when an Endless level has finished loading, but only in intercept mode and only when `server` and `slot` are set. Disconnect when the player leaves the level. Log connection success, failure and disconnects, and show them as in-game notifications.
- Handle every network callback (items, socket errors, disconnects) on the Unity main thread: callbacks go into a queue that a MonoBehaviour drains each frame.
- **Send checks.** A slot bought while connected is sent at once. Its location name resolves to its location ID through the datapackage. On connect, the client sends a check for every slot already marked bought (`Researched`), which covers slots bought offline or in an earlier session and saved with the game.
- **Receive items.** Map AP item names to game upgrades: one item per binary upgrade, plus Progressive Track Speed, Progressive Station Count and Progressive Contract Offers, and one Custom Contracts item for both variants. Feed each item to `UpgradeReceiver.Receive`. On connect, the server resends every item already received. The client applies that replay silently (no unlock popup per item) and shows one summary notification. This restores upgrade effects after a save is reloaded.
- **Validate slot data.** Compare `map`, `expect_delays` and `happy_passengers` with the loaded level's UUID and its enabled DLCs. Warn in the log and in a notification on any mismatch, but stay connected.
- Document the item-name contract: add an "Item names" table to `apworld/README.md`, plus a client test that fails if the client's item table differs from it (the same approach as the location-name test).
- Add an in-game test round against a locally generated seed, together with the open M3 test-round questions.

## Capabilities

### New Capabilities
- `client/ap-connection`: connection settings, the connect/disconnect lifecycle per Endless level, main-thread delivery of network events, slot-data validation, and shipping the client library without a second Newtonsoft.Json.
- `client/location-checks`: sending bought slots to the server as location checks, both live and as a full resend on connect.

### Modified Capabilities
- `client/received-items`: items now also come from the server, mapped by AP item name; the replay on connect is silent; and the "last for the level session" requirement now says effects are restored from the server on each connect. The F9 debug receive is unchanged.

## Impact

- **Code**:
  - new `client/src/Net/` for the session, main-thread queue, check sender and item router
  - new `client/src/Core/ItemNames.cs` and the connection fields in `ModSettings`
  - `SlotPurchaseHandler` hands checks to the sender
  - `UpgradeReceiver.Receive` gets a silent mode
  - `ArchipelagoMod` wires in the lifecycle
- **Build**:
  - a `PackageReference` to `Archipelago.MultiClient.Net` 6.7.1 whose bundled Newtonsoft.Json is kept out of both compilation and the output
  - the `DeployMod` target copies `Archipelago.MultiClient.Net.dll`
- **Docs**: `client/README.md` (settings, connection, files in the mod folder), `apworld/README.md` (the item-names table), `FINDINGS.md` (Newtonsoft binding, test-round results) and `ROADMAP.md` (M3 status and follow-ups).
- **Tests**: `client.Tests` cases for the new settings fields, the item-name table and its README sync, and the pure parts of the replay and resend logic.
- **Out of scope** (listed as follow-ups in `ROADMAP.md`):
  - goal completion reported to the server (M5)
  - an in-game connect screen (M5)
  - saving checked and received state per save file, bound to the seed
  - the upgrade panel's "Check sent" label
  - filler items: "Green XP Bundle" is logged and ignored (M4)
  - DeathLink
