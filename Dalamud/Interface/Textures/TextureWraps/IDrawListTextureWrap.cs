using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps.Internal;

namespace Dalamud.Interface.Textures.TextureWraps;

/// <summary>A texture wrap that can be drawn using ImGui draw data.</summary>
public interface IDrawListTextureWrap : IDalamudTextureWrap
{
    /// <summary>Gets or sets the width of the texture.</summary>
    /// <remarks>If <see cref="Height"/> is to be set together, set use <see cref="Size"/> instead.</remarks>
    new int Width { get; set; }

    /// <summary>Gets or sets the width of the texture.</summary>
    /// <remarks>If <see cref="Width"/> is to be set together, set use <see cref="Size"/> instead.</remarks>
    new int Height { get; set; }

    /// <summary>Gets or sets the size of the texture.</summary>
    /// <remarks>Components will be rounded up.</remarks>
    new Vector2 Size { get; set; }

    /// <inheritdoc/>
    int IDalamudTextureWrap.Width => this.Width;

    /// <inheritdoc/>
    int IDalamudTextureWrap.Height => this.Height;

    /// <inheritdoc/>
    Vector2 IDalamudTextureWrap.Size => this.Size;

    /// <summary>Gets or sets the color to use when clearing this texture.</summary>
    /// <value>Color in RGBA. Defaults to <see cref="Vector4.Zero"/>, which is full transparency.</value>
    Vector4 ClearColor { get; set; }

    /// <summary>Draws a draw list to this texture.</summary>
    /// <param name="drawListPtr">Draw list to draw from.</param>
    /// <param name="displayPos">Left-top coordinates of the draw commands in the draw list.</param>
    /// <param name="scale">Scale to apply to all draw commands in the draw list.</param>
    /// <remarks>This function can be called only from the main thread.</remarks>
    void Draw(ImDrawListPtr drawListPtr, Vector2 displayPos, Vector2 scale);

    /// <inheritdoc cref="DrawListTextureWrap.Draw(ImDrawDataPtr)"/>
    void Draw(scoped in ImDrawData drawData);

    /// <summary>Draws from a draw data to this texture.</summary>
    /// <param name="drawData">Draw data to draw.</param>
    /// <remarks><ul>
    /// <li>Texture size will be kept as specified in <see cref="Size"/>. <see cref="ImDrawData.DisplaySize"/> will be
    /// used only as shader parameters.</li>
    /// <li>This function can be called only from the main thread.</li>
    /// </ul></remarks>
    void Draw(ImDrawDataPtr drawData);

    /// <summary>Resizes this texture and draws an ImGui window.</summary>
    /// <param name="windowName">Name and ID of the window to draw. Use the value that goes into
    /// <see cref="ImGui.Begin(ImU8String, ImGuiWindowFlags)"/>.</param>
    /// <param name="scale">Scale to apply to all draw commands in the draw list.</param>
    void ResizeAndDrawWindow(ReadOnlySpan<char> windowName, Vector2 scale);
}
