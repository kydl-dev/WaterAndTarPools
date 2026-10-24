using System.Linq;
using BepInEx.Bootstrap;
using Kydl.Common;

namespace WaterAndTarPools.Testing
{
    /// <summary>
    /// In-game tests run by the user's Kydl.GameProbe (dev tool, never a dependency: it finds this class by its NAME `InGameTests`).
    ///   Startup_*  at the main menu;   World_*  5 s after a world is loaded and again on GameProbe's RetestKey (PageDown).
    /// Each returns "PASS: ..." / "FAIL: ..." / "SKIP: ..." with the numbers that decided it. Read-only: nothing is changed.
    /// </summary>
    internal static class InGameTests
    {
        static string Startup_ConfigBound()
        {
            if (PoolSettings.EnableAll == null || PoolSettings.DebugLog == null) return "FAIL: PoolSettings.Bind did not run (EnableAll / DebugLog null)";
            string r = PoolSettings.On ? "PASS: on (own EnableAll and DrainTarPit's), debug log " + PoolSettings.DebugLog.Value : "SKIP: off (EnableAll here or in DrainTarPit)";
            ModLog.Verbose("InGameTests: Startup_ConfigBound -> " + r);
            return r;
        }

        static string Startup_BundleLoaded()
        {
            var b = Plugin.Bundle;
            string r = b != null ? "PASS: bundle '" + Plugin.BundleName + "' with " + b.GetAllAssetNames().Length + " asset(s)" : "FAIL: no water bundle - only the Game water style works";
            ModLog.Verbose("InGameTests: Startup_BundleLoaded -> " + r);
            return r;
        }

        /// The base mod is loaded (hard dependency) and its version is reported.
        static string Startup_DrainTarPitLoaded()
        {
            BepInEx.PluginInfo info;
            string r = Chainloader.PluginInfos.TryGetValue(DrainTarPit.Plugin.ModGuid, out info) && info.Instance != null
                ? "PASS: DrainTarPit " + info.Metadata.Version + " loaded" : "FAIL: DrainTarPit (" + DrainTarPit.Plugin.ModGuid + ") not loaded";
            ModLog.Verbose("InGameTests: Startup_DrainTarPitLoaded -> " + r);
            return r;
        }

        /// The pool sign piece is known to ZNetScene and in the Hammer's build table.
        static string World_PoolSignRegistered()
        {
            if (ZNetScene.instance == null || ObjectDB.instance == null) return "SKIP: ZNetScene / ObjectDB not ready";
            string name = Signs.PoolSignPiece.PrefabName;
            bool inScene = ZNetScene.instance.GetPrefab(name) != null;
            var hammer = ObjectDB.instance.GetItemPrefab("Hammer");
            var table = hammer != null && hammer.GetComponent<ItemDrop>() != null ? hammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces : null;
            bool inHammer = table != null && table.m_pieces.Any(p => p != null && p.name == name);
            string r = inScene && inHammer ? "PASS: " + name + " in ZNetScene and the Hammer" : "FAIL: " + name + (inScene ? "" : " not in ZNetScene") + (inHammer ? "" : " not in the Hammer build table");
            ModLog.Verbose("InGameTests: World_PoolSignRegistered -> " + r);
            return r;
        }
    }
}
