using Dalamud.RichPresence.Models;
using DiscordRPC;

namespace Dalamud.RichPresence.Helpers
{
    internal static class PresenceBuilder
    {
        private const string DEFAULT_LARGE_IMAGE_KEY = "li_1";
        private const string DEFAULT_SMALL_IMAGE_KEY = "class_0";

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

            if (status.IsAFK && configuration.HideEntirelyWhenAfk)
                return null;

            // --- Details (line 1) ---
            string details;
            if (configuration.ShowName)
            {
#if DEBUG
                details = "Y'shtola Rhul";
#else
                details = player.PlayerName;
#endif

                if (configuration.ShowFreeCompany && player.IsOnHomeWorld && !string.IsNullOrEmpty(player.FcTag))
#if DEBUG
                    details = $"{details} \u00abFC\u00bb";
#else
                    details = $"{details} \u00ab{player.FcTag}\u00bb";
#endif
                if (configuration.ShowWorld && !player.IsOnHomeWorld)
                    details = $"{details} \u2740 {player.HomeWorld}";
                else if (configuration.AlwaysShowHomeWorld)
                    details = $"{details} \u2740 {player.HomeWorld}";
            }
            else
            {
                details = player.TerritoryName;
            }

            // --- State (line 2) — lowest-priority default, overwritten by duty/AFK below ---
            string state;
            if (configuration.ShowWorld)
            {
#if DEBUG
                state = "Test World";
                if (configuration.ShowDataCenter)
                    state = $"{state} (Test Data Center)";
#else
                state = player.CurrentWorld;
                if (configuration.ShowDataCenter)
                    state = $"{state} ({player.DataCenterName})";
#endif
            }
            else
            {
                state = configuration.ShowName ? player.TerritoryName : player.TerritoryRegion;
            }

            // --- Images ---
            var largeImageKey = player.TerritoryLoadingImageId != 0
                ? $"li_{player.TerritoryLoadingImageId}"
                : DEFAULT_LARGE_IMAGE_KEY;
            var largeImageText = player.TerritoryName;

            var smallImageKey = DEFAULT_SMALL_IMAGE_KEY;
            var smallImageText = loc.Localize("DalamudRichPresenceOnline", LocalizationLanguage.Client);

            if (configuration.ShowJob)
            {
                smallImageKey = $"class_{player.ClassJobId}";
                smallImageText = configuration.AbbreviateJob
                    ? player.ClassJobAbbreviation
                    : loc.TitleCase(player.ClassJob);

                if (configuration.ShowLevel)
                    smallImageText = $"{smallImageText} {string.Format(loc.Localize("DalamudRichPresenceLevel", LocalizationLanguage.Client), player.Level)}";
            }

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
                    presence.State = loc.Localize("DalamudRichPresenceInADuty", LocalizationLanguage.Client);

                presence.Party = new Party
                {
                    Size = party.PartySize,
                    Max = party.PartyMaxSize,
                    ID = party.HashedPartyId,
                };
            }

            // --- AFK overrides State and small image (lower priority than hide-entirely, already handled above) ---
            if (status.IsAFK && configuration.ShowAfk)
            {
                presence.State = status.StatusName;
                presence.Assets.SmallImageKey = "away";
            }

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
                ? string.Format(loc.Localize("DalamudRichPresenceQueueEstimate", LocalizationLanguage.Client), queue.Estimate)
                : string.Empty;

            return new DiscordRPC.RichPresence
            {
                Details = string.Format(loc.Localize("DalamudRichPresenceInLoginQueue", LocalizationLanguage.Client), queue.Position),
                State = eta,
                Assets = new Assets
                {
                    LargeImageKey = DEFAULT_LARGE_IMAGE_KEY,
                    SmallImageKey = DEFAULT_SMALL_IMAGE_KEY,
                },
                Timestamps = timestamps,
            };
        }
    }
}
