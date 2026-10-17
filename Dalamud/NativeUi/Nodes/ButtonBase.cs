using System.Numerics;

using Dalamud.NativeUi.BaseTypes.Component;
using Dalamud.NativeUi.BaseTypes.Node;
using Dalamud.NativeUi.Timelines;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// Implementation of the games ButtonNode and associated component.
/// </summary>
/// <remarks>
/// This is implemented as an abstract base class to make the various button implementations simpler.
/// </remarks>
internal abstract unsafe class ButtonBase : ComponentNode<AtkComponentButton, AtkUldComponentDataButton>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ButtonBase"/> class.
    /// </summary>
    protected ButtonBase()
    {
        this.SetInternalComponentType(ComponentType.Button);
        this.AddEvent(AtkEventType.ButtonClick, this.ClickHandler);
    }

    /// <summary>
    /// Gets or sets action that is invoked when the button is clicked.
    /// </summary>
    public Action? OnClick { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the button is considered checked.
    /// </summary>
    /// <remarks>
    /// Not quite sure what this is meant to do.
    /// </remarks>
    public bool IsChecked
    {
        get => Component->IsChecked;
        set => Component->SetChecked(value);
    }

    /// <summary>
    /// Loads timelines for a button with two parts.
    /// </summary>
    /// <param name="parent">Parent node.</param>
    /// <param name="foreground">Foreground Node.</param>
    protected static void LoadTwoPartTimelines(NodeBase parent, NodeBase foreground)
    {
        parent.AddTimeline(new TimelineBuilder()
                           .BeginFrameSet(1, 59)
                           .AddLabelPair(1, 9, 1)
                           .AddLabelPair(10, 19, 2)
                           .AddLabelPair(20, 29, 3)
                           .AddLabelPair(30, 39, 7)
                           .AddLabelPair(40, 49, 6)
                           .AddLabelPair(50, 59, 4)
                           .EndFrameSet()
                           .Build());

        foreground.AddTimeline(new TimelineBuilder()
                               .AddFrameSetWithFrame(1, 9, 1, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f))
                               .BeginFrameSet(10, 19)
                               .AddFrame(10, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f))
                               .AddFrame(12, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f), addColor: new Vector3(16.0f))
                               .EndFrameSet()
                               .AddFrameSetWithFrame(20, 29, 20, new Vector2(0.0f, 1.0f), 255, multiplyColor: new Vector3(100.0f), addColor: new Vector3(16.0f))
                               .AddFrameSetWithFrame(30, 39, 30, Vector2.Zero, 178, multiplyColor: new Vector3(50.0f))
                               .AddFrameSetWithFrame(40, 49, 40, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f), addColor: new Vector3(16.0f))
                               .BeginFrameSet(50, 59)
                               .AddFrame(50, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f), addColor: new Vector3(16.0f))
                               .AddFrame(52, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f))
                               .EndFrameSet()
                               .AddFrameSetWithFrame(130, 139, 130, Vector2.Zero, 255, new Vector3(16.0f), new Vector3(100.0f))
                               .AddFrameSetWithFrame(140, 149, 140, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f))
                               .AddFrameSetWithFrame(150, 159, 150, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f))
                               .Build());
    }

    /// <summary>
    /// Loads a timeline for a button with three parts.
    /// </summary>
    /// <param name="parent">Parent node.</param>
    /// <param name="background">Background Node.</param>
    /// <param name="foreground">Foreground Node.</param>
    /// <param name="foregroundPositionOffset">Position Offset.</param>
    protected static void LoadThreePartTimelines(NodeBase parent, NodeBase background, NodeBase foreground, Vector2 foregroundPositionOffset)
    {
        parent.AddTimeline(new TimelineBuilder()
                           .BeginFrameSet(1, 53)
                           .AddLabelPair(1, 10, 1)
                           .AddLabelPair(11, 17, 2)
                           .AddLabelPair(18, 26, 3)
                           .AddLabelPair(27, 36, 7)
                           .AddLabelPair(37, 46, 6)
                           .AddLabelPair(47, 53, 4)
                           .EndFrameSet()
                           .Build());

        background.AddTimeline(new TimelineBuilder()
                               .AddFrameSetWithFrame(1, 10, 1, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f))
                               .BeginFrameSet(11, 17)
                               .AddFrame(11, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f))
                               .AddFrame(13, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f), addColor: new Vector3(16.0f))
                               .EndFrameSet()
                               .AddFrameSetWithFrame(18, 26, 18, new Vector2(0.0f, 1.0f), 255, new Vector3(16.0f))
                               .AddFrameSetWithFrame(27, 36, 27, Vector2.Zero, 178, multiplyColor: new Vector3(50.0f))
                               .AddFrameSetWithFrame(37, 46, 37, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f), addColor: new Vector3(16.0f))
                               .BeginFrameSet(47, 53)
                               .AddFrame(47, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f), addColor: new Vector3(16.0f))
                               .AddFrame(53, Vector2.Zero, 255, multiplyColor: new Vector3(100.0f))
                               .EndFrameSet()
                               .Build());

        foreground.AddTimeline(new TimelineBuilder()
                               .AddFrameSetWithFrame(1, 10, 1, foregroundPositionOffset, 255, multiplyColor: new Vector3(100.0f))
                               .AddFrameSetWithFrame(11, 17, 11, foregroundPositionOffset, 255, multiplyColor: new Vector3(100.0f))
                               .AddFrameSetWithFrame(18, 26, 18, foregroundPositionOffset + new Vector2(0.0f, 1.0f), 255, multiplyColor: new Vector3(100.0f))
                               .AddFrameSetWithFrame(27, 36, 27, foregroundPositionOffset, 153, multiplyColor: new Vector3(80.0f))
                               .AddFrameSetWithFrame(37, 46, 37, foregroundPositionOffset, 255, multiplyColor: new Vector3(100.0f))
                               .AddFrameSetWithFrame(47, 53, 47, foregroundPositionOffset, 255, multiplyColor: new Vector3(100.0f))
                               .Build());
    }

    /// <summary>
    /// Function that is called when this button is clicked on.
    /// </summary>
    /// <remarks>
    /// This will invoke<see cref="OnClick"/>.
    /// </remarks>
    protected virtual void ClickHandler()
        => this.OnClick?.Invoke();
}
