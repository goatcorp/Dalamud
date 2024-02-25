using System.Numerics;

using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Base TextureWrap interface for all Dalamud-owned texture wraps.
/// Used to avoid referencing ImGuiScene.
/// </summary>
public interface IDalamudTextureWrap : IDisposable, ICloneable
{
    /// <summary>
    /// Gets a texture handle suitable for direct use with ImGui functions.
    /// </summary>
    IntPtr ImGuiHandle { get; }

    /// <summary>
    /// Gets the width of the texture.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the texture.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the size vector of the texture using Width, Height.
    /// </summary>
    Vector2 Size => new(this.Width, this.Height);

    /// <summary>
    /// Creates a new reference to this texture wrap.
    /// </summary>
    /// <returns>The new reference to this texture wrap.</returns>
    /// <remarks>The default implementation will treat <see cref="ImGuiHandle"/> as an <see cref="IUnknown"/>.</remarks>
    new unsafe IDalamudTextureWrap Clone()
    {
        // Dalamud specific: IDalamudTextureWrap always points to an ID3D11ShaderResourceView.
        var handle = (IUnknown*)this.ImGuiHandle;
        return new UnknownTextureWrap(handle, this.Width, this.Height, true);
    }

    /// <inheritdoc />
    object ICloneable.Clone() => this.Clone();
}
