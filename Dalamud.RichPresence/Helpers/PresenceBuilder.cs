using Dalamud.RichPresence.Models;
using Dalamud.RichPresence.Services;
using DiscordRPC;

namespace Dalamud.RichPresence.Helpers
{
    internal static class PresenceBuilder
    {
        private const string DefaultLargeImageKey = "li_1";
        private const string DefaultSmallImageKey = "class_0";

        /// <summary>
        /// Builds a Discord rich presence from the collected context structs.
        /// Returns null if the presence should be cleared (cutscene/AFK hide rules).
        /// </summary>
        public static DiscordRPC.RichPresence? Build(
            PlayerContext player,
            PartyContext party,
            OnlineStatusContext status,
            Timestamps? timestamps,
            Configuration configuration)
        {
            var loc = Plugin.LocalizationService;

            // Highest-priority visibility rules — clear presence entirely
            if (status.WatchingCutscene && configuration.HideInCutscene)
                return null;

            if (status.IsAfk && configuration.HideEntirelyWhenAfk)
                return null;

            // --- Details (line 1) ---
            var details = ParserService.Parse(configuration.DiscordDetailField, player, party, status, configuration);

            // --- State (line 2) — lowest-priority default, overwritten by duty/AFK below ---
            var state = ParserService.Parse(configuration.DiscordStateField, player, party, status, configuration);

            // --- Images ---
            var largeImageKey = player.TerritoryLoadingImageId != 0
                ? $"li_{player.TerritoryLoadingImageId}"
                : DefaultLargeImageKey;
            var largeImageText = ParserService.Parse(configuration.DiscordLargeImageTextField, player, party, status,
                configuration);

            var smallImageKey = configuration.ShowJobIcon
                ? $"class_{player.ClassJobId}"
                : DefaultSmallImageKey;
            var smallImageText = ParserService.Parse(configuration.DiscordSmallImageTextField, player, party, status,
                configuration);

            var presence = new DiscordRPC.RichPresence
            {
                Details = details,
                State = state,
                Assets = new Assets
                {
                    LargeImageKey = largeImageKey,
                    LargeImageText = largeImageText,
                    SmallImageKey = smallImageKey,
                    SmallImageText = smallImageText,
                },
                Timestamps = timestamps,
            };

            // --- Party / Duty overrides State ---
            if (party.InParty)
            {
                if (party.InDuty)
                    presence.State = loc.Localize("DalamudRichPresenceInADuty",
                        LocalizationLanguage.Client);

                presence.Party = new Party
                {
                    Size = party.PartySize,
                    Max = party.PartyMaxSize,
                    ID = party.HashedPartyId,
                };
            }

            // --- AFK overrides State and small image (lower priority than hide-entirely, already handled above) ---
            if (!status.IsAfk || !configuration.ShowAfk) return presence;

            presence.State = status.StatusName;
            presence.Assets.SmallImageKey = "away";

            return presence;
        }

        /// <summary>
        /// Builds a queue presence. Returns null if not in queue or position not loaded.
        /// </summary>
        public static DiscordRPC.RichPresence? BuildQueue(QueueContext queue, Timestamps? timestamps)
        {
            if (!queue.IsInQueue || queue.Position < 0)
                return null;

            var loc = Plugin.LocalizationService;

            var eta = queue.Estimate?.TotalSeconds >= 1d
                ? string.Format(loc.Localize("DalamudRichPresenceQueueEstimate",
                    LocalizationLanguage.Client), queue.Estimate)
                : string.Empty;

            return new DiscordRPC.RichPresence
            {
                Details = string.Format(loc.Localize("DalamudRichPresenceInLoginQueue",
                    LocalizationLanguage.Client), queue.Position),
                State = eta,
                Assets = new Assets
                {
                    LargeImageKey = DefaultLargeImageKey,
                    SmallImageKey = DefaultSmallImageKey,
                },
                Timestamps = timestamps,
            };
        }
    }
}
