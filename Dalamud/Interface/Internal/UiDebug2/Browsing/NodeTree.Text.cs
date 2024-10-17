using System.Runtime.InteropServices;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Internal.UiDebug2.Utility;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static Dalamud.Interface.ColorHelpers;
using static Dalamud.Interface.Internal.UiDebug2.Utility.Gui;
using static Dalamud.Utility.Util;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

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

        ImGui.TextColored(new(1), "Text:");
        ImGui.SameLine();

        try
        {
            var style = new SeStringDrawParams
            {
                Color = this.TxtNode->TextColor.RGBA,
                EdgeColor = this.TxtNode->EdgeColor.RGBA,
                ForceEdgeColor = true,
                EdgeStrength = 1f
            };

#pragma warning disable SeStringRenderer
            ImGuiHelpers.SeStringWrapped(this.NodeText.AsSpan(), style);
#pragma warning restore SeStringRenderer
        }
        catch
        {
            ImGui.Text(Marshal.PtrToStringAnsi(new(this.NodeText.StringPtr)) ?? "");
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

        if (tree)
        {
            var utf8String = this.NodeText;
            var seStringBytes = new byte[utf8String.BufUsed];
            for (var i = 0L; i < utf8String.BufUsed; i++)
            {
                seStringBytes[i] = utf8String.StringPtr[i];
            }

            var seString = SeString.Parse(seStringBytes);
            for (var i = 0; i < seString.Payloads.Count; i++)
            {
                var payload = seString.Payloads[i];
                ImGui.Text($"[{i}]");
                ImGui.SameLine();
                switch (payload.Type)
                {
                    case PayloadType.RawText when payload is TextPayload tp:
                    {
                        Gui.PrintFieldValuePair("Raw Text", tp.Text ?? string.Empty);
                        break;
                    }

                    default:
                    {
                        ImGui.Text(payload.ToString());
                        break;
                    }
                }
            }
        }
    }
}
