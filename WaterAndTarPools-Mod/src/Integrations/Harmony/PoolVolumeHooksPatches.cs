using DrainTarPit;
using DrainTarPit.Drain;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WaterAndTarPools.Visuals;
using static DrainTarPit.Drain.TarPits;
using static WaterAndTarPools.Drain.PoolFilms;
using static WaterAndTarPools.Drain.TarPitDeposit;
using static WaterAndTarPools.Drain.TarPitStats;
using static WaterAndTarPools.Drain.TarPitWalls;

namespace WaterAndTarPools.Drain;

/// <summary>Harmony patches of <c>PoolVolumeHooks</c> (MIGRATION_DECISIONS A5: every patch lives in Integrations/Harmony; the parent is partial so private members stay reachable).</summary>
internal static partial class PoolVolumeHooks
{
    [HarmonyPatch(typeof(LiquidVolume), "UpdateHeights")]
    static class Patch_UpdateHeights
    {
        // The game rewrites every ground height here (walls drop back to terrain) while the pool's worker thread keeps stepping the simulation. Between that
        // write and our wall pass one step could run over the rim and carry a lot of liquid out through every wall cell (a step moves up to half the height
        // difference). The pool's lock is held from before the game's write until the wall pass is done (Monitor, same thread: prefix and finalizer).
        static void Prefix(LiquidVolume __instance, out object __state)
        {
            __state = null;
            try { if (ModSettings.PoolWalls.Value && IsLiquid(__instance) && IsPumpPool(__instance)) __state = Hold(__instance); }
            catch (System.Exception e) { ModLog.VerboseWarn("UpdateHeights lock: " + e.Message); }
        }

        static void Finalizer(object __state)
        {
            var held = __state as System.IDisposable;
            if (held != null) held.Dispose();
        }

        static void Postfix(LiquidVolume __instance)
        {
            try { FixMissedHeights(__instance); }
            catch (System.Exception e) { ModLog.VerboseWarn("FixMissedHeights: " + e.Message); }
            // A synthetic test tank (dtp ptest tank / shape / walls) owns its heights: real colliders under it are not walls unless the test asks for them.
            bool synthetic = WaterAndTarPools.Debug.PoolTankTests.IsSynthetic(__instance, out bool realWalls);
            if (!synthetic || realWalls)
            {
                try { ApplyWalls(__instance); }
                catch (System.Exception e) { ModLog.VerboseWarn("ApplyWalls: " + e.Message); }
            }
            try { WaterAndTarPools.Debug.PoolTankTests.ApplySynthetic(__instance); }
            catch (System.Exception e) { ModLog.VerboseWarn("PoolTankTests.ApplySynthetic: " + e.Message); }
        }
    }
}
