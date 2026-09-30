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
