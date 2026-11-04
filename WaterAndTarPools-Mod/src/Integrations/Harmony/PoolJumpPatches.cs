using System.Collections.Generic;
using DrainTarPit;
using DrainTarPit.Drain;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WaterAndTarPools.Signs;
using WaterAndTarPools.Ui;
using WaterAndTarPools.Visuals;

namespace WaterAndTarPools.Drain;

/// <summary>Harmony patches of <c>PoolJump</c> (MIGRATION_DECISIONS A5: every patch lives in Integrations/Harmony; the parent is partial so private members stay reachable).</summary>
internal static partial class PoolJump
{
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    static class Patch_SetControls
    {
        static void Prefix(Player __instance, bool jump)
        {
            if (!jump) return;
            try { Pressed(__instance); }
            catch (System.NullReferenceException e) { ModLog.VerboseWarn("PoolJump: pool lookup failed (" + e.Message + "), jump not counted"); }
        }
    }

    [HarmonyPatch(typeof(Character), "UpdateMotion")]
    static class Patch_UpdateMotion
    {
        static void Postfix(Character __instance)
        {
            if (_liftEnd < 0f) return;
            try { Lift(__instance); }
            catch (System.MissingFieldException e) { ModLog.Warn("PoolJump: Character.m_body missing in this game version (" + e.Message + "), no lift"); _liftEnd = -1f; }
        }
    }
}
