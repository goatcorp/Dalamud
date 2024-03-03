using System.Numerics;

using Dalamud.Plugin.Services;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Textures;

/// <summary>Describes how to modify an existing texture.</summary>
public record struct ExistingTextureModificationArgs()
{
    /// <summary>Gets or sets a value indicating whether to make the texture opaque.</summary>
    /// <remarks>If <c>true</c>, then the alpha channel values will be filled with 1.0.</remarks>
    public bool MakeOpaque { get; set; } = false;

    /// <summary>Gets or sets the new DXGI format.</summary>
    /// <remarks>
    /// <para>Set to 0 (<see cref="DXGI_FORMAT.DXGI_FORMAT_UNKNOWN"/>) to use the source pixel format.</para>
    /// <para>Supported values can be queried with
    /// <see cref="ITextureProvider.IsDxgiFormatSupportedForCreateFromExistingTextureAsync"/>. This may not necessarily
    /// match <see cref="ITextureProvider.IsDxgiFormatSupported"/>.
    /// </para></remarks>
    public int DxgiFormat { get; set; } = (int)DXGI_FORMAT.DXGI_FORMAT_UNKNOWN;

    /// <summary>Gets or sets the new width.</summary>
    /// <remarks>Set to 0 to automatically calculate according to the original texture size, <see cref="Uv0"/>, and
    /// <see cref="Uv1"/>.</remarks>
    public int NewWidth { get; set; }

    /// <summary>Gets or sets the new height.</summary>
    /// <remarks>Set to 0 to automatically calculate according to the original texture size, <see cref="Uv0"/>, and
    /// <see cref="Uv1"/>.</remarks>
    public int NewHeight { get; set; }

    /// <summary>Gets or sets the left top coordinates relative to the size of the source texture.</summary>
    /// <para>Coordinates should be in range between 0 and 1.</para>
    public Vector2 Uv0 { get; set; } = Vector2.Zero;

    /// <summary>Gets or sets the right bottom coordinates relative to the size of the source texture.</summary>
    /// <para>Coordinates should be in range between 0 and 1.</para>
    /// <remarks>If set to <see cref="Vector2.Zero"/>, then it will be interpreted as <see cref="Vector2.One"/>,
    /// to accommodate the use of default value of this record struct.</remarks>
    public Vector2 Uv1 { get; set; } = Vector2.One;

    /// <summary>Gets or sets the format (typed).</summary>
    internal DXGI_FORMAT Format
    {
        get => (DXGI_FORMAT)this.DxgiFormat;
        set => this.DxgiFormat = (int)value;
    }

    /// <summary>Gets the effective value of <see cref="Uv1"/>.</summary>
    internal Vector2 Uv1Effective => this.Uv1 == Vector2.Zero ? Vector2.One : this.Uv1;

    /// <summary>Test if this instance of <see cref="ExistingTextureModificationArgs"/> does not instruct to change the
    /// underlying data of a texture.</summary>
    /// <param name="desc">The texture description to test against.</param>
    /// <returns><c>true</c> if this instance of <see cref="ExistingTextureModificationArgs"/> does not instruct to
    /// change the underlying data of a texture.</returns>
    internal bool IsCompleteSourceCopy(in D3D11_TEXTURE2D_DESC desc) =>
        this.Uv0 == Vector2.Zero
        && this.Uv1 == Vector2.One
        && (this.NewWidth == 0 || this.NewWidth == desc.Width)
        && (this.NewHeight == 0 || this.NewHeight == desc.Height)
        && !this.MakeOpaque
        && (this.Format == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN || this.Format == desc.Format);

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

        if (this.NewWidth < 0)
            throw new ArgumentException($"{nameof(this.NewWidth)} cannot be a negative number.");

        if (this.NewHeight < 0)
            throw new ArgumentException($"{nameof(this.NewHeight)} cannot be a negative number.");
    }
}
