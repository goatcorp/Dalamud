using System.Numerics;

using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Extensions;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// Simplified implementation of an <see cref="ImageNode"/> meant to represent a single texture.
/// </summary>
/// <remarks>
/// This node is not intended to be used with multiple <see cref="Part" />'s.
/// </remarks>
internal unsafe class SimpleImageNode : ImageNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleImageNode"/> class.
    /// </summary>
    public SimpleImageNode()
    {
        this.PartsList.Add(new Part());
    }

    /// <summary>
    /// Gets or sets the textures U position.
    /// </summary>
    public float U
    {
        get => this.PartsList[0]->U;
        set => this.PartsList[0]->U = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the textures V position.
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
    /// Gets or sets the textures length.
    /// </summary>
    public float TextureHeight
    {
        get => this.PartsList[0]->Height;
        set => this.PartsList[0]->Height = (ushort)value;
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
    /// Setting this will cause the texture to be loaded from game or from filesystem.
    /// </remarks>
    public virtual string TexturePath
    {
        get => this.PartsList[0]->LoadedPath;
        set => this.PartsList[0]->LoadTexture(value);
    }

    /// <summary>
    /// Gets the textures actual size.
    /// </summary>
    /// <remarks>
    /// Is Vector2.Zero when texture is invalid or not ready.
    /// </remarks>
    public Vector2 ActualTextureSize
        => this.PartsList[0]->LoadedTextureSize;

    /// <summary>
    /// Loads a texture of the given path, optionally with theme resolution disabled.
    /// </summary>
    /// <param name="path">Texture path to load. (Omit _hr1 as that's resolved automatically).</param>
    /// <param name="resolveTheme">If the current game theme should be resolved.</param>
    public void LoadTexture(string path, bool resolveTheme = true)
        => this.PartsList[0]->LoadTexture(path, resolveTheme);

    /// <summary>
    /// Loads a specified iconId directly.
    /// </summary>
    /// <param name="iconId">Icon id to load.</param>
    public void LoadIcon(uint iconId)
        => this.PartsList[0]->LoadIcon(iconId);
}
