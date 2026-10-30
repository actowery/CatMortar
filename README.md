# Cat Mortar

[![CI](https://github.com/actowery/CatMortar/actions/workflows/ci.yml/badge.svg)](https://github.com/actowery/CatMortar/actions/workflows/ci.yml)
[![Codecov](https://codecov.io/gh/actowery/CatMortar/graph/badge.svg)](https://codecov.io/gh/actowery/CatMortar)
[![CodeQL](https://github.com/actowery/CatMortar/actions/workflows/codeql.yml/badge.svg)](https://github.com/actowery/CatMortar/actions/workflows/codeql.yml)

Cat Mortar is a standalone RimWorld 1.6 and 1.7 mod by **konduct**. Build a cat-themed mortar after researching Mortars, designate a colony cat or corpse, and fire it as costly artillery.

## Features

- One living colony cat or cat corpse is held in a dedicated suspended mortar chamber.
- Living cats receive a random orange, black, cream, or brown launch coat and remain the exact same pawn through launch and landing.
- A 4.5-tile Bomb blast, large vanilla-style blood field, spinning 1.25× cat flight, and catastrophic post-blast payload.
- Orange ignites, black EMPs and obscures, cream stuns, and brown produces a scaled anti-grain aftershock.
- Cat corpses give a weaker 3-tile blast and lingering toxic gas instead of a coat payload.

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
