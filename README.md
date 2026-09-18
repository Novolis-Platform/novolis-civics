<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start — embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.Civics.Agents` | `dotnet add package Novolis.Civics.Agents` | [README](https://github.com/Novolis-Platform/novolis-civics/blob/main/src/Novolis.Civics.Agents/README.md) |
| `Novolis.Civics.Core` | `dotnet add package Novolis.Civics.Core` | [README](https://github.com/Novolis-Platform/novolis-civics/blob/main/src/Novolis.Civics.Core/README.md) |
| `Novolis.Civics.EconomyBridge` | `dotnet add package Novolis.Civics.EconomyBridge` | [README](https://github.com/Novolis-Platform/novolis-civics/blob/main/src/Novolis.Civics.EconomyBridge/README.md) |
| `Novolis.Civics.Simulation` | `dotnet add package Novolis.Civics.Simulation` | [README](https://github.com/Novolis-Platform/novolis-civics/blob/main/src/Novolis.Civics.Simulation/README.md) |

For NuGet.org and Visual Studio, the **embedded** README.md inside each package is authoritative.

<!-- novolis-package-index:end -->

<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/banners/novolis-civics.svg" width="100%" alt="novolis-civics"/>
</p>

<p align="center">
  <strong>Civic agents and firm bridges</strong><br/>
  Civics agents, core ledgers, and economy bridges for polity sims.
</p>

<p align="center">
  <a href="https://novolis-platform.github.io/.github/novolis-civics/"><img src="https://img.shields.io/badge/docs-portfolio-0a7ea3" alt="docs"/></a>
  <a href="https://github.com/Novolis-Platform/novolis-civics/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-civics/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-civics"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://novolis-platform.github.io/.github/novolis-civics/">Docs</a>
  ·
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
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

Cross-repo: open `d:\novolis\Novolis.Platform.slnx` (ProjectReference mode). Package sources: nuget.org + GitHub Packages only.

