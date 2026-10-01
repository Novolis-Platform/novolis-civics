<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-civics/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-civics/) · [Source](https://github.com/Novolis-Platform/novolis-civics)
<!-- novolis-pkg-brand:end -->

# Novolis.Civics.EconomyBridge

Maps civic fiscal **intent** onto Economy cash policy without merging kernels.

- `ToEconomyStatePolicy` — Civics rates → `StatePolicy` tax/transfer knobs
- `BindEconomyState` — store Economy State `LegalEntityId` on `NationState`
- `PeriodContextFromDelivery` — feed observed tax/transfer into civic settlement

## Install

```bash
dotnet add package Novolis.Civics.EconomyBridge
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (net10.0).

