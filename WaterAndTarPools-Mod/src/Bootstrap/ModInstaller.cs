using BepInEx.Configuration;
using WaterAndTarPools.Presentation.Hub.Registration;
using KydlCore.Common;
using KydlCore.Core;

namespace WaterAndTarPools.Bootstrap;

/// <summary>Composition root: the mod's KY hub page (title + settings tab) and its feature in the FeatureRegistry (MIGRATION_DECISIONS A4).</summary>
internal sealed class ModInstaller
{
    private readonly ConfigFile _config;

    internal ModInstaller(ConfigFile config)
    {
        _config = config;
        Features = new FeatureRegistry(line => ModLog.Verbose(line));
    }

    internal static ModInstaller Current { get; private set; }

    internal FeatureRegistry Features { get; }

    internal bool Install()
    {
        Current = this;
        bool page = HubTabs.RegisterModPage(_config);
        bool added = true;
        foreach (IFeature feature in new IFeature[] { new WaterAndTarPoolsFeature() })
        {
            OperationResponse<int> add = Features.Add(feature);
            if (!add.IsSuccess)
            {
                added = false;
                ModLog.Error("ModInstaller: " + add.Reason);
            }
        }

        return page && added && Refresh();
    }

    internal bool Refresh()
    {
        OperationResponse<int> r = Features.Refresh();
        if (r.Status == ResponseStatus.Failed)
        {
            ModLog.Warn("ModInstaller: " + r.Reason);
        }
        else
        {
            ModLog.Verbose("ModInstaller: refresh - " + r);
        }

        return r.IsSuccess;
    }

    internal bool Uninstall()
    {
        OperationResponse<int> r = Features.StopAll();
        bool gone = HubTabs.UnregisterModPage();
        if (Current == this)
        {
            Current = null;
        }

        return r.IsSuccess && gone;
    }
}
