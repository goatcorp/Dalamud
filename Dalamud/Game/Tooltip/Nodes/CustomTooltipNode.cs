using Dalamud.Game.Tooltip.Entries;
using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Nodes;
using Dalamud.NativeUi.Nodes.Layout;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Text.ReadOnly;

using ClientStructs = FFXIVClientStructs.FFXIV.Component.GUI;
using Vector2 = System.Numerics.Vector2;

namespace Dalamud.Game.Tooltip.Nodes;

/// <summary>
/// Custom node representing a plugins added tooltip information.
/// </summary>
internal class CustomTooltipNode : ResNode
{
    private readonly WindowBackgroundTextureNode backgroundTextureNode;
    private readonly TextNode pluginSourceNode;
    private readonly VerticalListNode contentsNode;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomTooltipNode"/> class.
    /// </summary>
    public CustomTooltipNode()
    {
        this.backgroundTextureNode = new WindowBackgroundTextureNode(false, "ui/uld/WindowF_Bg")
        {
            PartsRenderType = 3,
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft |
                        NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.Fill | NodeFlags.EmitsEvents,
        };
        this.backgroundTextureNode.AttachNode(this);

        this.contentsNode = new VerticalListNode
        {
            Width = 342.0f,
            Position = new Vector2(16.0f, 9.0f),
            FitContents = true,
            ClipListContents = true,
        };
        this.contentsNode.AttachNode(this);

        this.pluginSourceNode = new TextNode
        {
            Size = new Vector2(342.0f, 20.0f),
            Position = new Vector2(16.0f, 9.0f),
            AlignmentType = ClientStructs.AlignmentType.Right,
            FontSize = 10,
            String = "SourcePluginNotSet",
            TextColor = NativeThemeColorHelper.GetColor(3),
        };
        this.pluginSourceNode.AttachNode(this);
    }

    /// <summary>
    /// Gets or sets the string used to display the plugin source.
    /// </summary>
    public ReadOnlySeString SourcePluginName
    {
        get => this.pluginSourceNode.String;
        set => this.pluginSourceNode.String = value;
    }

    /// <summary>
    /// Adds the specified tooltip entry to this tooltip node.
    /// </summary>
    /// <param name="entry">Tooltip entry data.</param>
    public void AddEntry(TooltipEntry entry)
    {
        switch (entry)
        {
            case StringTooltipEntry { String: var text, Options: { } textOptions }:
                var newTextNode = new TextNode
                {
                    Width = 342.0f,
                    String = text,
                    TextColor = textOptions.TextColor,
                    TextOutlineColor = textOptions.TextOutlineColor,
                    AlignmentType = (ClientStructs.AlignmentType)textOptions.TextAlignment,
                    FontType = (ClientStructs.FontType)textOptions.FontType,
                    TextFlags = (ClientStructs.TextFlags)textOptions.TextFlags,
                    FontSize = textOptions.TextSize,
                    LineSpacing = textOptions.LineSpacing,
                    CharSpacing = textOptions.CharacterSpacing,
                };

                newTextNode.Height = newTextNode.GetTextDrawSize().Y;
                this.contentsNode.AddNode(newTextNode);
                break;

            case ImageTooltipEntry { ImagePath: var path, Options: { } imageOptions, Size: var size }:
                var newImageNode = new ImGuiImageNode
                {
                    Size = size,
                    FitTexture = true,
                    Color = imageOptions.Color,
                    TexturePath = path,
                };

                this.contentsNode.AddNode(newImageNode);
                break;

            case IconTooltipEntry { IconId: var iconId, Options: { } iconOptions, Size: var size }:
                var newIconNode = new IconImageNode
                {
                    IconId = iconId,
                    FitTexture = true,
                    Size = size,
                    Color = iconOptions.Color,
                };

                this.contentsNode.AddNode(newIconNode);
                break;
        }

        this.Size = new Vector2(376.0f, this.contentsNode.Height + 18.0f);
    }

    /// <inheritdoc/>
    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        this.backgroundTextureNode.Size = this.Size;
    }
}
