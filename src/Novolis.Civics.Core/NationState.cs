namespace Novolis.Civics.Core;

/// <summary>
/// Nation as a political-economy unit. Territory, wars, and force domain stocks are host concerns.
/// </summary>
public sealed class NationState
{
    public required NationId Id { get; init; }
    public required string Name { get; set; }
    public GovernmentType Government { get; set; } = GovernmentType.Democracy;
    public FiscalPolicy Policy { get; init; } = new();
    public CivicState Civic { get; init; } = new();

    /// <summary>Aggregate output measure (game/geo compatible).</summary>
    public double Gdp { get; set; } = 100_000;

    /// <summary>Simplified treasury when Economy cash is not wired.</summary>
    public double Treasury { get; set; } = 10_000;

    /// <summary>Operational stability synthesis [0, 1].</summary>
    public double Stability { get; set; } = 0.6;

    /// <summary>Technology stock (productivity / capability index).</summary>
    public double TechnologyStock { get; set; } = 1.0;

    /// <summary>Progress toward next technology stock increment.</summary>
    public double TechnologyProgress { get; set; }

    /// <summary>Optional Economy State legal-entity binding.</summary>
    public Guid? EconomyStateEntityId { get; set; }

    /// <summary>Nation-level demography (synced from Geopolitics / Economy hosts).</summary>
    public DemographicState Demography { get; init; } = new();

    public static NationState Create(string name, GovernmentType? government = null, int? seed = null)
    {
        var rng = seed is { } s ? new Random(s) : Random.Shared;
        return new NationState
        {
            Id = NationId.New(),
            Name = name,
            Government = government ?? GovernmentRules.Roll(rng),
        };
    }
}

/// <summary>
/// Nation demography stocks. Spatial distribution lives in Geopolitics provinces;
/// labor cohorts live in Economy — hosts sync into this aggregate.
/// </summary>
public sealed class DemographicState
{
    /// <summary>Total population. When ≤ 0, tax uses GDP-only path (backward compatible).</summary>
    public double Population { get; set; }

    /// <summary>Share of population in working age [0, 1].</summary>
    public double WorkingAgeShare { get; set; } = 0.65;

    /// <summary>Monthly natural growth rate (e.g. 0.0008 ≈ 1%/year).</summary>
    public double NaturalGrowthRate { get; set; } = 0.0008;

    /// <summary>Unemployment proxy [0, 1] (host-observed or engine estimate).</summary>
    public double Unemployment { get; set; }

    /// <summary>Last period net migration (people; +in / −out).</summary>
    public double LastNetMigration { get; set; }

    /// <summary>Last computed emigration pressure [0, 1] (also on PeriodOutcome).</summary>
    public double LastEmigrationPressure { get; set; }

    public DemographicState Clone() => new()
    {
        Population = Population,
        WorkingAgeShare = WorkingAgeShare,
        NaturalGrowthRate = NaturalGrowthRate,
        Unemployment = Unemployment,
        LastNetMigration = LastNetMigration,
        LastEmigrationPressure = LastEmigrationPressure,
    };
}

/// <summary>External facts supplied by Geopolitics / game hosts for one period.</summary>
public sealed class PeriodContext
{
    public double ControlRatio { get; init; } = 1.0;
    public int ActiveWars { get; init; }
    public double ResourceShortage { get; init; }
    public bool OccupyingForeignLand { get; init; }
    public bool LostHomeTerritory { get; init; }
    public double ResearchMultiplier { get; init; } = 1.0;

    /// <summary>
    /// When set (≥ 0), overrides engine tax collection with host/Economy delivery
    /// (e.g. actual tax receipts from an Economy period).
    /// </summary>
    public double? ObservedTaxCollected { get; init; }

    /// <summary>When set (≥ 0), overrides transfer payment with observed Economy transfers.</summary>
    public double? ObservedTransfersPaid { get; init; }

    /// <summary>Host-observed net migration this period (people; +in / −out).</summary>
    public double? NetMigration { get; init; }

    /// <summary>Host-observed unemployment [0, 1].</summary>
    public double? UnemploymentObserved { get; init; }

    public static PeriodContext Neutral { get; } = new();
}

/// <summary>Period settlement outputs for hosts (Economy, Geopolitics, games).</summary>
public sealed class PeriodOutcome
{
    public double TaxCollected { get; init; }
    public double TransfersPaid { get; init; }
    public double InfrastructureOutlay { get; init; }
    public double PropagandaOutlay { get; init; }
    public double MilitaryOutlay { get; init; }

    /// <summary>
    /// Dimensionless capability accumulation demand for the period.
    /// Hosts map this onto force stocks; Civics does not own land/air/naval units.
    /// </summary>
    public double ForceCapabilityDemand { get; init; }

    /// <summary>Push factor [0, 1] for Geopolitics population migration.</summary>
    public double EmigrationPressure { get; init; }

    /// <summary>Pull factor [0, 1] for inbound migration attractiveness.</summary>
    public double ImmigrationAttractiveness { get; init; }

    /// <summary>Hint for hosts mapping working-age pop × (1 − unemployment) to labor.</summary>
    public double LaborForceDemandHint { get; init; }
}
