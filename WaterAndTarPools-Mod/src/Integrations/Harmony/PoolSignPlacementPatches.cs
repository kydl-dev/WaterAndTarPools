using DrainTarPit;
using HarmonyLib;

namespace WaterAndTarPools.Signs;

/// <summary>Harmony patches of the pool sign placement (MIGRATION_DECISIONS A5: every patch lives in Integrations/Harmony).</summary>
[HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
static class Patch_PoolSignGhost
{
    static void Postfix(Player __instance) { PoolSignPlacement.Check(__instance); }
}

[HarmonyPatch(typeof(Player), "TryPlacePiece")]
static class Patch_PoolSignPlace
{
    static void Prefix(Player __instance, Piece __0) { PoolSignPlacement.Tell(__instance, __0); }   // __0: the parameter's name is not checked
}
