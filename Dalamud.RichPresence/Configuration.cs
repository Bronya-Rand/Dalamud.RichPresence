using System;
using System.Text.Json.Serialization;
using Dalamud.Configuration;

namespace Dalamud.RichPresence;

/// <summary>
/// Discord RPC Configuration Settings
/// </summary>
[Serializable]
public class Configuration : IPluginConfiguration
{
    /// <summary>
    /// Schema version of the configuration.
    /// </summary>
    public int Version { get; set; } = 2;

    /// <summary>
    /// The template strings to use for displaying in Discord RPC.
    /// </summary>
    public string DiscordDetailField = "{playername} {fc}";

    public string DiscordStateField = "{world}";
    public string DiscordSmallImageTextField = "{job} Lv. {level}";
    public string DiscordLargeImageTextField = "{location} {ward}";
    public bool DisplayDiscordTimestamp = true;

    /// <summary>
    /// Shows the queue you are on the Login Screen (uses Waitingway)
    /// </summary>
    public bool ShowLoginQueuePosition = true;

    /// <summary>
    /// Determines whether to reset the RPC timer when moving zones
    /// </summary>
    public bool ResetTimeWhenChangingZones = true;

    /// <summary>
    /// Whether to use "RDM" over "Red Mage"
    /// </summary>
    public bool AbbreviateJob = true;

    /// <summary>
    /// Determines whether to use the job icon for the small image instead of the online status.
    /// </summary>
    public bool ShowJobIcon = true;

    /// <summary>
    /// Whether to show party information in RPC
    /// </summary>
    public bool ShowPartyData = true;

    /// <summary>
    /// Whether to show AFK status in RPC
    /// </summary>
    public bool ShowAfk = true;

    /// <summary>
    /// Determines whether to hide info whilst AFK.
    /// </summary>
    public bool HideEntirelyWhenAfk;

    /// <summary>
    /// Determines whether to hide info whilst in a cutscene.
    /// </summary>
    public bool HideInCutscene;

    public bool UseCustomRpcTcpBridgePort;
    public int RpcTcpBridgePort = 2026;

    private bool useNative;

    /// <summary>
    /// Whether to use Native UI over ImGui.
    /// </summary>
    public bool UseNative
    {
        get => useNative;
        set
        {
            if (useNative == value) return;
            useNative = value;
            OnUseNativeChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// Determines whether the user has picked a UI to display RPC settings.
    /// </summary>
    public bool HasChosenConfigUi;

    /// <summary>
    /// Action when switching between Native UI and ImGui.
    /// </summary>
    [field: JsonIgnore]
    public event Action<bool>? OnUseNativeChanged;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}