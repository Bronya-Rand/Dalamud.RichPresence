using System;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using Lumina.Text.ReadOnly;

namespace Dalamud.RichPresence.Services
{
    internal class LuminaService : IDisposable
    {
        public static LuminaService Instance { get; private set; } = null!;
        private IDataManager DataManager { get; }
        
        private readonly ExcelSheet<TerritoryType> territoryTypes;
        private readonly ExcelSheet<ContentFinderCondition> contentFinderConditions;
        private readonly ExcelSheet<WorldDCGroupType> worldDc;

        public LuminaService(IDataManager dataManager)
        {
            Instance = this;

            DataManager = dataManager;
            territoryTypes = DataManager.GetExcelSheet<TerritoryType>();
            contentFinderConditions = DataManager.GetExcelSheet<ContentFinderCondition>();
            worldDc = DataManager.GetExcelSheet<WorldDCGroupType>();
        }

        public TerritoryType? GetTerritoryType(uint territoryId)
        {
            if (territoryTypes.TryGetRow(territoryId, out var row))
                return row;
            return null;
        }

        public uint GetOriginalTerritoryId(uint territoryId) => territoryTypes.GetRow(territoryId).RowId;
        
        // TODO: Use CurrentWorld for this once added
        public ReadOnlySeString GetLogicalDcTerritoryName(byte logicalDataCenterByte) =>
            worldDc.GetRow(logicalDataCenterByte).Name;

        public ContentFinderCondition? GetContentFinderConditionOfClient() =>
            contentFinderConditions.FirstOrNull(c => c.TerritoryType.RowId == Plugin.ClientState.TerritoryType);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}