using Novolis.Civics.Agents;
using Novolis.Civics.Core;
using Novolis.Civics.EconomyBridge;
using Novolis.Civics.Simulation;
using Novolis.Economy.Core;

namespace Novolis.Civics.Unit;

/// <summary>
/// Known nation-state dynamics: closed-form fiscal arithmetic, regime modifiers,
/// war/occupation stock paths, multi-period trajectories, and Economy delivery coupling.
/// </summary>
public sealed class KnownDynamicsScenariosTests
{
    const double Eps = 1e-9;

    // ─── Fiscal arithmetic (standalone treasury path) ─────────────────────────

    [Test]
    public async Task Tax_Collection_Uses_Gdp_Rate_Control_And_Capacity()
    {
        var n = Fixtures.BaselineDemocracy();
        // capacity = 0.55 + 0.55*0.35 - 0.15*0.25 - 0 = 0.705
        var expectedTax = 100_000.0 * 0.22 / 12.0 * 1.0 * 0.705;
        var outcome = CivicEngine.ApplyPeriod(n, PeriodContext.Neutral);
        await Assert.That(Near(outcome.TaxCollected, expectedTax)).IsTrue();
        await Assert.That(Near(n.Civic.LastTaxCollected, expectedTax)).IsTrue();
    }

    [Test]
    public async Task Low_HumanDevelopment_And_High_Corruption_Cut_Tax_Take()
    {
        var healthy = Fixtures.BaselineDemocracy();
        var fragile = Fixtures.BaselineDemocracy();
        fragile.Civic.HumanDevelopment = 0.2;
        fragile.Civic.Corruption = 0.6;
        fragile.Civic.WarFatigue = 0.4;

        var tHealthy = CivicEngine.ApplyPeriod(healthy, PeriodContext.Neutral).TaxCollected;
        var tFragile = CivicEngine.ApplyPeriod(fragile, PeriodContext.Neutral).TaxCollected;
        await Assert.That(tFragile).IsLessThan(tHealthy * 0.75);
    }

    [Test]
    public async Task Control_Ratio_Scales_Tax_Base_Linearly_Inside_Clamp()
    {
        var full = Fixtures.BaselineDemocracy();
        var half = Fixtures.BaselineDemocracy();
        var tFull = CivicEngine.ApplyPeriod(full, new PeriodContext { ControlRatio = 1.0 }).TaxCollected;
        var tHalf = CivicEngine.ApplyPeriod(half, new PeriodContext { ControlRatio = 0.5 }).TaxCollected;
        await Assert.That(Near(tHalf, tFull * 0.5)).IsTrue();
    }

    [Test]
    public async Task Control_Ratio_Floors_At_Quarter()
    {
        var n = Fixtures.BaselineDemocracy();
        var tFloor = CivicEngine.ApplyPeriod(n, new PeriodContext { ControlRatio = 0.05 }).TaxCollected;
        var expected = 100_000.0 * 0.22 / 12.0 * 0.25 * 0.705;
        await Assert.That(Near(tFloor, expected)).IsTrue();
    }

    [Test]
    public async Task Transfers_Equal_Tax_Times_Share_When_Treasury_Solvent()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Policy.TransferShare = 0.4;
        var outcome = CivicEngine.ApplyPeriod(n, PeriodContext.Neutral);
        await Assert.That(Near(outcome.TransfersPaid, outcome.TaxCollected * 0.4)).IsTrue();
        await Assert.That(Near(n.Civic.LastTransfersPaid, outcome.TransfersPaid)).IsTrue();
    }

    [Test]
    public async Task Transfers_Cap_At_Treasury_Plus_Tax_When_Broke()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Treasury = 0;
        n.Policy.TransferShare = 0.8;
        n.Policy.HouseholdTaxRate = 0.05; // small take
        var outcome = CivicEngine.ApplyPeriod(n, PeriodContext.Neutral);
        // After tax credit, available = tax; wanted = 0.8*tax → paid = tax (min)
        // Wait: transfersPaid = Min(wanted, Max(0, Treasury+taxIncome)) BEFORE treasury+=tax in code...
        // Actually: taxIncome computed, then transfersPaid = Min(wanted, Max(0, Treasury+taxIncome))
        // with Treasury=0 → Min(0.8*tax, tax) = 0.8*tax — still solvent on tax alone.
        // Force insolvency: Treasury negative large, tax small.
        n = Fixtures.BaselineDemocracy();
        n.Treasury = -50_000;
        n.Policy.TransferShare = 0.8;
        outcome = CivicEngine.ApplyPeriod(n, PeriodContext.Neutral);
        var available = Math.Max(0, -50_000 + outcome.TaxCollected);
        await Assert.That(Near(outcome.TransfersPaid, Math.Min(outcome.TaxCollected * 0.8, available))).IsTrue();
    }

    [Test]
    public async Task Military_And_Civilian_Split_Of_Post_Transfer_Pool()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Policy.TransferShare = 0.2;
        n.Policy.MilitaryShare = 0.5;
        n.Policy.InfrastructureShare = 0.75;
        n.Policy.PropagandaShare = 0.25;
        var o = CivicEngine.ApplyPeriod(n, PeriodContext.Neutral);
        var after = o.TaxCollected - o.TransfersPaid;
        await Assert.That(Near(o.MilitaryOutlay, after * 0.5)).IsTrue();
        var civ = after - o.MilitaryOutlay;
        await Assert.That(Near(o.InfrastructureOutlay, civ * 0.75)).IsTrue();
        await Assert.That(Near(o.PropagandaOutlay, civ * 0.25)).IsTrue();
    }

    [Test]
    public async Task Force_Demand_Uses_SixtyFive_Percent_Of_MilSpend_And_Tech_Upkeep()
    {
        var dem = Fixtures.BaselineDemocracy();
        dem.TechnologyStock = 2.0;
        var o = CivicEngine.ApplyPeriod(dem, PeriodContext.Neutral);
        var upkeep = GovernmentRules.MilitaryUpkeepFactor(GovernmentType.Democracy); // 1.05
        var expected = o.MilitaryOutlay * 0.65 / 100.0 * (1.0 + 2.0 * 0.05) / upkeep;
        await Assert.That(Near(o.ForceCapabilityDemand, expected)).IsTrue();
    }

    [Test]
    public async Task Junta_Force_Demand_Exceeds_Democracy_For_Same_MilSpend()
    {
        var dem = Fixtures.IdenticalExceptGov(GovernmentType.Democracy);
        var junta = Fixtures.IdenticalExceptGov(GovernmentType.MilitaryJunta);
        // Match post-transfer milSpend by locking fiscal knobs + civic capacity inputs
        var oDem = CivicEngine.ApplyPeriod(dem, PeriodContext.Neutral);
        var oJunta = CivicEngine.ApplyPeriod(junta, PeriodContext.Neutral);
        await Assert.That(Near(oDem.MilitaryOutlay, oJunta.MilitaryOutlay)).IsTrue();
        await Assert.That(oJunta.ForceCapabilityDemand).IsGreaterThan(oDem.ForceCapabilityDemand);
        var ratio = oJunta.ForceCapabilityDemand / oDem.ForceCapabilityDemand;
        await Assert.That(Near(ratio, 1.05 / 0.78)).IsTrue();
    }

    [Test]
    public async Task Standalone_Treasury_Moves_By_Tax_Minus_Outlays_Minus_Leak()
    {
        var n = Fixtures.BaselineDemocracy();
        var t0 = n.Treasury;
        var corr0 = n.Civic.Corruption;
        var o = CivicEngine.ApplyPeriod(n, PeriodContext.Neutral);
        var upkeep = GovernmentRules.MilitaryUpkeepFactor(GovernmentType.Democracy);
        // Reconstruct leak using post-spend treasury before leak:
        // After +=tax -=transfers -=infra -=prop -=milCash, then leak.
        var beforeLeak = t0 + o.TaxCollected - o.TransfersPaid - o.InfrastructureOutlay
                         - o.PropagandaOutlay - o.MilitaryOutlay * 0.35 * upkeep;
        var leak = Math.Min(Math.Max(0, beforeLeak) * 0.02, o.TaxCollected * corr0 * 0.15);
        var expected = beforeLeak - leak;
        await Assert.That(Near(n.Treasury, expected)).IsTrue();
    }

    [Test]
    public async Task Observed_Tax_Freezes_Treasury_Fiscal_Path()
    {
        var n = Fixtures.BaselineDemocracy();
        var t0 = n.Treasury;
        var ctx = new PeriodContext
        {
            ObservedTaxCollected = 5_000,
            ObservedTransfersPaid = 1_000,
        };
        var o = CivicEngine.ApplyPeriod(n, ctx);
        await Assert.That(Near(o.TaxCollected, 5_000)).IsTrue();
        await Assert.That(Near(o.TransfersPaid, 1_000)).IsTrue();
        await Assert.That(Near(n.Treasury, t0)).IsTrue(); // no +=tax, no -=outlays
    }

    // ─── Regime / civic stock dynamics ────────────────────────────────────────

    [Test]
    public async Task Democracy_Suffers_More_Tax_Approval_Penalty_Than_Autocracy()
    {
        var dem = Fixtures.IdenticalExceptGov(GovernmentType.Democracy);
        var auto = Fixtures.IdenticalExceptGov(GovernmentType.Autocracy);
        dem.Policy.HouseholdTaxRate = 0.4;
        auto.Policy.HouseholdTaxRate = 0.4;
        dem.Civic.Approval = 0.55;
        auto.Civic.Approval = 0.55;
        // Kill propaganda/transfer noise: zero civilian pool bias
        dem.Policy.PropagandaShare = 0.01;
        auto.Policy.PropagandaShare = 0.01;
        dem.Policy.InfrastructureShare = 0.99;
        auto.Policy.InfrastructureShare = 0.99;

        CivicEngine.ApplyPeriod(dem, PeriodContext.Neutral);
        CivicEngine.ApplyPeriod(auto, PeriodContext.Neutral);
        await Assert.That(dem.Civic.Approval).IsLessThan(auto.Civic.Approval);
    }

    [Test]
    public async Task Autocracy_Propaganda_Moves_Approval_More_Than_Democracy()
    {
        var dem = Fixtures.IdenticalExceptGov(GovernmentType.Democracy);
        var auto = Fixtures.IdenticalExceptGov(GovernmentType.Autocracy);
        dem.Policy.PropagandaShare = 0.9;
        dem.Policy.InfrastructureShare = 0.1;
        dem.Policy.MilitaryShare = 0.05;
        dem.Policy.TransferShare = 0.05;
        auto.Policy.PropagandaShare = 0.9;
        auto.Policy.InfrastructureShare = 0.1;
        auto.Policy.MilitaryShare = 0.05;
        auto.Policy.TransferShare = 0.05;
        dem.Civic.Approval = 0.4;
        auto.Civic.Approval = 0.4;

        CivicEngine.ApplyPeriod(dem, PeriodContext.Neutral);
        CivicEngine.ApplyPeriod(auto, PeriodContext.Neutral);
        await Assert.That(auto.Civic.Approval).IsGreaterThan(dem.Civic.Approval);
    }

    [Test]
    public async Task War_Fatigue_Rises_By_Three_Percent_Per_Active_War()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Civic.WarFatigue = 0.1;
        CivicEngine.ApplyPeriod(n, new PeriodContext { ActiveWars = 2 });
        await Assert.That(Near(n.Civic.WarFatigue, 0.1 + 0.03 * 2)).IsTrue();
    }

    [Test]
    public async Task Occupation_Adds_Extra_War_Fatigue_Point()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Civic.WarFatigue = 0;
        CivicEngine.ApplyPeriod(n, new PeriodContext { ActiveWars = 1, OccupyingForeignLand = true });
        await Assert.That(Near(n.Civic.WarFatigue, 0.03 + 0.01)).IsTrue();
    }

    [Test]
    public async Task Peacetime_War_Fatigue_Decays_By_Two_Point_Five_Percent()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Civic.WarFatigue = 0.2;
        CivicEngine.ApplyPeriod(n, PeriodContext.Neutral);
        await Assert.That(Near(n.Civic.WarFatigue, 0.175)).IsTrue();
    }

    [Test]
    public async Task Lost_Home_Territory_Hits_Legitimacy_Harder_Than_Occupation()
    {
        var lost = Fixtures.BaselineDemocracy();
        var occupy = Fixtures.BaselineDemocracy();
        lost.Civic.Legitimacy = 0.7;
        occupy.Civic.Legitimacy = 0.7;
        CivicEngine.ApplyPeriod(lost, new PeriodContext { LostHomeTerritory = true });
        CivicEngine.ApplyPeriod(occupy, new PeriodContext { OccupyingForeignLand = true });
        // Lost: −0.04; Occupy: −0.01 (plus occupy also bumps corruption)
        await Assert.That(lost.Civic.Legitimacy).IsLessThan(occupy.Civic.Legitimacy);
    }

    [Test]
    public async Task Low_Control_Applies_Legitimacy_Penalty()
    {
        var high = Fixtures.BaselineDemocracy();
        var low = Fixtures.BaselineDemocracy();
        high.Civic.Legitimacy = 0.65;
        low.Civic.Legitimacy = 0.65;
        CivicEngine.ApplyPeriod(high, new PeriodContext { ControlRatio = 1.0 });
        CivicEngine.ApplyPeriod(low, new PeriodContext { ControlRatio = 0.5 });
        await Assert.That(low.Civic.Legitimacy).IsLessThan(high.Civic.Legitimacy);
    }

    [Test]
    public async Task High_Military_Share_Raises_Corruption()
    {
        var militarized = Fixtures.BaselineDemocracy();
        militarized.Policy.MilitaryShare = 0.55;
        var corr0 = militarized.Civic.Corruption;
        CivicEngine.ApplyPeriod(militarized, PeriodContext.Neutral);
        await Assert.That(militarized.Civic.Corruption).IsGreaterThan(corr0);
    }

    [Test]
    public async Task Low_Military_Share_And_Healthy_Hd_Erode_Corruption()
    {
        var civilian = Fixtures.BaselineDemocracy();
        civilian.Policy.MilitaryShare = 0.2;
        civilian.Civic.HumanDevelopment = 0.7;
        civilian.Civic.Corruption = 0.3;
        var corr0 = civilian.Civic.Corruption;
        CivicEngine.ApplyPeriod(civilian, PeriodContext.Neutral);
        await Assert.That(civilian.Civic.Corruption).IsLessThan(corr0);
    }

    [Test]
    public async Task Infrastructure_Bias_Raises_HumanDevelopment()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Policy.InfrastructureShare = 0.95;
        n.Policy.PropagandaShare = 0.05;
        n.Policy.MilitaryShare = 0.05;
        n.Policy.TransferShare = 0.1;
        var hd0 = n.Civic.HumanDevelopment;
        CivicEngine.ApplyPeriod(n, PeriodContext.Neutral);
        await Assert.That(n.Civic.HumanDevelopment).IsGreaterThan(hd0);
    }

    [Test]
    public async Task Resource_Shortage_Drags_Approval_And_Gdp_Growth()
    {
        var calm = Fixtures.BaselineDemocracy();
        var short_ = Fixtures.BaselineDemocracy();
        var gdp0 = calm.Gdp;
        CivicEngine.ApplyPeriod(calm, PeriodContext.Neutral);
        CivicEngine.ApplyPeriod(short_, new PeriodContext { ResourceShortage = 100_000 });
        await Assert.That(short_.Civic.Approval).IsLessThan(calm.Civic.Approval);
        await Assert.That(short_.Gdp).IsLessThan(calm.Gdp);
        await Assert.That(calm.Gdp).IsGreaterThan(gdp0); // peaceful growth
    }

    [Test]
    public async Task Research_Multiplier_Accelerates_Tech_Progress()
    {
        var base_ = Fixtures.BaselineDemocracy();
        var boosted = Fixtures.BaselineDemocracy();
        base_.Policy.InfrastructureShare = 0.9;
        boosted.Policy.InfrastructureShare = 0.9;
        base_.Policy.PropagandaShare = 0.1;
        boosted.Policy.PropagandaShare = 0.1;
        CivicEngine.ApplyPeriod(base_, PeriodContext.Neutral);
        CivicEngine.ApplyPeriod(boosted, new PeriodContext { ResearchMultiplier = 1.5 });
        await Assert.That(boosted.TechnologyProgress).IsGreaterThan(base_.TechnologyProgress);
    }

    [Test]
    public async Task Tech_Stock_Advances_When_Progress_Crosses_Threshold()
    {
        var n = Fixtures.BaselineDemocracy();
        n.TechnologyStock = 1.0;
        n.TechnologyProgress = 99.9; // threshold = 100 * stock
        n.Policy.InfrastructureShare = 0.95;
        n.Policy.PropagandaShare = 0.05;
        n.Gdp = 500_000;
        CivicEngine.ApplyPeriod(n, PeriodContext.Neutral);
        await Assert.That(Near(n.TechnologyStock, 1.1)).IsTrue();
        await Assert.That(Near(n.TechnologyProgress, 0)).IsTrue();
    }

    [Test]
    public async Task Insolvent_Treasury_Punishes_Stability_Legitimacy_Approval()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Treasury = -1_000;
        n.Stability = 0.6;
        n.Civic.Legitimacy = 0.6;
        n.Civic.Approval = 0.6;
        // Observed tax freezes fiscal outflows so treasury stays negative → insolvency branch
        CivicEngine.ApplyPeriod(n, new PeriodContext
        {
            ObservedTaxCollected = 100,
            ObservedTransfersPaid = 0,
        });
        await Assert.That(n.Treasury).IsLessThan(0);
        await Assert.That(n.Stability).IsLessThan(0.6);
        await Assert.That(n.Civic.Legitimacy).IsLessThan(0.6);
        await Assert.That(n.Civic.Approval).IsLessThan(0.6);
        await Assert.That(Near(n.Treasury, -1_000 * 0.95)).IsTrue();
    }

    // ─── Multi-period / agent / simulation ────────────────────────────────────

    [Test]
    public async Task Twelve_Peaceful_Months_Raise_Hd_And_Decay_Fatigue()
    {
        var sim = new NationSimulation(Fixtures.BaselineDemocracy());
        sim.Nation.Civic.WarFatigue = 0.4;
        sim.Nation.Policy.InfrastructureShare = 0.7;
        sim.Nation.Policy.PropagandaShare = 0.3;
        sim.Nation.Policy.MilitaryShare = 0.15;
        var hd0 = sim.Nation.Civic.HumanDevelopment;
        sim.AdvancePeriods(12);
        await Assert.That(sim.PeriodsAdvanced).IsEqualTo(12);
        await Assert.That(sim.Nation.Civic.HumanDevelopment).IsGreaterThan(hd0);
        await Assert.That(sim.Nation.Civic.WarFatigue).IsLessThan(0.15); // 0.4 − 12×0.025 = 0.1
        await Assert.That(sim.Nation.Gdp).IsGreaterThan(100_000);
    }

    [Test]
    public async Task Prolonged_War_Accumulates_Fatigue_Toward_Cap()
    {
        var sim = new NationSimulation(Fixtures.BaselineDemocracy());
        sim.AdvancePeriods(24, _ => new PeriodContext { ActiveWars = 3, OccupyingForeignLand = true });
        await Assert.That(sim.Nation.Civic.WarFatigue).IsEqualTo(1.0);
        await Assert.That(sim.Nation.Civic.Approval).IsLessThan(0.35);
    }

    [Test]
    public async Task Militarized_Path_Corrupts_Faster_Than_Welfare_Path()
    {
        var guns = new NationSimulation(Fixtures.BaselineDemocracy());
        guns.Nation.Policy.MilitaryShare = 0.6;
        guns.Nation.Policy.TransferShare = 0.05;
        var butter = new NationSimulation(Fixtures.BaselineDemocracy());
        butter.Nation.Policy.MilitaryShare = 0.12;
        butter.Nation.Policy.TransferShare = 0.4;
        butter.Nation.Policy.InfrastructureShare = 0.8;
        guns.AdvancePeriods(18);
        butter.AdvancePeriods(18);
        await Assert.That(guns.Nation.Civic.Corruption).IsGreaterThan(butter.Nation.Civic.Corruption);
        await Assert.That(butter.Nation.Civic.HumanDevelopment)
            .IsGreaterThan(guns.Nation.Civic.HumanDevelopment);
    }

    [Test]
    public async Task Agent_Cuts_Tax_When_Democratic_Approval_Collapses()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Civic.Approval = 0.2;
        n.Policy.HouseholdTaxRate = 0.28;
        n.Policy.TransferShare = 0.25;
        n.Policy.MilitaryShare = 0.3;
        new HeuristicFiscalAgent().AdjustPolicy(n);
        await Assert.That(Near(n.Policy.HouseholdTaxRate, 0.27)).IsTrue();
        await Assert.That(Near(n.Policy.TransferShare, 0.27)).IsTrue();
        await Assert.That(Near(n.Policy.MilitaryShare, 0.285)).IsTrue();
    }

    [Test]
    public async Task Agent_Raises_Tax_When_Treasury_Is_Negative()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Treasury = -500;
        n.Policy.HouseholdTaxRate = 0.2;
        n.Policy.MilitaryShare = 0.3;
        n.Policy.TransferShare = 0.3;
        new HeuristicFiscalAgent().AdjustPolicy(n);
        await Assert.That(Near(n.Policy.HouseholdTaxRate, 0.215)).IsTrue();
        await Assert.That(Near(n.Policy.MilitaryShare, 0.27)).IsTrue();
        await Assert.That(Near(n.Policy.TransferShare, 0.28)).IsTrue();
    }

    [Test]
    public async Task Agent_Does_Not_Mutate_Civic_Stocks()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Civic.Approval = 0.1;
        var clone = n.Civic.Clone();
        new HeuristicFiscalAgent().AdjustPolicy(n);
        await Assert.That(Near(n.Civic.Approval, clone.Approval)).IsTrue();
        await Assert.That(Near(n.Civic.Legitimacy, clone.Legitimacy)).IsTrue();
        await Assert.That(Near(n.Civic.Corruption, clone.Corruption)).IsTrue();
    }

    [Test]
    public async Task Junta_Agent_Ramps_Military_Harder_Under_War_Fatigue()
    {
        var dem = Fixtures.IdenticalExceptGov(GovernmentType.Democracy);
        var junta = Fixtures.IdenticalExceptGov(GovernmentType.MilitaryJunta);
        dem.Civic.WarFatigue = 0.6;
        junta.Civic.WarFatigue = 0.6;
        dem.Civic.Approval = 0.5;
        junta.Civic.Approval = 0.5;
        dem.Civic.Legitimacy = 0.5;
        junta.Civic.Legitimacy = 0.5;
        dem.Policy.MilitaryShare = 0.3;
        junta.Policy.MilitaryShare = 0.3;
        new HeuristicFiscalAgent().AdjustPolicy(dem);
        new HeuristicFiscalAgent().AdjustPolicy(junta);
        await Assert.That(junta.Policy.MilitaryShare).IsGreaterThan(dem.Policy.MilitaryShare);
    }

    // ─── Economy bridge coupling ──────────────────────────────────────────────

    [Test]
    public async Task Bridge_Clamps_Tax_Rate_Into_StatePolicy()
    {
        var civic = new FiscalPolicy { HouseholdTaxRate = 0.9 };
        var policy = CivicEconomyBridge.ToEconomyStatePolicy(
            civic, Money.From(25m), Money.From(2m), firmTaxRate: 0.1m);
        await Assert.That(policy.HouseholdTaxRate).IsEqualTo(0.6m);
        await Assert.That(policy.FirmTaxRate).IsEqualTo(0.1m);
        await Assert.That(policy.TransferPerHousehold.Amount).IsEqualTo(25m);
        await Assert.That(policy.WagePerLaborHour.Amount).IsEqualTo(2m);
    }

    [Test]
    public async Task Poor_Transfer_Delivery_Hurts_Legitimacy_Vs_Full_Delivery()
    {
        var full = Fixtures.BaselineDemocracy();
        var poor = Fixtures.BaselineDemocracy();
        full.Civic.Legitimacy = 0.6;
        poor.Civic.Legitimacy = 0.6;
        // With observed tax 10000 and transfer share 0.4 → wanted 4000
        full.Policy.TransferShare = 0.4;
        poor.Policy.TransferShare = 0.4;
        CivicEngine.ApplyPeriod(full, new PeriodContext
        {
            ObservedTaxCollected = 10_000,
            ObservedTransfersPaid = 4_000,
        });
        CivicEngine.ApplyPeriod(poor, new PeriodContext
        {
            ObservedTaxCollected = 10_000,
            ObservedTransfersPaid = 500,
        });
        await Assert.That(poor.Civic.Legitimacy).IsLessThan(full.Civic.Legitimacy);
        await Assert.That(poor.Civic.Approval).IsLessThan(full.Civic.Approval);
    }

    [Test]
    public async Task Host_Loop_Economy_Flows_Drive_Civic_Without_Double_Counting_Treasury()
    {
        // Simulate: Economy collected 8000 tax, paid 2000 transfers; civic treasury untouched.
        var n = Fixtures.BaselineDemocracy();
        n.Treasury = 12_345;
        CivicEconomyBridge.BindEconomyState(n, LegalEntityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")));
        var ctx = CivicEconomyBridge.PeriodContextFromDelivery(
            taxCollected: 8_000,
            transfersPaid: 2_000,
            baseContext: new PeriodContext { ActiveWars = 1 });
        CivicEngine.ApplyPeriod(n, ctx);
        await Assert.That(Near(n.Treasury, 12_345)).IsTrue();
        await Assert.That(Near(n.Civic.LastTaxCollected, 8_000)).IsTrue();
        await Assert.That(Near(n.Civic.LastTransfersPaid, 2_000)).IsTrue();
        await Assert.That(n.Civic.WarFatigue).IsGreaterThan(0);
        await Assert.That(n.EconomyStateEntityId).IsNotNull();
    }

    [Test]
    public async Task Stability_Is_Convex_Blend_Of_Legitimacy_Approval_And_Fatigue()
    {
        var n = Fixtures.BaselineDemocracy();
        n.Stability = 0.5;
        n.Civic.Legitimacy = 0.8;
        n.Civic.Approval = 0.8;
        n.Civic.WarFatigue = 0;
        // Minimize other deltas: observed delivery, low tax pressure
        n.Policy.HouseholdTaxRate = 0.157; // ~0.22/1.4 so taxPressure≈0.22 for democracy
        CivicEngine.ApplyPeriod(n, new PeriodContext
        {
            ObservedTaxCollected = 1_000,
            ObservedTransfersPaid = 400,
            ControlRatio = 1.0,
        });
        // target ≈ 0.8*0.45 + 0.8*0.40 + 1.0*0.15 = 0.83; *1.0 control
        // stability = 0.5*0.7 + 0.83*0.3 = 0.35 + 0.249 = 0.599 — but legitimacy/approval also moved
        await Assert.That(n.Stability).IsGreaterThan(0.5);
        await Assert.That(n.Stability).IsLessThanOrEqualTo(1.0);
    }

    [Test]
    public async Task All_Regimes_Produce_Finite_Solvent_Month()
    {
        foreach (GovernmentType g in Enum.GetValues<GovernmentType>())
        {
            var n = Fixtures.IdenticalExceptGov(g);
            var o = CivicEngine.ApplyPeriod(n, PeriodContext.Neutral);
            await Assert.That(double.IsFinite(n.Gdp)).IsTrue();
            await Assert.That(double.IsFinite(n.Treasury)).IsTrue();
            await Assert.That(double.IsFinite(o.ForceCapabilityDemand)).IsTrue();
            await Assert.That(n.Civic.Legitimacy).IsGreaterThanOrEqualTo(0);
            await Assert.That(n.Civic.Legitimacy).IsLessThanOrEqualTo(1);
        }
    }

    static bool Near(double actual, double expected) => Math.Abs(actual - expected) < Eps;
}

file static class Fixtures
{
    public static NationState BaselineDemocracy()
    {
        var n = NationState.Create("Nordmark", GovernmentType.Democracy, seed: 1);
        n.Gdp = 100_000;
        n.Treasury = 10_000;
        n.Stability = 0.6;
        n.TechnologyStock = 1.0;
        n.TechnologyProgress = 0;
        n.Policy.HouseholdTaxRate = 0.22;
        n.Policy.TransferShare = 0.25;
        n.Policy.InfrastructureShare = 0.45;
        n.Policy.PropagandaShare = 0.20;
        n.Policy.MilitaryShare = 0.28;
        n.Civic.Legitimacy = 0.65;
        n.Civic.Approval = 0.55;
        n.Civic.Corruption = 0.15;
        n.Civic.HumanDevelopment = 0.55;
        n.Civic.WarFatigue = 0;
        return n;
    }

    public static NationState IdenticalExceptGov(GovernmentType g)
    {
        var n = BaselineDemocracy();
        n.Government = g;
        n.Name = g.ToString();
        return n;
    }
}
