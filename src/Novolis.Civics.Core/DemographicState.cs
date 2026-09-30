namespace Novolis.Civics.Core;

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
