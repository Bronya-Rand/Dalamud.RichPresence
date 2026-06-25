using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.RichPresence.Helpers;
using Dalamud.Utility;

namespace Dalamud.RichPresence.Services;

public static partial class ParserService
{
    private static readonly List<string> AcceptedTags =
    [
        "playername",
        "world",
        "dc",
        "job",
        "level",
        "location",
        "ward",
        "fc",
        "homeworld",
        "status"
    ];

    /// <summary>
    /// Validates whether the string input is valid.
    /// </summary>
    /// <param name="input">The string data to check</param>
    /// <returns></returns>
    public static bool Validate(string input)
    {
        if (string.IsNullOrEmpty(input)) return true;

        var matches = DiscordXivRegex().Matches(input);
        foreach (Match match in matches)
        {
            if (!AcceptedTags.Contains(match.Groups[1].Value.ToLowerInvariant()))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns the values for an applicable tag
    /// </summary>
    /// <param name="tag">The tag to resolve.</param>
    /// <param name="player">Player Information</param>
    /// <param name="party">Party Information</param>
    /// <param name="status">Online Status Info</param>
    /// <param name="config">Config Settings</param>
    /// <returns></returns>
    private static string Resolve(string tag, PlayerContext player, PartyContext party, OnlineStatusContext status, Configuration config)
    {
        return tag.ToLowerInvariant() switch
        {
#if DEBUG
            "playername" => "Y'shtola Rhul",
            "world" => "Eorzea",
            "dc" => "Scion",
            "job" => config.AbbreviateJob ? "BLM" : "Black Mage",
            "level" => "100",
            "location" => player.TerritoryName,
            "ward" => string.Empty,
            "fc" => "\u00abFFXIV\u00bb",
            "homeworld" => "Hydaleyn",
            "status" => status.StatusName,
            _ => string.Empty
#else
            "playername" => player.PlayerName,
            "world" => player.CurrentWorld,
            "dc" => player.DataCenterName,
            "job" => config.AbbreviateJob ? player.ClassJobAbbreviation : player.ClassJob,
            "level" => player.Level > 0 ? player.Level.ToString() : string.Empty,
            "location" => player.TerritoryName,
            "ward" => player.WardId > -1 ? $"Ward {player.WardId + 1}" : string.Empty,
            "fc" => player.FcTag.IsNullOrEmpty() ? string.Empty : $"\u00ab{player.FcTag}\u00bb",
            "homeworld" => player.HomeWorld,
            "status" => status.StatusName,
            _ => string.Empty
#endif
        };
    }

    /// <summary>
    /// Parses a given string of tags into a readable string
    /// </summary>
    /// <param name="input">The string of data to parse in</param>
    /// <param name="player">Player Information</param>
    /// <param name="party">Party Information</param>
    /// <param name="status">Online Status Info</param>
    /// <param name="config">Config Settings</param>
    /// <returns></returns>
    public static string Parse(string input, PlayerContext player, PartyContext party, OnlineStatusContext status, Configuration config)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        var compiledRegex = DiscordXivRegex();

        return compiledRegex.Replace(input, match =>
        {
            var tag = match.Groups[1].Value.ToLowerInvariant();
            return AcceptedTags.Contains(tag) ? Resolve(tag, player, party, status, config) : match.Value;
        });
    }

    [GeneratedRegex(@"\{([^}]+)\}", RegexOptions.Compiled)]
    private static partial Regex DiscordXivRegex();
}