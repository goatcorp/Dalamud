using System.Numerics;

using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Extensions;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// Simplified implementation of <see cref="NineGridNode"/>.
/// </summary>
internal unsafe class SimpleNineGridNode : NineGridNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleNineGridNode"/> class.
    /// </summary>
    public SimpleNineGridNode()
    {
        this.PartsList.Add(new Part());
    }

    /// <summary>
    /// Gets or sets the textures U coordinate.
    /// </summary>
    public float U
    {
        get => this.PartsList[0]->U;
        set => this.PartsList[0]->U = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the textures V coordinate.
    /// </summary>
    public float V
    {
        get => this.PartsList[0]->V;
        set => this.PartsList[0]->V = (ushort)value;
    }

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
    /// Gets or sets the textures width.
    /// </summary>
    public float TextureWidth
    {
        get => this.PartsList[0]->Width;
        set => this.PartsList[0]->Width = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the textures height.
    /// </summary>
    public float TextureHeight
    {
        get => this.PartsList[0]->Height;
        set => this.PartsList[0]->Height = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the textures size.
    /// </summary>
    public Vector2 TextureSize
    {
        get => new(this.TextureWidth, this.TextureHeight);
        set
        {
            this.TextureWidth = value.X;
            this.TextureHeight = value.Y;
        }
    }

    /// <summary>
    /// Gets or sets the textures path.
    /// </summary>
    /// <remarks>
    /// When setting loads the texture from the game or filesystem.
    /// </remarks>
    public string TexturePath
    {
        get => this.PartsList[0]->LoadedPath;
        set => this.PartsList[0]->LoadTexture(value);
    }
}
