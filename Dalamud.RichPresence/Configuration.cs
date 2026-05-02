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
    /// The template string to use for displaying in Discord RPC.
    /// </summary>
    public string DiscordDetailField = "";

    public string DiscordStateField = "";

    public string DiscordSmallImageTextField = "";
    public string DiscordLargeImageTextField = "";

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
    
    /// <summary>
    /// Determines whether to enable the Wine RPC bridge (for Linux users).
    /// </summary>
    public bool RPCBridgeEnabled = true;
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
