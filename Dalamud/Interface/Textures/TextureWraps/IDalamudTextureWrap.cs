using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps.Internal;
using Dalamud.Utility;

using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Textures.TextureWraps;

/// <summary>
/// Base TextureWrap interface for all Dalamud-owned texture wraps.
/// Used to avoid referencing ImGuiScene.
/// </summary>
/// <remarks>If you want to implement this, see if you're actually wrapping an existing instance of
/// <see cref="IDalamudTextureWrap"/>; if you are, then use <see cref="ForwardingTextureWrap"/>.</remarks>
public interface IDalamudTextureWrap : IDisposable
{
    /// <summary>Gets a texture handle suitable for direct use with ImGui functions.</summary>
    ImTextureID Handle { get; }

    /// <summary>Gets a texture handle suitable for direct use with ImGui functions.</summary>
    [ImGuiBindingsToDo("Remove.")]
    IntPtr ImGuiHandle => new((long)this.Handle.Handle);

    /// <summary>Gets the width of the texture.</summary>
    int Width { get; }

    /// <summary>Gets the height of the texture.</summary>
    int Height { get; }

    /// <summary>Gets the size vector of the texture using Width, Height.</summary>
    Vector2 Size => new(this.Width, this.Height);

    /// <summary>
    /// Creates a new reference to the resource being pointed by this instance of <see cref="IDalamudTextureWrap"/>.
    /// </summary>
    /// <returns>The new reference to this texture wrap.</returns>
    /// <remarks>
    /// On calling this function, a new instance of <see cref="IDalamudTextureWrap"/> will be returned, but with
    /// the same <see cref="ImGuiHandle"/>. The new instance must be <see cref="IDisposable.Dispose"/>d, as the backing
    /// resource will stay alive until all the references are released. The old instance may be disposed as needed,
    /// once this function returns; the new instance will stay alive regardless of whether the old instance has been
    /// disposed.<br />
    /// Primary purpose of this function is to share textures across plugin boundaries. When texture wraps get passed
    /// across plugin boundaries for use for an indeterminate duration, the receiver should call this function to
    /// obtain a new reference to the texture received, so that it gets its own "copy" of the texture and the caller
    /// may dispose the texture anytime without any care for the receiver.<br />
    /// The default implementation will treat <see cref="ImGuiHandle"/> as an <see cref="IUnknown"/>.
    /// </remarks>
    unsafe IDalamudTextureWrap CreateWrapSharingLowLevelResource()
    {
        // Dalamud specific: IDalamudTextureWrap always points to an ID3D11ShaderResourceView.
        var handle = (IUnknown*)this.Handle.Handle;
        return new UnknownTextureWrap(handle, this.Width, this.Height, true);
    }
}
