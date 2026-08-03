# Novolis.Civics.Core — bounded-minimum specification

Models a **nation as a political-economy unit**: regime, fiscal intent, and civic stocks that settle each period. Inspired by stock–flow consistent state accounting (Economy kinship) and IR notions of legitimacy and capacity—not by game UI meters.

## In boundary

| Concept | Type |
|---------|------|
| Nation identity | `NationId`, `NationState` |
| Regime | `GovernmentType`, `GovernmentRules` |
| Fiscal intent | `FiscalPolicy` (rates/shares — not cash ledgers) |
| Civic stocks | `CivicState` (legitimacy, approval, corruption, HD, war fatigue) |
| Period settlement | `CivicEngine.ApplyPeriod` |
| Host facts | `PeriodContext` (control, wars, shortages, research multiplier) |
| Host outputs | `PeriodOutcome` (outlays + capability demand) |

## Out of boundary

| Concern | Owner |
|---------|--------|
| Money-conserving tax/transfer settlement | `Novolis.Economy.Core` |
| Territory, wars, treaties, trade clearing | `Novolis.Geopolitics.*` |
| Force domain stocks (land/air/naval) | Host / Geopolitics (consume `ForceCapabilityDemand`) |
| Visual / map presentation | Product hosts |
| Agent agendas | `Novolis.Civics.Agents` |

## Settlement authority

Only `CivicEngine` mutates civic stocks and simplified treasury/GDP/technology on `NationState` from policy + context. Agents and hosts adjust `FiscalPolicy` only.

When Economy is wired, hosts should prefer Economy cash for treasury truth and feed delivery facts into `PeriodContext` (or use `EconomyBridge` helpers).

## Game use

`NationState` + `ApplyPeriod` is a complete loop for a single-nation game layer. Provide `PeriodContext` from your scenario; apply `PeriodOutcome.ForceCapabilityDemand` to your combat model.
