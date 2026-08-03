# Layering

```text
Hosts / games / GeoPolity
        │
        ▼
Novolis.Civics.Agents          — adjust FiscalPolicy only
        │
        ▼
Novolis.Civics.Simulation      — AdvancePeriod(s), wire context
        │
   ┌────┴────┐
   ▼         ▼
Core      EconomyBridge ──► Novolis.Economy.Core (StatePolicy / cash)
   │
   └── PeriodContext ◄── Geopolitics / host (control, wars, shortages)
```

Core never references Economy or Geopolitics assemblies.
