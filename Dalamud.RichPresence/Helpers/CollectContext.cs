using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.RichPresence.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace Dalamud.RichPresence.Helpers
{
    internal readonly record struct QueueContext(bool IsInQueue, int Position, TimeSpan? Estimate);
    /// <summary>
    /// A record containing a player's online status info
    /// </summary>
    /// <param name="IsAfk">Whether the player is AFK/Idle</param>
    /// <param name="WatchingCutscene">Whether the player is watching a cutscene</param>
    /// <param name="StatusName">The status the player has at the moment</param>
    public readonly record struct OnlineStatusContext(bool IsAfk, bool WatchingCutscene, string StatusName);

    /// <summary>
    /// A record containing a player's party info
    /// </summary>
    /// <param name="InParty">Whether the player is in a party</param>
    /// <param name="InDuty">Whether the player is in a duty</param>
    /// <param name="IsPartyCrossRealm">Whether the player is in a cross-realm party</param>
    /// <param name="PartySize">The current party size</param>
    /// <param name="PartyMaxSize">The max players the party can hold</param>
    /// <param name="HashedPartyId">An SHA-256 ID of the party</param>
    public readonly record struct PartyContext(bool InParty, bool InDuty, bool IsPartyCrossRealm, int PartySize, int PartyMaxSize, string HashedPartyId);

    /// <summary>
    /// A record containing a player's info
    /// </summary>
    /// <param name="PlayerName">The name of the player</param>
    /// <param name="FcTag">The Free Company the player is in</param>
    /// <param name="CurrentWorldId">The ID current world the player is in</param>
    /// <param name="CurrentWorld">The string name of the world the player is in</param>
    /// <param name="HomeWorldId">The ID of the player's home world</param>
    /// <param name="HomeWorld">The string name of the player's home world</param>
    /// <param name="IsOnHomeWorld">Whether the player is in their homeworld</param>
    /// <param name="DataCenterName">The name of the data center the player is in</param>
    /// <param name="TerritoryName">The name of the territory the player is in</param>
    /// <param name="TerritoryLoadingImageId">The loading ID image of the region used for large images</param>
    /// <param name="WardId">The ward ID of the player's current residential area</param>
    /// <param name="ClassJobId">The ID of the player's current class</param>
    /// <param name="ClassJob">The string of the player's current class</param>
    /// <param name="ClassJobAbbreviation">The 3 letter abbreviation of the player's current class</param>
    /// <param name="Level">The player's level</param>
    public readonly record struct PlayerContext(
        string PlayerName, string? FcTag,
        uint CurrentWorldId, string CurrentWorld,
        uint HomeWorldId, string HomeWorld,
        bool IsOnHomeWorld,
        string DataCenterName,
        string TerritoryName, uint TerritoryLoadingImageId, sbyte WardId,
        uint ClassJobId, string ClassJob, string ClassJobAbbreviation, int Level);
    internal class CollectContext(Configuration configuration)
    {
        private readonly Configuration configuration = configuration;

        /// <summary>
        /// Stores the FC name (if any) of a player. Used for when transitioning from overworld to
        /// duty where duties omit FC tags.
        /// </summary>
        private string? CachedFCName;

        public void ClearCache()
        {
            CachedFCName = null;
        }

        public QueueContext GetQueueStatus()
        {
            // Exit if user has disabled queue position or Waitingway states that we're not in a login queue.
            if (!configuration.ShowLoginQueuePosition || !Plugin.WaitingwayIPC.IsInLoginQueue())
                return new QueueContext(false, -1, null);

            var positionInQueue = Plugin.WaitingwayIPC.GetQueuePosition();
            if (positionInQueue == -1)
                // Queue position hasn't loaded yet.
                return new QueueContext(false, -1, null);

            var eta = Plugin.WaitingwayIPC.GetQueueEstimate();
            return new QueueContext(true, positionInQueue, eta);
        }
        public static OnlineStatusContext OnlineStatus
        {
            get
            {
                if (Plugin.ObjectTable.LocalPlayer == null) return new OnlineStatusContext(false, false, string.Empty);

                var localPlayer = Plugin.ObjectTable.LocalPlayer;
                var watchingCutscene = Plugin.Condition[ConditionFlag.WatchingCutscene]
                    || Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent]
                    || Plugin.Condition[ConditionFlag.WatchingCutscene78];

                var onlineStatusRowId = localPlayer.OnlineStatus.Value.RowId;
                var onlineStatusStr = localPlayer.OnlineStatus.Value.Name.ExtractText();

                return new OnlineStatusContext(
                    IsAfk: onlineStatusRowId == 17, // Row 17 has the AFK status in the game
                    WatchingCutscene: watchingCutscene,
                    StatusName: onlineStatusStr
                );
            }
        }

        public unsafe PartyContext GetPartyStatus()
        {
            if (!configuration.ShowPartyData) return new PartyContext(false, false, false, -1, -1, string.Empty);
            if (Plugin.PartyList.Length > 0 && Plugin.PartyList.PartyId != 0)
            {
                var contentFinderConditionTerritory = LuminaService.Instance.GetContentFinderConditionOfClient();

                var maxPartySize = contentFinderConditionTerritory is { ContentType.RowId: 2 } ? 4 : 8;
                if (Plugin.PartyList.Length > maxPartySize)
                    maxPartySize = Plugin.PartyList.Length;

                return new PartyContext(
                    InParty: true,
                    InDuty: contentFinderConditionTerritory != null,
                    IsPartyCrossRealm: false,
                    PartySize: Plugin.PartyList.Length,
                    PartyMaxSize: maxPartySize,
                    HashedPartyId: GetStringSha256Hash(Plugin.PartyList.PartyId.ToString())
                );
            }
            else
            {
                var infoProxyCrossRealm = InfoProxyCrossRealm.Instance();
                if (infoProxyCrossRealm == null || !infoProxyCrossRealm->IsInCrossRealmParty) return new PartyContext(false, false, false, -1, -1, string.Empty);

                var partySize = InfoProxyCrossRealm.GetGroupMemberCount(infoProxyCrossRealm->LocalPlayerGroupIndex);

                if (partySize <= 0)
                    return new PartyContext(false, false, false, -1, -1, string.Empty);

                var memberArray = new CrossRealmMember[partySize];
                for (var i = 0u; i < partySize; i++)
                {
                    memberArray[i] = *InfoProxyCrossRealm.GetGroupMember(i, infoProxyCrossRealm->LocalPlayerGroupIndex);
                }
                var lowestContentId = memberArray.OrderBy(m => m.ContentId).Select(m => m.ContentId).First();

                return new PartyContext(
                    InParty: true,
                    InDuty: false,
                    IsPartyCrossRealm: true,
                    PartySize: partySize,
                    PartyMaxSize: 8,
                    HashedPartyId: GetStringSha256Hash(lowestContentId.ToString())
                );
            }
        }
        public unsafe PlayerContext GetPlayerStatus()
        {
            if (Plugin.ObjectTable.LocalPlayer == null)
                return new PlayerContext(string.Empty, string.Empty, 0, string.Empty, 0, string.Empty, false, string.Empty, string.Empty, 0, -1, 0, string.Empty, string.Empty, -1);

            var localPlayer = Plugin.ObjectTable.LocalPlayer;
            var fcTag = localPlayer.CompanyTag.TextValue;

            // Cache the FC name for use in duties where the game doesn't provide it.
            if ((CachedFCName == null && !fcTag.IsNullOrEmpty()) || (CachedFCName != null && !fcTag.IsNullOrEmpty() && CachedFCName != fcTag))
                CachedFCName = fcTag;

            var territoryId = Plugin.ClientState.TerritoryType;

            var housingManager = HousingManager.Instance();
            if (housingManager != null && housingManager->IsInside())
                territoryId = LuminaService.Instance.GetOriginalTerritoryId(HousingManager.GetOriginalHouseTerritoryTypeId());

            var wardId = housingManager->GetCurrentWard();
            var territoryName = string.Empty;
            var territoryLoadingImageId = (uint)1; // default loading image

            if (territoryId != 0)
            {
                var territory = LuminaService.Instance.GetTerritoryType(territoryId);
                if (territory != null)
                {
                    territoryName = territory.Value.PlaceName.Value.Name.ExtractText();
                    territoryLoadingImageId = territory.Value.LoadingImage.RowId;
                }
                else
                    territoryName = $"Unknown Territory {territoryId}";
            }

            var currentWorldId = localPlayer.CurrentWorld.RowId;
            var currentWorld = localPlayer.CurrentWorld.Value.Name.ExtractText();
            var homeWorldId = localPlayer.HomeWorld.RowId;
            var homeWorld = localPlayer.HomeWorld.Value.Name.ExtractText();
            var dcName = localPlayer.CurrentWorld.Value.DataCenter.Value.Name.ExtractText();
            var classJob = localPlayer.ClassJob.Value.Name.ExtractText();

            return new PlayerContext(
                PlayerName: localPlayer.Name.TextValue,
                FcTag: CachedFCName,
                CurrentWorldId: currentWorldId,
                CurrentWorld: currentWorld,
                HomeWorldId: homeWorldId,
                HomeWorld: homeWorld,
                IsOnHomeWorld: currentWorldId == homeWorldId,
                DataCenterName: dcName,
                TerritoryName: territoryName,
                TerritoryLoadingImageId: territoryLoadingImageId,
                WardId: wardId,
                ClassJobId: localPlayer.ClassJob.RowId,
                ClassJob: string.Concat(classJob[0].ToString().ToUpperInvariant(), classJob.AsSpan(1)),
                ClassJobAbbreviation: localPlayer.ClassJob.Value.Abbreviation.ExtractText(),
                Level: localPlayer.Level
            );
        }
        private static string GetStringSha256Hash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var textData = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(textData);
            return Convert.ToHexStringLower(hash);
        }
    }
}