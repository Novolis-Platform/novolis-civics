# Novolis.Civics.EconomyBridge

Maps civic fiscal **intent** onto Economy cash policy without merging kernels.

- `ToEconomyStatePolicy` — Civics rates → `StatePolicy` tax/transfer knobs
- `BindEconomyState` — store Economy State `LegalEntityId` on `NationState`
- `PeriodContextFromDelivery` — feed observed tax/transfer into civic settlement
