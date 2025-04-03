using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static System.MathF;
using static Dalamud.Interface.ColorHelpers;

namespace Dalamud.Interface.Internal.UiDebug2.Utility;

/// <summary>
/// A struct representing the perimeter of an <see cref="AtkResNode"/>, accounting for all transformations.
/// </summary>
public unsafe struct NodeBounds
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeBounds"/> struct.
    /// </summary>
    /// <param name="node">The node to calculate the bounds of.</param>
    internal NodeBounds(AtkResNode* node)
    {
        if (node == null)
        {
            return;
        }

        var w = node->Width;
        var h = node->Height;
        this.Points = w == 0 && h == 0 ? [new(0)] : [new(0), new(w, 0), new(w, h), new(0, h)];

        this.TransformPoints(node);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeBounds"/> struct, containing only a single given point.
    /// </summary>
    /// <param name="point">The point onscreen.</param>
    /// <param name="node">The node used to calculate transformations.</param>
    internal NodeBounds(Vector2 point, AtkResNode* node)
    {
        this.Points = [point];
        this.TransformPoints(node);
    }

    private List<Vector2> Points { get; set; } = [];

    /// <summary>
    /// Draws the bounds onscreen.
    /// </summary>
    /// <param name="col">The color of line to use.</param>
    /// <param name="thickness">The thickness of line to use.</param>
    /// <remarks>If there is only a single point to draw, it will be indicated with a circle and dot.</remarks>
    internal readonly void Draw(Vector4 col, int thickness = 1)
    {
        if (this.Points == null || this.Points.Count == 0)
        {
            return;
        }

        if (this.Points.Count == 1)
        {
            ImGui.GetBackgroundDrawList().AddCircle(this.Points[0], 10, RgbaVector4ToUint(col with { W = col.W / 2 }), 12, thickness);
            ImGui.GetBackgroundDrawList().AddCircle(this.Points[0], thickness, RgbaVector4ToUint(col), 12, thickness + 1);
        }
        else
        {
            var path = new ImVectorWrapper<Vector2>(this.Points.Count);
            foreach (var p in this.Points)
            {
                path.Add(p);
            }

            ImGui.GetBackgroundDrawList()
                 .AddPolyline(ref path[0], path.Length, RgbaVector4ToUint(col), ImDrawFlags.Closed, thickness);

            path.Dispose();
        }
    }

    /// <summary>
    /// Draws the bounds onscreen, filled in.
    /// </summary>
    /// <param name="col">The fill and border color.</param>
    /// <param name="thickness">The border thickness.</param>
    internal readonly void DrawFilled(Vector4 col, int thickness = 1)
    {
        if (this.Points == null || this.Points.Count == 0)
        {
            return;
        }

        if (this.Points.Count == 1)
        {
            ImGui.GetBackgroundDrawList()
                 .AddCircleFilled(this.Points[0], 10, RgbaVector4ToUint(col with { W = col.W / 2 }), 12);
            ImGui.GetBackgroundDrawList().AddCircle(this.Points[0], 10, RgbaVector4ToUint(col), 12, thickness);
        }
        else
        {
            var path = new ImVectorWrapper<Vector2>(this.Points.Count);
            foreach (var p in this.Points)
            {
                path.Add(p);
            }

            ImGui.GetBackgroundDrawList()
                 .AddConvexPolyFilled(ref path[0], path.Length, RgbaVector4ToUint(col with { W = col.W / 2 }));
            ImGui.GetBackgroundDrawList()
                 .AddPolyline(ref path[0], path.Length, RgbaVector4ToUint(col), ImDrawFlags.Closed, thickness);

            path.Dispose();
        }
    }

    /// <summary>
    /// Checks whether the bounds contain a given point.
    /// </summary>
    /// <param name="p">The point to check.</param>
    /// <returns>True if the point exists within the bounds.</returns>
    internal readonly bool ContainsPoint(Vector2 p)
    {
        var count = this.Points.Count;
        var inside = false;

        for (var i = 0; i < count; i++)
        {
            var p1 = this.Points[i];
            var p2 = this.Points[(i + 1) % count];

            if (p.Y > Min(p1.Y, p2.Y) &&
                p.Y <= Max(p1.Y, p2.Y) &&
                p.X <= Max(p1.X, p2.X) &&
                (p1.X.Equals(p2.X) || p.X <= (((p.Y - p1.Y) * (p2.X - p1.X)) / (p2.Y - p1.Y)) + p1.X))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static Vector2 TransformPoint(Vector2 p, Vector2 o, float r, Vector2 s)
    {
        var cosR = Cos(r);
        var sinR = Sin(r);
        var d = (p - o) * s;

        return new(
            (o.X + (d.X * cosR)) - (d.Y * sinR),
            o.Y + (d.X * sinR) + (d.Y * cosR));
    }

    private void TransformPoints(AtkResNode* transformNode)
    {
        while (transformNode != null)
        {
            var offset = new Vector2(transformNode->X, transformNode->Y);
            var origin = offset + new Vector2(transformNode->OriginX, transformNode->OriginY);
            var rotation = transformNode->Rotation;
            var scale = new Vector2(transformNode->ScaleX, transformNode->ScaleY);

            this.Points = this.Points.Select(b => TransformPoint(b + offset, origin, rotation, scale)).ToList();

            transformNode = transformNode->ParentNode;
        }
    }
}
