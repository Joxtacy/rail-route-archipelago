# CLAUDE.md

This repo is an Archipelago integration for Rail Route: a C# client mod in `client/` and a Python APWorld in `apworld/rail_route/`.
Start with `ROADMAP.md` (where we are), `openspec/specs/` (what's built) and `FINDINGS.md` (game internals).

## Commands

- Build the mod and deploy it to the local game: `cd client && dotnet build -c Release` (add `-p:DeployMod=false` to skip the deploy)
- Unit tests: `dotnet test --project client.Tests` from the repo root. The root `global.json` opts into Microsoft.Testing.Platform.
- APWorld tests, from the Archipelago 0.6.7 checkout at `~/PrivateProjects/Archipelago` (it symlinks `worlds/rail_route` to this repo's `apworld/rail_route`, and has a Python 3.13 `.venv`): `.venv/bin/python -m pytest worlds/rail_route/test`. AP's generic suites: `.venv/bin/python -m pytest test/general -k "not Webhost"`. Setup is in `apworld/README.md`.
- Build the APWorld from the same checkout: `.venv/bin/python Launcher.py "Build APWorlds" -- "Rail Route"` writes `build/apworlds/rail_route.apworld`.
- Game log: macOS `~/Library/Logs/bitrich/Rail Route/Player.log`, Windows `%USERPROFILE%\AppData\LocalLow\bitrich\Rail Route\Player.log`. Mod lines start with `[Archipelago]`.

## Workflow

- Plan every non-trivial change with OpenSpec: `/opsx:propose` → `/opsx:apply` → `/opsx:archive`. Keep `ROADMAP.md` status in sync.
- Two commits per OpenSpec change:
  1. `docs(openspec): propose <change>` with the proposal artifacts only
  2. the implementation plus the archive move and synced `openspec/specs/`, in one `feat`/`fix` commit. Run `/opsx:archive` before committing it.
- Conventional Commits: `type(scope): summary`. Scopes are `client`, `apworld`, `openspec`.
- VCS is Jujutsu (colocated with git). Push with `jj bookmark set main -r @-` and then `jj git push`.

## Game and mod facts

- The mod uses the game's native loader (`Game.ModController`), not BepInEx. The files must be in `mods/RailRouteArchipelago/`; DLLs placed directly in `mods/` are silently ignored.
- The game logic is in `RailRoute.dll` and isn't obfuscated. Decompile it into a scratch folder (never into the repo) with the command in `FINDINGS.md`. Re-check patch targets after game updates.
- On Apple Silicon, test through Steam. The game runs under Rosetta because `libsteam_api.bundle` is x86_64-only, and native `arch -arm64` launches crash.
- Player purchases go through `UnlockUpgradeCommand.Run`. Every other unlock calls `ResearchController.CompleteResearch`, which must never be intercepted.
- In intercept mode on Endless levels, `ResearchItem.Researched` means "slot bought", and upgrade effects come from the patched `HasResearched`/`ResearchedValue` (and `IsAutomationEnabled`), which answer from received items.
- In-game behavior can only be verified by the user clicking through the game. Batch those checks into one test round with explicit steps, then confirm them against `Player.log`.
