using System.IO;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace Dalamud.RichPresence;

internal sealed class LegacyConfigurationV1 : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool ShowLoginQueuePosition = true;
    public bool ShowName = true;
    public bool ShowFreeCompany = true;
    public bool ShowWorld = true;
    public bool AlwaysShowHomeWorld = false;
    public bool ShowDataCenter = false;

    public bool ShowStartTime = false;
    public bool ResetTimeWhenChangingZones = true;

    public bool ShowJob = true;
    public bool AbbreviateJob = true;
    public bool ShowLevel = true;

    public bool ShowParty = true;

    public bool ShowAfk = true;
    public bool HideEntirelyWhenAfk = false;
    public bool HideInCutscene = false;
    public bool RPCBridgeEnabled = true;
}

public static class Migrate
{
    public static Configuration? TryMigrateFromLegacyConfig(IPluginConfiguration? pluginConfiguration)
    {
        // If there's no existing config, nothing to migrate.
        if (pluginConfiguration == null)
            return null;

        // If it's already V2+, no migration needed.
        if (pluginConfiguration.Version >= 2)
        {
            Plugin.Log.Debug("Config is already V2+, no migration needed.");
            return pluginConfiguration as Configuration;
        }

        // V1 Config
        Plugin.Log.Info("Detected V1 config. Attempting migration to V2.");

        var configPath = Plugin.PluginInterface.ConfigFile.FullName;
        if (!File.Exists(configPath))
        {
            Plugin.Log.Warning("Config file not found on disk for migration. Using defaults.");
            return null;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var oldCfg = JsonConvert.DeserializeObject<LegacyConfigurationV1>(json);
            if (oldCfg == null)
            {
                Plugin.Log.Warning("Failed to deserialize V1 config. Using defaults.");
                return null;
            }

            var newCfg = MigrateV1ToV2(oldCfg);
            newCfg.Save();
            Plugin.Log.Info("Successfully migrated V1 config to V2.");
            return newCfg;
        }
        catch (System.Exception ex)
        {
            Plugin.Log.Error(ex, "Error during V1 -> V2 config migration. Using defaults.");
            return null;
        }
    }

    private static Configuration MigrateV1ToV2(LegacyConfigurationV1 oldCfg)
    {
        var cfg = new Configuration
        {
            Version = 2,

            // renamed / same meaning
            ShowLoginQueuePosition = oldCfg.ShowLoginQueuePosition,
            ResetTimeWhenChangingZones = oldCfg.ResetTimeWhenChangingZones,
            AbbreviateJob = oldCfg.AbbreviateJob,
            ShowPartyData = oldCfg.ShowParty,
            ShowAfk = oldCfg.ShowAfk,
            HideEntirelyWhenAfk = oldCfg.HideEntirelyWhenAfk,
            HideInCutscene = oldCfg.HideInCutscene,
            RpcBridgeEnabled = oldCfg.RPCBridgeEnabled,
            DisplayDiscordTimestamp = oldCfg.ShowStartTime,

            // new option: choose a sensible default
            ShowJobIcon = oldCfg.ShowJob,
            // templates based on old toggles
            DiscordDetailField = BuildDetails(oldCfg),
            DiscordStateField = BuildState(oldCfg),
            DiscordLargeImageTextField = "{location}",
            DiscordSmallImageTextField = BuildSmallImageText(oldCfg)
        };

        return cfg;
    }

    private static string BuildDetails(LegacyConfigurationV1 o)
    {
        if (!o.ShowName) return "{location}";

        var s = "{playername}";
        if (o.ShowFreeCompany) s += " {fc}";
        if (o.AlwaysShowHomeWorld) s += " \u2740 {homeworld}";
        return s;
    }

    private static string BuildState(LegacyConfigurationV1 o)
    {
        if (!o.ShowWorld) return "{location}";
        var s = "{world}";
        if (o.ShowDataCenter) s += " ({dc})";
        return s;
    }

    private static string BuildSmallImageText(LegacyConfigurationV1 o)
    {
        if (!o.ShowJob) return ""; // closest equivalent to "Online" without a tag
        return o.ShowLevel ? "{job} Lv. {level}" : "{job}";
    }
}