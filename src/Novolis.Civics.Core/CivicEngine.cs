namespace Novolis.Civics.Core;

/// <summary>
/// Pure civic / fiscal period settlement over <see cref="NationState"/>.
/// No AI, no UI, no territory mutation, no force-domain unit stocks.
/// </summary>
public static class CivicEngine
{
    /// <summary>
    /// Unused for absolute tax base (GDP remains anchor). Kept for host calibration docs /
    /// implied labor ratios in demography-coupling notes.
    /// </summary>
    public const double ReferenceGdpPerCapita = 40.0;

    /// <summary>Apply one period of fiscal + civic dynamics.</summary>
    public static PeriodOutcome ApplyPeriod(NationState nation, PeriodContext ctx)
    {
        ArgumentNullException.ThrowIfNull(nation);
        ArgumentNullException.ThrowIfNull(ctx);

        var policy = nation.Policy;
        var civic = nation.Civic;
        var demo = nation.Demography;
        var g = nation.Government;

        var taxRate = Math.Clamp(policy.HouseholdTaxRate, 0, 0.6);
        var milShare = Math.Clamp(policy.MilitaryShare, 0, 0.7);
        var transferShare = Math.Clamp(policy.TransferShare, 0, 0.8);
        var control = Math.Clamp(ctx.ControlRatio, 0.25, 1.25);

        if (ctx.UnemploymentObserved is { } uObs)
            demo.Unemployment = Math.Clamp(uObs, 0, 1);

        var netMigration = ctx.NetMigration ?? 0;
        if (demo.Population > 0)
        {
            demo.Population = Math.Max(0, demo.Population * (1.0 + demo.NaturalGrowthRate) + netMigration);
            demo.LastNetMigration = netMigration;
        }
        else if (ctx.NetMigration is not null)
        {
            demo.LastNetMigration = netMigration;
        }

        var collectionCapacity = Math.Clamp(
            0.55 + civic.HumanDevelopment * 0.35 - civic.Corruption * 0.25 - civic.WarFatigue * 0.15,
            0.35,
            1.15);

        // Tax base stays GDP-anchored; population modulates via labor capacity (backward compatible).
        double taxIncome;
        if (ctx.ObservedTaxCollected is { } observed)
        {
            taxIncome = observed;
        }
        else
        {
            var laborCapacity = 1.0;
            if (demo.Population > 0)
            {
                laborCapacity = 0.75
                    + 0.35 * Math.Clamp(demo.WorkingAgeShare, 0.1, 1.0) * (1.0 - demo.Unemployment);
                laborCapacity = Math.Clamp(laborCapacity, 0.55, 1.25);
            }

            taxIncome = nation.Gdp * taxRate / 12.0 * control * collectionCapacity * laborCapacity;
        }

        civic.LastTaxCollected = taxIncome;

        var transfersWanted = taxIncome * transferShare;
        var transfersPaid = ctx.ObservedTransfersPaid ??
                            Math.Min(transfersWanted, Math.Max(0, nation.Treasury + taxIncome));
        civic.LastTransfersPaid = transfersPaid;

        if (ctx.ObservedTaxCollected is null)
        {
            nation.Treasury += taxIncome;
        }

        if (ctx.ObservedTransfersPaid is null)
        {
            nation.Treasury -= transfersPaid;
        }

        var afterTransfers = Math.Max(0, taxIncome - transfersPaid);
        var milSpend = afterTransfers * milShare;
        var civPool = Math.Max(0, afterTransfers - milSpend);

        var infraShare = Math.Clamp(policy.InfrastructureShare, 0, 1);
        var propShare = Math.Clamp(policy.PropagandaShare, 0, 1);
        var shareSum = Math.Max(0.01, infraShare + propShare);
        var infra = civPool * (infraShare / shareSum);
        var propagandaBudget = civPool * (propShare / shareSum);
        var propagandaEffect = propagandaBudget * GovernmentRules.PropagandaEffectiveness(g);

        if (ctx.ObservedTaxCollected is null)
        {
            nation.Treasury -= infra + propagandaBudget;
        }

        var upkeepFactor = GovernmentRules.MilitaryUpkeepFactor(g);
        var forceDemand = milSpend * 0.65 / 100.0 * (1.0 + nation.TechnologyStock * 0.05) / upkeepFactor;
        if (ctx.ObservedTaxCollected is null)
        {
            nation.Treasury -= milSpend * 0.35 * upkeepFactor;
        }

        var leak = Math.Min(Math.Max(0, nation.Treasury) * 0.02, taxIncome * civic.Corruption * 0.15);
        if (ctx.ObservedTaxCollected is null)
        {
            nation.Treasury -= leak;
        }

        var transferDelivery = transfersWanted <= 0 ? 0.5 : transfersPaid / Math.Max(1e-9, transfersWanted);
        var taxPressure = taxRate * GovernmentRules.TaxApprovalSensitivity(g);
        var gdp = Math.Max(1, nation.Gdp);

        // Emigration pressure: high tax + weak transfers/HD + war smother policy.
        var transferGap = Math.Clamp(0.45 - transferShare, 0, 0.45);
        var hdGap = Math.Clamp(0.55 - civic.HumanDevelopment, 0, 0.55);
        var emigrationPressure = Clamp01(
            0.15
            + 1.1 * Math.Max(0, taxRate - 0.22)
            + 0.55 * transferGap
            + 0.35 * hdGap
            + 0.25 * civic.WarFatigue
            + 0.08 * ctx.ActiveWars
            + 0.12 * demo.Unemployment
            - 0.2 * (transferDelivery - 0.5));

        var immigrationAttractiveness = Clamp01(
            0.35
            + 0.4 * civic.HumanDevelopment
            + 0.25 * (1.0 - taxRate)
            + 0.2 * transferShare
            + 0.15 * civic.Approval
            - 0.25 * civic.WarFatigue
            - 0.1 * ctx.ActiveWars);

        demo.LastEmigrationPressure = emigrationPressure;

        // Migration civic impacts
        var migShare = demo.Population > 0
            ? netMigration / Math.Max(1.0, demo.Population)
            : 0;
        var outMigDrag = migShare < 0 ? -migShare : 0; // fraction leaving
        var inMigStrain = migShare > 0 ? migShare : 0;

        civic.HumanDevelopment = Clamp01to2(
            civic.HumanDevelopment
            + infra / gdp * 2.0
            + transfersPaid / gdp * 0.4
            - civic.Corruption * 0.002
            - Math.Min(0.01, ctx.ResourceShortage * 0.00008)
            - inMigStrain * 0.15); // short-run HD pressure from inflows

        civic.WarFatigue = ctx.ActiveWars > 0
            ? Math.Min(1, civic.WarFatigue + 0.03 * ctx.ActiveWars + (ctx.OccupyingForeignLand ? 0.01 : 0))
            : Math.Max(0, civic.WarFatigue - 0.025);

        var legitimacyDelta =
            0.02 * (transferDelivery - 0.5)
            + 0.015 * (civic.Approval - 0.5)
            - 0.04 * civic.Corruption
            - 0.03 * civic.WarFatigue
            - (ctx.LostHomeTerritory ? 0.04 : 0)
            - (ctx.OccupyingForeignLand ? 0.01 : 0)
            + propagandaEffect / gdp * 0.8
            + (control < 0.7 ? -0.025 : 0.005)
            - outMigDrag * 0.35
            - inMigStrain * 0.08;
        civic.Legitimacy = Clamp01(civic.Legitimacy + legitimacyDelta);

        var approvalDelta =
            -0.05 * (taxPressure - 0.22)
            + 0.03 * (transferDelivery - 0.4)
            + 0.02 * (civic.Legitimacy - 0.5)
            + propagandaEffect / gdp * 1.2
            - Math.Min(0.05, ctx.ResourceShortage * 0.0004)
            - 0.04 * civic.WarFatigue
            - 0.04 * Math.Max(0, taxRate - 0.28) // smother: punitive tax
            - outMigDrag * 0.25
            - 0.03 * demo.Unemployment;
        civic.Approval = Clamp01(civic.Approval + approvalDelta);

        civic.Corruption = Clamp01(
            civic.Corruption
            + (milShare > 0.4 ? 0.004 : -0.001)
            + (civic.HumanDevelopment < 0.4 ? 0.003 : -0.002)
            + (ctx.OccupyingForeignLand ? 0.002 : 0)
            - propagandaEffect / gdp * 0.3);

        var targetStability = civic.Legitimacy * 0.45 + civic.Approval * 0.40 + (1.0 - civic.WarFatigue) * 0.15;
        targetStability *= 0.7 + 0.3 * control;
        targetStability *= 1.0 - 0.15 * outMigDrag;
        nation.Stability = Clamp01(nation.Stability * 0.7 + targetStability * 0.3);

        var researchMult = (0.85 + civic.HumanDevelopment * 0.35) * Math.Max(1.0, ctx.ResearchMultiplier);
        nation.TechnologyProgress += (infra * 0.02 + nation.Gdp * 0.00001) * nation.Stability * researchMult;
        if (nation.TechnologyProgress >= 100.0 * nation.TechnologyStock)
        {
            nation.TechnologyProgress = 0;
            nation.TechnologyStock += 0.1;
        }

        var growth = 0.001 * nation.Stability * nation.TechnologyStock * (0.8 + civic.HumanDevelopment * 0.4) / 12.0;
        growth -= Math.Min(0.002, ctx.ResourceShortage * 0.00005);
        growth -= civic.Corruption * 0.0003;
        if (demo.Population > 0)
            growth += 0.0004 * (demo.LastNetMigration / Math.Max(1.0, demo.Population));
        nation.Gdp *= 1.0 + growth;

        if (nation.Treasury < 0)
        {
            nation.Stability = Math.Max(0.05, nation.Stability - 0.04);
            civic.Legitimacy = Math.Max(0.05, civic.Legitimacy - 0.03);
            civic.Approval = Math.Max(0.05, civic.Approval - 0.02);
            nation.Treasury *= 0.95;
        }

        var laborHint = demo.Population > 0
            ? demo.Population * Math.Clamp(demo.WorkingAgeShare, 0.1, 1.0) * (1.0 - demo.Unemployment)
            : 0;

        return new PeriodOutcome
        {
            TaxCollected = taxIncome,
            TransfersPaid = transfersPaid,
            InfrastructureOutlay = infra,
            PropagandaOutlay = propagandaBudget,
            MilitaryOutlay = milSpend,
            ForceCapabilityDemand = forceDemand,
            EmigrationPressure = emigrationPressure,
            ImmigrationAttractiveness = immigrationAttractiveness,
            LaborForceDemandHint = laborHint,
        };
    }

    private static double Clamp01(double x) => Math.Clamp(x, 0, 1);
    private static double Clamp01to2(double x) => Math.Clamp(x, 0, 2);
}
