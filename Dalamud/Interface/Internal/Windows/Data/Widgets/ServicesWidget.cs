﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.IoC.Internal;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying start info.
/// </summary>
internal class ServicesWidget : IDataWindowWidget
{
    private readonly Dictionary<ServiceDependencyNode, Vector4> nodeRects = new();
    private readonly HashSet<Type> selectedNodes = new();
    private readonly HashSet<Type> tempRelatedNodes = new();

    private bool includeUnloadDependencies;
    private List<List<ServiceDependencyNode>>? dependencyNodes;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "services" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Service Container"; 

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
        var container = Service<ServiceContainer>.Get();

        if (ImGui.CollapsingHeader("Dependencies"))
        {
            if (ImGui.Button("Clear selection"))
                this.selectedNodes.Clear();
            
            ImGui.SameLine();
            switch (this.includeUnloadDependencies)
            {
                case true when ImGui.Button("Show load-time dependencies"):
                    this.includeUnloadDependencies = false;
                    this.dependencyNodes = null;
                    break;
                case false when ImGui.Button("Show unload-time dependencies"):
                    this.includeUnloadDependencies = true;
                    this.dependencyNodes = null;
                    break;
            }

            this.dependencyNodes ??= ServiceDependencyNode.CreateTreeByLevel(this.includeUnloadDependencies);
            var cellPad = ImGui.CalcTextSize("WW");
            var margin = ImGui.CalcTextSize("W\nW\nW");
            var rowHeight = cellPad.Y * 3;
            var width = ImGui.GetContentRegionAvail().X;
            if (ImGui.BeginChild(
                    "dependency-graph",
                    new(width, (this.dependencyNodes.Count * (rowHeight + margin.Y)) + cellPad.Y),
                    false,
                    ImGuiWindowFlags.HorizontalScrollbar))
            {
                const uint rectBaseBorderColor = 0xFFFFFFFF;
                const uint rectHoverFillColor = 0xFF404040;
                const uint rectHoverRelatedFillColor = 0xFF802020;
                const uint rectSelectedFillColor = 0xFF20A020;
                const uint rectSelectedRelatedFillColor = 0xFF204020;
                const uint lineBaseColor = 0xFF808080;
                const uint lineHoverColor = 0xFFFF8080;
                const uint lineHoverNotColor = 0xFF404040;
                const uint lineSelectedColor = 0xFF80FF00;
                const uint lineInvalidColor = 0xFFFF0000;

                ServiceDependencyNode? hoveredNode = null;

                var pos = ImGui.GetCursorScreenPos();
                var dl = ImGui.GetWindowDrawList();
                var mouse = ImGui.GetMousePos();
                var maxRowWidth = 0f;
                
                // 1. Layout
                for (var level = 0; level < this.dependencyNodes.Count; level++)
                {
                    var levelNodes = this.dependencyNodes[level];
                    
                    var rowWidth = 0f;
                    foreach (var node in levelNodes)
                        rowWidth += ImGui.CalcTextSize(node.TypeName).X + cellPad.X + margin.X;

                    var off = cellPad / 2;
                    if (rowWidth < width)
                        off.X += ImGui.GetScrollX() + ((width - rowWidth) / 2);
                    else if (rowWidth - ImGui.GetScrollX() < width)
                        off.X += width - (rowWidth - ImGui.GetScrollX());
                    off.Y = (rowHeight + margin.Y) * level;

                    foreach (var node in levelNodes)
                    {
                        var textSize = ImGui.CalcTextSize(node.TypeName);
                        var cellSize = textSize + cellPad;

                        var rc = new Vector4(pos + off, pos.X + off.X + cellSize.X, pos.Y + off.Y + cellSize.Y);
                        this.nodeRects[node] = rc;
                        if (rc.X <= mouse.X && mouse.X < rc.Z && rc.Y <= mouse.Y && mouse.Y < rc.W)
                        {
                            hoveredNode = node;
                            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                            {
                                if (this.selectedNodes.Contains(node.Type))
                                    this.selectedNodes.Remove(node.Type);
                                else
                                    this.selectedNodes.Add(node.Type);
                            }
                        }

                        off.X += cellSize.X + margin.X;
                    }

                    maxRowWidth = Math.Max(maxRowWidth, rowWidth);
                }

                // 2. Draw non-hovered lines
                foreach (var levelNodes in this.dependencyNodes)
                {
                    foreach (var node in levelNodes)
                    {
                        var rect = this.nodeRects[node];
                        var point1 = new Vector2((rect.X + rect.Z) / 2, rect.Y);
                        
                        foreach (var parent in node.InvalidParents)
                        {
                            rect = this.nodeRects[parent];
                            var point2 = new Vector2((rect.X + rect.Z) / 2, rect.W);
                            if (node == hoveredNode || parent == hoveredNode)
                                continue;

                            dl.AddLine(point1, point2, lineInvalidColor, 2f * ImGuiHelpers.GlobalScale);
                        }
                        
                        foreach (var parent in node.Parents)
                        {
                            rect = this.nodeRects[parent];
                            var point2 = new Vector2((rect.X + rect.Z) / 2, rect.W);
                            if (node == hoveredNode || parent == hoveredNode)
                                continue;

                            var isSelected = this.selectedNodes.Contains(node.Type) ||
                                             this.selectedNodes.Contains(parent.Type);
                            dl.AddLine(
                                point1,
                                point2,
                                isSelected
                                    ? lineSelectedColor
                                    : hoveredNode is not null
                                        ? lineHoverNotColor
                                        : lineBaseColor);
                        }
                    }
                }
                
                // 3. Draw boxes
                foreach (var levelNodes in this.dependencyNodes)
                {
                    foreach (var node in levelNodes)
                    {
                        var textSize = ImGui.CalcTextSize(node.TypeName);
                        var cellSize = textSize + cellPad;

                        var rc = this.nodeRects[node];
                        if (hoveredNode == node)
                            dl.AddRectFilled(new(rc.X, rc.Y), new(rc.Z, rc.W), rectHoverFillColor);
                        else if (this.selectedNodes.Contains(node.Type))
                            dl.AddRectFilled(new(rc.X, rc.Y), new(rc.Z, rc.W), rectSelectedFillColor);
                        else if (node.Relatives.Any(x => this.selectedNodes.Contains(x.Type)))
                            dl.AddRectFilled(new(rc.X, rc.Y), new(rc.Z, rc.W), rectSelectedRelatedFillColor);
                        else if (hoveredNode?.Relatives.Select(x => x.Type).Contains(node.Type) is true)
                            dl.AddRectFilled(new(rc.X, rc.Y), new(rc.Z, rc.W), rectHoverRelatedFillColor);

                        dl.AddRect(new(rc.X, rc.Y), new(rc.Z, rc.W), rectBaseBorderColor);
                        ImGui.SetCursorPos((new Vector2(rc.X, rc.Y) - pos) + ((cellSize - textSize) / 2));
                        ImGui.TextUnformatted(node.TypeName);
                    }
                }

                // 4. Draw hovered lines
                if (hoveredNode is not null)
                {
                    foreach (var levelNodes in this.dependencyNodes)
                    {
                        foreach (var node in levelNodes)
                        {
                            var rect = this.nodeRects[node];
                            var point1 = new Vector2((rect.X + rect.Z) / 2, rect.Y);
                            foreach (var parent in node.Parents)
                            {
                                if (node == hoveredNode || parent == hoveredNode)
                                {
                                    rect = this.nodeRects[parent];
                                    var point2 = new Vector2((rect.X + rect.Z) / 2, rect.W);
                                    dl.AddLine(
                                        point1,
                                        point2,
                                        lineHoverColor,
                                        2 * ImGuiHelpers.GlobalScale);
                                }
                            }
                        }
                    }
                }
                
                ImGui.SetCursorPos(default);
                ImGui.Dummy(new(maxRowWidth, this.dependencyNodes.Count * rowHeight));
                ImGui.EndChild();
            }
        }

        if (ImGui.CollapsingHeader("Plugin-facing Services"))
        {
            foreach (var instance in container.Instances)
            {
                var hasInterface = container.InterfaceToTypeMap.Values.Any(x => x == instance.Key);
                var isPublic = instance.Key.IsPublic;

                ImGui.BulletText($"{instance.Key.FullName} ({instance.Key.GetServiceKind()})");

                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed, !hasInterface))
                {
                    ImGui.Text(
                        hasInterface
                            ? $"\t => Provided via interface: {container.InterfaceToTypeMap.First(x => x.Value == instance.Key).Key.FullName}"
                            : "\t => NO INTERFACE!!!");
                }

                if (isPublic)
                {
                    using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                    ImGui.Text("\t => PUBLIC!!!");
                }

                ImGuiHelpers.ScaledDummy(2);
            }
        }
    }

    private class ServiceDependencyNode
    {
        private readonly List<ServiceDependencyNode> parents = new();
        private readonly List<ServiceDependencyNode> children = new();
        private readonly List<ServiceDependencyNode> invalidParents = new();

        private ServiceDependencyNode(Type t) => this.Type = t;

        public Type Type { get; }

        public string TypeName => this.Type.Name;

        public IReadOnlyList<ServiceDependencyNode> Parents => this.parents;

        public IReadOnlyList<ServiceDependencyNode> Children => this.children;

        public IReadOnlyList<ServiceDependencyNode> InvalidParents => this.invalidParents;

        public IEnumerable<ServiceDependencyNode> Relatives =>
            this.parents.Concat(this.children).Concat(this.invalidParents);
        
        public int Level { get; private set; }

        public static List<ServiceDependencyNode> CreateTree(bool includeUnloadDependencies)
        {
            var nodes = new Dictionary<Type, ServiceDependencyNode>();
            foreach (var t in ServiceManager.GetConcreteServiceTypes())
                nodes.Add(typeof(Service<>).MakeGenericType(t), new(t));
            foreach (var t in ServiceManager.GetConcreteServiceTypes())
            {
                var st = typeof(Service<>).MakeGenericType(t);
                var node = nodes[st];
                foreach (var depType in ServiceHelpers.GetDependencies(st, includeUnloadDependencies))
                {
                    var depServiceType = typeof(Service<>).MakeGenericType(depType);
                    var depNode = nodes[depServiceType];
                    if (node.IsAncestorOf(depType))
                    {
                        node.invalidParents.Add(depNode);
                    }
                    else
                    {
                        depNode.UpdateNodeLevel(1);
                        node.UpdateNodeLevel(depNode.Level + 1);
                        node.parents.Add(depNode);
                        depNode.children.Add(node);
                    }
                }
            }

            return nodes.Values.OrderBy(x => x.Level).ThenBy(x => x.Type.Name).ToList();
        }

        public static List<List<ServiceDependencyNode>> CreateTreeByLevel(bool includeUnloadDependencies)
        {
            var res = new List<List<ServiceDependencyNode>>();
            foreach (var n in CreateTree(includeUnloadDependencies))
            {
                while (res.Count <= n.Level)
                    res.Add(new());
                res[n.Level].Add(n);
            }

            return res;
        }

        private bool IsAncestorOf(Type type) =>
            this.children.Any(x => x.Type == type) || this.children.Any(x => x.IsAncestorOf(type));

        private void UpdateNodeLevel(int newLevel)
        {
            if (this.Level >= newLevel)
                return;

            this.Level = newLevel;
            foreach (var c in this.children)
                c.UpdateNodeLevel(newLevel + 1);
        }
    }
}
