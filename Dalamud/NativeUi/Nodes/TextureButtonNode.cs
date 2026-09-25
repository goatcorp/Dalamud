using System.Numerics;

using Dalamud.NativeUi.Enums;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// Specialization of a button that allows setting a texture part as a button.
/// </summary>
internal class TextureButtonNode : ButtonBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextureButtonNode"/> class.
    /// </summary>
    public TextureButtonNode()
    {
        this.ImageNode = new ImGuiImageNode
        {
            WrapMode = WrapMode.Stretch,
        };
        this.ImageNode.AttachNode(this);

        this.LoadTimelines();

        this.InitializeComponentEvents();
    }

    /// <summary>
    /// Gets the inner image node.
    /// </summary>
    public SimpleImageNode ImageNode { get; }

    /// <summary>
    /// Gets or sets the texture path used for the image.
    /// </summary>
    public string TexturePath
    {
        get => this.ImageNode.TexturePath;
        set => this.ImageNode.TexturePath = value;
    }

    /// <summary>
    /// Gets or sets the UV coordinates of the texture.
    /// </summary>
    public Vector2 TextureCoordinates
    {
        get => this.ImageNode.TextureCoordinates;
        set => this.ImageNode.TextureCoordinates = value;
    }

    /// <summary>
    /// Gets or sets the texture Width/Height.
    /// </summary>
    public Vector2 TextureSize
    {
        get => this.ImageNode.TextureSize;
        set => this.ImageNode.TextureSize = value;
    }

    /// <inheritdoc />
    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        this.ImageNode.Size = this.Size;
    }

    private void LoadTimelines()
        => LoadTwoPartTimelines(this, this.ImageNode);
}
