# Novolis.Civics.Core — bounded-minimum specification

Models a **nation as a political-economy unit**: regime, fiscal intent, civic stocks, and nation-level demography that settle each period. Inspired by stock–flow consistent state accounting (Economy kinship) and IR notions of legitimacy and capacity—not by game UI meters.

## In boundary

| Concept | Type |
|---------|------|
| Nation identity | `NationId`, `NationState` |
| Regime | `GovernmentType`, `GovernmentRules` |
| Fiscal intent | `FiscalPolicy` (rates/shares — not cash ledgers) |
| Civic stocks | `CivicState` (legitimacy, approval, corruption, HD, war fatigue) |
| Demography | `DemographicState` (population, working-age share, unemployment, net migration) |
| Period settlement | `CivicEngine.ApplyPeriod` |
| Host facts | `PeriodContext` (control, wars, shortages, research, optional net migration / unemployment) |
| Host outputs | `PeriodOutcome` (outlays, capability demand, emigration pressure, attractiveness) |

## Out of boundary

| Concern | Owner |
|---------|--------|
| Money-conserving tax/transfer settlement | `Novolis.Economy.Core` |
| Territory, wars, treaties, trade clearing, spatial population flows | `Novolis.Geopolitics.*` |
| Force domain stocks (land/air/naval) | Host / Geopolitics (consume `ForceCapabilityDemand`) |
| Visual / map presentation | Product hosts |
| Agent agendas | `Novolis.Civics.Agents` |

## Demography (Wave 1)

- When `DemographicState.Population ≤ 0`, tax uses the legacy GDP×rate path (backward compatible).
- When `Population > 0`, tax multiplies GDP capacity by a labor factor from working-age share × (1 − unemployment) in ~[0.55, 1.25] (does not invent a second absolute tax base).
- High household tax + weak transfers/HD + war fatigue raise `EmigrationPressure` for Geopolitics hosts.
- Host-supplied `NetMigration` updates population and drags legitimacy/approval on net outflow.
- Natural growth: `Population *= (1 + NaturalGrowthRate)` then apply net migration.

## Settlement authority

Only `CivicEngine` mutates civic stocks, demography aggregates, and simplified treasury/GDP/technology on `NationState` from policy + context. Agents and hosts adjust `FiscalPolicy` only.

When Economy is wired, hosts should prefer Economy cash for treasury truth and feed delivery facts into `PeriodContext` (or use `EconomyBridge` helpers). Sync spatial population from Geopolitics provinces into `DemographicState.Population` each period.

## Game use

`NationState` + `ApplyPeriod` is a complete loop for a single-nation game layer. Provide `PeriodContext` from your scenario; apply `PeriodOutcome.ForceCapabilityDemand` to combat and `EmigrationPressure` / `ImmigrationAttractiveness` to population migration.
