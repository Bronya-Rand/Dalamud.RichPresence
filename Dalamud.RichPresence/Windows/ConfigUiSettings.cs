using System;
using Dalamud.RichPresence.Helpers;
using Dalamud.Utility;

namespace Dalamud.RichPresence.Windows;

internal sealed record CheckboxSetting(
    string Id,
    string Label,
    string Tooltip,
    Func<Configuration, bool> Get,
    Action<Configuration, bool> Set)
{
    public void Save(Configuration configuration, bool value)
    {
        Set(configuration, value);
        configuration.Save();
    }
}

internal sealed record TemplateSetting(
    string Label,
    string Tooltip,
    string DefaultValue,
    Func<Configuration, string> Get,
    Action<Configuration, string> Set)
{
    public void Save(Configuration configuration, string value)
    {
        Set(configuration, value);
        configuration.Save();
    }
}

// UI-independent definitions and actions. Each window owns its editing drafts.
internal static class ConfigUiSettings
{
    public const int MinPort = 1024;
    public const int MaxPort = 65535;
    public const string WineSectionTitle = "Wine/Proton RPC Bridge Settings";
    public const string CustomPortLabel = "Use Custom RPC TCP Bridge Port";

    public const string CustomPortTooltip =
        "Enables the use of a custom port for the RPC TCP Bridge. Only enable this if you have a specific reason to change the port.";

    public const string PortLabel = "RPC TCP Bridge Port";

    public const string PortTooltip =
        "Sets the port for the RPC TCP Bridge. Only change this if you have a specific reason to do so.";

    public static bool ShowBridgeSettings => Util.IsWine() && !AfUnixHelper.IsAfUnixSupported();
    public static int ClampPort(int port) => Math.Clamp(port, MinPort, MaxPort);

    public static void SaveCustomPortEnabled(Configuration configuration, bool enabled, Action reloadClient)
    {
        configuration.UseCustomRpcTcpBridgePort = enabled;
        configuration.Save();
        reloadClient();
    }

    public static void SavePort(Configuration configuration, int port, Action reloadClient)
    {
        configuration.RpcTcpBridgePort = ClampPort(port);
        configuration.Save();
        reloadClient();
    }

    public static readonly CheckboxSetting[] Conditions =
    [
        new("ShowJobIcon", "Display Job Icon for Small Image",
            "Displays your current job class icon instead of the online status icon for the small image in Discord RPC.",
            c => c.ShowJobIcon, (c, v) => c.ShowJobIcon = v),
        new("AbbreviateJob", "Abbreviate Job Name", "Shortens the Job Name from \"Summoner\" to \"SMN\".",
            c => c.AbbreviateJob, (c, v) => c.AbbreviateJob = v),
        new("ShowPartyData", "Display Party Information",
            "Displays how many people are in your party in Discord RPC (e.g. 7 of 8 players).", c => c.ShowPartyData,
            (c, v) => c.ShowPartyData = v),
        new("DisplayDiscordTimestamp", "Display Timestamps", "Displays the time you have been in a region/on XIV.",
            c => c.DisplayDiscordTimestamp, (c, v) => c.DisplayDiscordTimestamp = v),
        new("ShowLoginQueuePosition", "Display Queue Times [Waitingway]",
            "Displays the queue time when logging in (requires Waitingway).", c => c.ShowLoginQueuePosition,
            (c, v) => c.ShowLoginQueuePosition = v),
        new("ShowAfk", "Display AFK Status", "Displays your AFK status to Discord", c => c.ShowAfk,
            (c, v) => c.ShowAfk = v),
        new("HideEntirelyWhenAfk", "Hide RPC Info Whilst AFK",
            "Hides most information about you when you are AFK to Discord.", c => c.HideEntirelyWhenAfk,
            (c, v) => c.HideEntirelyWhenAfk = v),
        new("HideInCutscene", "Hide RPC Info Whilst In Cutscene",
            "Hides most information about you when you are watching a cutscene.", c => c.HideInCutscene,
            (c, v) => c.HideInCutscene = v),
        new("ResetTimeWhenChangingZones", "Reset Timestamps When Changing Zones",
            "Resets the timestamp timer when changing zones/duties. Otherwise, the timer will count based off login time.",
            c => c.ResetTimeWhenChangingZones, (c, v) => c.ResetTimeWhenChangingZones = v),
    ];

    public static readonly CheckboxSetting NativeUi = new(
        "UseNative", "Use Native UI over ImGui", "Toggle the use of Native UI over ImGui in Discord RPC.",
        c => c.UseNative, (c, v) => c.UseNative = v);

    public static readonly TemplateSetting[] Templates =
    [
        new("Details", "Sets the top part of the Discord RPC header below the game's name.",
            Constants.DefaultDiscordDetailStr, c => c.DiscordDetailField, (c, v) => c.DiscordDetailField = v),
        new("State", "Sets the bottom part of the Discord RPC header below the game's name.",
            Constants.DefaultDiscordStateStr, c => c.DiscordStateField, (c, v) => c.DiscordStateField = v),
        new("Large Image Text", "Sets the text that appears when hovering over the large image.",
            Constants.DefaultDiscordLargeImageStr, c => c.DiscordLargeImageTextField,
            (c, v) => c.DiscordLargeImageTextField = v),
        new("Small Image Text", "Sets the text that appears when hovering over the small image.",
            Constants.DefaultDiscordSmallImageStr, c => c.DiscordSmallImageTextField,
            (c, v) => c.DiscordSmallImageTextField = v),
    ];

    public static (PlayerContext Player, OnlineStatusContext Status) GetPreviewContext(Plugin plugin)
    {
        if (Plugin.ObjectTable.LocalPlayer != null)
        {
            var player = plugin.CollectContext.GetPlayerStatus();
            var status = CollectContext.OnlineStatus;
            return (player, status);
        }
        else
        {
            var player = new PlayerContext(
                PlayerName: "Y'shtola Rhul",
                FcTag: "FFXIV",
                CurrentWorldId: 0,
                CurrentWorld: "Eorzea",
                HomeWorldId: 0,
                HomeWorld: "Hydaelyn",
                IsOnHomeWorld: true,
                DataCenterName: "Scion",
                TerritoryName: "Limsa Lominsa Lower Decks",
                TerritoryLoadingImageId: 1,
                WardId: 0,
                InstanceId: 0,
                ClassJobId: 0,
                ClassJob: "Black Mage",
                ClassJobAbbreviation: "BLM",
                Level: 100
            );

            var status = new OnlineStatusContext(
                IsAfk: false,
                WatchingCutscene: false,
                StatusName: "Online"
            );
            return (player, status);
        }
    }
}