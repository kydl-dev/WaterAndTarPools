using UnityEditor;
using UnityEngine;

/// T-071 (user, 2026-10-21: "people that install only the DrainTarPit mod should get only the assets of that mod, and the water stuff should be
/// available and downloaded only by the WaterAndTarPools mod users"): this Unity project holds the water assets of WaterAndTarPools (pool water
/// shader DTP/PoolWater + the Tropical / Lake / Ashlands materials + the water preview scene). Menu WTP / Build AssetBundle (or batchmode
/// -executeMethod WTP_BuildAssetBundle.Build) builds 'waterandtarpools' and copies it to WaterAndTarPools-Mod/Assets (embedded in the DLL).
public static class WTP_BuildAssetBundle
{
    [MenuItem("WTP/Build AssetBundle")]
    public static void Build()
    {
        // T-073: the water splash sound is in the bundle too (it used to be embedded in the DLL and written to the temp folder)
        var splash = AssetImporter.GetAtPath("Assets/audio/water_splash.ogg");
        if (splash != null && splash.assetBundleName != "waterandtarpools") { splash.assetBundleName = "waterandtarpools"; splash.SaveAndReimport(); }
        string root = System.IO.Path.GetDirectoryName(Application.dataPath);
        string outDir = System.IO.Path.Combine(root, "Deliverables/AssetBundles");
        System.IO.Directory.CreateDirectory(outDir);
        var manifest = BuildPipeline.BuildAssetBundles(outDir, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        Debug.Log("[WTP] AssetBundles built to " + outDir + ": " + (manifest != null ? string.Join(", ", manifest.GetAllAssetBundles()) : "nothing"));
        string built = System.IO.Path.Combine(outDir, "waterandtarpools");
        string modAssets = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, "../WaterAndTarPools-Mod/Assets"));
        if (!System.IO.File.Exists(built)) { Debug.LogError("[WTP] Bundle 'waterandtarpools' was not built (is anything assigned to it?)"); return; }
        System.IO.Directory.CreateDirectory(modAssets);
        System.IO.File.Copy(built, System.IO.Path.Combine(modAssets, "waterandtarpools"), true);
        Debug.Log("[WTP] AssetBundle 'waterandtarpools' (" + UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundle("waterandtarpools").Length + " assets) copied to " + modAssets);
    }
}
