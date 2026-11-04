using System;
using System.Reflection;
using HarmonyLib;
using KydlCore.Common;

namespace WaterAndTarPools;

/// <summary>Applies the mod's Harmony patches (all patch classes live in Integrations/Harmony).</summary>
internal static class PatchInstaller
{
    /// <summary>Applies every [HarmonyPatch] class of this assembly. Expected outcome: true when the patching ran without an exception.</summary>
    /// <param name="harmony">The Harmony instance of this mod.</param>
    /// <param name="assembly">The assembly that holds the patch classes.</param>
    /// <returns>True when patching worked.</returns>
    internal static bool Apply(Harmony harmony, Assembly assembly)
    {
        try
        {
            harmony.PatchAll(assembly);
            int count = 0;
            foreach (MethodBase m in harmony.GetPatchedMethods())
            {
                count++;
                ModLog.Verbose("PatchInstaller: patched " + m.DeclaringType + "." + m.Name);
            }

            ModLog.Verbose("PatchInstaller: Harmony patched " + count + " method(s)");
            return true;
        }
        catch (HarmonyException e)
        {
            ModLog.Error("PatchInstaller: Harmony PatchAll failed. " + e.GetType().Name + ": " + e.Message);
            return false;
        }
    }
}
