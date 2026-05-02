using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using System;

namespace Dalamud.RichPresence.Services
{
    internal class LuminaService : IDisposable
    {
        public static LuminaService Instance { get; private set; } = null!;
        private IDataManager DataManager { get; set;  }

        private readonly ExcelSheet<World> worlds;
        private readonly ExcelSheet<TerritoryType> territoryTypes;
        private readonly ExcelSheet<OnlineStatus> onlineStatus;
        private readonly ExcelSheet<ContentFinderCondition> contentFinderConditions;

        public LuminaService(IDataManager dataManager)
        {
            Instance = this;

            DataManager = dataManager;
            // TODO: Add settings to control language of these sheets
            worlds = DataManager.GetExcelSheet<World>();
            territoryTypes = DataManager.GetExcelSheet<TerritoryType>();
            onlineStatus = DataManager.GetExcelSheet<OnlineStatus>();
            contentFinderConditions = DataManager.GetExcelSheet<ContentFinderCondition>();
        }
        public string GetWorldName(uint worldId) => 
            worlds.TryGetRow(worldId, out var row) ? row.Name.ExtractText() : $"Unknown World ({worldId})";
        public string GetTerritoryName(uint territoryId)
        {
            if (territoryTypes.TryGetRow(territoryId, out var row))
            {
                var placeNameRow = row.PlaceName.ValueNullable;
                if (placeNameRow.HasValue)
                {
                    var placeName = placeNameRow.Value.Name.ExtractText();
                    if (!string.IsNullOrEmpty(placeName))
                        return placeName;
                }
            }
            return $"Unknown Territory ({territoryId})";
        }
        public TerritoryType? GetTerritoryType(uint territoryId)
        {
            if (territoryTypes.TryGetRow(territoryId, out var row))
                return row;
            return null;
        }
        public uint GetOriginalTerritoryId(uint territoryId) => territoryTypes.GetRow(territoryId).RowId;
        public string GetOnlineStatusName(uint statusId) => onlineStatus.TryGetRow(statusId, out var row)
            ? row.Name.ExtractText()
            : $"Unknown Status ({statusId})";
        public ContentFinderCondition? GetContentFinderConditionOfClient() => contentFinderConditions.FirstOrNull(c => c.TerritoryType.RowId == Plugin.ClientState.TerritoryType);
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
