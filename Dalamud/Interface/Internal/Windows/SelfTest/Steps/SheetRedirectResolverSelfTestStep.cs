using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Game.Text.Evaluator.Internal;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for SheetRedirectResolver.
/// </summary>
internal class SheetRedirectResolverSelfTestStep : ISelfTestStep
{
    private RedirectEntry[] redirects =
    [
        new("Item", 10, SheetRedirectFlags.Item),
        new("ItemHQ", 10, SheetRedirectFlags.Item | SheetRedirectFlags.HighQuality),
        new("ItemMP", 10, SheetRedirectFlags.Item | SheetRedirectFlags.Collectible),
        new("Item", 35588, SheetRedirectFlags.Item),
        new("Item", 1035588, SheetRedirectFlags.Item | SheetRedirectFlags.HighQuality),
        new("Item", 2000217, SheetRedirectFlags.Item | SheetRedirectFlags.EventItem),
        new("ActStr", 10, SheetRedirectFlags.Action),       // Trait
        new("ActStr", 1000010, SheetRedirectFlags.Action | SheetRedirectFlags.ActionSheet),  // Action
        new("ActStr", 2000010, SheetRedirectFlags.Action),  // Item
        new("ActStr", 3000010, SheetRedirectFlags.Action),  // EventItem
        new("ActStr", 4000010, SheetRedirectFlags.Action),  // EventAction
        new("ActStr", 5000010, SheetRedirectFlags.Action),  // GeneralAction
        new("ActStr", 6000010, SheetRedirectFlags.Action),  // BuddyAction
        new("ActStr", 7000010, SheetRedirectFlags.Action),  // MainCommand
        new("ActStr", 8000010, SheetRedirectFlags.Action),  // Companion
        new("ActStr", 9000010, SheetRedirectFlags.Action),  // CraftAction
        new("ActStr", 10000010, SheetRedirectFlags.Action | SheetRedirectFlags.ActionSheet), // Action
        new("ActStr", 11000010, SheetRedirectFlags.Action), // PetAction
        new("ActStr", 12000010, SheetRedirectFlags.Action), // CompanyAction
        new("ActStr", 13000010, SheetRedirectFlags.Action), // Mount
        // new("ActStr", 14000010, RedirectFlags.Action),
        // new("ActStr", 15000010, RedirectFlags.Action),
        // new("ActStr", 16000010, RedirectFlags.Action),
        // new("ActStr", 17000010, RedirectFlags.Action),
        // new("ActStr", 18000010, RedirectFlags.Action),
        new("ActStr", 19000010, SheetRedirectFlags.Action), // BgcArmyAction
        new("ActStr", 20000010, SheetRedirectFlags.Action), // Ornament
        new("ObjStr", 10),       // BNpcName
        new("ObjStr", 1000010),  // ENpcResident
        new("ObjStr", 2000010),  // Treasure
        new("ObjStr", 3000010),  // Aetheryte
        new("ObjStr", 4000010),  // GatheringPointName
        new("ObjStr", 5000010),  // EObjName
        new("ObjStr", 6000010),  // Mount
        new("ObjStr", 7000010),  // Companion
        // new("ObjStr", 8000010),
        // new("ObjStr", 9000010),
        new("ObjStr", 10000010), // Item
        new("EObj", 2003479), // EObj => EObjName
        new("Treasure", 1473), // Treasure (without name, falls back to rowId 0)
        new("Treasure", 1474), // Treasure (with name)
        new("WeatherPlaceName", 0),
        new("WeatherPlaceName", 28),
        new("WeatherPlaceName", 40),
        new("WeatherPlaceName", 52),
        new("WeatherPlaceName", 2300),
    ];

    private unsafe delegate SheetRedirectFlags ResolveSheetRedirect(RaptureTextModule* thisPtr, Utf8String* sheetName, uint* rowId, uint* flags);

    /// <inheritdoc/>
    public string Name => "Test SheetRedirectResolver";

    /// <inheritdoc/>
    public unsafe SelfTestStepResult RunStep()
    {
        // Client::UI::Misc::RaptureTextModule_ResolveSheetRedirect
        if (!Service<TargetSigScanner>.Get().TryScanText("E8 ?? ?? ?? ?? 44 8B E8 A8 10", out var addr))
            return SelfTestStepResult.Fail;

        var sheetRedirectResolver = Service<SheetRedirectResolver>.Get();
        var resolveSheetRedirect = Marshal.GetDelegateForFunctionPointer<ResolveSheetRedirect>(addr);
        var utf8SheetName = Utf8String.CreateEmpty();

        try
        {
            for (var i = 0; i < this.redirects.Length; i++)
            {
                var redirect = this.redirects[i];

                utf8SheetName->SetString(redirect.SheetName);

                var rowId1 = redirect.RowId;
                uint colIndex1 = ushort.MaxValue;
                var flags1 = resolveSheetRedirect(RaptureTextModule.Instance(), utf8SheetName, &rowId1, &colIndex1);

                var sheetName2 = redirect.SheetName;
                var rowId2 = redirect.RowId;
                uint colIndex2 = ushort.MaxValue;
                var flags2 = sheetRedirectResolver.Resolve(ref sheetName2, ref rowId2, ref colIndex2);

                if (utf8SheetName->ToString() != sheetName2 || rowId1 != rowId2 || colIndex1 != colIndex2 || flags1 != flags2)
                {
                    ImGui.TextUnformatted($"Mismatch detected (Test #{i}):");
                    ImGui.TextUnformatted($"Input: {redirect.SheetName}#{redirect.RowId}");
                    ImGui.TextUnformatted($"Game: {utf8SheetName->ToString()}#{rowId1}-{colIndex1} ({flags1})");
                    ImGui.TextUnformatted($"Evaluated: {sheetName2}#{rowId2}-{colIndex2} ({flags2})");

                    if (ImGui.Button("Continue"))
                        return SelfTestStepResult.Fail;

                    return SelfTestStepResult.Waiting;
                }
            }

            return SelfTestStepResult.Pass;
        }
        finally
        {
            utf8SheetName->Dtor(true);
        }
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }

    private record struct RedirectEntry(string SheetName, uint RowId, SheetRedirectFlags Flags = SheetRedirectFlags.None);
}
