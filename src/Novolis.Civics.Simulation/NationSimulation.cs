using Novolis.Civics.Core;

namespace Novolis.Civics.Simulation;

/// <summary>Standalone / host composition over a single <see cref="NationState"/>.</summary>
public sealed class NationSimulation
{
    public NationSimulation(NationState nation) =>
        Nation = nation ?? throw new ArgumentNullException(nameof(nation));

    public NationState Nation { get; }
    public int PeriodsAdvanced { get; private set; }
    public PeriodOutcome? LastOutcome { get; private set; }

    /// <summary>Advance one period with the given host context (defaults to neutral).</summary>
    public PeriodOutcome AdvancePeriod(PeriodContext? context = null)
    {
        LastOutcome = CivicEngine.ApplyPeriod(Nation, context ?? PeriodContext.Neutral);
        PeriodsAdvanced++;
        return LastOutcome;
    }

    public void AdvancePeriods(int count, Func<int, PeriodContext>? contextFactory = null)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        for (var i = 0; i < count; i++)
        {
            AdvancePeriod(contextFactory?.Invoke(PeriodsAdvanced));
        }
    }
}
