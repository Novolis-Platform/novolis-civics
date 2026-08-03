namespace Novolis.Civics.Core;

/// <summary>Stable nation identity (Guid — bridges cleanly to Economy <c>LegalEntityId</c>).</summary>
public readonly record struct NationId(Guid Value) : IEquatable<NationId>
{
    public static NationId New() => new(Guid.NewGuid());

    public static NationId From(Guid value) => new(value);

    public override string ToString() => Value.ToString("N");
}

/// <summary>Domestic regime type; affects fiscal and civic modifiers.</summary>
public enum GovernmentType
{
    Democracy = 0,
    Multiparty = 1,
    SingleParty = 2,
    Autocracy = 3,
    MilitaryJunta = 4,
    Monarchy = 5,
}

/// <summary>
/// Fiscal policy intent (shares/rates). Cash conservation lives in Economy;
/// this package uses treasury/GDP as optional simplified stocks for standalone/game use.
/// </summary>
public sealed class FiscalPolicy
{
    /// <summary>Household/income tax rate in [0, 0.6].</summary>
    public double HouseholdTaxRate { get; set; } = 0.22;

    /// <summary>Share of tax revenue paid as transfers in [0, 0.8].</summary>
    public double TransferShare { get; set; } = 0.25;

    /// <summary>Share of remaining civilian budget to infrastructure (HD) in [0, 1].</summary>
    public double InfrastructureShare { get; set; } = 0.45;

    /// <summary>Share of remaining civilian budget to propaganda/approval in [0, 1].</summary>
    public double PropagandaShare { get; set; } = 0.20;

    /// <summary>Military budget share of post-transfer funds in [0, 0.7].</summary>
    public double MilitaryShare { get; set; } = 0.28;

    public static FiscalPolicy Default { get; } = new();

    public FiscalPolicy Clone() => new()
    {
        HouseholdTaxRate = HouseholdTaxRate,
        TransferShare = TransferShare,
        InfrastructureShare = InfrastructureShare,
        PropagandaShare = PropagandaShare,
        MilitaryShare = MilitaryShare,
    };
}

/// <summary>Civic stocks (period-settled).</summary>
public sealed class CivicState
{
    public double Legitimacy { get; set; } = 0.65;
    public double Approval { get; set; } = 0.55;
    public double Corruption { get; set; } = 0.15;
    public double HumanDevelopment { get; set; } = 0.55;
    public double WarFatigue { get; set; }
    public double LastTaxCollected { get; set; }
    public double LastTransfersPaid { get; set; }

    public CivicState Clone() => new()
    {
        Legitimacy = Legitimacy,
        Approval = Approval,
        Corruption = Corruption,
        HumanDevelopment = HumanDevelopment,
        WarFatigue = WarFatigue,
        LastTaxCollected = LastTaxCollected,
        LastTransfersPaid = LastTransfersPaid,
    };
}

/// <summary>Regime modifiers (tax sensitivity, propaganda, military upkeep pressure).</summary>
public static class GovernmentRules
{
    public static double MilitaryUpkeepFactor(GovernmentType g) => g switch
    {
        GovernmentType.MilitaryJunta => 0.78,
        GovernmentType.Autocracy => 0.92,
        GovernmentType.Democracy or GovernmentType.Multiparty => 1.05,
        _ => 1.0,
    };

    public static double PropagandaEffectiveness(GovernmentType g) => g switch
    {
        GovernmentType.Autocracy or GovernmentType.SingleParty => 1.35,
        GovernmentType.MilitaryJunta => 1.15,
        GovernmentType.Democracy => 0.75,
        _ => 1.0,
    };

    public static double TaxApprovalSensitivity(GovernmentType g) => g switch
    {
        GovernmentType.Democracy or GovernmentType.Multiparty => 1.4,
        GovernmentType.Autocracy or GovernmentType.MilitaryJunta => 0.7,
        _ => 1.0,
    };

    public static GovernmentType Roll(Random rng)
    {
        var r = rng.NextDouble();
        return r switch
        {
            < 0.28 => GovernmentType.Democracy,
            < 0.48 => GovernmentType.Multiparty,
            < 0.62 => GovernmentType.SingleParty,
            < 0.78 => GovernmentType.Autocracy,
            < 0.90 => GovernmentType.MilitaryJunta,
            _ => GovernmentType.Monarchy,
        };
    }
}
