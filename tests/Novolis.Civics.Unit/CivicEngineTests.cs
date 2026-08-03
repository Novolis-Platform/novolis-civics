using Novolis.Civics.Agents;
using Novolis.Civics.Core;
using Novolis.Civics.EconomyBridge;
using Novolis.Civics.Simulation;
using Novolis.Economy.Core;

namespace Novolis.Civics.Unit;

public sealed class CivicEngineTests
{
    [Test]
    public async Task ApplyPeriod_RaisesHumanDevelopment_WithInfrastructureBias()
    {
        var nation = NationState.Create("Testland", GovernmentType.Democracy, seed: 7);
        nation.Gdp = 200_000;
        nation.Treasury = 50_000;
        nation.Policy.InfrastructureShare = 0.8;
        nation.Policy.PropagandaShare = 0.2;
        nation.Policy.MilitaryShare = 0.1;
        nation.Policy.TransferShare = 0.1;
        var hd0 = nation.Civic.HumanDevelopment;

        var outcome = CivicEngine.ApplyPeriod(nation, PeriodContext.Neutral);

        await Assert.That(outcome.InfrastructureOutlay).IsGreaterThan(0);
        await Assert.That(nation.Civic.HumanDevelopment).IsGreaterThanOrEqualTo(hd0);
        await Assert.That(outcome.ForceCapabilityDemand).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task ApplyPeriod_WarContext_RaisesWarFatigue()
    {
        var nation = NationState.Create("Warland", GovernmentType.Autocracy, seed: 3);
        var ctx = new PeriodContext { ActiveWars = 2, OccupyingForeignLand = true };
        CivicEngine.ApplyPeriod(nation, ctx);
        await Assert.That(nation.Civic.WarFatigue).IsGreaterThan(0);
    }

    [Test]
    public async Task NationSimulation_AdvancesPeriods()
    {
        var sim = new NationSimulation(NationState.Create("Simland", seed: 11));
        sim.AdvancePeriods(3);
        await Assert.That(sim.PeriodsAdvanced).IsEqualTo(3);
        await Assert.That(sim.LastOutcome).IsNotNull();
    }

    [Test]
    public async Task HeuristicAgent_AdjustsPolicy_WhenApprovalLow()
    {
        var nation = NationState.Create("Voters", GovernmentType.Democracy, seed: 2);
        nation.Civic.Approval = 0.2;
        var tax0 = nation.Policy.HouseholdTaxRate;
        new HeuristicFiscalAgent().AdjustPolicy(nation);
        await Assert.That(nation.Policy.HouseholdTaxRate).IsLessThanOrEqualTo(tax0);
        await Assert.That(nation.Policy.TransferShare).IsGreaterThanOrEqualTo(0.25);
    }

    [Test]
    public async Task EconomyBridge_MapsTaxRate()
    {
        var civic = new FiscalPolicy { HouseholdTaxRate = 0.3 };
        var policy = CivicEconomyBridge.ToEconomyStatePolicy(
            civic,
            transferPerHousehold: Money.From(10m),
            wagePerLaborHour: Money.From(1m));
        await Assert.That(policy.HouseholdTaxRate).IsEqualTo(0.3m);

        var nation = NationState.Create("Bound", seed: 1);
        var entity = LegalEntityId.New();
        CivicEconomyBridge.BindEconomyState(nation, entity);
        await Assert.That(nation.EconomyStateEntityId).IsEqualTo(entity.Value);

        var ctx = CivicEconomyBridge.PeriodContextFromDelivery(1000, 200);
        await Assert.That(ctx.ObservedTaxCollected).IsEqualTo(1000);
        await Assert.That(ctx.ObservedTransfersPaid).IsEqualTo(200);
    }
}
