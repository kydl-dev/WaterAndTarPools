using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using DrainTarPit;
using DrainTarPit.Building;
using DrainTarPit.Drain;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WaterAndTarPools.Signs;
using WaterAndTarPools.Ui;
using WaterAndTarPools.Visuals;

namespace WaterAndTarPools.Drain;

/// <summary>Harmony patches of <c>TarPool</c> (MIGRATION_DECISIONS A5: every patch lives in Integrations/Harmony; the parent is partial so private members stay reachable).</summary>
internal static partial class TarPool
{
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    static class Patch_ZNetSceneAwake
    {
        static void Postfix(ZNetScene __instance) { RegisterIn(__instance); }
    }
}
