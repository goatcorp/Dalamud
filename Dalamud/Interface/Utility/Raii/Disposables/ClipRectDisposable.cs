using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public sealed class ClipRectDisposable : IDisposable
{
    /// <summary> Gets the number of ClipRects currently pushed with this object. </summary>
    public int Count { get; private set; }

    /// <summary> Push a new global ClipRect. </summary>
    /// <param name="topLeft"> The top-left corner of the rectangle in screen coordinates. </param>
    /// <param name="bottomRight"> The bottom-right corner of the rectangle in screen coordinates. </param>
    /// <param name="intersectWithCurrentClipRect"> Whether to intersect the rectangle with the current existing ClipRect or to replace it. </param>
    /// <param name="condition"> If this is false, the ClipRect is not pushed. </param>
    /// <returns> A disposable object that can be used to push further ClipRects and pops those ClipRects after leaving scope. Use with using. </returns>
    /// <remarks> If you need to keep ClipRects pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
    [OverloadResolutionPriority(50)]
    public ClipRectDisposable Push(in Vector2 topLeft, in Vector2 bottomRight, bool intersectWithCurrentClipRect = false, bool condition = true)
        => condition ? this.Push(topLeft, bottomRight, intersectWithCurrentClipRect) : this;

    /// <inheritdoc cref="Push(in Vector2,in Vector2,bool,bool)"/>
    [OverloadResolutionPriority(100)]
    public ClipRectDisposable Push(in Vector2 topLeft, in Vector2 bottomRight, bool intersectWithCurrentClipRect = false)
    {
        ImGui.PushClipRect(topLeft, bottomRight, intersectWithCurrentClipRect);
        ++this.Count;
        return this;
    }

    /// <inheritdoc cref="Push(in Vector2,in Vector2,bool,bool)"/>
    /// <param name="rectangle"> The rectangle in screen coordinates. </param>
    /// <param name="intersectWithCurrentClipRect"> Whether to intersect the rectangle with the current existing ClipRect or to replace it. </param>
    /// <param name="condition"> If this is false, the ClipRect is not pushed. </param>
    [OverloadResolutionPriority(50)]
    public ClipRectDisposable Push(in Rectangle rectangle, bool intersectWithCurrentClipRect = false, bool condition = true)
        => condition ? this.Push(rectangle, intersectWithCurrentClipRect) : this;

    /// <inheritdoc cref="Push(in Rectangle,bool,bool)"/>
    [OverloadResolutionPriority(100)]
    public ClipRectDisposable Push(in Rectangle rectangle, bool intersectWithCurrentClipRect = false)
    {
        ImGui.PushClipRect(rectangle.Minimum, rectangle.Maximum, intersectWithCurrentClipRect);
        ++this.Count;
        return this;
    }

    /// <summary> Pop a number of ClipRects in this draw list. </summary>
    /// <param name="count"> The number of ClipRects to pop. This is clamped to the number of ClipRects pushed by this object. </param>
    public void Pop(int count = 1)
    {
        if (count is 0)
            return;

        count =  Math.Min(count, this.Count);
        this.Count -= count;
        while (count-- > 0)
            ImGui.PopClipRect();
    }

    /// <summary> Pop all ClipRects pushed with this object. </summary>
    public void Dispose()
        => this.Pop(this.Count);

#pragma warning disable SA1204
    /// <summary> Pop a number of ClipRects from the ClipRect stack without using an IDisposable. </summary>
    /// <param name="count"> The number of rects to pop. The number is not checked against the rect stack. </param>
    /// <remarks> Avoid using this function, and ClipRects across scopes, as much as possible. </remarks>
    public static void PopUnsafe(int count = 1)
    {
        while (count-- > 0)
            ImGui.PopClipRect();
    }
#pragma warning restore SA1204
}
