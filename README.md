# Cat Mortar

[![CI](https://github.com/actowery/CatMortar/actions/workflows/ci.yml/badge.svg)](https://github.com/actowery/CatMortar/actions/workflows/ci.yml)
[![Codecov](https://codecov.io/gh/actowery/CatMortar/graph/badge.svg)](https://codecov.io/gh/actowery/CatMortar)
[![CodeQL](https://github.com/actowery/CatMortar/actions/workflows/codeql.yml/badge.svg)](https://github.com/actowery/CatMortar/actions/workflows/codeql.yml)

Cat Mortar is a standalone RimWorld 1.6 and 1.7 mod by **konduct**. Build a cat-themed mortar after researching Mortars, designate a colony cat or corpse, and fire it as costly artillery.

## Features

- One living colony cat, cat corpse, or up to 20 cat meat is held in a dedicated suspended mortar chamber.
- Living cats receive a random orange, black, cream, or brown launch coat and remain the exact same pawn through launch and landing.
- A 4.5-tile Bomb blast, large vanilla-style blood field, spinning 1.25× cat flight, and catastrophic post-blast payload.
- Orange ignites, black EMPs and obscures, cream stuns, and brown produces a scaled anti-grain aftershock.
- Cat corpses give a weaker 3-tile blast and lingering toxic gas instead of a coat payload.
- Cat meat is manual-load-only: every loaded piece becomes one unpredictable direct-hit chunk scattered across a 7-tile field. Damage scales from 4 to 7 `meat slapped` damage per chunk and brief-stun chance rises from 15% to 35% as the rack fills; it leaves blood and lightly damages buildings. It never creates toxic gas, a coat payload, or a survivor.

## Build

Install the .NET 10 SDK selected by `global.json`, then run:

```powershell
dotnet restore CatMortar.slnx --locked-mode
dotnet build Source/CatMortar/CatMortar.csproj --configuration Release --no-restore
dotnet test Tests/CatMortar.Tests/CatMortar.Tests.csproj --configuration Release --no-restore
./scripts/validate-package.ps1 -RequireBinaries
./scripts/package.ps1
```

## License

MIT. See [LICENSE](LICENSE).
