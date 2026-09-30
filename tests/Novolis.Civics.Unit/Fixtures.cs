using Novolis.Civics.Agents;
using Novolis.Civics.Core;
using Novolis.Civics.EconomyBridge;
using Novolis.Civics.Simulation;
using Novolis.Economy.Core;
using Novolis.Economy.Primitives;

namespace Novolis.Civics.Unit;

static class Fixtures
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
