using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static Dalamud.Interface.ColorHelpers;
using static Dalamud.Interface.Internal.UiDebug2.Utility.Gui;
using static Dalamud.Utility.Util;
using static FFXIVClientStructs.FFXIV.Component.GUI.NodeType;
using static ImGuiNET.ImGuiTableColumnFlags;
using static ImGuiNET.ImGuiTableFlags;
using static ImGuiNET.ImGuiTreeNodeFlags;

// ReSharper disable SuggestBaseTypeForParameter
namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <summary>
/// A struct allowing a node's animation timeline to be printed and browsed.
/// </summary>
public readonly unsafe partial struct TimelineTree
{
    private readonly AtkResNode* node;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineTree"/> struct.
    /// </summary>
    /// <param name="node">The node whose timelines are to be displayed.</param>
    internal TimelineTree(AtkResNode* node)
    {
        this.node = node;
    }

    private AtkTimeline* NodeTimeline => this.node->Timeline;

    private AtkTimelineResource* Resource => this.NodeTimeline->Resource;

    private AtkTimelineAnimation* ActiveAnimation => this.NodeTimeline->ActiveAnimation;

    /// <summary>
    /// Prints out this timeline tree within a window.
    /// </summary>
    internal void Print()
    {
        if (this.NodeTimeline == null)
        {
            return;
        }

        var count = this.Resource->AnimationCount;

        if (count > 0)
        {
            var tree = ImRaii.TreeNode($"Timeline##{(nint)this.node:X}timeline", SpanFullWidth);

            if (tree)
            {
                PrintFieldValuePair("Timeline", $"{(nint)this.NodeTimeline:X}");

                ImGui.SameLine();

                ShowStruct(this.NodeTimeline);

                PrintFieldValuePairs(
                    ("Id", $"{this.NodeTimeline->Resource->Id}"),
                    ("Parent Time", $"{this.NodeTimeline->ParentFrameTime:F2} ({this.NodeTimeline->ParentFrameTime * 30:F0})"),
                    ("Frame Time", $"{this.NodeTimeline->FrameTime:F2} ({this.NodeTimeline->FrameTime * 30:F0})"));

                PrintFieldValuePairs(("Active Label Id", $"{this.NodeTimeline->ActiveLabelId}"), ("Duration", $"{this.NodeTimeline->LabelFrameIdxDuration}"), ("End Frame", $"{this.NodeTimeline->LabelEndFrameIdx}"));
                ImGui.TextColored(new(0.6f, 0.6f, 0.6f, 1), "Animation List");

                for (var a = 0; a < count; a++)
                {
                    var animation = this.Resource->Animations[a];
                    var isActive = this.ActiveAnimation != null && &animation == this.ActiveAnimation;
                    this.PrintAnimation(animation, a, isActive, (nint)(this.NodeTimeline->Resource->Animations + (a * sizeof(AtkTimelineAnimation))));
                }
            }

            tree.Dispose();
        }
    }

    private static void GetFrameColumn(Span<AtkTimelineKeyGroup> keyGroups, List<IKeyGroupColumn> columns, ushort endFrame)
    {
        for (var i = 0; i < keyGroups.Length; i++)
        {
            if (keyGroups[i].Type != AtkTimelineKeyGroupType.None)
            {
                var keyGroup = keyGroups[i];
                var idColumn = new KeyGroupColumn<ushort>("Frame");

                for (var f = 0; f < keyGroup.KeyFrameCount; f++)
                {
                    idColumn.Add(keyGroup.KeyFrames[f].FrameIdx);
                }

                if (idColumn.Values.Last() != endFrame)
                {
                    idColumn.Add(endFrame);
                }

                columns.Add(idColumn);
                break;
            }
        }
    }

    private static void GetPosColumns(AtkTimelineKeyGroup keyGroup, List<IKeyGroupColumn> columns)
    {
        if (keyGroup.KeyFrameCount <= 0)
        {
            return;
        }

        var xColumn = new KeyGroupColumn<float>("X");
        var yColumn = new KeyGroupColumn<float>("Y");

        for (var f = 0; f < keyGroup.KeyFrameCount; f++)
        {
            var (x, y) = keyGroup.KeyFrames[f].Value.Float2;

            xColumn.Add(x);
            yColumn.Add(y);
        }

        columns.Add(xColumn);
        columns.Add(yColumn);
    }

    private static void GetRotationColumn(AtkTimelineKeyGroup keyGroup, List<IKeyGroupColumn> columns)
    {
        if (keyGroup.KeyFrameCount <= 0)
        {
            return;
        }

        var rotColumn = new KeyGroupColumn<float>("Rotation", static r => ImGui.Text($"{r * (180d / Math.PI):F1}°"));

        for (var f = 0; f < keyGroup.KeyFrameCount; f++)
        {
            rotColumn.Add(keyGroup.KeyFrames[f].Value.Float);
        }

        columns.Add(rotColumn);
    }

    private static void GetScaleColumns(AtkTimelineKeyGroup keyGroup, List<IKeyGroupColumn> columns)
    {
        if (keyGroup.KeyFrameCount <= 0)
        {
            return;
        }

        var scaleXColumn = new KeyGroupColumn<float>("ScaleX");
        var scaleYColumn = new KeyGroupColumn<float>("ScaleY");

        for (var f = 0; f < keyGroup.KeyFrameCount; f++)
        {
            var (scaleX, scaleY) = keyGroup.KeyFrames[f].Value.Float2;

            scaleXColumn.Add(scaleX);
            scaleYColumn.Add(scaleY);
        }

        columns.Add(scaleXColumn);
        columns.Add(scaleYColumn);
    }

    private static void GetAlphaColumn(AtkTimelineKeyGroup keyGroup, List<IKeyGroupColumn> columns)
    {
        if (keyGroup.KeyFrameCount <= 0)
        {
            return;
        }

        var alphaColumn = new KeyGroupColumn<byte>("Alpha", PrintAlpha);

        for (var f = 0; f < keyGroup.KeyFrameCount; f++)
        {
            alphaColumn.Add(keyGroup.KeyFrames[f].Value.Byte);
        }

        columns.Add(alphaColumn);
    }

    private static void GetTintColumns(AtkTimelineKeyGroup keyGroup, List<IKeyGroupColumn> columns)
    {
        if (keyGroup.KeyFrameCount <= 0)
        {
            return;
        }

        var addRGBColumn = new KeyGroupColumn<Vector3>("Add", PrintAddCell) { Width = 110 };
        var multiplyRGBColumn = new KeyGroupColumn<ByteColor>("Multiply", PrintMultiplyCell) { Width = 110 };

        for (var f = 0; f < keyGroup.KeyFrameCount; f++)
        {
            var nodeTint = keyGroup.KeyFrames[f].Value.NodeTint;

            addRGBColumn.Add(new Vector3(nodeTint.AddR, nodeTint.AddG, nodeTint.AddB));
            multiplyRGBColumn.Add(nodeTint.MultiplyRGB);
        }

        columns.Add(addRGBColumn);
        columns.Add(multiplyRGBColumn);
    }

    private static void GetTextColorColumn(AtkTimelineKeyGroup keyGroup, List<IKeyGroupColumn> columns)
    {
        if (keyGroup.KeyFrameCount <= 0)
        {
            return;
        }

        var textColorColumn = new KeyGroupColumn<ByteColor>("Text Color", PrintRGB);

        for (var f = 0; f < keyGroup.KeyFrameCount; f++)
        {
            textColorColumn.Add(keyGroup.KeyFrames[f].Value.RGB);
        }

        columns.Add(textColorColumn);
    }

    private static void GetPartIdColumn(AtkTimelineKeyGroup keyGroup, List<IKeyGroupColumn> columns)
    {
        if (keyGroup.KeyFrameCount <= 0)
        {
            return;
        }

        var partColumn = new KeyGroupColumn<ushort>("Part ID");

        for (var f = 0; f < keyGroup.KeyFrameCount; f++)
        {
            partColumn.Add(keyGroup.KeyFrames[f].Value.UShort);
        }

        columns.Add(partColumn);
    }

    private static void GetEdgeColumn(AtkTimelineKeyGroup keyGroup, List<IKeyGroupColumn> columns)
    {
        if (keyGroup.KeyFrameCount <= 0)
        {
            return;
        }

        var edgeColorColumn = new KeyGroupColumn<ByteColor>("Edge Color", PrintRGB);

        for (var f = 0; f < keyGroup.KeyFrameCount; f++)
        {
            edgeColorColumn.Add(keyGroup.KeyFrames[f].Value.RGB);
        }

        columns.Add(edgeColorColumn);
    }

    private static void GetLabelColumn(AtkTimelineKeyGroup keyGroup, List<IKeyGroupColumn> columns)
    {
        if (keyGroup.KeyFrameCount <= 0)
        {
            return;
        }

        var labelColumn = new KeyGroupColumn<ushort>("Label");

        for (var f = 0; f < keyGroup.KeyFrameCount; f++)
        {
            labelColumn.Add(keyGroup.KeyFrames[f].Value.Label.LabelId);
        }

        columns.Add(labelColumn);
    }

    private static void PrintRGB(ByteColor c) => PrintColor(c, $"0x{SwapEndianness(c.RGBA):X8}");

    private static void PrintAlpha(byte b) => PrintColor(new Vector4(b / 255f), PadEvenly($"{b}", 25));

    private static void PrintAddCell(Vector3 add)
    {
        var fmt = PadEvenly($"{PadEvenly($"{add.X}", 30)}{PadEvenly($"{add.Y}", 30)}{PadEvenly($"{add.Z}", 30)}", 100);
        PrintColor(new Vector4((add / new Vector3(510f)) + new Vector3(0.5f), 1), fmt);
    }

    private static void PrintMultiplyCell(ByteColor byteColor)
    {
        var multiply = new Vector3(byteColor.R, byteColor.G, byteColor.B);
        var fmt = PadEvenly($"{PadEvenly($"{multiply.X}", 25)}{PadEvenly($"{multiply.Y}", 25)}{PadEvenly($"{multiply.Z}", 25)}", 100);
        PrintColor(multiply / 255f, fmt);
    }

    private static string PadEvenly(string str, float size)
    {
        while (ImGui.CalcTextSize(str).X < size * ImGuiHelpers.GlobalScale)
        {
            str = $" {str} ";
        }

        return str;
    }

    private void PrintAnimation(AtkTimelineAnimation animation, int a, bool isActive, nint address)
    {
        var columns = this.BuildColumns(animation);

        var col = ImRaii.PushColor(ImGuiCol.Text, isActive ? new Vector4(1, 0.65F, 0.4F, 1) : new(1));
        var tree = ImRaii.TreeNode($"[#{a}] [Frames {animation.StartFrameIdx}-{animation.EndFrameIdx}] {(isActive ? " (Active)" : string.Empty)}###{(nint)this.node}animTree{a}");
        col.Dispose();

        if (tree)
        {
            PrintFieldValuePair("Animation", $"{address:X}");

            ShowStruct((AtkTimelineAnimation*)address);

            if (columns.Count > 0)
            {
                var table = ImRaii.Table($"##{(nint)this.node}animTable{a}", columns.Count, Borders | SizingFixedFit | RowBg | NoHostExtendX);

                foreach (var c in columns)
                {
                    ImGui.TableSetupColumn(c.Name, WidthFixed, c.Width);
                }

                ImGui.TableHeadersRow();

                var rows = columns.Select(static c => c.Count).Max();

                for (var i = 0; i < rows; i++)
                {
                    ImGui.TableNextRow();

                    foreach (var c in columns)
                    {
                        ImGui.TableNextColumn();
                        c.PrintValueAt(i);
                    }
                }

                table.Dispose();
            }
        }

        tree.Dispose();
    }

    private List<IKeyGroupColumn> BuildColumns(AtkTimelineAnimation animation)
    {
        var keyGroups = animation.KeyGroups;
        var columns = new List<IKeyGroupColumn>();

        GetFrameColumn(keyGroups, columns, animation.EndFrameIdx);

        GetPosColumns(keyGroups[0], columns);

        GetRotationColumn(keyGroups[1], columns);

        GetScaleColumns(keyGroups[2], columns);

        GetAlphaColumn(keyGroups[3], columns);

        GetTintColumns(keyGroups[4], columns);

        if (this.node->Type is Image or NineGrid or ClippingMask)
        {
            GetPartIdColumn(keyGroups[5], columns);
        }
        else if (this.node->Type == Text)
        {
            GetTextColorColumn(keyGroups[5], columns);
        }

        GetEdgeColumn(keyGroups[6], columns);

        GetLabelColumn(keyGroups[7], columns);

        return columns;
    }
}
