<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-civics">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
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

