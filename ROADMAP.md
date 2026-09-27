# Roadmap

The order of work for the Rail Route Archipelago integration. It keeps each milestone to one line.
Each milestone becomes one or more OpenSpec changes (`/opsx:propose`), and those carry the specs, design and tasks.
Research notes and platform results live in [FINDINGS.md](FINDINGS.md).

Status: ✅ done · 🔜 next · ⬜ planned

| # | Milestone | Goal | Status | OpenSpec changes |
|---|---|---|---|---|
| M0 | Mod skeleton | The native mod loader loads our DLL; one cross-platform `net48` build | ✅ | none; predates OpenSpec (`feat(client): add hello-world Rail Route mod`) |
| M1 | Purchase interception | Harmony patches turn a system-upgrade purchase into a pending check; verified on macOS (Steam/Rosetta) and Windows | ✅ | `2026-09-27-add-harmony-upgrade-intercept` |
| M2 | APWorld v0 | A minimal Python `rail_route` world that generates a playable seed | 🔜 | |
| M3 | Client ↔ server | The mod connects to an Archipelago server, sends checks and receives items | ⬜ | |
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
- Questions to settle at the start (`/opsx:explore` or `/grill-me`):
  - Endless only, or Timetable and Rush Hour too?
  - Which maps?
  - Exactly what counts as a location?

### M3 — Client ↔ server
- Add Archipelago.MultiClient.Net. Check for a clash with the game's Newtonsoft.Json 13.0.2.
- An Archipelago purchase handler replaces `PendingCheckHandler`.
- Received items are granted via `ResearchController.CompleteResearch`, the same path F9 tests today.
- Network callbacks are marshalled to the Unity main thread.
- Checked and received state is saved per save file and bound to the seed.
- The upgrade panel shows "Check sent" instead of the misleading "Not enough points".
- Connection settings (host, slot, password) come from the settings file for now.

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
