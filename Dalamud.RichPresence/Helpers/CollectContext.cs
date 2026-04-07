using Dalamud.Game.ClientState.Conditions;
using Dalamud.RichPresence.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Dalamud.RichPresence.Helpers
{
    internal readonly record struct QueueContext(bool IsInQueue, int Position, TimeSpan? Estimate);
    internal readonly record struct OnlineStatusContext(bool IsAFK, bool WatchingCutscene, string StatusName);
    internal readonly record struct PartyContext(bool InParty, bool InDuty, bool IsPartyCrossRealm, int PartySize, int PartyMaxSize, string HashedPartyId);
    internal readonly record struct PlayerContext(
        string PlayerName, string FcTag,
        uint CurrentWorldId, string CurrentWorld,
        uint HomeWorldId, string HomeWorld,
        bool IsOnHomeWorld,
        string DataCenterName,
        string TerritoryName, string TerritoryRegion, uint TerritoryLoadingImageId,
        uint ClassJobId, string ClassJob, string ClassJobAbbreviation, int Level);
    internal class CollectContext(Configuration configuration)
    {
        private readonly Configuration configuration = configuration;

        public QueueContext GetQueueStatus()
        {
            // Exit if user has disabled queue position or Waitingway states that we're not in a login queue.
            if (!configuration.ShowLoginQueuePosition || !Plugin.IPCService.IsInLoginQueue())
                return new QueueContext(false, -1, null);

            var positionInQueue = Plugin.IPCService.GetQueuePosition();
            if (positionInQueue == -1)
                // Queue position hasn't loaded yet.
                return new QueueContext(false, -1, null);

            var eta = Plugin.IPCService.GetQueueEstimate();
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

                var onlineStatusRowId = localPlayer.OnlineStatus.RowId;
                var onlineStatusStr = LuminaService.Instance.GetOnlineStatusName(onlineStatusRowId);

                return new OnlineStatusContext(
                    IsAFK: onlineStatusRowId == 17, // Row 17 has the AFK icon status
                    WatchingCutscene: watchingCutscene,
                    StatusName: onlineStatusStr
                );
            }
        }

        public unsafe PartyContext GetPartyStatus()
        {
            if (!configuration.ShowParty) return new PartyContext(false, false, false, -1, -1, string.Empty);
            if (Plugin.PartyList.Length > 0 && Plugin.PartyList.PartyId != 0)
            {
                var contentFinderConditionTerritory = LuminaService.Instance.GetContentFinderConditionOfClient();

                var maxPartySize = contentFinderConditionTerritory != null && contentFinderConditionTerritory.Value.ContentType.RowId == 2 ? 4 : 8;
                if (Plugin.PartyList.Length > maxPartySize)
                    maxPartySize = Plugin.PartyList.Length;

                return new PartyContext(
                    InParty: true,
                    InDuty: contentFinderConditionTerritory != null,
                    IsPartyCrossRealm: false,
                    PartySize: Plugin.PartyList.Length,
                    PartyMaxSize: maxPartySize,
                    HashedPartyId: GetStringSHA256Hash(Plugin.PartyList.PartyId.ToString())
                );
            }
            else
            {
                var infoProxyCrossRealm = InfoProxyCrossRealm.Instance();
                if (infoProxyCrossRealm == null) return new PartyContext(false, false, false, -1, -1, string.Empty);

                if (infoProxyCrossRealm->IsInCrossRealmParty)
                {
                    var partySize = InfoProxyCrossRealm.GetGroupMemberCount(infoProxyCrossRealm->LocalPlayerGroupIndex);

                    if (partySize > 0)
                    {
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
                            HashedPartyId: GetStringSHA256Hash(lowestContentId.ToString())
                        );
                    }
                }
            }
            return new PartyContext(false, false, false, -1, -1, string.Empty);
        }
        public unsafe PlayerContext GetPlayerStatus()
        {
            if (Plugin.ObjectTable.LocalPlayer == null)
                return new PlayerContext(string.Empty, string.Empty, 0, string.Empty, 0, string.Empty, false, string.Empty, string.Empty, string.Empty, 0, 0, string.Empty, string.Empty, -1);

            var localPlayer = Plugin.ObjectTable.LocalPlayer;
            uint territoryId = Plugin.ClientState.TerritoryType;

            var housingManager = HousingManager.Instance();
            if (housingManager != null && housingManager->IsInside())
            {
                territoryId = LuminaService.Instance.GetOriginalTerritoryId(HousingManager.GetOriginalHouseTerritoryTypeId());
            }

            string territoryName = string.Empty;
            string territoryRegion = string.Empty;
            uint territoryLoadingImageId = 1; // default loading image

            if (territoryId != 0)
            {
                var territory = LuminaService.Instance.GetTerritoryType(territoryId);
                if (territory != null)
                {
                    territoryName = territory.Value.PlaceName.Value.Name.ExtractText() ?? $"Unknown Territory {territoryId}";
                    territoryRegion = territory.Value.PlaceNameRegion.Value.Name.ExtractText() ?? "Unknown Region";
                    territoryLoadingImageId = territory.Value.LoadingImage.RowId;
                }
                else
                {
                    territoryName = $"Unknown Territory {territoryId}";
                    territoryRegion = "Unknown Region";
                }
            }

            var currentWorldId = localPlayer.CurrentWorld.RowId;
            var currentWorld = localPlayer.CurrentWorld.Value.Name.ExtractText();
            var homeWorldId = localPlayer.HomeWorld.RowId;
            var homeWorld = localPlayer.HomeWorld.Value.Name.ExtractText();
            var dcName = localPlayer.CurrentWorld.Value.DataCenter.Value.Name.ExtractText();

            return new PlayerContext(
                PlayerName: localPlayer.Name.TextValue,
                FcTag: localPlayer.CompanyTag.TextValue,
                CurrentWorldId: currentWorldId,
                CurrentWorld: currentWorld,
                HomeWorldId: homeWorldId,
                HomeWorld: homeWorld,
                IsOnHomeWorld: currentWorldId == homeWorldId,
                DataCenterName: dcName,
                TerritoryName: territoryName,
                TerritoryRegion: territoryRegion,
                TerritoryLoadingImageId: territoryLoadingImageId,
                ClassJobId: localPlayer.ClassJob.RowId,
                ClassJob: localPlayer.ClassJob.Value.Name.ExtractText(),
                ClassJobAbbreviation: localPlayer.ClassJob.Value.Abbreviation.ExtractText(),
                Level: localPlayer.Level
            );
        }
        private static string GetStringSHA256Hash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var textData = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(textData);
            return Convert.ToHexStringLower(hash);
        }
    }
}