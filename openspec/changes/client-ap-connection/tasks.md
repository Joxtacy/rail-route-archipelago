# Tasks

## 1. Client library and the Newtonsoft.Json binding (spike first)

- [ ] 1.1 Add `Archipelago.MultiClient.Net` 6.7.1 to `client/RailRouteArchipelago.csproj` (D1):
  - a `PackageReference` with `ExcludeAssets="all"` and `GeneratePathProperty="true"`
  - a `Reference` to `$(PkgArchipelago_MultiClient_Net)/lib/net45/Archipelago.MultiClient.Net.dll`
  - add `Archipelago.MultiClient.Net.dll` and `.pdb` to `DeployMod`'s `ModFiles`

  Verify with `cd client && dotnet build -c Release -p:DeployMod=false`: 0 warnings, and `bin/Release/net48/` contains `Archipelago.MultiClient.Net.dll` and **no** `Newtonsoft.Json.dll`.
- [ ] 1.2 Add the `AppDomain.AssemblyResolve` handler (D1), registered first in `ArchipelagoMod.OnEnable`. It answers requests for the simple name `Newtonsoft.Json` with the loaded game assembly and logs each redirect once. Add a spike log line at enable that loads a MultiClient type without connecting (for example `ArchipelagoSessionFactory.CreateSession("localhost", 38281)`) and logs `MultiClient <version>, Newtonsoft.Json bound to <assembly full name>`. Verify with a deploy build (`cd client && dotnet build -c Release`) that the three DLLs are in `mods/RailRouteArchipelago/`.
- [ ] 1.3 **Spike check in the game** (the only in-game check before the test round, because every later group depends on it):
  1. Generate a Haarlem seed and start a local server (task 5.1's commands).
  2. The user starts the game once and opens the main menu.
  3. Confirm in `Player.log` the `Newtonsoft.Json bound to Newtonsoft.Json, Version=13.0.0.0, … PublicKeyToken=30ad4fe6b2a6aeed` line, with no `FileNotFoundException`/`TypeLoadException`, and still exactly one `Loaded MOD` entry.

  If the binding fails, switch to shipping the bundled copy (D1, second alternative) before continuing. Record the result either way in a new "Client library" subsection of `FINDINGS.md`: the assembly identities table from design.md, the binding result and the Mono version from the `Runtime:` line.

## 2. Game-independent logic with unit tests

- [ ] 2.1 Extend `ModSettings` with `server`, `slot` and `password` (D6), and add a `ServerAddress` parser in `Core`: `host:port`, a bare `host` (port 38281), and `ws://`/`wss://` URIs; anything else is invalid. Add `ModSettingsTests` cases:
  - the new fields load
  - missing fields → offline
  - each address form parses to the expected host, port and scheme
  - invalid addresses are rejected
  - `ToString`/log text never contains the password

  Verify that `dotnet test --project client.Tests` passes.
- [ ] 2.2 Add an "Item names" section to `apworld/README.md` (D4) with a row per item: item name, item ID and game `Id`(s). Include 44 rows: the 40 upgrade items (Custom Contracts lists both variant `Id`s), the 3 progressive items with their chain `Id`s, and "Green XP Bundle" marked as filler. Take the values from `apworld/rail_route/data.py`. Add `client/src/Core/ItemNames.cs`: item name → game `Id` (a chain's first item for progressive items, `custom_contracts` for Custom Contracts), plus `IsFiller(name)` and `TryGet(name, out id)`. Add `ItemNamesTests`:
  - the README sync in both directions, naming the item on failure
  - the parsed row count
  - `"Progressive Track Speed"` → `track_speed1`
  - `"Custom Contracts"` → `custom_contracts`
  - every binary item name equals its `LocationNames` name
  - an unknown name → false

  Verify that the tests pass. Then temporarily rename one README item, confirm the test fails naming it, and revert the rename.
- [ ] 2.3 Add `Core/MainThreadQueue` (D3): enqueue from any thread with an epoch, and drain on the caller's thread, running only current-epoch actions in FIFO order and catching and reporting exceptions through a callback. Add tests:
  - FIFO order
  - stale epochs are dropped
  - an exception in one action doesn't stop the drain
  - concurrent enqueue from several threads loses nothing

  Verify that the tests pass.
- [ ] 2.4 Add `Core/ItemSync` (D4): it takes a packet's `index` and item count and returns `Replace` (index 0), `Append` (index == expected) or `Resync` (anything else), and tracks the expected next index. Add tests for:
  - the first full list
  - an increment
  - a gap
  - a duplicate or old index
  - a second index-0 list after a sync, which resets the expected index

  Verify that the tests pass.
- [ ] 2.5 Add `Core/CheckSelection` (D5): given the bought slot `Id`s and the game-granted `Id`s, it returns the de-duplicated location names to resend, plus the bought `Id`s without a location name. Add tests:
  - granted slots are excluded
  - both Custom Contracts variants give one name
  - unknown `Id`s are reported
  - no slots → empty

  Verify that the tests pass.

## 3. Connection lifecycle

- [ ] 3.1 Add `client/src/Net/ArchipelagoPump` (D2/D3), a `DontDestroyOnLoad` MonoBehaviour installed once in `OnEnable` next to `DebugReceiveKey`. Each `Update`:
  1. Drain `MainThreadQueue`.
  2. Detect "`SplitFlags.Active && GameController.Loaded` for a `Level` not yet handled", which starts a connection.
  3. Detect "level changed or split mode ended", which disconnects.

  Log `Archipelago offline: no server/slot configured` once per level when the settings are incomplete. Verify the build.
- [ ] 3.2 Add `client/src/Net/ApSession` (D2):
  - Create the session from the parsed address.
  - Subscribe `PacketReceived`, `ErrorReceived` and `SocketClosed`, each enqueuing onto the queue with the session epoch.
  - Run `ConnectAsync` and `LoginAsync("Rail Route", slot, ItemsHandlingFlags.AllItems, …, password, requestSlotData: true)` as tasks.
  - Enqueue the result:
    - success: log `Connected to <server> as <slot>` and notify "Archipelago connected"
    - failure: log each error and notify "Archipelago connection failed – see Player.log"
  - `Disconnect()` bumps the epoch and closes the socket without blocking.
  - A drop logs and notifies "Archipelago disconnected".
  - Log the Newtonsoft binding line from 1.2 on the first connect.

  Verify the build.
- [ ] 3.3 Add the slot-data check (D7) on login success: read `map`, `expect_delays` and `happy_passengers` via `Convert.ToString` and `bool.TryParse`, and compare them with `LevelDefinition.Uuid` and `IsDlcOwned(dlc) && StartSettings.<Dlc>Enabled`. Log one warning per mismatch (`expected … , level has …`) and notify once "This level doesn't match the Archipelago seed". Missing keys give a warning. Verify the build.
- [ ] 3.4 Add the level-load diagnostics (D8): at each Endless level load in intercept mode, log one line per upgrade (`Id`, `Researched`, locked, game grant), plus `LevelDefinition.Storage` and the runtime `UpgradeTiersConfigurations` and `ThroughputRewards` thresholds. Verify the build.
- [ ] 3.5 Update `client/README.md`:
  - the settings table (`server`, `slot`, `password`, with an example for a local server and one for an archipelago.gg room)
  - when the mod connects and disconnects
  - the connection notifications
  - the new DLL in the mod-folder tree
  - the healthy-start log lines

  Update the intro sentence "There's no Archipelago connection yet". Verify that the README's build and test commands still run as written.

## 4. Sending checks and receiving items

- [ ] 4.1 Add `client/src/Net/CheckSender` (D5):
  - `Send(locationName)` resolves the ID via `GetLocationIdFromName("Rail Route", name)`. It skips `-1` or IDs missing from `Locations.AllLocations` with a warning, calls `CompleteLocationChecksAsync`, and logs `Check sent: <name> (<id>)`. Failures are logged via the queue.
  - `ResendAll()` on login success uses `CheckSelection` over `ResearchItems.Where(Researched)` and the `EffectState` grants, then logs `Resent <n> checks` plus a warning for each skipped slot.

  Call `Send` from `SlotPurchaseHandler` after the existing log line. It does nothing when not connected. Verify the build.
- [ ] 4.2 Add `UpgradeReceiver.Receive(item, silent)` (D4): `silent` skips the `UnlockPopup` only. Keep F9 on the non-silent path. Add `EffectState.ClearReceived()`, which clears counts but keeps game grants. Verify the build.
- [ ] 4.3 Add `client/src/Net/ItemRouter` (D4) for `ReceivedItemsPacket` on the main thread, driven by `ItemSync`:
  - `Replace`: `ClearReceived`, apply all silently, then notify "Restored <n> Archipelago items" when n > 0.
  - `Append`: apply each normally.
  - `Resync`: log and send a `Sync` packet.

  Each item: resolve the name with `Items.GetItemName(id, "Rail Route")`, then `ItemNames.TryGet`.
  - Custom Contracts goes to the variant the level shows.
  - Filler logs `Filler item ignored: <name>`.
  - An unknown name warns with the ID.
  - Otherwise, find the `ResearchItem` and call `Receive`, logging the item name and the sending player's name.

  Verify the build and that `dotnet test --project client.Tests` passes.
- [ ] 4.4 Update `client/README.md` (sending and receiving, the replay notification, filler ignored for now) and `CLAUDE.md` "Game and mod facts" (one line: network callbacks only enqueue, and the pump applies them on the main thread per session epoch). Verify that the README renders and that its commands still run.

## 5. In-game test round against a local seed (one batch, macOS via Steam)

- [ ] 5.1 Prepare the seed and server from the Archipelago 0.6.7 checkout (`~/PrivateProjects/Archipelago`):
  - A scratch `Players/player.yaml` (`name: Player`, `game: Rail Route`, `map: haarlem`, both DLCs false).
  - `.venv/bin/python Generate.py --player_files_path <scratch>/Players --outputpath <scratch>/output --spoiler 2`
  - `.venv/bin/python MultiServer.py <scratch>/output/AP_*.zip` (port 38281)
  - Settings: `{"interceptUpgradePurchases": true, "server": "localhost:38281", "slot": "Player"}`

  Verify that the server log shows the room is hosted and that the spoiler lists 43 locations (50 slots, minus the 6 Expect Delays slots and the hidden Custom Contracts variant).
- [ ] 5.2 Run the test round. The user clicks through each step, and every step is then confirmed against `Player.log` and the server console:
  1. **Connect.** Start a fresh Haarlem Endless game. The log shows `Connected to localhost:38281 as Player` and the Newtonsoft binding line, and there's no slot-data warning. The notification says "Archipelago connected", and the server shows the client joined.
  2. **Send a check.** Buy Autoblocks. The log shows `Check sent: Autoblocks (1000)`, and the server console shows the location checked. Whatever item was there (from the spoiler) arrives if it is this slot's own item.
  3. **Receive an item.** On the server, use `/send Player Autoblocks` (or another item from the spoiler). Autoblocks become buildable with an unlock popup, and the log shows the item and the sender.
  4. **Progressive.** `/send Player Progressive Track Speed`. 80 km/h becomes available, and 120 km/h doesn't.
  5. **Filler.** `/send Player Green XP Bundle`. The log shows `Filler item ignored`, and nothing changes.
  6. **Save/reload replay.** Save, go to the main menu, and load the save. The log shows `Effect state reset`, then a reconnect, `Resent <n> checks` covering every bought slot, and a "Restored <n> Archipelago items" notification. There are no unlock popups for replayed items, and autoblocks and 80 km/h work again.
  7. **Offline purchase.** Stop the server, buy a slot (the log records the check, nothing is sent, and the "Archipelago disconnected" notification appears), then restart the server and reload the save. The resend includes that slot, and the server shows it checked.
  8. **Connection failure.** Set a wrong `slot` and load the save. The failure notification appears, the log lists the server's errors, and the level stays playable.
  9. **Slot-data mismatch.** With the Haarlem seed, start a Prague Endless game. The log warns `expected map Haarlem, level has prague` and a notification appears. Then start Haarlem with Expect Delays enabled (if owned): the log warns about `expect_delays`.
  10. **Not Endless.** Play the Arrival Sensor mini-tutorial. No connection attempt appears in the log.
  11. **No settings.** Remove `server` and `slot`. `Archipelago offline: no server/slot configured` is logged, and F9 still works.
  12. **Open M3 questions.** Load Haarlem, Prague and Amsterdam (fresh Endless games). From the D8 diagnostics, record for each map:
      - whether any upgrade starts `Researched` or with a non-default locked state, or logs a `Game grant:`
      - the runtime tier thresholds (and whether they match Green 10/25 and Red 8/30)
      - the `Storage` value (`CuratedMapPack` or not)
  13. Optional: connect to a hosted `wss://` room on archipelago.gg to check TLS (design risk 2).

  Record the results in a new "AP connection test round" subsection of `FINDINGS.md`.

## 6. Roadmap and wrap-up

- [ ] 6.1 Update `ROADMAP.md`:
  - add `client-ap-connection` to the M3 row
  - mark the MultiClient.Net/Newtonsoft, main-thread, settings and test-round notes done
  - record the answers to the three test-round questions
  - list the follow-ups under M3 (or M5 where noted):
    - per-save checked and received state bound to the seed
    - the "Check sent" panel label and the offline notification wording
    - excluding a map's game-granted slots from the resend if step 12 finds any
    - goal completion (M5)
    - the in-game connect screen (M5)
    - filler handling (M4)

  Set M3's status to ✅ only if nothing blocking remains. Verify that `openspec validate client-ap-connection --strict` passes.
