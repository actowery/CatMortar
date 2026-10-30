# Contributing

Open an issue before proposing a broad behavior or balance change. Small bug fixes may go directly to a pull request.

## Development checks

```powershell
dotnet restore CatMortar.slnx --locked-mode
dotnet build Source/CatMortar/CatMortar.csproj --configuration Release --no-restore
dotnet test Tests/CatMortar.Tests/CatMortar.Tests.csproj --configuration Release --no-restore
./scripts/validate-package.ps1 -RequireBinaries
```

Keep gameplay changes narrowly scoped, preserve vanilla explosive behavior, add tests for pure logic changes, and update [MANUAL_TESTS.md](MANUAL_TESTS.md) when an in-game behavior changes.
