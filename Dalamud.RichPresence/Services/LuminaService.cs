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

        private readonly ExcelSheet<World> Worlds;
        private readonly ExcelSheet<TerritoryType> TerritoryTypes;
        public readonly ExcelSheet<PlaceName> PlaceNames;
        public readonly ExcelSheet<ClassJob> ClassJobs;
        private readonly ExcelSheet<OnlineStatus> OnlineStatus;
        private readonly ExcelSheet<ContentFinderCondition> ContentFinderConditions;

        public LuminaService(IDataManager dataManager)
        {
            Instance = this;

            DataManager = dataManager;
            // TODO: Add settings to control language of these sheets
            Worlds = DataManager.GetExcelSheet<World>()!;
            TerritoryTypes = DataManager.GetExcelSheet<TerritoryType>()!;
            PlaceNames = DataManager.GetExcelSheet<PlaceName>()!;
            ClassJobs = DataManager.GetExcelSheet<ClassJob>()!;
            OnlineStatus = DataManager.GetExcelSheet<OnlineStatus>()!;
            ContentFinderConditions = DataManager.GetExcelSheet<ContentFinderCondition>()!;
        }
        public string GetWorldName(uint worldId) => 
            Worlds.TryGetRow(worldId, out var row) ? row.Name.ExtractText() : $"Unknown World ({worldId})";
        public string GetTerritoryName(uint territoryId)
        {
            if (TerritoryTypes.TryGetRow(territoryId, out var row))
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
            if (TerritoryTypes.TryGetRow(territoryId, out var row))
                return row;
            return null;
        }
        public uint GetOriginalTerritoryId(uint territoryId) => TerritoryTypes.GetRow(territoryId).RowId;
        public string GetOnlineStatusName(uint statusId) => OnlineStatus.TryGetRow(statusId, out var row)
            ? row.Name.ExtractText()
            : $"Unknown Status ({statusId})";
        public ContentFinderCondition? GetContentFinderConditionOfClient() => ContentFinderConditions.FirstOrNull(c => c.TerritoryType.RowId == Plugin.ClientState.TerritoryType);
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
