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

/// <summary>
/// T-069: what the pools do on every liquid volume, hooked into DrainTarPit's LiquidVolume patches (PoolHooks): the deposit RPC, deposits waiting
/// for the grid, the ledger, runoff cleanup, the pump pool mesh, thin films; and the walls pass after the game rewrote the ground heights.
/// </summary>
internal static partial class PoolVolumeHooks
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

}
