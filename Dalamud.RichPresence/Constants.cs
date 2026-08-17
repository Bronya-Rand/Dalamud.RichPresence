namespace Dalamud.RichPresence;

public readonly record struct TagDefinition(string Tag, string Description);
public static class Constants
{
    public const string DefaultDiscordDetailStr = "{playername} {fc}";
    public const string DefaultDiscordStateStr = "{world}";
    public const string DefaultDiscordLargeImageStr = "{location}";
    public const string DefaultDiscordSmallImageStr = "{job} Lv. {level}";

    public static readonly TagDefinition[] AvailableTags =
    [
        new("playername", "Your character's name (e.g. Y'shtola Rhul)"),
        new("fc",         "Your free company's tag (e.g. \u00abFFXIV\u00bb)"),
        new("world",      "The current world you are in (e.g. Balmung)"),
        new("homeworld",  "Your home world (e.g. Goblin)"),
        new("dc",         "The data center you are in (e.g. Aether)"),
        new("ward",       "The ward number of your current residential area (e.g. Ward 20)"),
        new("job",        "Your job name or abbreviation [if enabled] (e.g. Summoner or SMN)"),
        new("level",      "Your current level (e.g. 100)"),
        new("location",   "The current location name/content you are in/doing (e.g. New Gridania/Windurst)"),
    ];

    public const string RPCTCPBridgeWarning = """
        Discord RPC in this version of Wine/Proton requires a TCP bridge to work. Run the Discord TCP Bridge from the plugin's Resources/binaries folder
        for your respective operating system.

        For more information, see the Discord RPC forum post in #plugin-help-forum or the Discord RPC GitHub page.
        """;
}