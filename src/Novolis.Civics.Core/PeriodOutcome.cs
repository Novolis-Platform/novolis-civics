namespace Novolis.Civics.Core;

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
