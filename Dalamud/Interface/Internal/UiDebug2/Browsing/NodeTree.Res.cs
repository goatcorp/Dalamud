using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.UiDebug2.Utility;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Bindings.ImGui.ImGuiCol;
using static Dalamud.Bindings.ImGui.ImGuiTreeNodeFlags;
using static Dalamud.Interface.ColorHelpers;
using static Dalamud.Interface.FontAwesomeIcon;
using static Dalamud.Interface.Internal.UiDebug2.Browsing.Events;
using static Dalamud.Interface.Internal.UiDebug2.ElementSelector;
using static Dalamud.Interface.Internal.UiDebug2.UiDebug2;
using static Dalamud.Interface.Internal.UiDebug2.Utility.Gui;
using static Dalamud.Utility.Util;
using static FFXIVClientStructs.FFXIV.Component.GUI.NodeFlags;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <summary>
/// A tree for an <see cref="AtkResNode"/> that can be printed and browsed via ImGui.
/// </summary>
/// <remarks>As with the structs they represent, this class serves as the base class for other types of NodeTree.</remarks>
internal unsafe partial class ResNodeTree : IDisposable
{
    private NodePopoutWindow? window;

    private bool editorOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResNodeTree"/> class.
    /// </summary>
    /// <param name="node">The node to create a tree for.</param>
    /// <param name="addonTree">The tree representing the containing addon.</param>
    private protected ResNodeTree(AtkResNode* node, AddonTree addonTree)
    {
        this.Node = node;
        this.AddonTree = addonTree;
        this.NodeType = node->Type;
        this.AddonTree.NodeTrees.Add((nint)this.Node, this);
    }

    /// <summary>
    /// Gets or sets the <see cref="AtkResNode"/> this tree represents.
    /// </summary>
    protected internal AtkResNode* Node { get; set; }

    /// <summary>
    /// Gets the <see cref="Browsing.AddonTree"/> containing this tree.
    /// </summary>
    protected internal AddonTree AddonTree { get; private set; }

    /// <summary>
    /// Gets this node's type.
    /// </summary>
    private protected NodeType NodeType { get; init; }

    /// <summary>
    /// Gets or sets the offset of this node within its parent Addon.
    /// </summary>
    private protected int? NodeFieldOffset { get; set; }

    /// <summary>
    /// Clears this NodeTree's popout window, if it has one.
    /// </summary>
    public void Dispose()
    {
        if (this.window != null && PopoutWindows.Windows.Contains(this.window))
        {
            PopoutWindows.RemoveWindow(this.window);
            this.window.Dispose();
        }
    }

    /// <summary>
    /// Gets an instance of <see cref="ResNodeTree"/> (or one of its inheriting types) for the given node. If no instance exists, one is created.
    /// </summary>
    /// <param name="node">The node to get a tree for.</param>
    /// <param name="addonTree">The tree for the node's containing addon.</param>
    /// <returns>An existing or newly-created instance of <see cref="ResNodeTree"/>.</returns>
    internal static ResNodeTree GetOrCreate(AtkResNode* node, AddonTree addonTree) =>
        addonTree.NodeTrees.TryGetValue((nint)node, out var nodeTree) ? nodeTree
            : (int)node->Type >= 1000
                ? new ComponentNodeTree(node, addonTree)
                : node->Type switch
                {
                    NodeType.Text => new TextNodeTree(node, addonTree),
                    NodeType.Image => new ImageNodeTree(node, addonTree),
                    NodeType.NineGrid => new NineGridNodeTree(node, addonTree),
                    NodeType.ClippingMask => new ClippingMaskNodeTree(node, addonTree),
                    NodeType.Counter => new CounterNodeTree(node, addonTree),
                    NodeType.Collision => new CollisionNodeTree(node, addonTree),
                    _ => new ResNodeTree(node, addonTree),
                };

    /// <summary>
    /// Prints a list of NodeTrees for a given list of nodes.
    /// </summary>
    /// <param name="nodeList">The address of the start of the list.</param>
    /// <param name="count">The number of nodes in the list.</param>
    /// <param name="addonTree">The tree for the containing addon.</param>
    internal static void PrintNodeList(AtkResNode** nodeList, int count, AddonTree addonTree)
    {
        for (uint j = 0; j < count; j++)
        {
            GetOrCreate(nodeList[j], addonTree).Print(j);
        }
    }

    /// <summary>
    /// Calls <see cref="PrintNodeList"/>, but outputs the results as a collapsible tree.
    /// </summary>
    /// <param name="nodeList">The address of the start of the list.</param>
    /// <param name="count">The number of nodes in the list.</param>
    /// <param name="label">The heading text of the tree.</param>
    /// <param name="addonTree">The tree for the containing addon.</param>
    /// <param name="color">The text color of the heading.</param>
    internal static void PrintNodeListAsTree(AtkResNode** nodeList, int count, string label, AddonTree addonTree, Vector4 color)
    {
        if (count <= 0)
        {
            return;
        }

        using var col = ImRaii.PushColor(Text, color);
        using var tree = ImRaii.TreeNode($"{label}##{(nint)nodeList:X}", SpanFullWidth);
        col.Pop();

        if (tree.Success)
        {
            var lineStart = ImGui.GetCursorScreenPos() + new Vector2(-10, 2);

            PrintNodeList(nodeList, count, addonTree);

            var lineEnd = lineStart with { Y = ImGui.GetCursorScreenPos().Y - 7 };

            if (lineStart.Y < lineEnd.Y)
            {
                ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, RgbaVector4ToUint(color), 1);
            }
        }
    }

    /// <summary>
    /// Prints this tree in the window.
    /// </summary>
    /// <param name="index">The index of the tree within its containing node or addon, if applicable.</param>
    /// <param name="forceOpen">Whether the tree should default to being open.</param>
    internal void Print(uint? index, bool forceOpen = false)
    {
        if (SearchResults.Length > 0 && SearchResults[0] == (nint)this.Node)
        {
            this.PrintWithHighlights(index);
        }
        else
        {
            this.PrintTree(index, forceOpen);
        }
    }

    /// <summary>
    /// Prints out the tree's header text.
    /// </summary>
    internal void WriteTreeHeading()
    {
        ImGui.Text(this.GetHeaderText());
        this.PrintFieldLabels();
    }

    /// <summary>
    /// If the given pointer is referenced with the addon struct, the offset within the addon will be printed. If the given pointer has been identified as a field within the addon struct, this method also prints that field's name.
    /// </summary>
    /// <param name="ptr">The pointer to check.</param>
    /// <param name="color">The text color to use.</param>
    /// <param name="fieldOffset">The field offset of the pointer, if it was found in the addon.</param>
    private protected void PrintFieldLabel(nint ptr, Vector4 color, int? fieldOffset)
    {
        if (fieldOffset != null)
        {
            ImGui.SameLine(0, -1);
            ImGui.TextColored(color * 0.85f, $"[0x{fieldOffset:X}]");
        }

        if (this.AddonTree.FieldNames.TryGetValue(ptr, out var result))
        {
            ImGui.SameLine(0, -1);
            ImGui.TextColored(color, string.Join(".", result));
        }
    }

    /// <summary>
    /// Builds a string that will serve as the header text for the tree. Indicates the node type, the number of direct children it contains, and its pointer.
    /// </summary>
    /// <returns>The resulting header text string.</returns>
    private protected virtual string GetHeaderText()
    {
        var count = this.GetDirectChildCount();
        return $"{this.NodeType} Node{(count > 0 ? $" [+{count}]" : string.Empty)}";
    }

    /// <summary>
    /// Prints any field names for the node.
    /// </summary>
    private protected virtual void PrintFieldLabels()
    {
        this.PrintFieldLabel((nint)this.Node, new(0, 0.85F, 1, 1), this.NodeFieldOffset);
    }

    /// <summary>
    /// Prints the node struct.
    /// </summary>
    private protected virtual void PrintNodeObject()
    {
        ShowStruct(this.Node);
        ImGui.SameLine();
        ImGui.NewLine();
    }

    /// <summary>
    /// Prints all direct children of this node.
    /// </summary>
    private protected virtual void PrintChildNodes()
    {
        var prevNode = this.Node->ChildNode;
        while (prevNode != null)
        {
            GetOrCreate(prevNode, this.AddonTree).Print(null);
            prevNode = prevNode->PrevSiblingNode;
        }
    }

    /// <summary>
    /// Prints any specific fields pertaining to the specific type of node.
    /// </summary>
    /// <param name="isEditorOpen">Whether the "Edit" box is currently checked.</param>
    private protected virtual void PrintFieldsForNodeType(bool isEditorOpen = false)
    {
    }

    /// <summary>
    /// Attempts to retrieve the field offset of the given pointer within the parent addon.
    /// </summary>
    private protected virtual void GetFieldOffset()
    {
        for (var i = 0; i < this.AddonTree.AddonSize; i += 0x8)
        {
            if (Marshal.ReadIntPtr(this.AddonTree.InitialPtr + i) == (nint)this.Node)
            {
                this.NodeFieldOffset = i;
                break;
            }
        }
    }

    private int GetDirectChildCount()
    {
        var count = 0;
        if (this.Node->ChildNode != null)
        {
            count++;

            var prev = this.Node->ChildNode;
            while (prev->PrevSiblingNode != null)
            {
                prev = prev->PrevSiblingNode;
                count++;
            }
        }

        return count;
    }

    private void PrintWithHighlights(uint? index)
    {
        if (!Scrolled)
        {
            ImGui.SetScrollHereY();
            Scrolled = true;
        }

        var start = ImGui.GetCursorScreenPos() - new Vector2(5);
        this.PrintTree(index, true);
        var end = new Vector2(ImGui.GetMainViewport().WorkSize.X, ImGui.GetCursorScreenPos().Y + 5);

        ImGui.GetWindowDrawList().AddRectFilled(start, end, RgbaVector4ToUint(new Vector4(1, 1, 0.2f, 1) { W = Countdown / 200f }));
    }

    private void PrintTree(uint? index, bool forceOpen = false)
    {
        var visible = this.Node->NodeFlags.HasFlag(Visible);
        var label = $"{(index == null ? string.Empty : $"[{index}] ")}[#{this.Node->NodeId}]###{(nint)this.Node:X}nodeTree";
        var displayColor = !visible ? new Vector4(0.8f, 0.8f, 0.8f, 1) :
                           this.Node->Color.A == 0 ? new(0.015f, 0.575f, 0.355f, 1) :
                           new(0.1f, 1f, 0.1f, 1f);

        if (forceOpen || SearchResults.Contains((nint)this.Node))
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);
        }

        this.GetFieldOffset();

        using var col = ImRaii.PushColor(Text, displayColor);
        using var tree = ImRaii.TreeNode(label, SpanFullWidth);

        if (ImGui.IsItemHovered())
        {
            new NodeBounds(this.Node).Draw(visible ? new(0.1f, 1f, 0.1f, 1f) : new(1f, 0f, 0.2f, 1f));
        }

        ImGui.SameLine(0, -1);
        this.WriteTreeHeading();

        col.Pop();

        if (tree.Success)
        {
            var lineStart = ImGui.GetCursorScreenPos() + new Vector2(-10, 2);
            try
            {
                PrintFieldValuePair("Node", $"{(nint)this.Node:X}");

                ImGui.SameLine();
                this.PrintNodeObject();

                PrintFieldValuePairs(
                    ("NodeID", $"{this.Node->NodeId}"),
                    ("Type", $"{this.Node->Type}"));

                this.DrawBasicControls();

                if (this.editorOpen)
                {
                    this.DrawNodeEditorTable();
                }
                else
                {
                    this.PrintResNodeFields();
                }

                this.PrintFieldsForNodeType(this.editorOpen);
                PrintEvents(this.Node);
                new TimelineTree(this.Node).Print();

                this.PrintChildNodes();
            }
            catch (Exception ex)
            {
                ImGui.TextDisabled($"Couldn't display node!\n\n{ex}");
            }

            var lineEnd = lineStart with { Y = ImGui.GetCursorScreenPos().Y - 7 };

            if (lineStart.Y < lineEnd.Y)
            {
                ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, RgbaVector4ToUint(displayColor), 1);
            }
        }
    }

    private void DrawBasicControls()
    {
        ImGui.SameLine();
        var y = ImGui.GetCursorPosY();

        ImGui.SetCursorPosY(y - 2);
        var isVisible = this.Node->NodeFlags.HasFlag(Visible);
        if (ImGuiComponents.IconButton("vis", isVisible ? Eye : EyeSlash, isVisible ? new Vector4(0.0f, 0.8f, 0.2f, 1f) : new(0.6f, 0.6f, 0.6f, 1)))
        {
            if (isVisible)
            {
                this.Node->NodeFlags &= ~Visible;
            }
            else
            {
                this.Node->NodeFlags |= Visible;
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggle Visibility"u8);
        }

        ImGui.SameLine();
        ImGui.SetCursorPosY(y - 2);
        ImGui.Checkbox($"Edit###editCheckBox{(nint)this.Node}", ref this.editorOpen);

        ImGui.SameLine();
        ImGui.SetCursorPosY(y - 2);
        if (ImGuiComponents.IconButton($"###{(nint)this.Node}popoutButton", this.window?.IsOpen == true ? Times : ArrowUpRightFromSquare, null))
        {
            this.TogglePopout();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggle Popout Window"u8);
        }
    }

    private void TogglePopout()
    {
        if (this.window != null)
        {
            this.window.IsOpen = !this.window.IsOpen;
        }
        else
        {
            this.window = new NodePopoutWindow(this, $"{this.AddonTree.AddonName}: {this.GetHeaderText()}###nodePopout{(nint)this.Node}");
            PopoutWindows.AddWindow(this.window);
        }
    }

    private void PrintResNodeFields()
    {
        PrintFieldValuePairs(
            ("X", $"{this.Node->X}"),
            ("Y", $"{this.Node->Y}"),
            ("Width", $"{this.Node->Width}"),
            ("Height", $"{this.Node->Height}"),
            ("Priority", $"{this.Node->Priority}"),
            ("Depth", $"{this.Node->Depth}"),
            ("DrawFlags", $"0x{this.Node->DrawFlags:X}"));

        PrintFieldValuePairs(
            ("ScaleX", $"{this.Node->ScaleX:F2}"),
            ("ScaleY", $"{this.Node->ScaleY:F2}"),
            ("OriginX", $"{this.Node->OriginX}"),
            ("OriginY", $"{this.Node->OriginY}"),
            ("Rotation", $"{this.Node->Rotation * (180d / Math.PI):F1}° / {this.Node->Rotation:F7}rad "));

        var color = this.Node->Color;
        var add = new Vector3(this.Node->AddRed, this.Node->AddGreen, this.Node->AddBlue);
        var multiply = new Vector3(this.Node->MultiplyRed, this.Node->MultiplyGreen, this.Node->MultiplyBlue);

        PrintColor(RgbaUintToVector4(color.RGBA) with { W = 1 }, $"RGB: {SwapEndianness(color.RGBA) >> 8:X6}");
        ImGui.SameLine();
        PrintColor(color, $"Alpha: {color.A}");
        ImGui.SameLine();
        PrintColor((add / new Vector3(510f)) + new Vector3(0.5f), $"Add: {add.X} {add.Y} {add.Z}");
        ImGui.SameLine();
        PrintColor(multiply / 255f, $"Multiply: {multiply.X} {multiply.Y} {multiply.Z}");

        PrintFieldValuePairs(("Flags", $"0x{(uint)this.Node->NodeFlags:X} ({this.Node->NodeFlags})"));
    }
}
