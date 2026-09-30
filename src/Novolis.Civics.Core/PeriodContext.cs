namespace Novolis.Civics.Core;

/// <summary>External facts supplied by Geopolitics / game hosts for one period.</summary>
public sealed class PeriodContext
{
    public double ControlRatio { get; init; } = 1.0;
    public int ActiveWars { get; init; }
    public double ResourceShortage { get; init; }
    public bool OccupyingForeignLand { get; init; }
    public bool LostHomeTerritory { get; init; }
    public double ResearchMultiplier { get; init; } = 1.0;

    /// <summary>
    /// When set (≥ 0), overrides engine tax collection with host/Economy delivery
    /// (e.g. actual tax receipts from an Economy period).
    /// </summary>
    public double? ObservedTaxCollected { get; init; }

    /// <summary>When set (≥ 0), overrides transfer payment with observed Economy transfers.</summary>
    public double? ObservedTransfersPaid { get; init; }

    /// <summary>Host-observed net migration this period (people; +in / −out).</summary>
    public double? NetMigration { get; init; }

    /// <summary>Host-observed unemployment [0, 1].</summary>
    public double? UnemploymentObserved { get; init; }

    public static PeriodContext Neutral { get; } = new();
}
