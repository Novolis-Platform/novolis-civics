namespace Novolis.Civics.Core;

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
