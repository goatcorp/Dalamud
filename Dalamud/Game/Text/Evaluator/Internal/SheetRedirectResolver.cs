using Dalamud.Data;

using Lumina.Excel.Sheets;
using Lumina.Extensions;

namespace Dalamud.Game.Text.Evaluator.Internal;

/// <summary>
/// A service to resolve sheet redirects in expressions.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class SheetRedirectResolver : IServiceType
{
    private static readonly string[] ActStrSheetNames = [
        "Trait",
        "Action",
        "Item",
        "EventItem",
        "EventAction",
        "GeneralAction",
        "BuddyAction",
        "MainCommand",
        "Companion",
        "CraftAction",
        "Action",
        "PetAction",
        "CompanyAction",
        "Mount",
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        "BgcArmyAction",
        "Ornament",
    ];

    private static readonly string[] ObjStrSheetNames = [
        "BNpcName",
        "ENpcResident",
        "Treasure",
        "Aetheryte",
        "GatheringPointName",
        "EObjName",
        "Mount",
        "Companion",
        string.Empty,
        string.Empty,
        "Item",
    ];

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceConstructor]
    private SheetRedirectResolver()
    {
    }

    /// <summary>
    /// Resolves the sheet redirect, if any is present.
    /// </summary>
    /// <param name="sheetName">The sheet name.</param>
    /// <param name="rowId">The row id.</param>
    /// <param name="flags">Optional flags (currently unknown).</param>
    internal void Resolve(ref string sheetName, ref uint rowId, ushort flags = 0xFFFF)
    {
        if (sheetName is "Item" or "ItemHQ" or "ItemMP") // MP means Masterpiece
        {
            if (rowId is > 500_000 and < 1_000_000) // Collectible
            {
                sheetName = "Item";
                rowId -= 500_000;
            }
            else if (rowId - 2_000_000 < this.dataManager.GetExcelSheet<EventItem>().Count) // EventItem
            {
                sheetName = "EventItem";
            }
            else if (rowId >= 1_000_000) // HighQuality
            {
                rowId -= 1_000_000;
            }
            else
            {
                sheetName = "Item";
            }
        }
        else if (sheetName == "ActStr")
        {
            var index = rowId / 1000000;

            if (index >= 0 && index < ActStrSheetNames.Length)
                sheetName = ActStrSheetNames[index];

            rowId %= 1000000;
        }
        else if (sheetName == "ObjStr")
        {
            var index = rowId / 1000000;

            if (index >= 0 && index < ObjStrSheetNames.Length)
                sheetName = ObjStrSheetNames[index];

            rowId %= 1000000;

            if (index == 0) // BNpcName
            {
                if (rowId >= 100000)
                    rowId += 900000;
            }
            else if (index == 1) // ENpcResident
            {
                rowId += 1000000;
            }
            else if (index == 2) // Treasure
            {
                if (this.dataManager.GetExcelSheet<Treasure>().TryGetRow(rowId, out var treasureRow) && treasureRow.Unknown0.IsEmpty)
                    rowId = 0; // defaulting to "Treasure Coffer"
            }
            else if (index == 3) // Aetheryte
            {
                rowId = this.dataManager.GetExcelSheet<Aetheryte>().TryGetRow(rowId, out var aetheryteRow) && aetheryteRow.IsAetheryte
                    ? 0u // "Aetheryte"
                    : 1; // "Aethernet Shard"
            }
            else if (index == 5) // EObjName
            {
                rowId += 2000000;
            }
        }
        else if (sheetName == "EObj" && (flags <= 7 || flags == 0xFFFF))
        {
            sheetName = "EObjName";
        }
        else if (sheetName == "Treasure")
        {
            if (this.dataManager.GetExcelSheet<Treasure>().TryGetRow(rowId, out var treasureRow) && treasureRow.Unknown0.IsEmpty)
                rowId = 0; // defaulting to "Treasure Coffer"
        }
        else if (sheetName == "WeatherPlaceName")
        {
            sheetName = "PlaceName";

            var placeNameSubId = rowId;
            if (this.dataManager.GetExcelSheet<WeatherReportReplace>().TryGetFirst(row => row.PlaceNameSub.RowId == placeNameSubId, out var row))
                rowId = row.PlaceNameParent.RowId;
        }
        else if (sheetName == "InstanceContent" && flags == 3)
        {
            sheetName = "ContentFinderCondition";

            if (this.dataManager.GetExcelSheet<InstanceContent>().TryGetRow(rowId, out var row))
                rowId = row.Order;
        }
        else if (sheetName == "PartyContent" && flags == 2)
        {
            sheetName = "ContentFinderCondition";

            if (this.dataManager.GetExcelSheet<PartyContent>().TryGetRow(rowId, out var row))
                rowId = row.ContentFinderCondition.RowId;
        }
        else if (sheetName == "PublicContent" && flags == 3)
        {
            sheetName = "ContentFinderCondition";

            if (this.dataManager.GetExcelSheet<PublicContent>().TryGetRow(rowId, out var row))
                rowId = row.ContentFinderCondition.RowId;
        }
        else if (sheetName == "AkatsukiNote")
        {
            sheetName = "AkatsukiNoteString";

            if (this.dataManager.Excel.GetSubrowSheet<AkatsukiNote>().TryGetRow(rowId, out var row))
                rowId = (uint)row[0].Unknown2;
        }
    }
}
