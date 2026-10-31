using BepInEx.Configuration;
using DrainTarPit;
using DrainTarPit.Drain;
using UnityEngine;
using WaterAndTarPools.Drain;
using Kydl.Common;
using WaterAndTarPools.Visuals;

namespace WaterAndTarPools
{
    /// <summary>
    /// The WaterAndTarPools config (T-069, user's config review 2026-10-21) and its fixed values (Tunable: not in the file, the user's KydlTools mod
    /// can change them for testing). The values DrainTarPit itself needs for water (AllowWater, MinWaterDepth, the splash, the water slug) are
    /// DrainTarPit Tunables and are set from here (Sync), so DrainTarPit works the same with or without this mod.
    /// </summary>
    internal static class ModSettings
    {
        // [General]
        public static ConfigEntry<bool> EnableAll;
        public static ConfigEntry<bool> DepositTar;
        public static ConfigEntry<bool> DepositWater;
        // [DebugLog]
        public static ConfigEntry<bool> DebugLog;
        // [Effects]
        public static ConfigEntry<bool> EffectsEnable;
        public static ConfigEntry<float> EffectsVolume;
        public static ConfigEntry<bool> WaterSplashSound;
        public static ConfigEntry<bool> WaterSlugFlow;
        // [Pools]
        public static ConfigEntry<bool> PoolsEnable;
        public static ConfigEntry<float> MinWaterDepth;
        public static ConfigEntry<bool> PoolWalls;
        public static ConfigEntry<bool> PoolOverflow;
        public static ConfigEntry<bool> PoolRimDisappear;
        public static ConfigEntry<bool> PoolRunoff;
        public static ConfigEntry<bool> PoolShedFilm;
        public static ConfigEntry<bool> PoolFilmEvaporates;
        public static ConfigEntry<bool> PoolFishStayIn;
        public static ConfigEntry<bool> PoolJumpBoost;
        public static ConfigEntry<int> PoolJumpPresses;
        public static ConfigEntry<float> PoolJumpHeight;
        public static ConfigEntry<float> LiquidCellSize;
        public static ConfigEntry<float> WaterFlow;
        public static ConfigEntry<float> WaterPoolSize;
        // [Visuals]
        public static ConfigEntry<bool> VisualsEnable;
        public static ConfigEntry<float> WaterDepthLook;

        /// Is the mod on? Both this mod's [General] EnableAll and DrainTarPit's (pools need the pump).
        public static bool On { get { return (EnableAll == null || EnableAll.Value) && DrainTarPit.ModSettings.On; } }

        /// Are the pools on? The mod is on and [Pools] Enable is on (pool deposits, the pool sign menu, fish that stay in, the jump boost).
        public static bool PoolsOn { get { return On && (PoolsEnable == null || PoolsEnable.Value); } }

        // --- fixed values (Tunables) -------------------------------------------------------------------------------------------------
        public static readonly Kydl.Common.Tunable<WaterStyleOption> WaterStyle = new("Visuals", "WaterStyle", WaterStyleOption.Game,
            "Water style of pools when no admin world style and no personal style is set.");
        public static readonly Kydl.Common.Tunable<bool> PoolMeshFix = new("Visuals", "PoolMeshFix", true,
            "Dry grid points near the water are put at the water level (no water sheet hanging on the walls).");
        public static readonly Kydl.Common.Tunable<int> PoolMeshReach = new("Visuals", "PoolMeshReach", 6,
            "PoolMeshFix: how many grid cells away from a wet point a dry point is still put at the water level.");
        public static readonly Kydl.Common.Tunable<float> SignBoardBrightness = new("Visuals", "SignBoardBrightness", 1.5f,
            "Pool sign: brightness of the wooden board.");
        public static readonly Kydl.Common.Tunable<float> SignTextBrightness = new("Visuals", "SignTextBrightness", 1.2f,
            "Pool sign: brightness of the name's colours.");
        public static readonly Kydl.Common.Tunable<float> PoolFilmDepth = new("Pools", "PoolFilmDepth", 0.05f,
            "Depth (m) below which liquid counts as a thin film for PoolShedFilm.");

        public static void Bind(ConfigFile c)
        {
            EnableAll = c.Bind("General", "EnableAll", true,
                "Switches the whole mod on / off while playing. Off: pumped liquid only pours out visually, no water pumping, no pool menus or sign editing, fish and the jump boost are left to the game. Pools already in the world stay.");
            DepositTar = c.Bind("General", "DepositTar", true,
                "Pumped tar is poured into a real tar pool where the stream from the nozzle lands: it fills the lowest basin around that point (a hole dug with the pickaxe, a dip enclosed by walls) up to its rim. The pool slows players, is saved in the world and can be pumped again. Off = the tar only pours out visually.");
            DepositWater = c.Bind("General", "DepositWater", true,
                "Pumped water is poured into a real water pool where the stream from the nozzle lands (a hole, a moat, a dip enclosed by walls), saved in the world and pumpable again. Water that lands in the sea or a lake just joins it. Off = the water only pours out visually.");
            DebugLog = c.Bind("DebugLog", "Enable", false,
                "Writes detailed lines about everything this mod does to the BepInEx log (for bug reports). Leave off while playing normally.");

            EffectsEnable = c.Bind("Effects", "Enable", true,
                "Sounds and moving water effects of this mod (the water splash, the water slug in the hoses). Off = water pours silently and no slug is shown.");
            EffectsVolume = c.Bind("Effects", "EffectsVolume", 0.6f,
                new ConfigDescription("Volume of the water splash (0 = silent). The pump sounds have their own volume in DrainTarPit.", new AcceptableValueRange<float>(0f, 1f)));
            WaterSplashSound = c.Bind("Effects", "WaterSplashSound", true,
                "Play a water splash where the pumped water comes down. Off = water pours silently.");
            WaterSlugFlow = c.Bind("Effects", "WaterSlugFlow", true,
                "Show a slug of water travelling through see-through hoses on every pump stroke (its speed is DrainTarPit's LiquidFlowSpeed).");

            PoolsEnable = c.Bind("Pools", "Enable", true,
                "The pools: pumped liquid is poured into real pools, the pool sign menu, fish that stay in a water pool, the jump out of a pool. Off = none of these; pools already in the world stay as they are.");
            MinWaterDepth = c.Bind("Pools", "MinWaterDepth", 0.3f,
                new ConfigDescription("Minimum water depth (m) at the strainer tip for it to count as 'in water'.", new AcceptableValueRange<float>(0.05f, 3f)));
            PoolWalls = c.Bind("Pools", "PoolWalls", true,
                "Built pieces (stone / wood walls, floors, ...) hold the liquid of the pools the pump makes, like terrain does: a ring of walls fills up to the lowest point of its rim. Pieces of DrainTarPit, players and items are ignored; a roof floating over the pool does not count.");
            PoolRimDisappear = c.Bind("Pools", "PoolRimDisappear", true,
                "The rim is the lowest point of the walls / ground around a pool. On: the liquid rises to the rim and never higher; whatever would run over it disappears right there. Off: the liquid runs over the rim (see PoolOverflow).");
            PoolOverflow = c.Bind("Pools", "PoolOverflow", true,
                "Only with PoolRimDisappear off: liquid running over the rim fills whatever basin lies behind it (up to 3 more); what finds no basin runs off. Off = it just runs off.");
            PoolRunoff = c.Bind("Pools", "PoolRunoff", true,
                "When the liquid reaches the edge of a pool's grid and the ground goes on downhill (a long moat, a ditch) it continues into the next pool beyond the edge (at most 6 in a row), so a moat longer than one pool fills end to end. On flat ground it runs off.");
            PoolShedFilm = c.Bind("Pools", "PoolShedFilm", true,
                "Liquid thinner than 5 cm (a film) on the outside of walls or on a slope slides off quickly instead of lingering. Off = the game's own slow spreading.");
            PoolFilmEvaporates = c.Bind("Pools", "PoolFilmEvaporates", true,
                "With PoolShedFilm: a film lying flat with nothing deeper next to it evaporates in a second or two. Off = such films stay.");
            PoolFishStayIn = c.Bind("Pools", "PoolFishStayIn", true,
                "Fish in a water pool stay in its water: they turn back at the walls and cannot jump out. Fish in the sea / lakes are never touched.");
            PoolJumpBoost = c.Bind("Pools", "PoolJumpBoost", true,
                "Getting out of a pool (water or tar): close to its edge (within 2 m), pressing Jump PoolJumpPresses times within 3 seconds lifts you PoolJumpHeight m up and towards the edge.");
            PoolJumpPresses = c.Bind("Pools", "PoolJumpPresses", 3,
                new ConfigDescription("Jump presses within 3 seconds at the edge of a pool that give the lift (PoolJumpBoost).", new AcceptableValueRange<int>(1, 6)));
            PoolJumpHeight = c.Bind("Pools", "PoolJumpHeight", 2f,
                new ConfigDescription("How high (m) the lift out of a pool takes your feet (PoolJumpBoost).", new AcceptableValueRange<float>(0.5f, 5f)));
            WaterPoolSize = c.Bind("Pools", "WaterPoolSize", 24f,
                new ConfigDescription("Side length (m) of one pool grid. Bigger pools hold longer moats in one piece but cost more simulation time. Read when the world loads; pools that already exist keep their size.",
                    new AcceptableValueRange<float>(8f, 48f)));
            LiquidCellSize = c.Bind("Pools", "LiquidCellSize", 0.25f,
                new ConfigDescription("Size (m) of one grid cell of a pool (tar and water). The liquid surface ends at a grid point, so smaller = it reaches the walls more closely, at more simulation time. Read when the world loads; pools that already exist keep their size.",
                    new AcceptableValueRange<float>(0.15f, 0.5f)));
            WaterFlow = c.Bind("Pools", "WaterFlow", 4f,
                new ConfigDescription("How freely water runs compared with tar: 1 = as thick as tar, 4 = about twice as fast, 16 = about four times.", new AcceptableValueRange<float>(1f, 64f)));

            VisualsEnable = c.Bind("Visuals", "Enable", true,
                "How pool water looks: uses the WaterDepthLook below. Off = pool water uses the built-in default look.");
            WaterDepthLook = c.Bind("Visuals", "WaterDepthLook", 0.15f,
                new ConfigDescription("Game water style only: how deep the water looks (0 = shallow, calm and clear like the shore, 1 = open sea). Applies live.", new AcceptableValueRange<float>(0f, 1f)));
            Sync();
            EnableAll.SettingChanged += (s, e) => { Sync(); ModLog.Info("Config: EnableAll = " + EnableAll.Value); };
            MinWaterDepth.SettingChanged += (s, e) => Sync();
            EffectsEnable.SettingChanged += (s, e) => Sync();
            PoolsEnable.SettingChanged += (s, e) => { if (!PoolsOn) Signs.PoolSignMenu.Shut("pools switched off"); };
            VisualsEnable.SettingChanged += (s, e) => PoolWater.RequestRefresh();
            WaterSplashSound.SettingChanged += (s, e) => Sync();
            EffectsVolume.SettingChanged += (s, e) => Sync();
            WaterSlugFlow.SettingChanged += (s, e) => Sync();
            WaterDepthLook.SettingChanged += (s, e) => PoolWater.RequestRefresh();
            WaterStyle.SettingChanged += (s, e) => { ModLog.Verbose("Config: WaterStyle = " + WaterStyle.Value + ", water pools redraw"); PoolWater.RequestRefresh(); };
            PoolMeshFix.SettingChanged += (s, e) => PoolMesh.Refresh();
            PoolMeshReach.SettingChanged += (s, e) => PoolMesh.Refresh();
            SignBoardBrightness.SettingChanged += (s, e) => ModLog.Verbose("Config: SignBoardBrightness = " + SignBoardBrightness.Value + ", " + Signs.PoolSign.BrightnessChanged() + " sign(s) redrawn");
            SignTextBrightness.SettingChanged += (s, e) => ModLog.Verbose("Config: SignTextBrightness = " + SignTextBrightness.Value + ", " + Signs.PoolSign.BrightnessChanged() + " sign(s) redrawn");
            WaterFlow.SettingChanged += (s, e) => TarPool.ApplyWaterFlow();       // live: existing water pools + the prefabs for new ones
            PoolMesh.Refresh();
            ModLog.Verbose("ModSettings: bound (EnableAll " + EnableAll.Value + ", debug log " + DebugLog.Value + ")");
        }

        /// Hands the water values DrainTarPit reads to its Tunables. Returns 1 when water pumping is allowed now.
        internal static int Sync()
        {
            DrainTarPit.ModSettings.AllowWater.Value = On;
            DrainTarPit.ModSettings.MinWaterDepth.Value = MinWaterDepth.Value;
            DrainTarPit.ModSettings.WaterSplashSound.Value = EffectsEnable.Value && WaterSplashSound.Value;
            DrainTarPit.ModSettings.WaterSplashVolume.Value = EffectsVolume.Value;
            DrainTarPit.ModSettings.WaterSlugFlow.Value = EffectsEnable.Value && WaterSlugFlow.Value;
            ModLog.Verbose("ModSettings: DrainTarPit water values set (water pumping " + (On ? "on" : "off") + ", min depth " + MinWaterDepth.Value.ToString("0.00") +
                " m, splash " + WaterSplashSound.Value + " at " + EffectsVolume.Value.ToString("0.00") + ", water slug " + WaterSlugFlow.Value + ")");
            return DrainTarPit.ModSettings.AllowWater.Value ? 1 : 0;
        }
    }
}
