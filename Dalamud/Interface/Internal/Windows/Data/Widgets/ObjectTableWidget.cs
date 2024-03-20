using System.Numerics;

using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget to display the Object Table.
/// </summary>
internal class ObjectTableWidget : IDataWindowWidget
{
    private bool resolveGameData;
    private bool drawCharacters;
    private float maxCharaDrawDistance = 20.0f;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "ot", "objecttable" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Object Table"; 

    /// <inheritdoc/>
    public bool Ready { get; set; }
    
    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }
    
    /// <inheritdoc/>
    public void Draw()
    {
        ImGui.Checkbox("Resolve GameData", ref this.resolveGameData);
        
        var chatGui = Service<ChatGui>.Get();
        var clientState = Service<ClientState>.Get();
        var gameGui = Service<GameGui>.Get();
        var objectTable = Service<ObjectTable>.Get();

        var stateString = string.Empty;

        if (clientState.LocalPlayer == null)
        {
            ImGui.TextUnformatted("LocalPlayer null.");
        }
        else if (clientState.IsPvPExcludingDen)
        {
            ImGui.TextUnformatted("Cannot access object table while in PvP.");
        }
        else
        {
            stateString += $"ObjectTableLen: {objectTable.Length}\n";
            stateString += $"LocalPlayerName: {clientState.LocalPlayer.Name}\n";
            stateString += $"CurrentWorldName: {(this.resolveGameData ? clientState.LocalPlayer.CurrentWorld.GameData?.Name : clientState.LocalPlayer.CurrentWorld.Id.ToString())}\n";
            stateString += $"HomeWorldName: {(this.resolveGameData ? clientState.LocalPlayer.HomeWorld.GameData?.Name : clientState.LocalPlayer.HomeWorld.Id.ToString())}\n";
            stateString += $"LocalCID: {clientState.LocalContentId:X}\n";
            stateString += $"LastLinkedItem: {chatGui.LastLinkedItemId}\n";
            stateString += $"TerritoryType: {clientState.TerritoryType}\n\n";

            ImGui.TextUnformatted(stateString);

            ImGui.Checkbox("Draw characters on screen", ref this.drawCharacters);
            ImGui.SliderFloat("Draw Distance", ref this.maxCharaDrawDistance, 2f, 40f);

            for (var i = 0; i < objectTable.Length; i++)
            {
                var obj = objectTable[i];

                if (obj == null)
                    continue;

                Util.PrintGameObject(obj, i.ToString(), this.resolveGameData);

                if (this.drawCharacters && gameGui.WorldToScreen(obj.Position, out var screenCoords))
                {
                    // So, while WorldToScreen will return false if the point is off of game client screen, to
                    // to avoid performance issues, we have to manually determine if creating a window would
                    // produce a new viewport, and skip rendering it if so
                    var objectText = $"{obj.Address.ToInt64():X}:{obj.ObjectId:X}[{i}] - {obj.ObjectKind} - {obj.Name}";

                    var screenPos = ImGui.GetMainViewport().Pos;
                    var screenSize = ImGui.GetMainViewport().Size;

                    var windowSize = ImGui.CalcTextSize(objectText);

                    // Add some extra safety padding
                    windowSize.X += ImGui.GetStyle().WindowPadding.X + 10;
                    windowSize.Y += ImGui.GetStyle().WindowPadding.Y + 10;

                    if (screenCoords.X + windowSize.X > screenPos.X + screenSize.X ||
                        screenCoords.Y + windowSize.Y > screenPos.Y + screenSize.Y)
                        continue;

                    if (obj.YalmDistanceX > this.maxCharaDrawDistance)
                        continue;

                    ImGui.SetNextWindowPos(new Vector2(screenCoords.X, screenCoords.Y));

                    ImGui.SetNextWindowBgAlpha(Math.Max(1f - (obj.YalmDistanceX / this.maxCharaDrawDistance), 0.2f));
                    if (ImGui.Begin(
                            $"Actor{i}##ActorWindow{i}",
                            ImGuiWindowFlags.NoDecoration |
                            ImGuiWindowFlags.AlwaysAutoResize |
                            ImGuiWindowFlags.NoSavedSettings |
                            ImGuiWindowFlags.NoMove |
                            ImGuiWindowFlags.NoMouseInputs |
                            ImGuiWindowFlags.NoDocking |
                            ImGuiWindowFlags.NoFocusOnAppearing |
                            ImGuiWindowFlags.NoNav))
                        ImGui.Text(objectText);
                    ImGui.End();
                }
            }
        }
    }
}
