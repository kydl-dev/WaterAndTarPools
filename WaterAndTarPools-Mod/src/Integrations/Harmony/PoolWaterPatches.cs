using System.Collections.Generic;
using System.Reflection;
using DrainTarPit;
using DrainTarPit.Drain;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WaterAndTarPools.Drain;
using WaterAndTarPools.Signs;
using WaterAndTarPools.Ui;

namespace WaterAndTarPools.Visuals;

/// <summary>Harmony patches of <c>PoolWater</c> (MIGRATION_DECISIONS A5: every patch lives in Integrations/Harmony; the parent is partial so private members stay reachable).</summary>
internal static partial class PoolWater
{
    [HarmonyPatch(typeof(LiquidVolume), "Update")]
    static class Patch_Update
    {
        static void Postfix(LiquidVolume __instance)
        {
            try { Tick(__instance); }
            catch (System.Exception e) { ModLog.VerboseWarn("PoolWater.Tick: " + e.Message); }
        }
    }

    [HarmonyPatch(typeof(LiquidVolume), "OnDestroy")]
    static class Patch_OnDestroy
    {
        static void Prefix(LiquidVolume __instance) { Done.Remove(__instance); }
    }
}
