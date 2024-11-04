using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static Dalamud.Interface.ColorHelpers;
using static Dalamud.Utility.Util;

using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <summary>
/// A tree for an <see cref="AtkNineGridNode"/> that can be printed and browsed via ImGui.
/// </summary>
internal unsafe partial class NineGridNodeTree : ImageNodeTree
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NineGridNodeTree"/> class.
    /// </summary>
    /// <param name="node">The node to create a tree for.</param>
    /// <param name="addonTree">The tree representing the containing addon.</param>
    internal NineGridNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    /// <inheritdoc/>
    private protected override uint PartId => this.NgNode->PartId;

    /// <inheritdoc/>
    private protected override AtkUldPartsList* PartsList => this.NgNode->PartsList;

    private AtkNineGridNode* NgNode => (AtkNineGridNode*)this.Node;

    private NineGridOffsets Offsets => new(this.NgNode);

    /// <inheritdoc/>
    private protected override void DrawPartOutline(uint partId, Vector2 cursorScreenPos, Vector2 cursorLocalPos, Vector4 col, bool reqHover = false)
    {
        var part = this.TexData.PartsList->Parts[partId];

        var hrFactor = this.TexData.HiRes ? 2f : 1f;
        var uv = new Vector2(part.U, part.V) * hrFactor;
        var wh = new Vector2(part.Width, part.Height) * hrFactor;
        var partBegin = cursorScreenPos + uv;
        var partEnd = cursorScreenPos + uv + wh;

        var savePos = ImGui.GetCursorPos();

        if (!reqHover || ImGui.IsMouseHoveringRect(partBegin, partEnd))
        {
            var adjustedOffsets = this.Offsets * hrFactor;
            var ngBegin1 = partBegin with { X = partBegin.X + adjustedOffsets.Left };
            var ngEnd1 = partEnd with { X = partEnd.X - adjustedOffsets.Right };

            var ngBegin2 = partBegin with { Y = partBegin.Y + adjustedOffsets.Top };
            var ngEnd2 = partEnd with { Y = partEnd.Y - adjustedOffsets.Bottom };

            var ngCol = RgbaVector4ToUint(col with { W = 0.75f * col.W });

            ImGui.GetWindowDrawList()
                 .AddRect(partBegin, partEnd, RgbaVector4ToUint(col));
            ImGui.GetWindowDrawList().AddRect(ngBegin1, ngEnd1, ngCol);
            ImGui.GetWindowDrawList().AddRect(ngBegin2, ngEnd2, ngCol);

            ImGui.SetCursorPos(cursorLocalPos + uv + new Vector2(0, -20));
            ImGui.TextColored(col, $"[#{partId}]\t{part.U}, {part.V}\t{part.Width}x{part.Height}");
        }

        ImGui.SetCursorPos(savePos);
    }

    /// <inheritdoc/>
    private protected override void PrintNodeObject() => ShowStruct(this.NgNode);

    /// <inheritdoc/>
    private protected override void PrintFieldsForNodeType(bool isEditorOpen = false)
    {
        if (!isEditorOpen)
        {
            ImGui.Text("NineGrid Offsets:\t");
            ImGui.SameLine();
            this.Offsets.Print();
        }

        this.DrawTextureAndParts();
    }
}
