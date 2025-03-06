using Dalamud.Data;
using Lumina.Extensions;

using ItemKind = Dalamud.Game.Text.SeStringHandling.Payloads.ItemPayload.ItemKind;
using ItemPayload = Dalamud.Game.Text.SeStringHandling.Payloads.ItemPayload;
using LSheets = Lumina.Excel.Sheets;

namespace Dalamud.Game.Text.Evaluator.Internal;

/// <summary>
/// A service to resolve sheet redirects in expressions.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class SheetRedirectResolver : IServiceType
{
    private static readonly string[] ActStrSheetNames =
    [
        nameof(LSheets.Trait),
        nameof(LSheets.Action),
        nameof(LSheets.Item),
        nameof(LSheets.EventItem),
        nameof(LSheets.EventAction),
        nameof(LSheets.GeneralAction),
        nameof(LSheets.BuddyAction),
        nameof(LSheets.MainCommand),
        nameof(LSheets.Companion),
        nameof(LSheets.CraftAction),
        nameof(LSheets.Action),
        nameof(LSheets.PetAction),
        nameof(LSheets.CompanyAction),
        nameof(LSheets.Mount),
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        nameof(LSheets.BgcArmyAction),
        nameof(LSheets.Ornament),
    ];

    private static readonly string[] ObjStrSheetNames =
    [
        nameof(LSheets.BNpcName),
        nameof(LSheets.ENpcResident),
        nameof(LSheets.Treasure),
        nameof(LSheets.Aetheryte),
        nameof(LSheets.GatheringPointName),
        nameof(LSheets.EObjName),
        nameof(LSheets.Mount),
        nameof(LSheets.Companion),
        string.Empty,
        string.Empty,
        nameof(LSheets.Item),
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
        switch (sheetName)
        {
            // MP means Masterpiece
            case "Item" or "ItemHQ" or "ItemMP":
            {
                var (itemId, kind) = ItemPayload.GetAdjustedId(rowId);
                if (kind == ItemKind.EventItem &&
                    rowId - 2_000_000 < this.dataManager.GetExcelSheet<LSheets.EventItem>().Count)
                {
                    sheetName = nameof(LSheets.EventItem);
                }
                else
                {
                    sheetName = nameof(LSheets.Item);
                    rowId = itemId;
                }

                break;
            }

            case "ActStr":
            {
                (var index, rowId) = uint.DivRem(rowId, 1000000);
                if (index < ActStrSheetNames.Length)
                    sheetName = ActStrSheetNames[index];

                break;
            }

            case "ObjStr":
            {
                (var index, rowId) = uint.DivRem(rowId, 1000000);
                if (index < ObjStrSheetNames.Length)
                    sheetName = ObjStrSheetNames[index];

                switch (index)
                {
                    case 0: // BNpcName
                        if (rowId >= 100000)
                            rowId += 900000;
                        break;

                    case 1: // ENpcResident
                        rowId += 1000000;
                        break;

                    case 2: // Treasure
                        if (this.dataManager.GetExcelSheet<LSheets.Treasure>().TryGetRow(rowId, out var treasureRow) &&
                            treasureRow.Unknown0.IsEmpty)
                            rowId = 0; // defaulting to "Treasure Coffer"
                        break;

                    case 3: // Aetheryte
                        rowId = this.dataManager.GetExcelSheet<LSheets.Aetheryte>()
                                    .TryGetRow(rowId, out var aetheryteRow) && aetheryteRow.IsAetheryte
                                    ? 0u // "Aetheryte"
                                    : 1; // "Aethernet Shard"
                        break;

                    case 5: // EObjName
                        rowId += 2000000;
                        break;
                }

                break;
            }

            case "EObj" when flags is <= 7 or 0xFFFF:
                sheetName = nameof(LSheets.EObjName);
                break;

            case "Treasure"
                when this.dataManager.GetExcelSheet<LSheets.Treasure>().TryGetRow(rowId, out var treasureRow) &&
                     treasureRow.Unknown0.IsEmpty:
                rowId = 0; // defaulting to "Treasure Coffer"
                break;

            case "WeatherPlaceName":
            {
                sheetName = nameof(LSheets.PlaceName);

                var placeNameSubId = rowId;
                if (this.dataManager.GetExcelSheet<LSheets.WeatherReportReplace>().TryGetFirst(
                        r => r.PlaceNameSub.RowId == placeNameSubId,
                        out var row))
                    rowId = row.PlaceNameParent.RowId;
                break;
            }

            case "InstanceContent" when flags == 3:
            {
                sheetName = nameof(LSheets.ContentFinderCondition);

                if (this.dataManager.GetExcelSheet<LSheets.InstanceContent>().TryGetRow(rowId, out var row))
                    rowId = row.Order;
                break;
            }

            case "PartyContent" when flags == 2:
            {
                sheetName = nameof(LSheets.ContentFinderCondition);

                if (this.dataManager.GetExcelSheet<LSheets.PartyContent>().TryGetRow(rowId, out var row))
                    rowId = row.ContentFinderCondition.RowId;
                break;
            }

            case "PublicContent" when flags == 3:
            {
                sheetName = nameof(LSheets.ContentFinderCondition);

                if (this.dataManager.GetExcelSheet<LSheets.PublicContent>().TryGetRow(rowId, out var row))
                    rowId = row.ContentFinderCondition.RowId;
                break;
            }

            case "AkatsukiNote":
            {
                sheetName = nameof(LSheets.AkatsukiNoteString);

                if (this.dataManager.Excel.GetSubrowSheet<LSheets.AkatsukiNote>().TryGetRow(rowId, out var row))
                    rowId = (uint)row[0].Unknown2;
                break;
            }
        }
    }
}
