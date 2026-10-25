using Dalamud.Data;
using Dalamud.Excel.Sheets;
using Dalamud.Utility;

using Lumina.Extensions;

using ActionSheet = Dalamud.Excel.Sheets.Action;

namespace Dalamud.Game.Text.Evaluator.Internal;

/// <summary>
/// A service to resolve sheet redirects in expressions.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class SheetRedirectResolver : IServiceType
{
    private static readonly (string SheetName, uint ColumnIndex, bool ReturnActionSheetFlag)[] ActStrSheets =
    [
        (nameof(Trait), 0, false),
        (nameof(ActionSheet), 0, true),
        (nameof(Item), 0, false),
        (nameof(EventItem), 0, false),
        (nameof(EventAction), 0, false),
        (nameof(GeneralAction), 0, false),
        (nameof(BuddyAction), 0, false),
        (nameof(MainCommand), 5, false),
        (nameof(Companion), 0, false),
        (nameof(CraftAction), 0, false),
        (nameof(ActionSheet), 0, true),
        (nameof(PetAction), 0, false),
        (nameof(CompanyAction), 0, false),
        (nameof(Mount), 0, false),
        (string.Empty, 0, false),
        (string.Empty, 0, false),
        (string.Empty, 0, false),
        (string.Empty, 0, false),
        (string.Empty, 0, false),
        (nameof(BgcArmyAction), 1, false),
        (nameof(Ornament), 8, false),
    ];

    private static readonly string[] ObjStrSheetNames =
    [
        nameof(BNpcName),
        nameof(ENpcResident),
        nameof(Treasure),
        nameof(Aetheryte),
        nameof(GatheringPointName),
        nameof(EObjName),
        nameof(Mount),
        nameof(Companion),
        string.Empty,
        string.Empty,
        nameof(Item),
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
            case nameof(Item) or "ItemHQ" or "ItemMP":
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
                    rowId - 2_000_000 <= this.dataManager.GetExcelSheet<EventItem>().Count)
                {
                    flags |= SheetRedirectFlags.EventItem;
                    sheetName = nameof(EventItem);
                }
                else
                {
                    sheetName = nameof(Item);
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

                if (sheetName != nameof(Companion) && colIndex != 13)
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
                        if (this.dataManager.GetExcelSheet<Treasure>().TryGetRow(rowId, out var treasureRow) &&
                            treasureRow.Singular.IsEmpty)
                            rowId = 0; // defaulting to "Treasure Coffer"
                        break;

                    case 3: // Aetheryte
                        rowId = this.dataManager.GetExcelSheet<Aetheryte>()
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

            case nameof(EObj) when colIndex is <= 7 or ushort.MaxValue:
                sheetName = nameof(EObjName);
                break;

            case nameof(Treasure)
                when this.dataManager.GetExcelSheet<Treasure>().TryGetRow(rowId, out var treasureRow) &&
                     treasureRow.Singular.IsEmpty:
                rowId = 0; // defaulting to "Treasure Coffer"
                break;

            case "WeatherPlaceName":
            {
                sheetName = nameof(PlaceName);

                var placeNameSubId = rowId;
                if (this.dataManager.GetExcelSheet<WeatherReportReplace>().TryGetFirst(
                        r => r.PlaceNameSub.RowId == placeNameSubId,
                        out var row))
                    rowId = row.PlaceNameParent.RowId;
                break;
            }

            case nameof(InstanceContent) when colIndex == 3:
            {
                sheetName = nameof(ContentFinderCondition);
                colIndex = 43;

                if (this.dataManager.GetExcelSheet<InstanceContent>().TryGetRow(rowId, out var row))
                    rowId = row.ContentFinderCondition.RowId;
                break;
            }

            case nameof(PartyContent) when colIndex == 2:
            {
                sheetName = nameof(ContentFinderCondition);
                colIndex = 43;

                if (this.dataManager.GetExcelSheet<PartyContent>().TryGetRow(rowId, out var row))
                    rowId = row.ContentFinderCondition.RowId;
                break;
            }

            case nameof(PublicContent) when colIndex == 3:
            {
                sheetName = nameof(ContentFinderCondition);
                colIndex = 43;

                if (this.dataManager.GetExcelSheet<PublicContent>().TryGetRow(rowId, out var row))
                    rowId = row.ContentFinderCondition.RowId;
                break;
            }

            case nameof(AkatsukiNote):
            {
                sheetName = nameof(AkatsukiNoteString);
                colIndex = 0;

                if (this.dataManager.Excel.GetSubrowSheet<AkatsukiNote>().TryGetSubrow(rowId, 0, out var row))
                    rowId = row.ListName.RowId;
                break;
            }
        }

        return flags;
    }
}
