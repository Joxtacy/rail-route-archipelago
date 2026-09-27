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
| M3 | Client ↔ server | The mod connects to an Archipelago server, sends checks and receives items | 🔜 | `2026-09-27-client-split-flags` |
| M4 | More checks and items | More locations, plus filler and trap items | ⬜ | |
| M5 | Playable release | Anyone can install and play a full seed from a release zip | ⬜ | |
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
- Add Archipelago.MultiClient.Net. Check for a clash with the game's Newtonsoft.Json 13.0.2.
- An Archipelago purchase handler replaces `SlotPurchaseHandler`'s logged check, and the network feeds `UpgradeReceiver.Receive`.
- ✅ Split flags (`client-split-flags`): the upgrade panel's `Researched` flag means "slot bought", and received items are tracked separately. So received items no longer go through `CompleteResearch` and never consume a slot's location. The APWorld's logic (M2) assumes this.
- ✅ The client's `Id` → location-name table must match the list in `apworld/README.md`. A test compares them (`client-split-flags`).
- Network callbacks are marshalled to the Unity main thread.
- Checked and received state is saved per save file and bound to the seed.
- The upgrade panel shows "Check sent" instead of the misleading "Not enough points".
- Connection settings (host, slot, password) come from the settings file for now.
- In-game test round. These questions came up while planning M2 and can only be answered in the game:
  - ✅ Do child upgrades work without their parent's item? Yes, for the two tested (2026-09-27, `client-split-flags` test round): Departure Sensor without Automatic Routing and Shunting Sensor without Shunting Commands can both be placed once received. Still untested: the arrival, routing and relay sensors, and the stabling sensor.
  - Log each upgrade's locked/researched state at level start on Haarlem, Prague and Amsterdam, to confirm that none of these maps overrides the upgrade set.
  - Log the tier thresholds at runtime, to see whether the serialized `SystemUpgradeTierDefaults` overrides the C# defaults (Green 10/25, Red 8/30).
  - Check whether any of the three maps comes from the Curated Map Pack DLC.

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
- Goal completion reported to the server.
- A release zip per OS (`mods/RailRouteArchipelago/` plus the `.apworld`).
- A player setup guide. Linux paths confirmed.

### M6 — Extras
- DeathLink. One idea from the community post: a map reset that keeps received items.
- A PopTracker pack.
- Maps as unlockable items.
- A DRM-free/GOG build: it may run natively on arm64 Macs, which would need its own Harmony verification.

## Before M2
- Contact the person who proposed a Rail Route Archipelago world in the community, to avoid duplicate work or to collaborate.
