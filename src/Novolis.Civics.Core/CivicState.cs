namespace Novolis.Civics.Core;

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
