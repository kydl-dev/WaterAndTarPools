using System.Collections.Generic;
using Kydl.Common;
using KydlCore;
using UnityEngine;
using WaterAndTarPools.Visuals;
using DrainTarPit;
using WaterAndTarPools.Ui;

namespace WaterAndTarPools.Signs
{
    /// <summary>
    /// The pool menu: the "Pools" tab of the KY hub (F6, KydlCore; before 2.0.0 its own window on [Visuals] PoolMenuKey, F9). Admins set the world water style
    /// (every pool, every player); every player sees the pools that have a named sign (name in its sign style, position, closest portal) and picks a style per
    /// pool that only they see. The page is DESCRIBED (KyPage) and drawn by KydlCore with its theme and text size.
    /// </summary>
    internal static class PoolMenu
    {
        internal const string TabId = "pools";
        static int _shownVersion = -1;

        /// Once, from Plugin.Awake: adds the tab and asks the server for the pool list whenever the tab is opened.
        internal static void Register()
        {
            KyHub.AddPage(Plugin.ModGuid, TabId, Loc.T("wtp_ui_pools_title"), Build, 0, 2);
            KyHub.TabShown += OnTabShown;
        }

        static void OnTabShown(string modGuid, string id)
        {
            if (modGuid != Plugin.ModGuid || id != TabId) return;
            int asked = PoolRegistry.Request();
            ModLog.Verbose("PoolMenu: tab shown, list request sent: " + asked + " (world style '" + WorldStyle + "', admin " + IsAdmin + ")");
        }

        static bool IsAdmin { get { return ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost(); } }
        static string WorldStyle { get { var w = PoolStyles.World(); return w.HasValue ? w.Value.ToString() : ""; } }
        static string DefaultStyle { get { return PoolStyles.Default.ToString(); } }
        static string[] Styles { get { return System.Enum.GetNames(typeof(WaterStyleOption)); } }
        static bool Waiting { get { return PoolRegistry.Version == 0 && Time.unscaledTime - PoolRegistry.RequestedAt < 10f; } }

        static string StyleName(string s) { return s.Length == 0 ? Loc.T("wtp_ui_style_notset") : Loc.T("wtp_ui_style_" + s.ToLowerInvariant()); }

        static IList<KyChoice> Choices(bool withEmpty, string emptyText)
        {
            var list = new List<KyChoice>();
            if (withEmpty) list.Add(new KyChoice("", emptyText));
            foreach (var s in Styles) list.Add(new KyChoice(s, StyleName(s)));
            return list;
        }

        /// The tab's page: the world style, then one table row per named pool.
        static KyPage Build()
        {
            var page = new KyPage();
            var pools = PoolRegistry.Received;
            var state = new System.Text.StringBuilder();
            state.Append(PoolRegistry.Version).Append('|').Append(WorldStyle).Append('|').Append(IsAdmin).Append('|').Append(Waiting).Append('|').Append(pools.Count);
            foreach (var p in pools) { var mine = PoolStyles.MineFor(p.Id); state.Append('|').Append(p.Id).Append(':').Append(p.Name).Append(':').Append(p.Style).Append(':').Append(mine.HasValue ? mine.Value.ToString() : ""); }
            page.State = state.ToString();
            _shownVersion = PoolRegistry.Version;

            var world = page.Section(Loc.T("wtp_ui_world_style"));
            if (IsAdmin)
            {
                world.Choice(Loc.T("wtp_ui_world_style_all"), Choices(true, StyleName("")), WorldStyle, SetWorldStyle);
                world.Note(Loc.T("wtp_ui_world_style_note"));
            }
            else
            {
                world.Value(Loc.T("wtp_ui_world_style_all"), StyleName(WorldStyle));
                world.Note(Loc.T("wtp_ui_world_style_admin_only"));
            }

            var list = page.Section(Loc.T("wtp_ui_pools"));
            list.Value(Loc.T("wtp_ui_pools_count", pools.Count.ToString()), "");
            list.Button(Loc.T("wtp_ui_refresh"), Refresh);
            list.Note(Loc.T("wtp_ui_pools_note"));
            if (Waiting && pools.Count == 0) { list.Note(Loc.T("wtp_ui_pools_loading")); return page; }
            if (pools.Count == 0) { list.Note(Loc.T("wtp_ui_pools_none")); return page; }

            var rows = new List<IList<KyCell>>();
            string def = Loc.T("wtp_ui_style_default", StyleName(DefaultStyle));
            foreach (var p in pools)
            {
                string id = p.Id;
                var mine = PoolStyles.MineFor(id);
                string mineText = mine.HasValue ? mine.Value.ToString() : "";
                var row = new List<KyCell>
                {
                    KyCell.Rich(SignStyle.Parse(p.Style).RenderForUi(p.Name)),
                    KyCell.Label(Loc.T("wtp_ui_pool_pos", p.Pos.x.ToString("0"), p.Pos.z.ToString("0"), p.Pos.y.ToString("0"))),
                    KyCell.Label(p.PortalDistance < 0f ? Loc.T("wtp_ui_pool_noportal")
                        : Loc.T("wtp_ui_pool_portal", p.Portal.Length > 0 ? p.Portal : Loc.T("wtp_ui_pool_portal_unnamed"), p.PortalDistance.ToString("0"))),
                };
                if (p.Liquid == "tar") row.Add(KyCell.Label(Loc.T("wtp_ui_tar_pool")));              // tar pools have no styles
                else row.Add(KyCell.Drop(Choices(true, def), mineText, s => SetPoolStyle(id, s)));
                rows.Add(row);
            }
            list.Table(new[] { Loc.T("wtp_ui_name"), Loc.T("wtp_ui_col_position"), Loc.T("wtp_ui_col_portal"), Loc.T("wtp_ui_my_style") }, rows);
            ModLog.Verbose("PoolMenu: page built (" + pools.Count + " pool(s), admin " + IsAdmin + ", world style '" + WorldStyle + "', waiting " + Waiting + ")");
            return page;
        }

        /// For the TestLab hook: builds the page once and describes it ("pools: N pool(s), M section(s), admin A").
        internal static string DescribeForTest()
        {
            var page = Build();
            return "pools: " + PoolRegistry.Received.Count + " pool(s), " + page.Sections.Count + " section(s), admin " + IsAdmin;
        }

        static void SetWorldStyle(string style)
        {
            int sent = PoolRegistry.SetWorldStyle(style);
            ModLog.Verbose("PoolMenu: world style '" + style + "' asked for (sent " + sent + ")");
            Plugin.Instance.RunCoroutine(RefreshSoon());                       // the global key comes back from the server a moment later
        }

        static System.Collections.IEnumerator RefreshSoon()
        {
            yield return new WaitForSecondsRealtime(0.6f);
            var tab = KyHub.AddPage(Plugin.ModGuid, TabId, Loc.T("wtp_ui_pools_title"), Build, 0, 2);       // same id: updates the tab, nothing new
            if (tab != null) tab.Refresh();
            ModLog.Verbose("PoolMenu: redrawn after the world style change (now '" + WorldStyle + "')");
        }

        static void SetPoolStyle(string id, string style)
        {
            WaterStyleOption s;
            WaterStyleOption? choice = style.Length > 0 && System.Enum.TryParse(style, true, out s) ? s : (WaterStyleOption?)null;
            int n = PoolStyles.SetMine(id, choice);
            ModLog.Verbose("PoolMenu: my style for pool " + id + " -> '" + style + "' (" + n + " personal choice(s) in this world)");
        }

        static void Refresh() { int asked = PoolRegistry.Request(); ModLog.Verbose("PoolMenu: refresh asked (" + asked + ")"); }
    }
}
