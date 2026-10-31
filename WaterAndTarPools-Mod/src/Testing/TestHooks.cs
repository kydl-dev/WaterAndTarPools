using Kydl.Common;
using WaterAndTarPools.Signs;

namespace WaterAndTarPools.Testing
{
    /// <summary>
    /// KydlCore D-084: hooks for DrakesTestLab suites (WaterAndTarPools-Tests/TestLab/suites/WaterAndTarPools). A hook is a static method with [TestHook("name")]; DrakesTestLab finds
    /// it by the attribute's NAME (the mod does not reference DrakesTestLab). Hook names carry the mod id so two mods never clash.
    /// Actions call the REAL mod code (never a copy of it); reads return short values (int / bool / short text) a suite compares with `expect` / `contains` / `min` / `max`.
    /// </summary>
    internal static class TestHooks
    {
        /// <summary>All Startup_* / World_* checks of InGameTests: "PASS: ..." or "FAIL: ...".</summary>
        [TestHook("WaterAndTarPools.selftest")]
        static string SelfTest() { return TestLabSupport.RunInGameTests(typeof(InGameTests)); }

        /// <summary>The Pools tab page builds (world style section + pool list) without throwing; its state text.</summary>
        [TestHook("WaterAndTarPools.poolsPage")]
        static string PoolsPage() { return PoolMenu.DescribeForTest(); }
    }
}