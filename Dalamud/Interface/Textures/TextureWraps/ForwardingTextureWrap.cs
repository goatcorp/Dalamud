using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps.Internal;

using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Textures.TextureWraps;

/// <summary>Base class for implementations of <see cref="IDalamudTextureWrap"/> that forwards to another.</summary>
public abstract class ForwardingTextureWrap : IDalamudTextureWrap
{
    /// <inheritdoc/>
    public IntPtr ImGuiHandle
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.GetWrap().ImGuiHandle;
    }

    /// <inheritdoc/>
    public int Width
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.GetWrap().Width;
    }

    /// <inheritdoc/>
    public int Height
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.GetWrap().Height;
    }

    /// <inheritdoc/>
    public Vector2 Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this.Width, this.Height);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public virtual unsafe IDalamudTextureWrap CreateWrapSharingLowLevelResource()
    {
        // Dalamud specific: IDalamudTextureWrap always points to an ID3D11ShaderResourceView.
        var handle = (IUnknown*)this.ImGuiHandle;
        return new UnknownTextureWrap(handle, this.Width, this.Height, true);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{this.GetType()}({(this.TryGetWrap(out var wrap) ? wrap : null)})";

    /// <summary>Called on <see cref="IDisposable.Dispose"/>.</summary>
    /// <param name="disposing"><c>true</c> if called from <see cref="IDisposable.Dispose"/>.</param>
    /// <remarks>
    /// <para>Base implementation will not dispose the result of <see cref="TryGetWrap"/>.</para>
    /// <para>If you need to implement a finalizer, then make it call this function with <c>false</c>.</para>
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <summary>Gets the inner wrap.</summary>
    /// <param name="wrap">The inner wrap.</param>
    /// <returns><c>true</c> if not disposed and <paramref name="wrap"/> is available.</returns>
    protected abstract bool TryGetWrap([NotNullWhen(true)] out IDalamudTextureWrap? wrap);

    /// <summary>Gets the inner wrap.</summary>
    /// <returns>The inner wrap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IDalamudTextureWrap GetWrap() =>
        this.TryGetWrap(out var wrap) ? wrap : throw new ObjectDisposedException(this.GetType().Name);
}
