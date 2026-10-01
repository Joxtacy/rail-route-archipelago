# Roadmap

The order of work for the Rail Route Archipelago integration. It keeps each milestone to one line.
Each milestone becomes one or more OpenSpec changes (`/opsx:propose`), and those carry the specs, design and tasks.
Research notes and platform results live in [FINDINGS.md](FINDINGS.md).

Status: ✅ done · 🔜 next · ⬜ planned

| # | Milestone | Goal | Status | OpenSpec changes |
|---|---|---|---|---|
| M0 | Mod skeleton | The native mod loader loads our DLL; one cross-platform `net48` build | ✅ | none; predates OpenSpec (`feat(client): add hello-world Rail Route mod`) |
| M1 | Purchase interception | Harmony patches turn a system-upgrade purchase into a pending check; verified on macOS (Steam/Rosetta) and Windows | ✅ | `2026-09-27-add-harmony-upgrade-intercept` |
| M2 | APWorld v0 | A minimal Python `rail_route` world that generates a playable seed | ✅ | `2026-09-27-add-apworld-v0` |
| M3 | Client ↔ server | The mod connects to an Archipelago server, sends checks and receives items | ✅ | `2026-09-27-client-split-flags`, `2026-09-27-client-ap-connection`, `2026-10-01-client-seed-binding` |
| M4 | More checks and items | More locations, plus filler and trap items | ⬜ | |
| M5 | Playable release | Anyone can install and play a full seed from a release zip | ⬜ | `2026-10-01-client-goal-completion`, `2026-10-01-client-notification-limits` |
| M6 | Extras | Optional features beyond a complete game | ⬜ | |

## Milestone notes

### M2 — APWorld v0
- Items: each system upgrade. Track speed, station count and contract offers are progressive.
- Locations: upgrade purchase slots (what M1 intercepts).
- Logic: tiers and colours (green/red XP) as regions or rules.
- Goal: earn the stars on one chosen Endless map.
- It comes before M3 because the client needs a generated slot to connect to, and the item/location ID tables are defined here.
- Settled: Endless only; the maps are Haarlem, Prague and Amsterdam; each upgrade slot is a location; the tier gates are throughput-item counts, to be tuned by playtesting.

### M3 — Client ↔ server
- ✅ Archipelago.MultiClient.Net 6.7.1 (net45) is bound to the game's Newtonsoft.Json 13, with no second copy shipped (`client-ap-connection`, FINDINGS.md "Client library").
- ✅ `SlotPurchaseHandler` sends the check when connected, every bought slot is resent on connect, and items from the server feed `UpgradeReceiver.Receive` by name (`client-ap-connection`).
- ✅ Split flags (`client-split-flags`): the upgrade panel's `Researched` flag means "slot bought", and received items are tracked separately. So received items no longer go through `CompleteResearch` and never consume a slot's location. The APWorld's logic (M2) assumes this.
- ✅ The client's `Id` → location-name table must match the list in `apworld/README.md`. A test compares them (`client-split-flags`).
- ✅ Network callbacks are marshalled to the Unity main thread, per session epoch (`client-ap-connection`).
- ✅ Connection settings (`server`, `slot`, `password`) come from the settings file for now (`client-ap-connection`).
- ✅ In-game test round against a local seed (`client-ap-connection`, FINDINGS.md "AP connection test round"). These questions came up while planning M2 and can only be answered in the game:
  - ✅ Do child upgrades work without their parent's item? Yes, for the two tested (2026-09-27, `client-split-flags` test round): Departure Sensor without Automatic Routing and Shunting Sensor without Shunting Commands can both be placed once received. Still untested: the arrival, routing and relay sensors, and the stabling sensor.
  - ✅ Does any map override the upgrade set? No. Haarlem, Prague and Amsterdam all start with every upgrade unbought and unlocked, with no game grants (`client-ap-connection` test round).
  - ✅ Runtime tier thresholds: Green 10/25 and Red 8/30 on all three maps, the C# defaults (plus Blue 2/5).
  - ✅ Curated Map Pack: Haarlem's `Storage` is `CuratedMapPack`, and Prague's and Amsterdam's is `Main`.
- Follow-ups (not blocking M3):
  - ✅ Save checked and received state per save file, bound to the seed: a `.ap.json` sidecar per save, the replay restores the save's items without side effects, and a save from another seed is refused (`client-seed-binding`, FINDINGS.md "Save files").
  - The upgrade panel's "Check sent" label instead of the misleading "Not enough points", and offline wording for the "Check sent" notification (it also shows when nothing was sent).
  - Check whether Haarlem needs the Curated Map Pack DLC. If it does, change the APWorld's default map.
  - A clearer connection-failure reason: an unreachable server logs `A task was canceled` after the real socket error.
  - Check a hosted `wss://` room (TLS on the game's Mono).
  - ✅ Logging in to a password-protected room, and buying a slot live on a map-mismatched level (checks stay blocked), both tested in the `client-seed-binding` test round.
  - The in-game connect screen (M5) and filler handling (M4).
  - ✅ Seed binding sends a goal from a bound save automatically and keeps a pending goal across game runs; the Shift+F10 fallback is retired (`client-seed-binding`).
  - Sidecars don't sync through Steam Cloud (it only syncs `saves/*.mp.lz4`). On another machine a synced save shows up unbound, and a star already in it can't be sent.

### M4 — More checks and items
- Locations:
  - Endless stars (green, red, total)
  - Tier unlocks
  - Cycle milestones
  - Achievement ladders (on-time dispatches, dispatches per train type, ongoing contracts)
  - Timetable and Rush Hour stars
- Filler: XP and money bundles.
- Traps: ideas include train delays or breakdowns (Expect Delays) and switch locks.

### M5 — Playable release
- An in-game connect screen.
- ✅ Goal completion reported to the server: the Endless-complete star earned live, or from a save bound to the seed, including a goal left pending offline in an earlier game run. A star from before the binding is never sent (`client-goal-completion`, `client-seed-binding`, FINDINGS.md "Goal completion").
- ✅ Side notifications don't pile up: routine ones expire after `notificationSeconds` (10), warnings and the goal stay until dismissed, and at most `notificationLimit` (5) are shown (`client-notification-limits`, FINDINGS.md "Side notifications").
- Follow-up: combine a burst of "Received …" notifications into one line.
- A release zip per OS (`mods/RailRouteArchipelago/` plus the `.apworld`).
- A player setup guide. Linux paths confirmed.

### M6 — Extras
- DeathLink. One idea from the community post: a map reset that keeps received items.
- A PopTracker pack.
- Maps as unlockable items.
- A DRM-free/GOG build: it may run natively on arm64 Macs, which would need its own Harmony verification.

## Before M2
- Contact the person who proposed a Rail Route Archipelago world in the community, to avoid duplicate work or to collaborate.
