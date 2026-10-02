using System.Numerics;

using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Enums;
using Dalamud.NativeUi.Timelines;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Text.ReadOnly;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// Implementation of the games WindowNode. Not intended for external use.
/// </summary>
internal unsafe class WindowNode : WindowNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowNode"/> class.
    /// </summary>
    public WindowNode()
    {
        this.NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents;

        this.CollisionNode.NodeId = 13;
        this.CollisionNode.NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.Fill | NodeFlags.HasCollision | NodeFlags.EmitsEvents;

        Component->ShowFlags = 1;

        this.HeaderCollisionNode = new CollisionNode
        {
            Uses = 2,
            NodeId = 12,
            Size = new Vector2(0.0f, 28.0f),
            Position = new Vector2(8.0f, 8.0f),
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.AnchorRight |
                        NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.HasCollision | NodeFlags.RespondToMouse | NodeFlags.EmitsEvents,
        };
        this.HeaderCollisionNode.AttachNode(this);

        this.BackgroundTextureNode = new WindowBackgroundTextureNode(false)
        {
            NodeId = 11,
            Offsets = new Vector4(64.0f, 32.0f, 32.0f, 32.0f),
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft |
                        NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.Fill | NodeFlags.EmitsEvents,
            PartsRenderType = 19,
        };
        this.BackgroundTextureNode.AttachNode(this);

        this.BorderTextureNode = new WindowBackgroundTextureNode(true)
        {
            NodeId = 10,
            Offsets = new Vector4(64.0f, 32.0f, 32.0f, 32.0f),
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft |
                        NodeFlags.Enabled | NodeFlags.Fill | NodeFlags.EmitsEvents,
            PartsRenderType = 7,
        };
        this.BorderTextureNode.AttachNode(this);

        this.BackgroundImageNode = new SimpleImageNode
        {
            NodeId = 9,
            WrapMode = WrapMode.Stretch,
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.AnchorRight | NodeFlags.AnchorBottom |
                        NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TexturePath = "ui/uld/WindowA_Gradation.tex",
            TextureCoordinates = new Vector2(6.0f, 2.0f),
            TextureSize = new Vector2(24.0f, 24.0f),
        };
        this.BackgroundImageNode.AttachNode(this);

        this.HeaderContainerNode = new ResNode
        {
            NodeId = 2,
            Size = new Vector2(0.0f, 38.0f),
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.AnchorRight |
                        NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
        };
        this.HeaderContainerNode.AttachNode(this);

        this.DividingLineNode = new SimpleNineGridNode
        {
            NodeId = 8,
            TexturePath = "ui/uld/WindowA_Line.tex",
            TextureCoordinates = Vector2.Zero,
            TextureSize = new Vector2(32.0f, 4.0f),
            Size = new Vector2(0.0f, 4.0f),
            LeftOffset = 12.0f,
            RightOffset = 12.0f,
            Position = new Vector2(10.0f, 33.0f),
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.AnchorRight |
                        NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
        };
        this.DividingLineNode.AttachNode(this.HeaderContainerNode);

        this.CloseButtonNode = new TextureButtonNode
        {
            NodeId = 7,
            Size = new Vector2(28.0f, 28.0f),
            Position = new Vector2(0.0f, 6.0f),
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorRight |
                        NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TexturePath = "ui/uld/WindowA_Button.tex",
            TextureCoordinates = new Vector2(0.0f, 0.0f),
            TextureSize = new Vector2(28.0f, 28.0f),
        };
        this.CloseButtonNode.AttachNode(this.HeaderContainerNode);

        this.ConfigurationButtonNode = new TextureButtonNode
        {
            NodeId = 6,
            Size = new Vector2(16.0f, 16.0f),
            Position = new Vector2(0.0f, 8.0f),
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorRight |
                        NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TexturePath = "ui/uld/WindowA_Button.tex",
            TextureCoordinates = new Vector2(44.0f, 0.0f),
            TextureSize = new Vector2(16.0f, 16.0f),
        };
        this.ConfigurationButtonNode.AttachNode(this.HeaderContainerNode);

        this.InformationButtonNode = new TextureButtonNode
        {
            NodeId = 5,
            Size = new Vector2(16.0f, 16.0f),
            Position = new Vector2(0.0f, 8.0f),
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorRight |
                        NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TexturePath = "ui/uld/WindowA_Button.tex",
            TextureCoordinates = new Vector2(28.0f, 0.0f),
            TextureSize = new Vector2(16.0f, 16.0f),
        };
        this.InformationButtonNode.AttachNode(this.HeaderContainerNode);

        this.SubtitleNode = new TextNode
        {
            NodeId = 4,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Left,
            FontSize = 12,
            FontType = FontType.Axis,
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft |
                        NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TextColor = NativeThemeColorHelper.GetColor(3),
            TextOutlineColor = NativeThemeColorHelper.GetColor(6),
            BackgroundColor = Vector4.Zero,
            Size = new Vector2(46.0f, 20.0f),
            Position = new Vector2(83.0f, 17.0f),
        };
        this.SubtitleNode.AttachNode(this.HeaderContainerNode);

        this.TitleNode = new TextNode
        {
            NodeId = 3,
            LineSpacing = 23,
            AlignmentType = AlignmentType.Left,
            FontSize = 23,
            FontType = FontType.TrumpGothic,
            TextFlags = TextFlags.AutoAdjustNodeSize,
            NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft |
                        NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TextColor = NativeThemeColorHelper.GetColor(2),
            TextOutlineColor = NativeThemeColorHelper.GetColor(7),
            BackgroundColor = Vector4.Zero,
            Size = new Vector2(86.0f, 31.0f),
            Position = new Vector2(12.0f, 7.0f),
        };
        this.TitleNode.AttachNode(this.HeaderContainerNode);

        Data->ShowCloseButton = 1;
        Data->ShowConfigButton = 0;
        Data->ShowHelpButton = 0;
        Data->ShowHeader = 1;
        Data->Nodes[0] = this.TitleNode.NodeId;
        Data->Nodes[1] = this.SubtitleNode.NodeId;
        Data->Nodes[2] = this.CloseButtonNode.NodeId;
        Data->Nodes[3] = this.ConfigurationButtonNode.NodeId;
        Data->Nodes[4] = this.InformationButtonNode.NodeId;
        Data->Nodes[5] = 0;
        Data->Nodes[6] = this.HeaderContainerNode.NodeId;
        Data->Nodes[7] = 0;

        this.LoadTimelines();

        this.InitializeComponentEvents();
    }

    /// <summary>
    /// Gets the background image node.
    /// </summary>
    public ImageNode BackgroundImageNode { get; }

    /// <summary>
    /// Gets the background base texture node.
    /// </summary>
    public WindowBackgroundTextureNode BackgroundTextureNode { get; }

    /// <summary>
    /// Gets the border texture node.
    /// </summary>
    public WindowBackgroundTextureNode BorderTextureNode { get; }

    /// <summary>
    /// Gets the close button node.
    /// </summary>
    public TextureButtonNode CloseButtonNode { get; }

    /// <summary>
    /// Gets the configuration button node.
    /// </summary>
    public TextureButtonNode ConfigurationButtonNode { get; }

    /// <summary>
    /// Gets the dividing line node.
    /// </summary>
    public SimpleNineGridNode DividingLineNode { get; }

    /// <summary>
    /// Gets the header collision node.
    /// </summary>
    public CollisionNode HeaderCollisionNode { get; }

    /// <summary>
    /// Gets the header container node.
    /// </summary>
    public ResNode HeaderContainerNode { get; }

    /// <summary>
    /// Gets the info button node.
    /// </summary>
    public TextureButtonNode InformationButtonNode { get; }

    /// <summary>
    /// Gets the subtitle text node.
    /// </summary>
    public TextNode SubtitleNode { get; }

    /// <summary>
    /// Gets the title text node.
    /// </summary>
    public TextNode TitleNode { get; }

    /// <summary>
    /// Gets or sets the reference to the owning addon.
    /// </summary>
    public AtkUnitBase* OwnerAddon
    {
        get => Component->OwnerUnitBase;
        set => Component->OwnerUnitBase = value;
    }

    /// <summary>
    /// Gets or sets the title text.
    /// </summary>
    public ReadOnlySeString Title
    {
        get => this.TitleNode.String;
        set
        {
            this.TitleNode.String = value;
            this.TitleNode.IsVisible = true;
        }
    }

    /// <summary>
    /// Gets or sets the subtitle text.
    /// </summary>
    public ReadOnlySeString Subtitle
    {
        get => this.SubtitleNode.String;
        set
        {
            this.SubtitleNode.String = value;
            this.SubtitleNode.IsVisible = true;
            this.SubtitleNode.X = this.TitleNode.X + this.TitleNode.Width + 2.0f;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the close button is visible.
    /// </summary>
    public bool ShowCloseButton
    {
        get => this.CloseButtonNode.IsVisible;
        set => this.CloseButtonNode.IsVisible = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the gear button is visible.
    /// </summary>
    /// <remarks>
    /// Seems to be unused by the game.
    /// </remarks>
    public bool ShowConfigButton
    {
        get => this.ConfigurationButtonNode.IsVisible;
        set => this.ConfigurationButtonNode.IsVisible = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the help button is visible.
    /// </summary>
    /// <remarks>
    /// Seems to only be used in very specific cases by the game.
    /// </remarks>
    public bool ShowHelpButton
    {
        get => this.InformationButtonNode.IsVisible;
        set => this.InformationButtonNode.IsVisible = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the border node is visible.
    /// </summary>
    public bool Focused
    {
        get => this.BorderTextureNode.IsVisible;
        set => this.BorderTextureNode.IsVisible = value;
    }

    /// <inheritdoc/>
    public override float HeaderHeight
        => this.HeaderContainerNode.Height;

    /// <inheritdoc/>
    public override Vector2 ContentSize
        => new(this.BackgroundImageNode.Width, this.BackgroundImageNode.Height - this.HeaderHeight);

    /// <inheritdoc/>
    public override Vector2 ContentStartPosition
        => new(this.BackgroundImageNode.X, this.BackgroundImageNode.Y + this.HeaderHeight);

    /// <inheritdoc/>
    public override ResNode WindowHeaderFocusNode
        => this.HeaderContainerNode;

    /// <inheritdoc/>
    public override void SetTitle(string title, string? subtitle = null)
    {
        base.SetTitle(title, subtitle);
        this.SubtitleNode.Position = new Vector2(this.TitleNode.Bounds.Right + 4.0f, this.SubtitleNode.Y);
    }

    /// <inheritdoc />
    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        this.HeaderContainerNode.Width = this.Width;
        this.HeaderCollisionNode.Width = this.Width - 14.0f;
        this.BackgroundTextureNode.Size = this.Size;
        this.BorderTextureNode.Size = this.Size;
        this.BackgroundImageNode.Size = new Vector2(this.Width - 8.0f, this.Height - 16.0f);
        this.BackgroundImageNode.Position = new Vector2(4.0f, 4.0f);

        this.CloseButtonNode.X = this.Width - 33.0f;
        this.ConfigurationButtonNode.X = this.Width - 47.0f;
        this.InformationButtonNode.X = this.Width - 61.0f;
        this.DividingLineNode.Width = this.Width - 20.0f;
    }

    private void LoadTimelines()
    {
        this.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 29)
            .AddLabelPair(1, 9, 17)
            .AddLabelPair(10, 19, 18)
            .AddLabelPair(20, 29, 7)
            .EndFrameSet()
            .Build());

        this.BackgroundTextureNode.AddTimeline(new TimelineBuilder()
                                               .AddFrameSetWithFrame(1, 9, 1, multiplyColor: new Vector3(100.0f))
                                               .AddFrameSetWithFrame(10, 19, 10, multiplyColor: new Vector3(100.0f))
                                               .AddFrameSetWithFrame(20, 29, 20, multiplyColor: new Vector3(50.0f))
                                               .Build());

        this.BorderTextureNode.AddTimeline(new TimelineBuilder()
                                           .BeginFrameSet(10, 19)
                                           .AddFrame(10, alpha: 0)
                                           .AddFrame(12, alpha: 255)
                                           .EndFrameSet()
                                           .Build());
    }
}
