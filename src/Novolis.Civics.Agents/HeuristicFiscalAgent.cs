using Novolis.Civics.Core;

namespace Novolis.Civics.Agents;

/// <summary>Heuristic fiscal adjustments from civic stocks (no stock settlement).</summary>
public sealed class HeuristicFiscalAgent
{
    public void AdjustPolicy(NationState nation)
    {
        ArgumentNullException.ThrowIfNull(nation);
        var p = nation.Policy;
        var c = nation.Civic;
        var d = nation.Demography;

        if (nation.Treasury < 0)
        {
            p.MilitaryShare = Math.Max(0.1, p.MilitaryShare - 0.03);
            p.HouseholdTaxRate = Math.Min(0.5, p.HouseholdTaxRate + 0.015);
            p.TransferShare = Math.Max(0.05, p.TransferShare - 0.02);
            p.PropagandaShare = Math.Min(0.6, p.PropagandaShare + 0.03);
        }
        else if (d.LastEmigrationPressure > 0.55 && nation.Treasury > nation.Gdp * 0.02)
        {
            // Ease smothering taxes when people are leaving and treasury can absorb it.
            p.HouseholdTaxRate = Math.Max(0.12, p.HouseholdTaxRate - 0.015);
            p.TransferShare = Math.Min(0.55, p.TransferShare + 0.015);
        }
        else if (c.Approval < 0.35)
        {
            p.HouseholdTaxRate = Math.Max(0.12, p.HouseholdTaxRate - 0.01);
            p.TransferShare = Math.Min(0.55, p.TransferShare + 0.02);
            if (nation.Government is GovernmentType.Democracy or GovernmentType.Multiparty)
            {
                p.MilitaryShare = Math.Max(0.12, p.MilitaryShare - 0.015);
            }
        }
        else if (c.Legitimacy < 0.4)
        {
            p.PropagandaShare = Math.Min(0.55, p.PropagandaShare + 0.025);
            p.InfrastructureShare = Math.Min(0.7, p.InfrastructureShare + 0.01);
        }
        else if (c.WarFatigue > 0.5)
        {
            var bump = nation.Government is GovernmentType.MilitaryJunta or GovernmentType.Autocracy
                ? 0.025
                : 0.012;
            p.MilitaryShare = Math.Min(0.6, p.MilitaryShare + bump);
            p.TransferShare = Math.Max(0.08, p.TransferShare - 0.01);
        }
        else if (c.HumanDevelopment < 0.45 && nation.Treasury > nation.Gdp * 0.05)
        {
            p.InfrastructureShare = Math.Min(0.75, p.InfrastructureShare + 0.02);
            p.PropagandaShare = Math.Max(0.05, p.PropagandaShare - 0.01);
        }
    }
}
