using System.Runtime.CompilerServices;
using DrainTarPit;
using DrainTarPit.Drain;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WaterAndTarPools.Signs;
using WaterAndTarPools.Ui;
using WaterAndTarPools.Visuals;

namespace WaterAndTarPools.Drain;

/// <summary>Harmony patches of <c>PoolFish</c> (MIGRATION_DECISIONS A5: every patch lives in Integrations/Harmony; the parent is partial so private members stay reachable).</summary>
internal static partial class PoolFish
{
    [HarmonyPatch(typeof(Fish), "RandomizeWaypoint")]
    static class Patch_RandomizeWaypoint
    {
        static bool Prefix(Fish __instance, float timeNow, ref bool __result)
        {
            try
            {
                State st;
                if (__instance == null || WaterVol(__instance) != null || !States.TryGetValue(__instance, out st) || !st.InPool) return true;   // the game's code
                st.GameCalls++;
                __result = PickTarget(__instance, st, timeNow);
                return false;
            }
            catch (System.MissingFieldException e) { ModLog.Warn("PoolFish: a Fish field is missing in this game version (" + e.Message + "), the game's own swim targets are used"); return true; }
            catch (System.Exception e) { ModLog.VerboseWarn("PoolFish waypoint: " + e.Message + " (the game's own code runs)"); return true; }
        }
    }

    [HarmonyPatch(typeof(Fish), nameof(Fish.CustomFixedUpdate))]
    static class Patch_FixedUpdate
    {
        static void Prefix(Fish __instance)
        {
            try
            {
                if (__instance == null) return;
                // T-46: NOT skipped when the fish has a WaterVolume: a pool on the coast lies inside the sea's water trigger, which kept setting the
                // fish's water level to the sea (~30 m, under the pool floor). A fish over pump-pool water whose surface is higher belongs to the pool.
                var st = States.GetOrCreateValue(__instance);
                if (--st.Countdown <= 0)
                {
                    st.Countdown = RecheckEvery;
                    float surface; string pool;
                    bool inPool = PoolSurface(__instance.transform.position, out surface, out pool);
                    // T-43: a fish touching a wall / the floor edge is briefly over a dry grid point; it only leaves after LeaveAfter misses in a row
                    if (!inPool && st.InPool && ++st.Misses < LeaveAfter) { inPool = true; surface = st.Surface; pool = st.Pool; }
                    else if (inPool) st.Misses = 0;
                    if (inPool != st.InPool)
                        ModLog.Verbose("Fish " + __instance.name + (inPool ? " is in pump pool " + pool + ", water level " + surface.ToString("0.00") : " left pump pool " + st.Pool + ", handed back to the game") +
                            " at " + __instance.transform.position.ToString("0.0"));
                    if (!inPool && st.InPool && Mathf.Approximately(InWater(__instance), st.Surface)) InWater(__instance) = -10000f;   // our level, not the game's: out of water again
                    st.InPool = inPool; st.Surface = surface; st.Pool = pool;
                    if (!inPool) Rescue(__instance, st);
                }
                if (st.InPool && WaterVol(__instance) != null) { WaterVol(__instance) = null; st.SeaOverrides++; }   // the pool, not the sea, decides
                if (st.InPool) InWater(__instance) = st.Surface;
                if (st.InPool) DriveTarget(__instance, st);
            }
            catch (System.MissingFieldException e) { ModLog.Warn("PoolFish: a Fish field is missing in this game version (" + e.Message + "), fish in pools are not helped"); }
            catch (System.Exception e) { ModLog.VerboseWarn("PoolFish: " + e.Message); }
        }

        // T-44: the state line is written AFTER the fish's own update (the T-43 line was written before it and always showed gravity on: the game's
        // ZSyncTransform turns gravity back on every frame and the fish turns it off again in its update).
        static void Postfix(Fish __instance)
        {
            try
            {
                State st;
                if (__instance == null || !States.TryGetValue(__instance, out st) || !st.InPool) return;
                KeepIn(__instance, st);
                if (Time.time < st.NextReport) return;
                st.NextReport = Time.time + ReportEvery;
                if (st.Stops + st.Returns > 0) ModLog.Verbose("Fish " + __instance.name + " @" + __instance.GetInstanceID() + " held in " + st.Pool + ": stopped at the edge " + st.Stops + " steps, put back into the water " + st.Returns + "x");
                st.Stops = 0; st.Returns = 0;
                Report(__instance, st);
            }
            catch (System.Exception e) { ModLog.VerboseWarn("PoolFish report: " + e.Message); }
        }
    }

    [HarmonyPatch(typeof(Fish), nameof(Fish.SetLiquidLevel))]
    static class Patch_SetLiquidLevel
    {
        static void Prefix(Fish __instance, ref float level, LiquidType type, ref Component liquidObj)
        {
            try
            {
                State st;
                if (__instance == null || type != LiquidType.Water || !States.TryGetValue(__instance, out st) || !st.InPool || level >= st.Surface) return;
                if (!(liquidObj is WaterVolume) && liquidObj != null) return;          // a LiquidSurface (our pool's own trigger): the game's value is right
                level = st.Surface; liquidObj = null; st.SeaOverrides++;
            }
            catch (System.Exception e) { ModLog.VerboseWarn("PoolFish liquid level: " + e.Message); }
        }
    }

    [HarmonyPatch(typeof(Fish), "SwimDirection")]
    static class Patch_SwimDirection
    {
        static void Prefix(Fish __instance, ref bool avoidLand)
        {
            if (!avoidLand || __instance == null) return;
            State st;
            if (States.TryGetValue(__instance, out st) && st.InPool && WaterVol(__instance) == null) { avoidLand = false; st.Flees++; }
        }
    }

    [HarmonyPatch(typeof(Fish), "GetWaterLevel")]
    static class Patch_GetWaterLevel
    {
        static void Postfix(Fish __instance, Vector3 point, ref float __result)
        {
            try
            {
                if (__instance == null || WaterVol(__instance) != null) return;
                State st;
                if (!States.TryGetValue(__instance, out st) || !st.InPool) return;     // only fish we know are in a pool ask the pools
                st.LevelAsks++;
                float surface; string pool;
                var probe = new Vector3(point.x, st.Surface, point.z);                  // waypoints carry a made-up y: test at the surface height
                bool wet = PoolSurface(probe, out surface, out pool);
                if (!wet) st.DryAsks++;
                __result = wet ? surface : -10000f;                                   // outside the pool's water = dry (the fish does not swim there)
            }
            catch (System.Exception e) { ModLog.VerboseWarn("PoolFish water level: " + e.Message); }
        }
    }
}
