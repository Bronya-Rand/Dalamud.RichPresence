using Lumina.Excel.Sheets;
using Lumina.Extensions;
using Lumina.Text.ReadOnly;

namespace Dalamud.RichPresence.Helpers
{
    internal static class LuminaHelper
    {
        public static TerritoryType? GetTerritoryType(uint territoryId)
        {
            var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
            if (sheet.TryGetRow(territoryId, out var row))
                return row;
            return null;
        }

        public static uint GetOriginalTerritoryId(uint territoryId)
        {
            var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
            return sheet.GetRow(territoryId).RowId;
        }
        
        // TODO: Use CurrentWorld for this once added
        public static ReadOnlySeString GetLogicalDcTerritoryName(byte logicalDataCenterByte)
        {
            var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
            return sheet.GetRow(logicalDataCenterByte).Name;
        }

        public static ContentFinderCondition? GetContentFinderConditionOfClient()
        {
            var sheet  = Plugin.DataManager.GetExcelSheet<ContentFinderCondition>();
            return sheet.FirstOrNull(c => c.TerritoryType.RowId == Plugin.ClientState.TerritoryType);
        }
    }
}