using System.Collections.Generic;
using DrainTarPit;
using DrainTarPit.Drain;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WaterAndTarPools.Drain;
using WaterAndTarPools.Signs;
using WaterAndTarPools.Ui;

namespace WaterAndTarPools.Visuals;

/// <summary>Harmony patches of <c>PoolMesh</c> (MIGRATION_DECISIONS A5: every patch lives in Integrations/Harmony; the parent is partial so private members stay reachable).</summary>
internal static partial class PoolMesh
{
    [HarmonyPatch(typeof(LiquidVolume), "PrebuildMesh")]
    static class Patch_PrebuildMesh
    {
        static void Prefix(LiquidVolume __instance, List<float> ___m_depths, List<float> ___m_heights)
        {
            _fixFor = null;
            if (!_enabled || (__instance.m_liquidType != LiquidType.Water && !PumpPools.ContainsKey(__instance))) return;
            try { _fixFor = __instance; Build(__instance, ___m_depths, ___m_heights); }
            catch (System.Exception) { _fixFor = null; }                 // never log from the worker thread; the game's own mesh is used
        }

        // User, 2026-10-20 (tar pool seen from above: a see-through zig-zag band along the walls): the game gives a dry grid point vertex alpha 0
        // and a wet one alpha 1, and the tar shader fades the quads between them - one cell of half-transparent tar along every edge. A dry point
        // next to drawn liquid (it is the one hidden in / under the wall by the fix above) now gets alpha 1 too: the liquid is opaque up to the wall.
        static void Postfix(LiquidVolume __instance, List<float> ___m_depths, List<Color32> ___m_tempColors)
        {
            if (!ReferenceEquals(_fixFor, __instance) || ___m_depths == null || ___m_tempColors == null) return;
            try
            {
                int w = __instance.m_width, stride = w + 1, n = stride * stride;
                var d = ___m_depths; var c = ___m_tempColors;
                if (d.Count < n || c.Count < n) return;
                for (int y = 0; y < stride; y++)
                    for (int x = 0; x < stride; x++)
                    {
                        int i = y * stride + x;
                        if (d[i] >= DrawDepth || c[i].a == 255) continue;
                        bool near = false;
                        for (int dy = -1; dy <= 1 && !near; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int xx = x + dx, yy = y + dy;
                                if (xx < 0 || yy < 0 || xx > w || yy > w) continue;
                                if (d[yy * stride + xx] >= DrawDepth) { near = true; break; }
                            }
                        if (near) { var col = c[i]; col.a = 255; c[i] = col; }
                    }
            }
            catch (System.Exception) { }                                    // worker thread: never log; the game's colours stay
        }

        static void Finalizer() { _fixFor = null; }
    }

    [HarmonyPatch(typeof(LiquidVolume), "CalcVertex")]
    static class Patch_CalcVertex
    {
        static void Postfix(LiquidVolume __instance, int __0, int __1, bool __2, ref Vector3 __result)
        {
            if (__2 || !ReferenceEquals(_fixFor, __instance)) return;
            var f = _fix;
            int i = __1 * (__instance.m_width + 1) + __0;
            if (f == null || i < 0 || i >= f.Length) return;
            float y = f[i];
            if (!float.IsNaN(y)) __result.y = y;
        }
    }
}
