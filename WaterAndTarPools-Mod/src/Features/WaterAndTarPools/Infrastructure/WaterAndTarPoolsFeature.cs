using KydlCore.Common;
using KydlCore.Core;

namespace WaterAndTarPools;

/// <summary>
/// The whole mod as one feature (same decision as DrainTarPit, 2026-11-03). Gate: [General] EnableAll, DrainTarPit on, and at least one of [Pools] / [Effects] / [Visuals] Enable.
/// The section switches keep gating their own parts inside the working code (read every time, so they work live); Start / Stop only carry the <see cref="Running"/> flag.
/// </summary>
internal sealed class WaterAndTarPoolsFeature : IFeature
{
    internal const string FeatureId = "waterandtarpools";

    /// <summary>True between Start and Stop.</summary>
    internal static bool Running { get; private set; }

    public string Id { get { return FeatureId; } }

    public bool IsEnabled()
    {
        bool all = ModSettings.EnableAll != null && ModSettings.EnableAll.Value && DrainTarPit.ModSettings.On;
        bool any = (ModSettings.PoolsEnable != null && ModSettings.PoolsEnable.Value)
            || (ModSettings.EffectsEnable != null && ModSettings.EffectsEnable.Value)
            || (ModSettings.VisualsEnable != null && ModSettings.VisualsEnable.Value);
        return Functions.IsOn(all, any);
    }

    public bool Start()
    {
        Running = true;
        ModLog.Verbose("WaterAndTarPoolsFeature: started");
        return Running;
    }

    public bool Stop()
    {
        Running = false;
        ModLog.Verbose("WaterAndTarPoolsFeature: stopped");
        return !Running;
    }
}