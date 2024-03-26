using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Spannables.Text;
using Dalamud.Utility.Numerics;

using ImGuiNET;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables.Rendering.Internal;

/// <summary>A custom text renderer implementation.</summary>
internal sealed unsafe partial class SpannableRenderer
{
    private readonly nint[] drawListPool = new nint[64];

    private readonly DrawListTexture?[] drawListTexturePool = new DrawListTexture?[64];

    private readonly ConcurrentBag<DrawListTexture> returnToPoolLater = new();

    private ObjectPool<TextSpannableBuilder>? textSpannableBuilderPool =
        new DefaultObjectPool<TextSpannableBuilder>(new DefaultPooledObjectPolicy<TextSpannableBuilder>());

    /// <inheritdoc/>
    // Let it throw NRE of builderPool is null (disposed).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TextSpannableBuilder RentBuilder() => this.textSpannableBuilderPool!.Get();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnBuilder(TextSpannableBuilder? builder)
    {
        // Let it throw NRE of builderPool is null (disposed).
        if (builder != null)
            this.textSpannableBuilderPool!.Return(builder);
    }

    /// <inheritdoc/>
    public ImDrawListPtr RentDrawList(ImDrawListPtr template)
    {
        foreach (ref var x in this.drawListPool.AsSpan())
        {
            if (Interlocked.Exchange(ref x, 0) is var y && y != 0)
            {
                var res = new ImDrawListPtr(y);
                res._ResetForNewFrame();
                res._Data = template._Data;
                res._CmdHeader = template._CmdHeader with { VtxOffset = 0 };
                res.CmdBuffer[0].ClipRect = template._CmdHeader.ClipRect;
                res.CmdBuffer[0].TextureId = template._CmdHeader.TextureId;
                return res;
            }
        }

        var drawList = new ImDrawListPtr(ImGuiNative.ImDrawList_ImDrawList(template._Data))
        {
            _CmdHeader = template._CmdHeader with { VtxOffset = 0 },
        };
        drawList.AddDrawCmd();
        drawList.PushTextureID(template._CmdHeader.TextureId);
        return drawList;
    }

    /// <inheritdoc/>
    public void ReturnDrawList(ImDrawListPtr drawListPtr)
    {
        var y = (nint)drawListPtr.NativePtr;
        if (y == 0)
            return;

        foreach (ref var x in this.drawListPool.AsSpan())
        {
            if (Interlocked.CompareExchange(ref x, y, 0) is 0)
                return;
        }

        drawListPtr.Destroy();
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap? RentDrawListTexture(
        ImDrawListPtr drawListPtr,
        RectVector4 clipRect,
        Vector4 clearColor,
        Vector2 scale,
        out RectVector4 clipRectUv)
    {
        if (drawListPtr.CmdBuffer.Size == 0 || drawListPtr.IdxBuffer.Size == 0 || drawListPtr.VtxBuffer.Size == 0)
        {
            clipRectUv = default;
            return null;
        }

        foreach (ref var x in this.drawListTexturePool.AsSpan())
        {
            if (Interlocked.Exchange(ref x, null) is { } y)
            {
                if (y.Draw(drawListPtr, clipRect, clearColor, scale, out clipRectUv).SUCCEEDED)
                    return y;

                this.ReturnDrawListTexture(y);
                return null;
            }
        }

        var t = new DrawListTexture(
            Service<InterfaceManager.InterfaceManagerWithScene>.Get().Manager.Device!.NativePointer);
        if (t.Draw(drawListPtr, clipRect, clearColor, scale, out clipRectUv).SUCCEEDED)
            return t;

        this.ReturnDrawListTexture(t);
        return null;
    }

    /// <inheritdoc/>
    public void ReturnDrawListTexture(IDalamudTextureWrap? textureWrap)
    {
        if (textureWrap is not DrawListTexture y)
        {
            textureWrap?.Dispose();
            return;
        }

        this.returnToPoolLater.Add(y);
    }

    private void FrameworkOnUpdateReturnPooledObjects()
    {
        var i = 0;
        while (this.returnToPoolLater.TryTake(out var y))
        {
            for (; i < this.drawListTexturePool.Length; i++)
            {
                if (Interlocked.CompareExchange(ref this.drawListTexturePool[i], y, null) is null)
                {
                    i++;
                    y = null;
                    break;
                }
            }

            y?.Dispose();
        }
    }

    private void DisposePooledObjects()
    {
        foreach (ref var x in this.drawListPool.AsSpan())
        {
            if (x != 0)
                ImGuiNative.ImDrawList_destroy((ImDrawList*)x);
            x = 0;
        }

        foreach (ref var x in this.drawListTexturePool.AsSpan())
        {
            x?.Dispose();
            x = null;
        }

        foreach (var x in this.returnToPoolLater)
            x.Dispose();
        this.returnToPoolLater.Clear();
    }
}
