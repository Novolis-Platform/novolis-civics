using Novolis.Civics.Core;
using Novolis.Economy.Core;

namespace Novolis.Civics.EconomyBridge;

/// <summary>Adapters between Civics fiscal intent and Economy Core State policy / cash facts.</summary>
public static class CivicEconomyBridge
{
    /// <summary>
    /// Project civic tax/transfer intent into Economy <see cref="StatePolicy"/>.
    /// Firm tax / reserve / insurance / wage remain host-supplied (Economy-specific).
    /// </summary>
    public static StatePolicy ToEconomyStatePolicy(
        FiscalPolicy civic,
        Money transferPerHousehold,
        Money wagePerLaborHour,
        decimal firmTaxRate = 0m,
        decimal depositReserveRequirement = 0m,
        decimal insuranceCapitalRequirement = 0m)
    {
        ArgumentNullException.ThrowIfNull(civic);
        return new StatePolicy(
            HouseholdTaxRate: (decimal)Math.Clamp(civic.HouseholdTaxRate, 0, 0.6),
            FirmTaxRate: firmTaxRate,
            TransferPerHousehold: transferPerHousehold,
            DepositReserveRequirement: depositReserveRequirement,
            InsuranceCapitalRequirement: insuranceCapitalRequirement,
            WagePerLaborHour: wagePerLaborHour);
    }

    /// <summary>Bind this nation to an Economy State legal entity.</summary>
    public static void BindEconomyState(NationState nation, LegalEntityId stateEntityId)
    {
        ArgumentNullException.ThrowIfNull(nation);
        nation.EconomyStateEntityId = stateEntityId.Value;
    }

    /// <summary>
    /// Build a civic <see cref="PeriodContext"/> that uses observed Economy delivery
    /// instead of the simplified GDP×rate tax model.
    /// </summary>
    public static PeriodContext PeriodContextFromDelivery(
        double taxCollected,
        double transfersPaid,
        PeriodContext? baseContext = null)
    {
        var b = baseContext ?? PeriodContext.Neutral;
        return new PeriodContext
        {
            ControlRatio = b.ControlRatio,
            ActiveWars = b.ActiveWars,
            ResourceShortage = b.ResourceShortage,
            OccupyingForeignLand = b.OccupyingForeignLand,
            LostHomeTerritory = b.LostHomeTerritory,
            ResearchMultiplier = b.ResearchMultiplier,
            ObservedTaxCollected = Math.Max(0, taxCollected),
            ObservedTransfersPaid = Math.Max(0, transfersPaid),
        };
    }
}
