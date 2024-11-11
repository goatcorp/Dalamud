using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.UiDebug2.Utility;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static Dalamud.Interface.ColorHelpers;
using static Dalamud.Interface.FontAwesomeIcon;
using static Dalamud.Interface.Internal.UiDebug2.Utility.Gui;
using static Dalamud.Interface.Utility.ImGuiHelpers;
using static ImGuiNET.ImGuiColorEditFlags;
using static ImGuiNET.ImGuiInputTextFlags;
using static ImGuiNET.ImGuiTableColumnFlags;
using static ImGuiNET.ImGuiTableFlags;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <inheritdoc cref="ResNodeTree"/>
internal unsafe partial class ResNodeTree
{
    /// <summary>
    /// Sets up the table for the node editor, if the "Edit" checkbox is ticked.
    /// </summary>
    private protected void DrawNodeEditorTable()
    {
        using (ImRaii.Table($"###Editor{(nint)this.Node}", 2, SizingStretchProp | NoHostExtendX))
        {
            this.DrawEditorRows();
        }
    }

    /// <summary>
    /// Draws each row in the node editor table.
    /// </summary>
    private protected virtual void DrawEditorRows()
    {
        var pos = new Vector2(this.Node->X, this.Node->Y);
        var size = new Vector2(this.Node->Width, this.Node->Height);
        var scale = new Vector2(this.Node->ScaleX, this.Node->ScaleY);
        var origin = new Vector2(this.Node->OriginX, this.Node->OriginY);
        var angle = (float)((this.Node->Rotation * (180 / Math.PI)) + 360);

        var rgba = RgbaUintToVector4(this.Node->Color.RGBA);
        var mult = new Vector3(this.Node->MultiplyRed, this.Node->MultiplyGreen, this.Node->MultiplyBlue) / 255f;
        var add = new Vector3(this.Node->AddRed, this.Node->AddGreen, this.Node->AddBlue);

        var hov = false;

        ImGui.TableSetupColumn("Labels", WidthFixed);
        ImGui.TableSetupColumn("Editors", WidthFixed);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Position:");

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{(nint)this.Node:X}position", ref pos, 1, default, default, "%.0f"))
        {
            this.Node->X = pos.X;
            this.Node->Y = pos.Y;
            this.Node->DrawFlags |= 0xD;
        }

        hov |= SplitTooltip("X", "Y") || ImGui.IsItemActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Size:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{(nint)this.Node:X}size", ref size, 1, 0, default, "%.0f"))
        {
            this.Node->Width = (ushort)Math.Max(size.X, 0);
            this.Node->Height = (ushort)Math.Max(size.Y, 0);
            this.Node->DrawFlags |= 0xD;
        }

        hov |= SplitTooltip("Width", "Height") || ImGui.IsItemActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Scale:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{(nint)this.Node:X}scale", ref scale, 0.05f))
        {
            this.Node->ScaleX = scale.X;
            this.Node->ScaleY = scale.Y;
            this.Node->DrawFlags |= 0xD;
        }

        hov |= SplitTooltip("ScaleX", "ScaleY") || ImGui.IsItemActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Origin:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{(nint)this.Node:X}origin", ref origin, 1, default, default, "%.0f"))
        {
            this.Node->OriginX = origin.X;
            this.Node->OriginY = origin.Y;
            this.Node->DrawFlags |= 0xD;
        }

        hov |= SplitTooltip("OriginX", "OriginY") || ImGui.IsItemActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Rotation:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        while (angle > 180)
        {
            angle -= 360;
        }

        if (ImGui.DragFloat($"##{(nint)this.Node:X}rotation", ref angle, 0.05f, default, default, "%.2f°"))
        {
            this.Node->Rotation = (float)(angle / (180 / Math.PI));
            this.Node->DrawFlags |= 0xD;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Rotation (deg)");
            hov = true;
        }

        hov |= ImGui.IsItemActive();

        if (hov)
        {
            Vector4 brightYellow = new(1, 1, 0.5f, 0.8f);
            new NodeBounds(this.Node).Draw(brightYellow);
            new NodeBounds(origin, this.Node).Draw(brightYellow);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("RGBA:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.ColorEdit4($"##{(nint)this.Node:X}RGBA", ref rgba, DisplayHex))
        {
            this.Node->Color = new() { RGBA = RgbaVector4ToUint(rgba) };
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Multiply:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.ColorEdit3($"##{(nint)this.Node:X}multiplyRGB", ref mult, DisplayHex))
        {
            this.Node->MultiplyRed = (byte)(mult.X * 255);
            this.Node->MultiplyGreen = (byte)(mult.Y * 255);
            this.Node->MultiplyBlue = (byte)(mult.Z * 255);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Add:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(124);

        if (ImGui.DragFloat3($"##{(nint)this.Node:X}addRGB", ref add, 1, -255, 255, "%.0f"))
        {
            this.Node->AddRed = (short)add.X;
            this.Node->AddGreen = (short)add.Y;
            this.Node->AddBlue = (short)add.Z;
        }

        SplitTooltip("+/- Red", "+/- Green", "+/- Blue");

        var addTransformed = (add / 510f) + new Vector3(0.5f);

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (4 * GlobalScale));
        if (ImGui.ColorEdit3($"##{(nint)this.Node:X}addRGBPicker", ref addTransformed, NoAlpha | NoInputs))
        {
            this.Node->AddRed = (short)Math.Floor((addTransformed.X * 510f) - 255f);
            this.Node->AddGreen = (short)Math.Floor((addTransformed.Y * 510f) - 255f);
            this.Node->AddBlue = (short)Math.Floor((addTransformed.Z * 510f) - 255f);
        }
    }
}

/// <inheritdoc cref="CounterNodeTree"/>
internal unsafe partial class CounterNodeTree
{
    /// <inheritdoc/>
    private protected override void DrawEditorRows()
    {
        base.DrawEditorRows();

        var str = this.CntNode->NodeText.ToString();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Counter:");
        ImGui.TableNextColumn();

        ImGui.SetNextItemWidth(150);
        if (ImGui.InputText($"##{(nint)this.Node:X}counterEdit", ref str, 512, EnterReturnsTrue))
        {
            this.CntNode->SetText(str);
        }
    }
}

/// <inheritdoc cref="ImageNodeTree"/>
internal unsafe partial class ImageNodeTree
{
    private static int TexDisplayStyle { get; set; }

    /// <inheritdoc/>
    private protected override void DrawEditorRows()
    {
        base.DrawEditorRows();

        var partId = (int)this.PartId;
        var partcount = this.ImgNode->PartsList->PartCount;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Part Id:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"##partId{(nint)this.Node:X}", ref partId, 1, 1))
        {
            if (partId < 0)
            {
                partId = 0;
            }

            if (partId >= partcount)
            {
                partId = (int)(partcount - 1);
            }

            this.ImgNode->PartId = (ushort)partId;
        }
    }
}

/// <inheritdoc cref="NineGridNodeTree"/>
internal unsafe partial class NineGridNodeTree
{
    /// <inheritdoc/>
    private protected override void DrawEditorRows()
    {
        base.DrawEditorRows();

        var lr = new Vector2(this.Offsets.Left, this.Offsets.Right);
        var tb = new Vector2(this.Offsets.Top, this.Offsets.Bottom);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Ninegrid Offsets:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{(nint)this.Node:X}ngOffsetLR", ref lr, 1, 0))
        {
            this.NgNode->LeftOffset = (short)Math.Max(0, lr.X);
            this.NgNode->RightOffset = (short)Math.Max(0, lr.Y);
        }

        SplitTooltip("Left", "Right");

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{(nint)this.Node:X}ngOffsetTB", ref tb, 1, 0))
        {
            this.NgNode->TopOffset = (short)Math.Max(0, tb.X);
            this.NgNode->BottomOffset = (short)Math.Max(0, tb.Y);
        }

        SplitTooltip("Top", "Bottom");
    }
}

/// <inheritdoc cref="TextNodeTree"/>
internal unsafe partial class TextNodeTree
{
    private static readonly List<FontType> FontList = [.. Enum.GetValues<FontType>()];

    private static readonly string[] FontNames = Enum.GetNames<FontType>();

    /// <inheritdoc/>
    private protected override void DrawEditorRows()
    {
        base.DrawEditorRows();

        var text = this.TxtNode->NodeText.ToString();
        var fontIndex = FontList.IndexOf(this.TxtNode->FontType);
        int fontSize = this.TxtNode->FontSize;
        var alignment = this.TxtNode->AlignmentType;
        var textColor = RgbaUintToVector4(this.TxtNode->TextColor.RGBA);
        var edgeColor = RgbaUintToVector4(this.TxtNode->EdgeColor.RGBA);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Text:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(Math.Max(ImGui.GetWindowContentRegionMax().X - ImGui.GetCursorPosX() - 50f, 150));
        if (ImGui.InputText($"##{(nint)this.Node:X}textEdit", ref text, 512, EnterReturnsTrue))
        {
            this.TxtNode->SetText(text);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Font:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.Combo($"##{(nint)this.Node:X}fontType", ref fontIndex, FontNames, FontList.Count))
        {
            this.TxtNode->FontType = FontList[fontIndex];
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Font Size:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"##{(nint)this.Node:X}fontSize", ref fontSize, 1, 10))
        {
            this.TxtNode->FontSize = (byte)fontSize;
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Alignment:");
        ImGui.TableNextColumn();
        if (InputAlignment($"##{(nint)this.Node:X}alignment", ref alignment))
        {
            this.TxtNode->AlignmentType = alignment;
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Text Color:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.ColorEdit4($"##{(nint)this.Node:X}TextRGB", ref textColor, DisplayHex))
        {
            this.TxtNode->TextColor = new() { RGBA = RgbaVector4ToUint(textColor) };
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Edge Color:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.ColorEdit4($"##{(nint)this.Node:X}EdgeRGB", ref edgeColor, DisplayHex))
        {
            this.TxtNode->EdgeColor = new() { RGBA = RgbaVector4ToUint(edgeColor) };
        }
    }

    private static bool InputAlignment(string label, ref AlignmentType alignment)
    {
        var hAlign = (int)alignment % 3;
        var vAlign = ((int)alignment - hAlign) / 3;

        var hAlignInput = ImGuiComponents.IconButtonSelect($"{label}H", ref hAlign, [AlignLeft, AlignCenter, AlignRight], [0, 1, 2], 3u, new(25, 0));
        var vAlignInput = ImGuiComponents.IconButtonSelect($"{label}V", ref vAlign, [ArrowsUpToLine, GripLines, ArrowsDownToLine], [0, 1, 2], 3u, new(25, 0));

        if (hAlignInput || vAlignInput)
        {
            alignment = (AlignmentType)((vAlign * 3) + hAlign);
            return true;
        }

        return false;
    }
}
