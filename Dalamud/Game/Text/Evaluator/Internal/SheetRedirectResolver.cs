using Dalamud.Data;
using Dalamud.Utility;

using Lumina.Extensions;

using LSheets = Lumina.Excel.Sheets;

namespace Dalamud.Game.Text.Evaluator.Internal;

/// <summary>
/// A service to resolve sheet redirects in expressions.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class SheetRedirectResolver : IServiceType
{
    private static readonly (string SheetName, uint ColumnIndex, bool ReturnActionSheetFlag)[] ActStrSheets =
    [
        (nameof(LSheets.Trait), 0, false),
        (nameof(LSheets.Action), 0, true),
        (nameof(LSheets.Item), 0, false),
        (nameof(LSheets.EventItem), 0, false),
        (nameof(LSheets.EventAction), 0, false),
        (nameof(LSheets.GeneralAction), 0, false),
        (nameof(LSheets.BuddyAction), 0, false),
        (nameof(LSheets.MainCommand), 5, false),
        (nameof(LSheets.Companion), 0, false),
        (nameof(LSheets.CraftAction), 0, false),
        (nameof(LSheets.Action), 0, true),
        (nameof(LSheets.PetAction), 0, false),
        (nameof(LSheets.CompanyAction), 0, false),
        (nameof(LSheets.Mount), 0, false),
        (string.Empty, 0, false),
        (string.Empty, 0, false),
        (string.Empty, 0, false),
        (string.Empty, 0, false),
        (string.Empty, 0, false),
        (nameof(LSheets.BgcArmyAction), 1, false),
        (nameof(LSheets.Ornament), 8, false),
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
    /// <param name="colIndex">The column index. Use <c>ushort.MaxValue</c> as default.</param>
    /// <returns>Flags giving additional information about the redirect.</returns>
    internal SheetRedirectFlags Resolve(ref string sheetName, ref uint rowId, ref uint colIndex)
    {
        var flags = SheetRedirectFlags.None;

        switch (sheetName)
        {
            case nameof(LSheets.Item) or "ItemHQ" or "ItemMP":
            {
                flags |= SheetRedirectFlags.Item;

                var (itemId, kind) = ItemUtil.GetBaseId(rowId);

                if (kind == ItemKind.Hq || sheetName == "ItemHQ")
                {
                    flags |= SheetRedirectFlags.HighQuality;
                }
                else if (kind == ItemKind.Collectible || sheetName == "ItemMP")
                {
                    // MP for Masterpiece?!
                    flags |= SheetRedirectFlags.Collectible;
                }

                if (kind == ItemKind.EventItem &&
                    rowId - 2_000_000 <= this.dataManager.GetExcelSheet<LSheets.EventItem>().Count)
                {
                    flags |= SheetRedirectFlags.EventItem;
                    sheetName = nameof(LSheets.EventItem);
                }
                else
                {
                    sheetName = nameof(LSheets.Item);
                    rowId = itemId;
                }

                if (colIndex is >= 4 and <= 7)
                    return SheetRedirectFlags.None;

                break;
            }

            case "ActStr":
            {
                var returnActionSheetFlag = false;
                (var index, rowId) = uint.DivRem(rowId, 1000000);
                if (index < ActStrSheets.Length)
                    (sheetName, colIndex, returnActionSheetFlag) = ActStrSheets[index];

                if (sheetName != nameof(LSheets.Companion) && colIndex != 13)
                    flags |= SheetRedirectFlags.Action;

                if (returnActionSheetFlag)
                    flags |= SheetRedirectFlags.ActionSheet;

                break;
            }

            case "ObjStr":
            {
                (var index, rowId) = uint.DivRem(rowId, 1000000);
                if (index < ObjStrSheetNames.Length)
                    sheetName = ObjStrSheetNames[index];

                colIndex = 0;

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

            case nameof(LSheets.EObj) when colIndex is <= 7 or ushort.MaxValue:
                sheetName = nameof(LSheets.EObjName);
                break;

            case nameof(LSheets.Treasure)
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

            case nameof(LSheets.InstanceContent) when colIndex == 3:
            {
                sheetName = nameof(LSheets.ContentFinderCondition);
                colIndex = 43;

                if (this.dataManager.GetExcelSheet<LSheets.InstanceContent>().TryGetRow(rowId, out var row))
                    rowId = row.ContentFinderCondition.RowId;
                break;
            }

            case nameof(LSheets.PartyContent) when colIndex == 2:
            {
                sheetName = nameof(LSheets.ContentFinderCondition);
                colIndex = 43;

                if (this.dataManager.GetExcelSheet<LSheets.PartyContent>().TryGetRow(rowId, out var row))
                    rowId = row.ContentFinderCondition.RowId;
                break;
            }

            case nameof(LSheets.PublicContent) when colIndex == 3:
            {
                sheetName = nameof(LSheets.ContentFinderCondition);
                colIndex = 43;

                if (this.dataManager.GetExcelSheet<LSheets.PublicContent>().TryGetRow(rowId, out var row))
                    rowId = row.ContentFinderCondition.RowId;
                break;
            }

            case nameof(LSheets.AkatsukiNote):
            {
                sheetName = nameof(LSheets.AkatsukiNoteString);
                colIndex = 0;

                if (this.dataManager.Excel.GetSubrowSheet<LSheets.AkatsukiNote>().TryGetSubrow(rowId, 0, out var row))
                    rowId = row.ListName.RowId;
                break;
            }
        }

        return flags;
    }
}
