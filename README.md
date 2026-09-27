# Rail Route Archipelago

An [Archipelago](https://archipelago.gg) multiworld randomizer integration for
[Rail Route](https://store.steampowered.com/app/1124180/Rail_Route/). It's a work in progress.

It has two parts:

- **Client mod** ([`client/`](client/)): a C# mod for the game's built-in mod loader. It uses Harmony to turn system-upgrade
  purchases into location checks. It runs on macOS and Windows (Steam builds).
- **APWorld**: the Python world definition, not started yet ([roadmap](ROADMAP.md) M2).

There's no Archipelago connection yet. See [ROADMAP.md](ROADMAP.md) for what's planned and
[FINDINGS.md](FINDINGS.md) for research notes on the game's internals and platform results.

## Development

- Build and install the mod: see [client/README.md](client/README.md).
- Tests: `dotnet test --project client.Tests`
- Planning uses [OpenSpec](https://github.com/Fission-AI/OpenSpec): current specs are in [`openspec/specs/`](openspec/specs/), and
  completed changes are in [`openspec/changes/archive/`](openspec/changes/archive/).
- Commits follow [Conventional Commits](https://www.conventionalcommits.org).

## Disclaimer

This is an unofficial fan project. It isn't affiliated with or endorsed by bitrich.info (the developers of Rail Route) or the Archipelago project.
Rail Route itself is required and isn't included.

## License

[MIT](LICENSE)
