using System.IO;
using System.Reflection;
using BepInEx;
using DrainTarPit;
using DrainTarPit.Drain;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using WaterAndTarPools.Debug;
using WaterAndTarPools.Drain;
using Kydl.Common;
using WaterAndTarPools.Signs;
using WaterAndTarPools.Visuals;

// T-070: the user's KydlTools mod (never published) holds the live water editor, which works on this mod's internals
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("KydlTools")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WaterAndTarPools.UnitTests")]

namespace WaterAndTarPools
{
    /// <summary>
    /// T-069 (user, 2026-10-21): the pools the pump makes (tar and water), pumping water, water styles, pool signs and the pool menu, fish in pools and
    /// the jump boost - split out of DrainTarPit. Needs DrainTarPit (hard dependency): it hooks into DrainTarPit's pump through PoolHooks.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(KydlCore.Plugin.ModGuid, "0.1.0")]
    [BepInDependency(DrainTarPit.Plugin.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.kydldev.WaterAndTarPools";
        public const string ModName = "WaterAndTarPools";
        public const string ModVersion = "2.0.0";
        public const string BundleName = "waterandtarpools";

        internal static Plugin Instance;
        /// The water assets (pool water shader DTP/PoolWater + the style materials), built in WaterAndTarPools-Unity (T-071: only this mod ships them).
        internal static AssetBundle Bundle { get; private set; }

        void Awake()
        {
            Instance = this;
            ModLog.Init(Logger, ModName, () => ModSettings.DebugLog != null && ModSettings.DebugLog.Value);
            ModLog.Info(ModName + " " + ModVersion + " loaded (dll built " + File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-dd HH:mm:ss") + ")");
            Loc.Register(WtpText.En);                  // the texts of the pool tab, the sign menu and the sign piece (English only)
            ModSettings.Bind(Config);
            OldFiles.Carry();
            Bundle = LoadBundle();
            var splash = Bundle != null ? Bundle.LoadAsset<AudioClip>("assets/audio/water_splash.ogg") : null;     // T-073: in the bundle, not written to disk
            if (splash != null) PoolHooks.Clips["water_splash"] = splash;
            ModLog.Verbose("Water splash sound " + (splash != null ? "handed to DrainTarPit (" + splash.length.ToString("0.00") + " s)" : "not in the water bundle - water pours silently"));

            PoolHooks.PoolsMod = ModName + " " + ModVersion;
            PoolHooks.Deposit = Deposit;
            PoolHooks.WaterColourFor = WaterTint.ColourFor;
            PoolVolumeHooks.Install();
            PoolMenu.Register();                       // the Pools tab of the KY hub (F6)
            PoolHooks.Commands["ptest"] = args => PoolTests.Run(args);
            PoolHooks.Commands["meshdump"] = args => DrainTarPit.Debug.DtpCommands.Print(MeshDump.Run(args.Length > 0 && int.TryParse(args[0], out var r) ? r : 6));
            PoolHooks.Commands["cleanpools"] = CleanPools;

            PrefabManager.OnVanillaPrefabsAvailable += RegisterSign;    // the pool sign is cloned from the game's sign, so it needs the vanilla prefabs
            // the pools themselves are registered from a ZNetScene.Awake patch (TarPool): this event fires in the main menu, before any ZNetScene exists
            new Harmony(ModGuid).PatchAll(Assembly.GetExecutingAssembly());
            ModLog.Info("Hooked into DrainTarPit " + DrainTarPit.Plugin.ModVersion + " (deposits, water colour, pool volume hooks, dtp ptest / meshdump / cleanpools)");
        }

        /// The pour of a stroke reached the nozzle (DrainTarPit, PoolHooks.Deposit): the liquid goes into a real pool where the stream lands.
        static void Deposit(Vector3 at, Vector3 dir, float volume, PumpLiquid liquid)
        {
            if (!ModSettings.PoolsOn) { ModLog.Verbose("Deposit: " + liquid + " not deposited, [General] EnableAll or [Pools] Enable is off"); return; }
            bool wanted = liquid == PumpLiquid.Tar ? ModSettings.DepositTar.Value : liquid == PumpLiquid.Water && ModSettings.DepositWater.Value;
            if (!wanted) { ModLog.Verbose("Deposit: " + liquid + " not deposited ([General] Deposit" + liquid + " off), it only pours out visually"); return; }
            TarPool.DepositAtNozzle(at, dir, volume, liquid);
        }

        static void CleanPools(string[] args)
        {
            float max = 1f;
            bool small = args.Length > 0 && args[0].ToLowerInvariant() == "small";
            if (small && args.Length > 1 && !float.TryParse(args[1].Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out max)) max = 1f;
            var report = small ? PoolDebug.RemoveSmallPumpPools(Mathf.Max(0.01f, max)) : PoolDebug.RemoveJunkPools();
            foreach (var line in report) DrainTarPit.Debug.DtpCommands.Print(line);
        }

        static void RegisterSign()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterSign;
            PoolSignPiece.Register();
            PoolSignPlacement.LogTargets();
        }

        /// This mod's water bundle: embedded resource, else a file next to the DLL. null = only the Game water style works (logged).
        static AssetBundle LoadBundle()
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var res in asm.GetManifestResourceNames())
            {
                if (res != BundleName && !res.EndsWith("." + BundleName)) continue;
                using (var stream = asm.GetManifestResourceStream(res))
                using (var ms = new MemoryStream())
                {
                    if (stream != null) stream.CopyTo(ms);
                    var b = AssetBundle.LoadFromMemory(ms.ToArray());
                    if (b != null) { ModLog.Info("Water bundle loaded from the DLL (" + b.GetAllAssetNames().Length + " assets)"); return b; }
                }
            }
            var file = Path.Combine(Path.GetDirectoryName(asm.Location) ?? "", BundleName);
            if (File.Exists(file))
            {
                var b = AssetBundle.LoadFromFile(file);
                if (b != null) { ModLog.Info("Water bundle loaded from " + file); return b; }
            }
            ModLog.Error("Water bundle '" + BundleName + "' not found: only the Game water style works (Tropical / Lake / Ashlands need it)");
            return null;
        }

        void Update()
        {
            PoolRegistry.Tick();                     // pool list / world style RPCs, registered once per game session
            PoolStyles.Tick();                       // an admin changed the world water style -> pools redraw
            if (!ModSettings.PoolsOn) { PoolSignMenu.Shut("mod switched off"); return; }
            PoolSignMenu.Tick();                     // Escape / walking away closes the sign menu
        }

        internal void RunCoroutine(System.Collections.IEnumerator routine) { StartCoroutine(routine); }

    }
}
