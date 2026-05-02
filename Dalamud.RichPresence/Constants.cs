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
        new("job",        "Your job name or abbreviation [if enabled] (e.g. Summoner or SMN)"),
        new("level",      "Your current level (e.g. 100)"),
        new("location",   "The current location name you are in (e.g. New Gridania)"),
        new("content",    "The name of the content you are doing (e.g. The Vault)"),
    ];
}