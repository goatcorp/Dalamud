using System.Numerics;
using System.Text;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

using ImGuiNET;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Textures;

/// <summary>Describes how to take a texture of an existing ImGui viewport.</summary>
public record struct ImGuiViewportTextureArgs()
{
    /// <summary>Gets or sets the ImGui Viewport ID to capture.</summary>
    /// <remarks>Use <see cref="ImGuiViewport.ID"/> from <see cref="ImGui.GetMainViewport"/> to take the main viewport,
    /// where the game renders to.</remarks>
    public uint ViewportId { get; set; }

    /// <summary>Gets or sets a value indicating whether to automatically update the texture.</summary>
    /// <remarks>Enabling this will also update <see cref="IDalamudTextureWrap.Size"/> as needed.</remarks>
    public bool AutoUpdate { get; set; }

    /// <summary>Gets or sets a value indicating whether to get the texture before rendering ImGui.</summary>
    /// <remarks>It probably makes no sense to enable this unless <see cref="ViewportId"/> points to the main viewport.
    /// </remarks>
    public bool TakeBeforeImGuiRender { get; set; }

    /// <summary>Gets or sets a value indicating whether to keep the transparency.</summary>
    /// <remarks>
    /// <para>If <c>true</c>, then the alpha channel values will be filled with 1.0.</para>
    /// <para>Keep in mind that screen captures generally do not need alpha values.</para>
    /// </remarks>
    // Intentionally not "MakeOpaque", to accommodate the use of default value of this record struct.
    public bool KeepTransparency { get; set; } = false;

    /// <summary>Gets or sets the left top coordinates relative to the size of the source texture.</summary>
    /// <para>Coordinates should be in range between 0 and 1.</para>
    public Vector2 Uv0 { get; set; } = Vector2.Zero;

    /// <summary>Gets or sets the right bottom coordinates relative to the size of the source texture.</summary>
    /// <para>Coordinates should be in range between 0 and 1.</para>
    /// <remarks>If set to <see cref="Vector2.Zero"/>, then it will be interpreted as <see cref="Vector2.One"/>,
    /// to accommodate the use of default value of this record struct.</remarks>
    public Vector2 Uv1 { get; set; } = Vector2.One;

    /// <summary>Gets the effective value of <see cref="Uv1"/>.</summary>
    internal Vector2 Uv1Effective => this.Uv1 == Vector2.Zero ? Vector2.One : this.Uv1;

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(nameof(ImGuiViewportTextureArgs)).Append('(');
        sb.Append($"0x{this.ViewportId:X}");
        if (this.AutoUpdate)
            sb.Append($", {nameof(this.AutoUpdate)}");
        if (this.TakeBeforeImGuiRender)
            sb.Append($", {nameof(this.TakeBeforeImGuiRender)}");
        if (this.KeepTransparency)
            sb.Append($", {nameof(this.KeepTransparency)}");

        if (this.Uv0 != Vector2.Zero || this.Uv1Effective != Vector2.One)
        {
            sb.Append(", ")
              .Append(this.Uv0.ToString())
              .Append('-')
              .Append(this.Uv1.ToString());
        }

        return sb.Append(')').ToString();
    }

    /// <summary>Checks the properties and throws an exception if values are invalid.</summary>
    internal void ThrowOnInvalidValues()
    {
        if (this.Uv0.X is < 0 or > 1 or float.NaN)
            throw new ArgumentException($"{nameof(this.Uv0)}.X is out of range.");

        if (this.Uv0.Y is < 0 or > 1 or float.NaN)
            throw new ArgumentException($"{nameof(this.Uv0)}.Y is out of range.");

        if (this.Uv1Effective.X is < 0 or > 1 or float.NaN)
            throw new ArgumentException($"{nameof(this.Uv1)}.X is out of range.");

        if (this.Uv1Effective.Y is < 0 or > 1 or float.NaN)
            throw new ArgumentException($"{nameof(this.Uv1)}.Y is out of range.");

        if (this.Uv0.X >= this.Uv1Effective.X || this.Uv0.Y >= this.Uv1Effective.Y)
        {
            throw new ArgumentException(
                $"{nameof(this.Uv0)} must be strictly less than {nameof(this.Uv1)} in a componentwise way.");
        }
    }
}
