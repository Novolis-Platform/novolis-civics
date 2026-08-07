using Novolis.Civics.Agents;
using Novolis.Civics.Core;
using Novolis.Civics.EconomyBridge;
using Novolis.Civics.Simulation;
using Novolis.Economy.Core;

namespace Novolis.Civics.Unit;

public sealed class PublicApiCoverageTests
{
    [Test]
    public async Task NationId_Factories_And_Clones_Preserve_Values()
    {
        var guid = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var id = NationId.From(guid);
        var generated = NationId.New();
        var policy = new FiscalPolicy
        {
            HouseholdTaxRate = 0.31,
            TransferShare = 0.42,
            InfrastructureShare = 0.53,
            PropagandaShare = 0.24,
            MilitaryShare = 0.15,
        };
        var civic = new CivicState
        {
            Legitimacy = 0.1,
            Approval = 0.2,
            Corruption = 0.3,
            HumanDevelopment = 0.4,
            WarFatigue = 0.5,
            LastTaxCollected = 6,
            LastTransfersPaid = 7,
        };
        var demography = new DemographicState
        {
            Population = 100,
            WorkingAgeShare = 0.6,
            NaturalGrowthRate = 0.01,
            Unemployment = 0.2,
            LastNetMigration = -3,
            LastEmigrationPressure = 0.8,
        };

        await Assert.That(id.Value).IsEqualTo(guid);
        await Assert.That(id.ToString()).IsEqualTo(guid.ToString("N"));
        await Assert.That(generated.Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(policy.Clone()).IsNotSameReferenceAs(policy);
        await Assert.That(policy.Clone().MilitaryShare).IsEqualTo(0.15);
        await Assert.That(civic.Clone()).IsNotSameReferenceAs(civic);
        await Assert.That(civic.Clone().LastTransfersPaid).IsEqualTo(7);
        await Assert.That(demography.Clone()).IsNotSameReferenceAs(demography);
        await Assert.That(demography.Clone().LastEmigrationPressure).IsEqualTo(0.8);
        await Assert.That(FiscalPolicy.Default.HouseholdTaxRate).IsEqualTo(0.22);
    }

    [Test]
    [Arguments(0.10, GovernmentType.Democracy)]
    [Arguments(0.30, GovernmentType.Multiparty)]
    [Arguments(0.50, GovernmentType.SingleParty)]
    [Arguments(0.70, GovernmentType.Autocracy)]
    [Arguments(0.85, GovernmentType.MilitaryJunta)]
    [Arguments(0.95, GovernmentType.Monarchy)]
    public async Task GovernmentRules_Roll_Covers_Each_Band(double value, GovernmentType expected)
    {
        await Assert.That(GovernmentRules.Roll(new FixedRandom(value))).IsEqualTo(expected);
    }

    [Test]
    public async Task GovernmentRules_Cover_Regime_Defaults()
    {
        await Assert.That(GovernmentRules.MilitaryUpkeepFactor(GovernmentType.Autocracy)).IsEqualTo(0.92);
        await Assert.That(GovernmentRules.MilitaryUpkeepFactor(GovernmentType.SingleParty)).IsEqualTo(1.0);
        await Assert.That(GovernmentRules.PropagandaEffectiveness(GovernmentType.MilitaryJunta)).IsEqualTo(1.15);
        await Assert.That(GovernmentRules.PropagandaEffectiveness(GovernmentType.Monarchy)).IsEqualTo(1.0);
        await Assert.That(GovernmentRules.TaxApprovalSensitivity(GovernmentType.Autocracy)).IsEqualTo(0.7);
        await Assert.That(GovernmentRules.TaxApprovalSensitivity(GovernmentType.Monarchy)).IsEqualTo(1.0);
    }

    [Test]
    public async Task NationSimulation_Uses_ContextFactory_And_Validates_Input()
    {
        var nation = NationState.Create("Coverage", GovernmentType.Monarchy, seed: 4);
        var simulation = new NationSimulation(nation);
        var indexes = new List<int>();

        simulation.AdvancePeriods(2, i =>
        {
            indexes.Add(i);
            return new PeriodContext { ActiveWars = i + 1 };
        });

        await Assert.That(simulation.Nation).IsSameReferenceAs(nation);
        await Assert.That(indexes).IsEquivalentTo(new[] { 0, 1 });
        await Assert.That(simulation.LastOutcome).IsNotNull();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        {
            simulation.AdvancePeriods(-1);
            return Task.CompletedTask;
        });
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            _ = new NationSimulation(null!);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task CivicEngine_Covers_Observed_Demography_And_Clamp_Branches()
    {
        var nation = NationState.Create("Observed", GovernmentType.SingleParty, seed: 2);
        nation.Demography.Population = 100;
        nation.Demography.NaturalGrowthRate = -2;
        nation.Policy.HouseholdTaxRate = 2;
        nation.Policy.TransferShare = -1;
        nation.Policy.InfrastructureShare = 0;
        nation.Policy.PropagandaShare = 0;
        nation.Policy.MilitaryShare = 2;
        nation.TechnologyProgress = 1;

        var outcome = CivicEngine.ApplyPeriod(nation, new PeriodContext
        {
            ControlRatio = 9,
            ObservedTaxCollected = 50,
            ObservedTransfersPaid = 0,
            NetMigration = -20,
            UnemploymentObserved = 4,
            ResearchMultiplier = 0,
        });

        await Assert.That(nation.Demography.Population).IsEqualTo(0);
        await Assert.That(nation.Demography.Unemployment).IsEqualTo(1);
        await Assert.That(outcome.TaxCollected).IsEqualTo(50);
        await Assert.That(outcome.TransfersPaid).IsEqualTo(0);
        await Assert.That(outcome.InfrastructureOutlay).IsEqualTo(0);
        await Assert.That(outcome.PropagandaOutlay).IsEqualTo(0);
        await Assert.That(nation.TechnologyProgress).IsGreaterThan(1);
    }

    [Test]
    public async Task CivicEngine_Records_Migration_When_Population_Is_Zero()
    {
        var nation = NationState.Create("Empty", GovernmentType.Monarchy, seed: 2);
        nation.Demography.Population = 0;

        var outcome = CivicEngine.ApplyPeriod(nation, new PeriodContext { NetMigration = 25 });

        await Assert.That(nation.Demography.LastNetMigration).IsEqualTo(25);
        await Assert.That(outcome.LaborForceDemandHint).IsEqualTo(0);
    }

    [Test]
    public async Task FiscalAgent_Covers_Remaining_Decision_Branches()
    {
        var legitimacy = HealthyNation();
        legitimacy.Civic.Legitimacy = 0.2;
        new HeuristicFiscalAgent().AdjustPolicy(legitimacy);

        var democraticFatigue = HealthyNation();
        democraticFatigue.Civic.WarFatigue = 0.8;
        new HeuristicFiscalAgent().AdjustPolicy(democraticFatigue);

        var development = HealthyNation();
        development.Civic.HumanDevelopment = 0.2;
        development.Treasury = development.Gdp;
        new HeuristicFiscalAgent().AdjustPolicy(development);

        var unchanged = HealthyNation();
        var original = unchanged.Policy.Clone();
        new HeuristicFiscalAgent().AdjustPolicy(unchanged);

        await Assert.That(Math.Abs(legitimacy.Policy.PropagandaShare - 0.225)).IsLessThan(1e-12);
        await Assert.That(Math.Abs(legitimacy.Policy.InfrastructureShare - 0.46)).IsLessThan(1e-12);
        await Assert.That(Math.Abs(democraticFatigue.Policy.MilitaryShare - 0.292)).IsLessThan(1e-12);
        await Assert.That(Math.Abs(development.Policy.InfrastructureShare - 0.47)).IsLessThan(1e-12);
        await Assert.That(Math.Abs(development.Policy.PropagandaShare - 0.19)).IsLessThan(1e-12);
        await Assert.That(unchanged.Policy.HouseholdTaxRate).IsEqualTo(original.HouseholdTaxRate);
    }

    [Test]
    public async Task Public_APIs_Reject_Null_Inputs_And_Clamp_Delivery()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            CivicEngine.ApplyPeriod(null!, PeriodContext.Neutral);
            return Task.CompletedTask;
        });
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            CivicEngine.ApplyPeriod(HealthyNation(), null!);
            return Task.CompletedTask;
        });
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            new HeuristicFiscalAgent().AdjustPolicy(null!);
            return Task.CompletedTask;
        });
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            CivicEconomyBridge.ToEconomyStatePolicy(null!, Money.From(0), Money.From(0));
            return Task.CompletedTask;
        });
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            CivicEconomyBridge.BindEconomyState(null!, LegalEntityId.New());
            return Task.CompletedTask;
        });

        var basis = new PeriodContext
        {
            ControlRatio = 0.7,
            ActiveWars = 2,
            ResourceShortage = 3,
            OccupyingForeignLand = true,
            LostHomeTerritory = true,
            ResearchMultiplier = 1.5,
            NetMigration = -4,
            UnemploymentObserved = 0.3,
        };
        var context = CivicEconomyBridge.PeriodContextFromDelivery(-10, -20, basis);
        await Assert.That(context.ObservedTaxCollected).IsEqualTo(0);
        await Assert.That(context.ObservedTransfersPaid).IsEqualTo(0);
        await Assert.That(context.ActiveWars).IsEqualTo(2);
        await Assert.That(context.NetMigration).IsEqualTo(-4);
    }

    [Test]
    public async Task Nation_Create_Uses_Explicit_And_Seeded_Governments()
    {
        var explicitNation = NationState.Create("  Republic  ", GovernmentType.Monarchy);
        var seededA = NationState.Create("A", seed: 91);
        var seededB = NationState.Create("B", seed: 91);

        await Assert.That(explicitNation.Name).IsEqualTo("  Republic  ");
        await Assert.That(explicitNation.Government).IsEqualTo(GovernmentType.Monarchy);
        await Assert.That(seededA.Government).IsEqualTo(seededB.Government);
        await Assert.That(seededA.Id).IsNotEqualTo(seededB.Id);
        await Assert.That(PeriodContext.Neutral.ControlRatio).IsEqualTo(1);
    }

    [Test]
    public async Task CivicEngine_Clamps_Extreme_Standalone_Inputs()
    {
        var nation = HealthyNation();
        nation.Gdp = 0;
        nation.Treasury = -10_000;
        nation.Stability = 0.01;
        nation.TechnologyStock = 2;
        nation.Policy.HouseholdTaxRate = -5;
        nation.Policy.TransferShare = 9;
        nation.Policy.InfrastructureShare = -4;
        nation.Policy.PropagandaShare = 8;
        nation.Policy.MilitaryShare = -2;
        nation.Civic.Corruption = 4;
        nation.Civic.HumanDevelopment = -2;
        nation.Civic.WarFatigue = 2;

        var outcome = CivicEngine.ApplyPeriod(nation, new PeriodContext
        {
            ControlRatio = -4,
            ResourceShortage = 1_000_000,
            LostHomeTerritory = true,
            OccupyingForeignLand = true,
            ActiveWars = 20,
        });

        await Assert.That(outcome.TaxCollected).IsEqualTo(0);
        await Assert.That(outcome.TransfersPaid).IsEqualTo(0);
        await Assert.That(outcome.EmigrationPressure).IsBetween(0, 1);
        await Assert.That(outcome.ImmigrationAttractiveness).IsBetween(0, 1);
        await Assert.That(nation.Stability).IsGreaterThanOrEqualTo(0.05);
        await Assert.That(nation.Civic.Legitimacy).IsGreaterThanOrEqualTo(0.05);
        await Assert.That(nation.Civic.Approval).IsGreaterThanOrEqualTo(0.05);
    }

    [Test]
    public async Task CivicEngine_Handles_Inbound_Migration_And_Labor_Clamps()
    {
        var nation = HealthyNation();
        nation.Demography.Population = 100;
        nation.Demography.WorkingAgeShare = 5;
        nation.Demography.NaturalGrowthRate = 0;

        var outcome = CivicEngine.ApplyPeriod(nation, new PeriodContext
        {
            NetMigration = 100,
            UnemploymentObserved = -2,
        });

        await Assert.That(nation.Demography.Population).IsEqualTo(200);
        await Assert.That(nation.Demography.LastNetMigration).IsEqualTo(100);
        await Assert.That(nation.Demography.Unemployment).IsEqualTo(0);
        await Assert.That(outcome.LaborForceDemandHint).IsEqualTo(200);
    }

    [Test]
    public async Task FiscalAgent_Clamps_Every_Decision_Path()
    {
        var agent = new HeuristicFiscalAgent();
        var debt = HealthyNation();
        debt.Treasury = -1;
        debt.Policy.MilitaryShare = 0.11;
        debt.Policy.HouseholdTaxRate = 0.499;
        debt.Policy.TransferShare = 0.051;
        debt.Policy.PropagandaShare = 0.59;
        agent.AdjustPolicy(debt);

        var lowApprovalMonarchy = HealthyNation();
        lowApprovalMonarchy.Government = GovernmentType.Monarchy;
        lowApprovalMonarchy.Civic.Approval = 0.1;
        lowApprovalMonarchy.Policy.HouseholdTaxRate = 0.121;
        lowApprovalMonarchy.Policy.TransferShare = 0.54;
        var militaryBefore = lowApprovalMonarchy.Policy.MilitaryShare;
        agent.AdjustPolicy(lowApprovalMonarchy);

        var junta = HealthyNation();
        junta.Government = GovernmentType.MilitaryJunta;
        junta.Civic.WarFatigue = 0.9;
        junta.Policy.MilitaryShare = 0.59;
        junta.Policy.TransferShare = 0.081;
        agent.AdjustPolicy(junta);

        await Assert.That(debt.Policy.MilitaryShare).IsEqualTo(0.1);
        await Assert.That(debt.Policy.HouseholdTaxRate).IsEqualTo(0.5);
        await Assert.That(debt.Policy.TransferShare).IsEqualTo(0.05);
        await Assert.That(debt.Policy.PropagandaShare).IsEqualTo(0.6);
        await Assert.That(lowApprovalMonarchy.Policy.HouseholdTaxRate).IsEqualTo(0.12);
        await Assert.That(lowApprovalMonarchy.Policy.TransferShare).IsEqualTo(0.55);
        await Assert.That(lowApprovalMonarchy.Policy.MilitaryShare).IsEqualTo(militaryBefore);
        await Assert.That(junta.Policy.MilitaryShare).IsEqualTo(0.6);
        await Assert.That(junta.Policy.TransferShare).IsEqualTo(0.08);
    }

    [Test]
    public async Task EconomyBridge_Maps_All_Policy_Fields_And_Empty_Demography()
    {
        var policy = CivicEconomyBridge.ToEconomyStatePolicy(
            new FiscalPolicy { HouseholdTaxRate = -1 },
            Money.From(12),
            Money.From(3),
            firmTaxRate: 0.2m,
            depositReserveRequirement: 0.15m,
            insuranceCapitalRequirement: 0.25m);
        var nation = HealthyNation();
        nation.Demography.Unemployment = 0.4;

        await Assert.That(policy.HouseholdTaxRate).IsEqualTo(0m);
        await Assert.That(policy.FirmTaxRate).IsEqualTo(0.2m);
        await Assert.That(policy.DepositReserveRequirement).IsEqualTo(0.15m);
        await Assert.That(policy.InsuranceCapitalRequirement).IsEqualTo(0.25m);
        await Assert.That(CivicEconomyBridge.PopulationHintFromCohorts(EconomyState.Empty)).IsEqualTo(0);

        CivicEconomyBridge.SyncDemographyFromEconomy(nation, EconomyState.Empty, unemployment: 2);
        await Assert.That(nation.Demography.Population).IsEqualTo(0);
        await Assert.That(nation.Demography.Unemployment).IsEqualTo(1);
    }

    [Test]
    public async Task EconomyBridge_Rejects_Null_Economy_Inputs()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            CivicEconomyBridge.PopulationHintFromCohorts(null!);
            return Task.CompletedTask;
        });
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            CivicEconomyBridge.SyncDemographyFromEconomy(HealthyNation(), null!);
            return Task.CompletedTask;
        });
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            CivicEconomyBridge.SyncDemographyFromEconomy(null!, EconomyState.Empty);
            return Task.CompletedTask;
        });
    }

    private static NationState HealthyNation()
    {
        var nation = NationState.Create("Healthy", GovernmentType.Democracy, seed: 1);
        nation.Treasury = nation.Gdp;
        nation.Civic.Approval = 0.7;
        nation.Civic.Legitimacy = 0.7;
        nation.Civic.WarFatigue = 0;
        nation.Civic.HumanDevelopment = 0.7;
        nation.Demography.LastEmigrationPressure = 0;
        return nation;
    }

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
}
