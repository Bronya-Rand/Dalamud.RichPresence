using System;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;

namespace Dalamud.RichPresence.Services
{
    internal class LuminaService : IDisposable
    {
        public static LuminaService Instance { get; private set; } = null!;
        private IDataManager DataManager { get; }

        private readonly ExcelSheet<World> worlds;
        private readonly ExcelSheet<TerritoryType> territoryTypes;
        private readonly ExcelSheet<ContentFinderCondition> contentFinderConditions;

        public LuminaService(IDataManager dataManager)
        {
            Instance = this;

            DataManager = dataManager;
            // TODO: Add settings to control language of these sheets
            worlds = DataManager.GetExcelSheet<World>();
            territoryTypes = DataManager.GetExcelSheet<TerritoryType>();
            contentFinderConditions = DataManager.GetExcelSheet<ContentFinderCondition>();
        }

        public TerritoryType? GetTerritoryType(uint territoryId)
        {
            if (territoryTypes.TryGetRow(territoryId, out var row))
                return row;
            return null;
        }

        public uint GetOriginalTerritoryId(uint territoryId) => territoryTypes.GetRow(territoryId).RowId;

        public ContentFinderCondition? GetContentFinderConditionOfClient() =>
            contentFinderConditions.FirstOrNull(c => c.TerritoryType.RowId == Plugin.ClientState.TerritoryType);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}