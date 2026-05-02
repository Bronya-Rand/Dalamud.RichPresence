using System;
using System.IO;
using System.Text.Json;
using Dalamud.Configuration;

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
    public static Configuration? TryMigrateFromLegacyConfig()
    {
        string configPath = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "Dalamud.RichPresence.json");
        if (!File.Exists(configPath)) return null;
        
        var json = File.ReadAllText(configPath);
        LegacyConfigurationV1? oldCfg;
        try
        {
            oldCfg = JsonSerializer.Deserialize<LegacyConfigurationV1>(json);
        }
        catch
        {
            return null;
        }

        return oldCfg == null ? null : MigrateV1ToV2(oldCfg);
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