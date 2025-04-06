using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;

using static Dalamud.Interface.ColorHelpers;
using static Dalamud.Interface.Internal.UiDebug2.Utility.Gui;
using static Dalamud.Utility.Util;
using static FFXIVClientStructs.FFXIV.Component.GUI.TextureType;
using static Dalamud.Bindings.ImGui.ImGuiTableColumnFlags;
using static Dalamud.Bindings.ImGui.ImGuiTableFlags;
using static Dalamud.Bindings.ImGui.ImGuiTreeNodeFlags;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <summary>
/// A tree for an <see cref="AtkImageNode"/> that can be printed and browsed via ImGui.
/// </summary>
internal unsafe partial class ImageNodeTree : ResNodeTree
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageNodeTree"/> class.
    /// </summary>
    /// <param name="node">The node to create a tree for.</param>
    /// <param name="addonTree">The tree representing the containing addon.</param>
    internal ImageNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    /// <summary>
    /// Gets the part ID that this node uses.
    /// </summary>
    private protected virtual uint PartId => this.ImgNode->PartId;

    /// <summary>
    /// Gets the parts list that this node uses.
    /// </summary>
    private protected virtual AtkUldPartsList* PartsList => this.ImgNode->PartsList;

    /// <summary>
    /// Gets or sets a summary of pertinent data about this <see cref="AtkImageNode"/>'s texture. Updated each time <see cref="DrawTextureAndParts"/> is called.
    /// </summary>
    private protected TextureData TexData { get; set; }

    private AtkImageNode* ImgNode => (AtkImageNode*)this.Node;

    /// <summary>
    /// Draws the texture inside the window, in either of two styles.<br/><br/>
    /// <term>Full Image (0)</term>presents the texture in full as a spritesheet.<br/>
    /// <term>Parts List (1)</term>presents the individual parts as rows in a table.
    /// </summary>
    private protected void DrawTextureAndParts()
    {
        this.TexData = new TextureData(this.PartsList, this.PartId);

        if (this.TexData.Texture == null)
        {
            return;
        }

        using var tree = ImRaii.TreeNode($"Texture##texture{(nint)this.TexData.Texture->D3D11ShaderResourceView:X}", SpanFullWidth);

        if (tree.Success)
        {
            PrintFieldValuePairs(
                ("Texture Type", $"{this.TexData.TexType}"),
                ("Part ID", $"{this.TexData.PartId}"),
                ("Part Count", $"{this.TexData.PartCount}"));

            if (this.TexData.Path != null)
            {
                PrintFieldValuePairs(("Texture Path", this.TexData.Path));
            }

            if (ImGui.RadioButton("Full Image##textureDisplayStyle0", TexDisplayStyle == 0))
            {
                TexDisplayStyle = 0;
            }

            ImGui.SameLine();
            if (ImGui.RadioButton("Parts List##textureDisplayStyle1", TexDisplayStyle == 1))
            {
                TexDisplayStyle = 1;
            }

            ImGui.NewLine();

            if (TexDisplayStyle == 1)
            {
                this.PrintPartsTable();
            }
            else
            {
                this.DrawFullTexture();
            }
        }
    }

    /// <summary>
    /// Draws an outline of a given part within the texture.
    /// </summary>
    /// <param name="partId">The part ID.</param>
    /// <param name="cursorScreenPos">The absolute position of the cursor onscreen.</param>
    /// <param name="cursorLocalPos">The relative position of the cursor within the window.</param>
    /// <param name="col">The color of the outline.</param>
    /// <param name="reqHover">Whether this outline requires the user to mouse over it.</param>
    private protected virtual void DrawPartOutline(uint partId, Vector2 cursorScreenPos, Vector2 cursorLocalPos, Vector4 col, bool reqHover = false)
    {
        var part = this.TexData.PartsList->Parts[partId];

        var hrFactor = this.TexData.HiRes ? 2f : 1f;

        var uv = new Vector2(part.U, part.V) * hrFactor;
        var wh = new Vector2(part.Width, part.Height) * hrFactor;

        var partBegin = cursorScreenPos + uv;
        var partEnd = partBegin + wh;

        if (reqHover && !ImGui.IsMouseHoveringRect(partBegin, partEnd))
        {
            return;
        }

        var savePos = ImGui.GetCursorPos();

        ImGui.GetWindowDrawList().AddRect(partBegin, partEnd, RgbaVector4ToUint(col));

        ImGui.SetCursorPos(cursorLocalPos + uv + new Vector2(0, -20));
        ImGui.TextColored(col, $"[#{partId}]\t{part.U}, {part.V}\t{part.Width}x{part.Height}");
        ImGui.SetCursorPos(savePos);
    }

    /// <inheritdoc/>
    private protected override void PrintNodeObject() => ShowStruct(this.ImgNode);

    /// <inheritdoc/>
    private protected override void PrintFieldsForNodeType(bool isEditorOpen = false)
    {
        PrintFieldValuePairs(
            ("Wrap", $"{this.ImgNode->WrapMode}"),
            ("Image Flags", $"0x{this.ImgNode->Flags:X}"));
        this.DrawTextureAndParts();
    }

    private static void PrintPartCoords(float u, float v, float w, float h, bool asFloat = false, bool lineBreak = false)
    {
        ImGui.TextDisabled($"{u}, {v},{(lineBreak ? "\n" : " ")}{w}, {h}");

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Click to copy as Vector2\nShift-click to copy as Vector4");
        }

        var suffix = asFloat ? "f" : string.Empty;

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(
                ImGui.IsKeyDown(ImGuiKey.ModShift)
                                       ? $"new Vector4({u}{suffix}, {v}{suffix}, {w}{suffix}, {h}{suffix})"
                                       : $"new Vector2({u}{suffix}, {v}{suffix});\nnew Vector2({w}{suffix}, {h}{suffix})");
        }
    }

    private void DrawFullTexture()
    {
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var cursorLocalPos = ImGui.GetCursorPos();

        ImGui.Image(new(this.TexData.Texture->D3D11ShaderResourceView), new(this.TexData.Texture->ActualWidth, this.TexData.Texture->ActualHeight));

        for (uint p = 0; p < this.TexData.PartsList->PartCount; p++)
        {
            if (p == this.TexData.PartId)
            {
                continue;
            }

            this.DrawPartOutline(p, cursorScreenPos, cursorLocalPos, new(0.6f, 0.6f, 0.6f, 1), true);
        }

        this.DrawPartOutline(this.TexData.PartId, cursorScreenPos, cursorLocalPos, new(0, 0.85F, 1, 1));
    }

    private void PrintPartsTable()
    {
        using var tbl = ImRaii.Table($"partsTable##{(nint)this.TexData.Texture->D3D11ShaderResourceView:X}", 3, Borders | RowBg | Reorderable);
        if (tbl.Success)
        {
            ImGui.TableSetupColumn("Part ID", WidthFixed);
            ImGui.TableSetupColumn("Part Texture", WidthFixed);
            ImGui.TableSetupColumn("Coordinates", WidthFixed);

            ImGui.TableHeadersRow();

            var tWidth = this.TexData.Texture->ActualWidth;
            var tHeight = this.TexData.Texture->ActualHeight;
            var textureSize = new Vector2(tWidth, tHeight);

            for (ushort i = 0; i < this.TexData.PartCount; i++)
            {
                ImGui.TableNextColumn();

                var col = i == this.TexData.PartId ? new Vector4(0, 0.85F, 1, 1) : new(1);
                ImGui.TextColored(col, $"#{i.ToString().PadLeft(this.TexData.PartCount.ToString().Length, '0')}");

                ImGui.TableNextColumn();

                var part = this.TexData.PartsList->Parts[i];
                var hiRes = this.TexData.HiRes;

                var u = hiRes ? part.U * 2f : part.U;
                var v = hiRes ? part.V * 2f : part.V;
                var width = hiRes ? part.Width * 2f : part.Width;
                var height = hiRes ? part.Height * 2f : part.Height;

                ImGui.Image(
                    new(this.TexData.Texture->D3D11ShaderResourceView),
                    new(width, height),
                    new Vector2(u, v) / textureSize,
                    new Vector2(u + width, v + height) / textureSize);

                ImGui.TableNextColumn();

                ImGui.TextColored(!hiRes ? new(1) : new(0.6f, 0.6f, 0.6f, 1), "Standard:\t");
                ImGui.SameLine();
                var cursX = ImGui.GetCursorPosX();

                PrintPartCoords(u / 2f, v / 2f, width / 2f, height / 2f);

                ImGui.TextColored(hiRes ? new(1) : new(0.6f, 0.6f, 0.6f, 1), "Hi-Res:\t");
                ImGui.SameLine();
                ImGui.SetCursorPosX(cursX);

                PrintPartCoords(u, v, width, height);

                ImGui.Text("UV:\t");
                ImGui.SameLine();
                ImGui.SetCursorPosX(cursX);

                PrintPartCoords(u / tWidth, v / tWidth, (u + width) / tWidth, (v + height) / tHeight, true, true);
            }
        }
    }

    /// <summary>
    /// A summary of pertinent data about a node's texture.
    /// </summary>
    protected struct TextureData
    {
        /// <summary>The texture's partslist.</summary>
        public AtkUldPartsList* PartsList;

        /// <summary>The number of parts in the texture.</summary>
        public uint PartCount;

        /// <summary>The part ID the node is using.</summary>
        public uint PartId;

        /// <summary>The texture itself.</summary>
        public Texture* Texture = null;

        /// <summary>The type of texture.</summary>
        public TextureType TexType = 0;

        /// <summary>The texture's file path (if <see cref="TextureType.Resource"/>, otherwise this value is null).</summary>
        public string? Path = null;

        /// <summary>Whether this is a high-resolution texture.</summary>
        public bool HiRes = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureData"/> struct.
        /// </summary>
        /// <param name="partsList">The texture's parts list.</param>
        /// <param name="partId">The part ID being used by the node.</param>
        public TextureData(AtkUldPartsList* partsList, uint partId)
        {
            this.PartsList = partsList;
            this.PartCount = this.PartsList->PartCount;
            this.PartId = partId >= this.PartCount ? 0 : partId;

            if (this.PartsList == null)
            {
                return;
            }

            var asset = this.PartsList->Parts[this.PartId].UldAsset;

            if (asset == null)
            {
                return;
            }

            this.TexType = asset->AtkTexture.TextureType;

            if (this.TexType == Resource)
            {
                var resource = asset->AtkTexture.Resource;
                this.Texture = resource->KernelTextureObject;
                this.Path = Marshal.PtrToStringAnsi(new(resource->TexFileResourceHandle->ResourceHandle.FileName.BufferPtr));
            }
            else
            {
                this.Texture = this.TexType == KernelTexture ? asset->AtkTexture.KernelTexture : null;
                this.Path = null;
            }

            this.HiRes = this.Path?.Contains("_hr1") ?? false;
        }
    }
}
