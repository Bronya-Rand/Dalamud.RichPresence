using System;
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
    public string DiscordSmallImageTextField = "{location} {ward}";
    public string DiscordLargeImageTextField = "{job} Lv. {level}";
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
    public bool HideEntirelyWhenAfk = false;

    /// <summary>
    /// Determines whether to hide info whilst in a cutscene.
    /// </summary>
    public bool HideInCutscene = false;
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
