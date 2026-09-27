# Design

## Context

See proposal.md for motivation. The requirements are in `specs/client/ap-connection`, `specs/client/location-checks` and `specs/client/received-items`.

**Client library** (Archipelago.MultiClient.Net 6.7.1, the latest on NuGet, read 2026-09-27):
- The package has no NuGet dependencies. Each `lib/` folder bundles its own `Newtonsoft.Json.dll`: `net35`, `net40`, `net45`, `netstandard2.0` and `net6.0`. `net35` and `net40` also bundle `websocket-sharp.dll`. `net45` and later use `System.Net.WebSockets.ClientWebSocket`.
- Assembly metadata, read with `System.Reflection.Metadata`:

  | Assembly | Identity | Newtonsoft.Json it references |
  |---|---|---|
  | `net45/Archipelago.MultiClient.Net.dll` | 6.7.1.0, unsigned | `Newtonsoft.Json 11.0.0.0, PublicKeyToken=null` |
  | `net45/Newtonsoft.Json.dll` (bundled) | 11.0.0.0 (file 11.0.1), **unsigned** | |
  | the game's `Managed/Newtonsoft.Json.dll` | 13.0.0.0 (file 13.0.2), **signed** | |

  So the two are different assembly identities. Shipping the bundled copy would load a second, unsigned `Newtonsoft.Json` next to the game's copy.
- The API used here:
  - `ArchipelagoSessionFactory.CreateSession(host, port)` or `CreateSession(Uri)`
  - `ConnectAsync` and `LoginAsync(game, name, ItemsHandlingFlags, version, tags, uuid, password, requestSlotData)`
  - `LoginSuccessful.SlotData`
  - `Locations.GetLocationIdFromName(game, name)`, `Locations.AllLocations` and `CompleteLocationChecksAsync(long[])`
  - `Items.GetItemName(id, game)` and `Players.GetPlayerName(slot)`
  - `Socket.PacketReceived`, `Socket.ErrorReceived` and `Socket.SocketClosed`

  MultiClient.Net raises all of them on socket or thread-pool threads.

**Game facts** (decompiled Rail Route 3.0.18, read 2026-09-27):
- `LevelController.Init(level, startSettings)` fires `BeforeLevelStarted` **before** it sets `StartSettings`, before `ResearchController.InitDefaults` runs, and before a level is cleared. Saved maps load through `SavedMap.Load` instead, which restores the bought slots. So `BeforeLevelStarted` is too early to read DLC settings or bought slots. `GameController.Loaded` becomes true once the level is fully running.
- A DLC is active in a level when `ModController.IsDlcOwned(dlc)` is true and `LevelController.StartSettings.ExpectDelaysEnabled` or `HappyPassengersEnabled` is set. This is the same test `SystemUpgradesPage.InstantiateUpgrades` uses to hide upgrades. Expect Delays can change during a level (`ExpectDelaysEnabledChanged`).
- For the test-round questions:
  - A map comes from the Curated Map Pack when `LevelDefinition.Storage == Storage.CuratedMapPack`.
  - Tier thresholds come from `ResearchController.UpgradeTiersConfigurations`, built from the `TierDefaults` asset (`SystemUpgradeTierDefaults`, C# defaults Green 10/25 and Red 8/30), and the star thresholds from `ThroughputRewards`.
- The current client already has `SplitFlags.Active`, `EffectState` (received counts plus game grants, cleared by the `ResearchController.Reset` postfix), `UpgradeReceiver.Receive(item)`, and `SlotPurchaseHandler`, which marks the slot bought and logs the check. `DebugReceiveKey` shows the pattern of a MonoBehaviour made at mod enable with `DontDestroyOnLoad`.

## Goals / Non-Goals

**Goals:**
- One place owns the connection, and no game object is touched off the main thread.
- The received-item state is always derived from the server's authoritative item list for the connection, so a resync can't double-count items.
- The pure logic stays in `client/src/Core` and has unit tests:
  - settings parsing (including the server address)
  - the item-name table and its README sync
  - the replay and index bookkeeping
  - choosing which bought slots to resend

**Non-Goals:**
- Automatic reconnect with backoff. A failure or disconnect is reported, and the next level load tries again. A retry key or connect screen belongs with the M5 connect screen.
- Handling DataStorage, hints, chat or the `!` commands. The console log of server messages beyond errors is left out.
- DeathLink tags, and any `Bounce` or `Say` traffic.

## Decisions

### D1: Use the `net45` build and bind it to the game's Newtonsoft.Json
- Reference `lib/net45/Archipelago.MultiClient.Net.dll` directly: a `PackageReference` with `ExcludeAssets="all"` and `GeneratePathProperty="true"`, plus a `Reference` whose `HintPath` points at `$(PkgArchipelago_MultiClient_Net)`. Neither the bundled Newtonsoft.Json nor its compile reference enters the build, so the mod keeps compiling against the game's 13.0.2. `DeployMod` copies `Archipelago.MultiClient.Net.dll` (and its `.pdb`).
- At mod enable, before any MultiClient type loads, register an `AppDomain.CurrentDomain.AssemblyResolve` handler. For a request whose simple name is `Newtonsoft.Json`, it returns the game's already loaded `Newtonsoft.Json` assembly. Mono may bind the unsigned `11.0.0.0` reference to the loaded 13.0.0.0 by simple name without asking; the handler covers the case where it doesn't. The first connect logs `typeof(JsonConvert).Assembly` as seen from MultiClient (spec: "Library loads on the game's runtime").
- Newtonsoft 13 is a compatible superset of the 11 API MultiClient uses (`JsonConvert`, `JToken`/`JObject`, `StringEnumConverter` and `SnakeCaseNamingStrategy`).
- The client code never touches Newtonsoft types through MultiClient's API. Slot-data values are read with `Convert.ToString` and `bool.TryParse`. So if the fallback below is ever needed, the rest of the design doesn't change.
- *Alternative: ship the bundled unsigned 11.0 copy side by side.* Two assemblies named `Newtonsoft.Json` in one AppDomain is exactly the clash the roadmap warns about. Whichever copy Mono binds first by simple name wins for every later bind, which could include other mods'. This is rejected unless the spike (task 1) shows the binding to 13.0.2 fails, and the result is recorded in FINDINGS.md.
- *Alternative: `net40` with websocket-sharp.* It's only kept as a fallback if `ClientWebSocket` doesn't work on the game's Mono (for example `wss://` with TLS). It has the same Newtonsoft question, plus one more DLL to ship.

### D2: Connect when the level is loaded, found by polling on the main thread
A new `ArchipelagoPump` MonoBehaviour, created once at mod enable with `DontDestroyOnLoad` like `DebugReceiveKey`, checks each frame:
- If `SplitFlags.Active && GameController.Loaded` and the current `Level` isn't the one it is connected (or connecting) for, it disconnects any old session and starts a new one for this level.
- If there is a session and `SplitFlags.Active` turned false, or `CurrentLevel` changed, it disconnects.

*Alternative: `BeforeLevelStarted`.* It fires before start settings and saved slots exist, and it isn't confirmed on the saved-map path. The string event `LevelStarted` would also work, but polling one reference per frame is cheap, covers every load path, and puts the disconnect in the same place.

Connect and login run as tasks (`ConnectAsync` followed by `LoginAsync` with `ItemsHandlingFlags.AllItems`, `requestSlotData: true`, and the version pinned to the protocol version the 0.6.7 server accepts). They never block a frame. Their result comes back through the queue in D3.

### D3: One main-thread queue, stamped with a session epoch
- `MainThreadQueue` is a `ConcurrentQueue<(int epoch, Action action)>`. Every network callback enqueues an action instead of acting.
- The pump drains the queue in `Update` and runs an action only if its epoch equals the current session's epoch. The epoch goes up on every connect and disconnect, so events from an old connection are dropped (spec: "Late event after leaving the level").
- Exceptions inside actions are caught and logged, and never escape into Unity's loop.
- The pure queue and epoch logic lives in `Core` so it can be unit-tested.

### D4: Items come from `ReceivedItems` packets, and index 0 means "replace everything"
- The client handles `ReceivedItemsPacket` from `Socket.PacketReceived` rather than the per-item `ItemReceived` event, because the packet carries the `Index` that tells a full list apart from an increment.
  - `Index == 0`: this is the full list, sent after login or after a sync. On the main thread, the client clears the received counts only (not the game grants) and applies the list.
    - Right after the `Connected` packet it is the replay: every item is applied silently, followed by one "Restored N items" notification. The server only sends it there when the slot has items (`MultiServer.py`, the `Connect` reply), and MultiClient raises one message's packets in order on one thread, so "the packet right after `Connected`" identifies it.
    - Anywhere else, the items the client had already applied (`ItemSync.Known`) are silent and the rest get popups. This case is the first item of a slot that had none at login, which also arrives at index 0 (found in test-round step 2), or a resync reply.
  - `Index == expected`: the client applies each item normally, with popups, and advances `expected`.
  - Any other index: the client logs it and sends a `Sync` packet. The server answers with an index-0 full list, which replaces the state.

  This keeps `ReceivedItems` equal to the server's list, whether items arrive late, twice, or out of order.
- Mapping is by name: `Items.GetItemName(id, "Rail Route")` gives the datapackage name, and `Core/ItemNames` maps the name to a game upgrade `Id`. For a levelled upgrade, that's the first item of its chain, and `UpgradeReceiver` already resolves the next level. For Custom Contracts it's `custom_contracts`, and the router passes the variant the level shows (`custom_contracts_alt` with Happy Passengers active), because `EffectState` keys both variants the same way. Filler names are logged and ignored. Unknown names warn with the ID.
- *Alternative: hard-code AP item IDs in the client.* That would copy a second table out of `data.py`. Names are already the documented contract (the location table works the same way), and the datapackage exists to map them. The new "Item names" README table records the name, the item ID and the game `Id`s. `ItemNamesTests` checks names and game `Id`s against it. The IDs are there for readers and trackers.
- `UpgradeReceiver.Receive(item, silent)`: `silent` skips the `UnlockPopup` and still applies the side effects and raises `ResearchCompleted` for the UI refresh. The log line stays, so the replay is visible in `Player.log`. When a non-silent receive can't show the popup (the system upgrades menu is open, which is always the case for the item from one's own purchase), it shows a side notification "Received <upgrade> from <player>" instead (added after test-round step 2).

### D5: Checks resolve through the datapackage; the resend on connect is derived from slot flags
- `IUpgradePurchaseHandler` stays as it is. `SlotPurchaseHandler` keeps its current behavior and also calls `CheckSender.Send(locationName)`. It does nothing when not connected, because the resend covers that case (spec: "Buy a slot while not connected").
- `CheckSender` resolves `Locations.GetLocationIdFromName("Rail Route", name)`. It skips `-1`, and IDs missing from `Locations.AllLocations` (the slot's own locations), with a warning. It sends with `CompleteLocationChecksAsync` and logs name and ID. A failed send is logged through the queue. The next connect's resend retries it.
- On login success, `CheckSender.ResendAll` collects every `ResearchItem` with `Researched == true`. It excludes the IDs `EffectState` recorded as game grants, maps them through `LocationNames`, de-duplicates the IDs (both Custom Contracts variants give one ID) and sends one batch. The pure selection (`Researched`, granted and known-location sets) lives in `Core` so it can be tested.
- *Alternative: keep a separate "checks sent" set per save.* Bought slots already persist with the save and resending is idempotent on the server, so a second store adds nothing until per-seed state (a follow-up).

### D6: Settings and the server address
`ModSettings` gets `server`, `slot` and `password`. The server-address parser in `Core`:
- accepts `host:port`
- accepts `ws://…` or `wss://…` URIs, which go to `CreateSession(Uri)`
- accepts a bare `host`, which gets port 38281, the AP default

An unparsable address is logged and treated as "no server". `password` goes only to `LoginAsync` and is never logged or included in the settings log line.

### D7: Slot-data check
It runs on the main thread right after login:
- `map` is compared with `LevelDefinition.Uuid`, ordinal and case-sensitive (`"prague"` is lower-case in the game).
- `expect_delays` and `happy_passengers` are compared with `IsDlcOwned(dlc) && StartSettings.<Dlc>Enabled`.

Each mismatch gives one warning line, and together they give one notification. The client doesn't disconnect: the player may be testing deliberately, and M5's connect screen can refuse.

A **map** mismatch also blocks every check for the connection, both the resend and live sends. The test round showed why: a Prague save auto-loaded at startup resent its 6 bought slots into a Haarlem seed and checked those locations for good. A DLC mismatch doesn't block. Expect Delays locations that aren't in the seed are already skipped by the `AllLocations` check, and both Custom Contracts variants are one location. Received items still apply either way, since their effects last only for the level.

### D8: Diagnostics for the test-round questions
At each Endless level load with intercept mode on, and independent of the connection, the client logs one line per upgrade: `Id`, `Researched`, locked state and whether it is a game grant. It also logs the level's `Storage` value and the runtime `UpgradeTiersConfigurations` and `ThroughputRewards` thresholds. These lines answer the three open M3 questions from `Player.log` in one test round. They stay in as startup diagnostics, since they are cheap and useful after game updates.

## Risks / Trade-offs

- [Mono refuses to bind the unsigned `Newtonsoft.Json 11.0.0.0` reference to the loaded signed 13.0.0.0, even through `AssemblyResolve`] → Task 1 is a spike that loads MultiClient in the real game before anything else is built. If it fails, fall back to shipping the bundled copy (D1, second alternative) and record why in FINDINGS.md.
- [`ClientWebSocket` misbehaves on Unity 2021 Mono, for example on TLS for `wss://` rooms on archipelago.gg] → The test round covers a local `ws://` server. A `wss://` check against a hosted room is a separate test-round step. The fallback is the `net40` build with websocket-sharp.
- [A slot the game granted in an earlier session is indistinguishable, after a save reload, from a bought slot: `SavedResearchItem.Load` sets `Researched` without an event, so no grant is recorded] → On Haarlem, no game grants were observed (FINDINGS.md). The D8 diagnostics answer this for Prague and Amsterdam. If one of those maps grants upgrades, a follow-up change excludes that map's grant set explicitly.
- [The "Check sent" notification also appears while offline, when nothing was sent] → Accepted for this change. The upgrade-panel label and the notification wording are an M3 follow-up. The log still distinguishes "recorded" from "sent".
- [The replay applies each item's side effects again, for example auto-accept on every station, after a reload in which the player turned it off on some stations] → This matches what receiving the item does in the unmodded unlock. Per-save received state (a follow-up) would avoid replaying effects that are already applied.
- [A late packet after a level change] → The epoch check (D3) drops it.
- [A server that dies without a WebSocket close handshake] → MultiClient 6.7.1 raises only `ErrorReceived`, and `SocketClosed` fires only for a close frame (found in test-round step 7). The session treats a socket error that leaves the socket not `Connected` as the disconnect.

## Migration Plan

The settings are additive: without `server` and `slot`, the mod behaves exactly as it does now (offline, F9). The only new file in the mod folder is `Archipelago.MultiClient.Net.dll` (plus its `.pdb`). To roll back, remove the settings keys or the DLL and rebuild without the change.

## Open Questions

These don't change the approach, and the D8 diagnostics answer them in the test round:
- Does Haarlem, Prague or Amsterdam override the upgrade set (any `Researched` or locked state at level start that differs from the default)?
- What are the runtime tier thresholds? Does the serialized `SystemUpgradeTierDefaults` override the C# defaults (Green 10/25, Red 8/30)?
- Does any of the three maps come from the Curated Map Pack DLC (`Storage.CuratedMapPack`)?
