using System.Collections.Generic;
using System.Diagnostics;

using Dalamud.Interface.Utility;

using ImGuiNET;

using Microsoft.Extensions.ObjectPool;

using Serilog;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Reusable font push/popper.
/// </summary>
internal sealed class SimplePushedFont : IDisposable
{
    // Using constructor instead of DefaultObjectPoolProvider, since we do not want the pool to call Dispose.
    private static readonly ObjectPool<SimplePushedFont> Pool =
        new DefaultObjectPool<SimplePushedFont>(new DefaultPooledObjectPolicy<SimplePushedFont>());

    private List<IDisposable>? stack;
    private ImFontPtr font;

    /// <summary>
    /// Pushes the font, and return an instance of <see cref="SimplePushedFont"/>.
    /// </summary>
    /// <param name="stack">The <see cref="IFontHandle"/>-private stack.</param>
    /// <param name="fontPtr">The font pointer being pushed.</param>
    /// <returns>The rented instance of <see cref="SimplePushedFont"/>.</returns>
    public static SimplePushedFont Rent(List<IDisposable> stack, ImFontPtr fontPtr)
    {
        var rented = Pool.Get();
        Debug.Assert(rented.font.IsNull(), "Rented object must not have its font set");
        rented.stack = stack;

        if (fontPtr.IsNotNullAndLoaded())
        {
            rented.font = fontPtr;
            ImGui.PushFont(fontPtr);
        }

        return rented;
    }

    /// <inheritdoc />
    public unsafe void Dispose()
    {
        if (this.stack is null || !ReferenceEquals(this.stack[^1], this))
        {
            throw new InvalidOperationException("Tried to pop a non-pushed font.");
        }

        this.stack.RemoveAt(this.stack.Count - 1);

        if (!this.font.IsNull())
        {
            if (ImGui.GetFont().NativePtr == this.font.NativePtr)
            {
                ImGui.PopFont();
            }
            else
            {
                Log.Warning(
                    $"{nameof(IFontHandle.Pop)}: The font currently being popped does not match the pushed font. " +
                    $"Doing nothing.");
            }
        }

        this.font = default;
        this.stack = null;
        Pool.Return(this);
    }
}
