# Demography coupling (Wave 1)

Closed loop across Civics, Economy, and Geopolitics for scientific and entertainment hosts.

## Authority

| Stock | Owner | Sync |
|-------|--------|------|
| `Province.Population` | Geopolitics | Spatial truth; mutated by `PopulationMigration.RunMonth` |
| `DemographicState` on `NationState` | Civics | Host / geo CivicEngine syncs from Σ owned province population |
| `HouseholdCohort.HouseholdCount` | Economy | Host scales from owned population (e.g. PolityTriad) |

## Month order

1. Trade clearing (balances / shortages)
2. Economy period (tax-sensitive cohort migrate + production)
3. Civics `ApplyPeriod` (emigration pressure, labor-capacity tax factor, legitimacy vs net migration)
4. `PopulationMigration.RunMonth` (apply pressures + tax/HD differentials on the map)
5. Treaty effects / agents / conflict as usual

## Policy smothering

High `HouseholdTaxRate` + weak transfers/HD raises `EmigrationPressure`. Geopolitics moves people toward high `ImmigrationAttractiveness` / low-tax neighbors. Net out-migration drags legitimacy and approval. Fiscal agents ease tax when pressure is high and treasury allows.

## Wave 2 / 3 roadmap

- **Wave 2:** education/health spend → HD → productivity; protest/unrest stock; refugee corridors; richer multi-objective agents.
- **Wave 3:** age structure / dependency ratios; progressive tax incidence; multi-region fiscal map in Economy; calibration harness against stylized facts.

## Packages

- `Novolis.Civics.Core` — `DemographicState`, pressure outcomes
- `Novolis.Economy.Core` — tax-push migrate in `HouseholdConsumeMigrateStep`
- `Novolis.Geopolitics.Core` — `PopulationMigration`, pop-weighted control
- Dogfood: `PolityTriad` evidence report includes population PASS checks
