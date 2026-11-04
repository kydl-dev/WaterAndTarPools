using KydlCore.Common;
using WaterAndTarPools.Bootstrap;
using WaterAndTarPools.Signs;

namespace WaterAndTarPools.Testing;

/// <summary>
/// KydlCore D-084: hooks for DrakesTestLab suites (tests/TestLab/suites/WaterAndTarPools). A hook is a static method with [TestHook("name")]; DrakesTestLab finds it by the
/// attribute's NAME (the mod does not reference DrakesTestLab). Hook names carry the mod id (lowercase) so two mods never clash.
/// Actions call the REAL mod code (never a copy of it); reads return short values (int / bool / short text) a suite compares with `expect` / `contains` / `min` / `max`.
/// </summary>
internal static class ModTestHooks
{
    /// <summary>Every test starts here (MIGRATION_DECISIONS C26): the player on the open arena spot 70 m from the lab start, facing +z, god mode on.</summary>
    [TestHook("waterandtarpools.arena")]
    private static string Arena() { return TestArena.MovePlayer(); }

    /// <summary>Is the feature running (FeatureRegistry state after its Enable switches)? id: waterandtarpools.</summary>
    [TestHook("waterandtarpools.featureRunning")]
    private static bool FeatureRunning(string id) { return ModInstaller.Current != null && ModInstaller.Current.Features.IsRunning(id); }

    /// <summary>Is this tab (settings / pools) registered in the KY hub right now?</summary>
    [TestHook("waterandtarpools.tabRegistered")]
    private static bool TabRegistered(string id) { return KydlCore.Features.Hub.Contracts.KyHub.HasTab(Plugin.ModGuid, id); }

    /// <summary>All Startup_* / World_* checks of InGameTests: "PASS: ..." or "FAIL: ...".</summary>
    [TestHook("waterandtarpools.selftest")]
    private static string SelfTest() { return TestLabSupport.RunInGameTests(typeof(InGameTests)); }

    /// <summary>The Pools tab page builds (world style section + pool list) without throwing; its state text.</summary>
    [TestHook("waterandtarpools.poolsPage")]
    private static string PoolsPage() { return PoolMenu.DescribeForTest(); }
}