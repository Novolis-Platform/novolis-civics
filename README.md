# Novolis.Civics

Academic **nation / polity** libraries: regime, fiscal intent, and civic stock–flow (legitimacy, approval, capacity).

Usable as a rigorous political-economy kernel **and** as a game-facing nation model. Not a game engine; not a map/war/trade engine.

| Package | Role |
|---------|------|
| `Novolis.Civics.Core` | Kernel — [`SPEC.md`](src/Novolis.Civics.Core/SPEC.md) |
| `Novolis.Civics.EconomyBridge` | Map fiscal intent ↔ `Novolis.Economy.Core` `StatePolicy` |
| `Novolis.Civics.Simulation` | Period composition for standalone / host wiring |
| `Novolis.Civics.Agents` | Heuristic fiscal/regime agents (knobs only) |

## Boundaries

| Owns | Does not own |
|------|----------------|
| Government type, fiscal **intent**, civic stocks, period civic settlement | Money-conserving ledgers (`Economy.Core`) |
| Stability / HD / legitimacy dynamics | Territory, wars, treaties (`Geopolitics`) |
| Capability **demand** signals for hosts | Instant force unit builds / StarMap |

Kinship: Economy settles cash; Civics settles political stocks; Geopolitics supplies control/war/shortage **context** and may consume capability demand.

## Local build

```powershell
dotnet test d:\novolis\novolis-civics\tests\Novolis.Civics.Unit\Novolis.Civics.Unit.csproj -p:NovolisUseProjectReferences=true
```

Cross-repo: open `d:\novolis\novolis-governance\build\Novolis.Platform.slnx` (ProjectReference mode). Package sources: nuget.org + GitHub Packages only.
