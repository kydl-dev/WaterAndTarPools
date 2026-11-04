using BepInEx.Configuration;
using KydlCore.Common;
using KydlCore.Features.Hub.Contracts;
using WaterAndTarPools.Signs;

namespace WaterAndTarPools.Presentation.Hub.Registration;

/// <summary>What WaterAndTarPools declares to the KY hub (F6, MIGRATION_DECISIONS B11): its title, the Pools page (PoolMenu) and the settings tab (every config entry).</summary>
internal static class HubTabs
{
    internal const string SettingsId = "settings";

    internal static bool RegisterModPage(ConfigFile config)
    {
        KyHub.SetModTitle(Plugin.ModGuid, "Water And Tar Pools");
        PoolMenu.Register();                       // the Pools tab (described page, refreshed while open)
        var tab = KyHub.AddTab(Plugin.ModGuid, SettingsId, "Settings", () => KyHub.ConfigEntries(config), 1);
        ModLog.Verbose("HubTabs: settings tab " + (tab != null ? "registered" : "FAILED"));
        return tab != null && KyHub.HasTab(Plugin.ModGuid, PoolMenu.TabId);
    }

    internal static bool UnregisterModPage()
    {
        KyHub.RemoveTab(Plugin.ModGuid, SettingsId);
        KyHub.RemoveTab(Plugin.ModGuid, PoolMenu.TabId);
        PoolMenu.Unregister();
        return !KyHub.HasTab(Plugin.ModGuid, SettingsId) && !KyHub.HasTab(Plugin.ModGuid, PoolMenu.TabId);
    }
}