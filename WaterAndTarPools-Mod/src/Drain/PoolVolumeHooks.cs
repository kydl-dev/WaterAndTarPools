using DrainTarPit;
using DrainTarPit.Drain;
using HarmonyLib;
using UnityEngine;
using WaterAndTarPools.Logging;
using WaterAndTarPools.Visuals;
using static DrainTarPit.Drain.TarPits;
using static WaterAndTarPools.Drain.TarPitDeposit;
using static WaterAndTarPools.Drain.TarPitWalls;
using static WaterAndTarPools.Drain.TarPitStats;
using static WaterAndTarPools.Drain.PoolFilms;

namespace WaterAndTarPools.Drain
{
    /// <summary>
    /// T-069: what the pools do on every liquid volume, hooked into DrainTarPit's LiquidVolume patches (PoolHooks): the deposit RPC, deposits waiting
    /// for the grid, the ledger, runoff cleanup, the pump pool mesh, thin films; and the walls pass after the game rewrote the ground heights.
    /// </summary>
    internal static class PoolVolumeHooks
    {
        /// Subscribes to DrainTarPit's volume events. Returns the number of handlers added.
        internal static int Install()
        {
            PoolHooks.VolumeRegister += Register;
            PoolHooks.VolumeUpdate += Update;
            PoolHooks.VolumeDestroyed += Destroyed;
            ModLog.Verbose("PoolVolumeHooks: deposit RPC, per-frame pool work and clean-up hooked into DrainTarPit");
            return 3;
        }

        static void Register(LiquidVolume vol, ZNetView nv)
        {
            nv.Register<ZPackage>(RpcDeposit, (sender, pkg) => OnDepositRequest(vol, sender, pkg));
            ModLog.Verbose("PoolVolumeHooks: deposit RPC registered on '" + vol.name + "' @ " + vol.transform.position.ToString("0"));
        }

        static void Update(LiquidVolume v)
        {
            if (PendingDeposits.Count > 0) FlushDeposits(v);
            if (TarPitLedger.Books.Count > 0) TarPitLedger.Tick(v);
            if (ZNetScene.instance != null) RunoffCleanup.Tick(v);
            if (v.m_liquidType == LiquidType.Tar && IsPumpPool(v)) PoolMesh.MarkPumpPool(v);
            float shedAt; ShedTimers.TryGetValue(v, out shedAt);
            if (Time.time >= shedAt)
            {
                float dt = shedAt > 0f ? Mathf.Min(ShedInterval * 4f, Time.time - (shedAt - ShedInterval)) : ShedInterval;
                ShedTimers[v] = Time.time + ShedInterval;
                ShedFilms(v, dt);
            }
        }

        static void Destroyed(LiquidVolume v)
        {
            Memos.Remove(v); PendingDeposits.Remove(v); ShedTimers.Remove(v); Budgets.Remove(v);
            TarPitLedger.Destroyed(v); RunoffCleanup.Forget(v); PoolMesh.Forget(v);
        }

        [HarmonyPatch(typeof(LiquidVolume), "UpdateHeights")]
        static class Patch_UpdateHeights
        {
            // The game rewrites every ground height here (walls drop back to terrain) while the pool's worker thread keeps stepping the simulation. Between that
            // write and our wall pass one step could run over the rim and carry a lot of liquid out through every wall cell (a step moves up to half the height
            // difference). The pool's lock is held from before the game's write until the wall pass is done (Monitor, same thread: prefix and finalizer).
            static void Prefix(LiquidVolume __instance, out object __state)
            {
                __state = null;
                try { if (PoolSettings.PoolWalls.Value && IsLiquid(__instance) && IsPumpPool(__instance)) __state = Hold(__instance); }
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
}
