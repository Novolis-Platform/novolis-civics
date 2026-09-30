namespace Novolis.Civics.Core;

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
