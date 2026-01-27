using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Text.ReadOnly;

using static Dalamud.Interface.ColorHelpers;
using static Dalamud.Interface.Internal.UiDebug.Utility.Gui;
using static Dalamud.Utility.Util;

namespace Dalamud.Interface.Internal.UiDebug.Browsing;

/// <summary>
/// A tree for an <see cref="AtkTextNode"/> that can be printed and browsed via ImGui.
/// </summary>
internal unsafe partial class TextNodeTree : ResNodeTree
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextNodeTree"/> class.
    /// </summary>
    /// <param name="node">The node to create a tree for.</param>
    /// <param name="addonTree">The tree representing the containing addon.</param>
    internal TextNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    private AtkTextNode* TxtNode => (AtkTextNode*)this.Node;

    private Utf8String NodeText => this.TxtNode->NodeText;

    /// <inheritdoc/>
    private protected override void PrintNodeObject() => ShowStruct(this.TxtNode);

    /// <inheritdoc/>
    private protected override void PrintFieldsForNodeType(bool isEditorOpen = false)
    {
        if (isEditorOpen)
        {
            return;
        }

        ImGui.TextColored(new Vector4(1), "Text:"u8);
        ImGui.SameLine();

        try
        {
            var style = new SeStringDrawParams
            {
                Color = this.TxtNode->TextColor.RGBA,
                EdgeColor = this.TxtNode->EdgeColor.RGBA,
                ForceEdgeColor = true,
                EdgeStrength = 1f,
            };

            ImGuiHelpers.SeStringWrapped(this.NodeText.AsSpan(), style);
        }
        catch
        {
            ImGui.Text(new ReadOnlySeStringSpan(this.NodeText.AsSpan()).ToMacroString());
        }

        PrintFieldValuePairs(
            ("Font", $"{this.TxtNode->FontType}"),
            ("Font Size", $"{this.TxtNode->FontSize}"),
            ("Alignment", $"{this.TxtNode->AlignmentType}"));

        PrintColor(this.TxtNode->TextColor, $"Text Color: {SwapEndianness(this.TxtNode->TextColor.RGBA):X8}");
        ImGui.SameLine();
        PrintColor(this.TxtNode->EdgeColor, $"Edge Color: {SwapEndianness(this.TxtNode->EdgeColor.RGBA):X8}");

        this.PrintPayloads();
    }

    private void PrintPayloads()
    {
        using var tree = ImRaii.TreeNode($"Text Payloads##{(nint)this.Node:X}");
        if (tree.Success)
        {
            var idx = 0;
            foreach (var payload in new ReadOnlySeString(this.NodeText.AsSpan()))
            {
                ImGui.Text($"[{idx}]");
                ImGui.SameLine();
                switch (payload.Type)
                {
                    case ReadOnlySePayloadType.Text:
                        PrintFieldValuePair("Raw Text", payload.ToString());
                        break;
                    default:
                        ImGui.Text(payload.ToString());
                        break;
                }

                idx++;
            }
        }
    }
}
