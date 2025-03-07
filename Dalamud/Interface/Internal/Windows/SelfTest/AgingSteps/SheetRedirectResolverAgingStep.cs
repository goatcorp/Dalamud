using System.Runtime.InteropServices;

using Dalamud.Game;
using Dalamud.Game.Text.Evaluator.Internal;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for SheetRedirectResolver.
/// </summary>
internal class SheetRedirectResolverAgingStep : IAgingStep
{
    private int step = 0;
    private RedirectEntry[] redirects =
    [
        new("Item", 10),
        new("ItemHQ", 10),
        new("ItemMP", 10),
        new("Item", 35588),
        new("Item", 1035588),
        new("Item", 2000217),
        new("ActStr", 10),       // Trait
        new("ActStr", 1000010),  // Action
        new("ActStr", 2000010),  // Item
        new("ActStr", 3000010),  // EventItem
        new("ActStr", 4000010),  // EventAction
        new("ActStr", 5000010),  // GeneralAction
        new("ActStr", 6000010),  // BuddyAction
        new("ActStr", 7000010),  // MainCommand
        new("ActStr", 8000010),  // Companion
        new("ActStr", 9000010),  // CraftAction
        new("ActStr", 10000010), // Action
        new("ActStr", 11000010), // PetAction
        new("ActStr", 12000010), // CompanyAction
        new("ActStr", 13000010), // Mount
        // new("ActStr", 14000010),
        // new("ActStr", 15000010),
        // new("ActStr", 16000010),
        // new("ActStr", 17000010),
        // new("ActStr", 18000010),
        new("ActStr", 19000010), // BgcArmyAction
        new("ActStr", 20000010), // Ornament
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

    private unsafe delegate uint ResolveSheetRedirect(RaptureTextModule* thisPtr, Utf8String* sheetName, uint* rowId, ushort* flags);

    /// <inheritdoc/>
    public string Name => "Test SheetRedirectResolver";

    /// <inheritdoc/>
    public unsafe SelfTestStepResult RunStep()
    {
        var sigScanner = Service<TargetSigScanner>.Get();
        var sheetRedirectResolver = Service<SheetRedirectResolver>.Get();

        if (!sigScanner.TryScanText("E8 ?? ?? ?? ?? 44 8B E8 A8 10", out var addr))
            return SelfTestStepResult.Fail;

        var resolveSheetRedirect = Marshal.GetDelegateForFunctionPointer<ResolveSheetRedirect>(addr);

        var utf8SheetName = Utf8String.CreateEmpty();

        var i = 0;
        try
        {
            foreach (var redirect in this.redirects)
            {
                utf8SheetName->SetString(redirect.SheetName);

                var rowId1 = redirect.RowId;
                ushort flags = 0xFFFF;
                resolveSheetRedirect(RaptureTextModule.Instance(), utf8SheetName, &rowId1, &flags);

                var sheetName2 = redirect.SheetName;
                var rowId2 = redirect.RowId;
                sheetRedirectResolver.Resolve(ref sheetName2, ref rowId2);

                if (utf8SheetName->ToString() != sheetName2 || rowId1 != rowId2)
                {
                    ImGui.TextUnformatted($"Mismatch detected (Test #{i}):");
                    ImGui.TextUnformatted($"Input: {redirect.SheetName}#{redirect.RowId}");
                    ImGui.TextUnformatted($"Game: {utf8SheetName->ToString()}#{rowId1}");
                    ImGui.TextUnformatted($"Evaluated: {sheetName2}#{rowId2}");

                    if (ImGui.Button("Continue"))
                        return SelfTestStepResult.Fail;

                    return SelfTestStepResult.Waiting;
                }

                i++;
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

    private record struct RedirectEntry(string SheetName, uint RowId);
}
