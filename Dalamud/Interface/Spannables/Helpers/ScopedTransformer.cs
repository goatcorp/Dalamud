using System.Numerics;

using Dalamud.Interface.Spannables.RenderPassMethodArgs;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Helpers;

/// <summary>Transformes all vertices rendered during the scope.</summary>
public readonly ref struct ScopedTransformer
{
    private readonly Matrix4x4 matrix;
    private readonly ImDrawListPtr drawListPtr;
    private readonly float opacityMultiplier;
    private readonly int numVertices;

    /// <summary>Initializes a new instance of the <see cref="ScopedTransformer"/> struct.</summary>
    /// <param name="mtx">The transformation matrix.</param>
    /// <param name="drawListPtr">The draw list.</param>
    /// <param name="opacityMultiplier">The opacity multiplier.</param>
    public ScopedTransformer(scoped in Matrix4x4 mtx, ImDrawListPtr drawListPtr, float opacityMultiplier)
    {
        this.matrix = mtx;
        this.drawListPtr = drawListPtr;
        this.opacityMultiplier = opacityMultiplier;
        this.numVertices = drawListPtr.VtxBuffer.Size;
    }

    /// <summary>Creates a new instance of <see cref="ScopedTransformer"/> from a <see cref="SpannableDrawArgs"/>.
    /// </summary>
    /// <param name="args">The argunents.</param>
    /// <param name="opacityMultiplier">The opacity multiplier.</param>
    /// <returns>A new instance of <see cref="ScopedTransformer"/>.</returns>
    public static ScopedTransformer From(SpannableDrawArgs args, float opacityMultiplier)
    {
        // var b = args.RenderPass.Boundary.Size * args.RenderPass.InnerOrigin;
        return new(args.RenderPass.Transformation, args.DrawListPtr, opacityMultiplier);
    }

    /// <summary>Transforms the vertices.</summary>
    public unsafe void Dispose()
    {
        var span =
            new Span<ImDrawVert>(
                (ImDrawVert*)this.drawListPtr.VtxBuffer.Data,
                this.drawListPtr.VtxBuffer.Size)[this.numVertices..];

        if (!this.matrix.IsIdentity)
        {
            foreach (ref var v in span)
                v.pos = Vector2.Transform(v.pos, this.matrix);
        }

        if (this.opacityMultiplier <= 0f)
        {
            foreach (ref var v in span)
                v.col = 0;
        }
        else if (this.opacityMultiplier < 1f)
        {
            foreach (ref var v in span)
            {
                var a = v.col >> 24;
                v.col &= 0xFFFFFF;
                a = (byte)Math.Clamp(a * this.opacityMultiplier, 0, 255);
                v.col |= a << 24;
            }
        }
    }
}
