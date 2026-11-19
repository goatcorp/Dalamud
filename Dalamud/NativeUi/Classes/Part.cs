using System.Numerics;

namespace Dalamud.NativeUi.Classes;

/// <summary>
/// Wrapper around a AtkUldPart.
/// </summary>
internal class Part
{
    /// <summary>
    /// Gets or sets the texture part's width.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Gets or sets the texture part's height.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Gets or sets the textures parts width and height.
    /// </summary>
    public Vector2 Size
    {
        get => new(this.Width, this.Height);
        set
        {
            this.Width = value.X;
            this.Height = value.Y;
        }
    }

    /// <summary>
    /// Gets or sets the textures x-coordinate.
    /// </summary>
    public float U { get; set; }

    /// <summary>
    /// Gets or sets the textures y-coordinate.
    /// </summary>
    public float V { get; set; }

    /// <summary>
    /// Gets or sets the textures coordinates.
    /// </summary>
    public Vector2 TextureCoordinates
    {
        get => new(this.U, this.V);
        set
        {
            this.U = value.X;
            this.V = value.Y;
        }
    }

    /// <summary>
    /// Gets or sets this textures part Id.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets this textures parts texture path.
    /// </summary>
    public string TexturePath { get; set; } = string.Empty;
}
