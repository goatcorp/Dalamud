using System.Numerics;

using FFXIVClientStructs.FFXIV.Component.GUI;

using AtkMouseData = FFXIVClientStructs.FFXIV.Component.GUI.AtkEventData.AtkMouseData;
using ModifierFlag = FFXIVClientStructs.FFXIV.Component.GUI.AtkEventData.AtkMouseData.ModifierFlag;

namespace Dalamud.Game.Addon.Events.EventDataTypes;

/// <inheritdoc />
public unsafe class AddonMouseEventData : AddonEventData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonMouseEventData"/> class.
    /// </summary>
    internal AddonMouseEventData()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonMouseEventData"/> class.
    /// </summary>
    /// <param name="eventData">Other event data to copy.</param>
    internal AddonMouseEventData(AddonEventData eventData)
        : base(eventData)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the event was a Left Mouse Click.
    /// </summary>
    public bool IsLeftClick => this.MouseData.ButtonId is 0;

    /// <summary>
    /// Gets a value indicating whether the event was a Right Mouse Click.
    /// </summary>
    public bool IsRightClick => this.MouseData.ButtonId is 1;

    /// <summary>
    /// Gets a value indicating whether there are any modifiers set such as alt, control, shift, or dragging.
    /// </summary>
    public bool IsNoModifier => this.MouseData.Modifier is 0;

    /// <summary>
    /// Gets a value indicating whether alt was being held when this event triggered.
    /// </summary>
    public bool IsAltHeld => this.MouseData.Modifier.HasFlag(ModifierFlag.Alt);

    /// <summary>
    /// Gets a value indicating whether control was being held when this event triggered.
    /// </summary>
    public bool IsControlHeld => this.MouseData.Modifier.HasFlag(ModifierFlag.Ctrl);

    /// <summary>
    /// Gets a value indicating whether shift was being held when this event triggered.
    /// </summary>
    public bool IsShiftHeld => this.MouseData.Modifier.HasFlag(ModifierFlag.Shift);

    /// <summary>
    /// Gets a value indicating whether this event is a mouse drag or not.
    /// </summary>
    public bool IsDragging => this.MouseData.Modifier.HasFlag(ModifierFlag.Dragging);

    /// <summary>
    /// Gets a value indicating whether the event was a scroll up.
    /// </summary>
    public bool IsScrollUp => this.MouseData.WheelDirection is 1;

    /// <summary>
    /// Gets a value indicating whether the event was a scroll down.
    /// </summary>
    public bool IsScrollDown => this.MouseData.WheelDirection is -1;

    /// <summary>
    /// Gets the position of the mouse when this event was triggered.
    /// </summary>
    public Vector2 Position => new(this.MouseData.PosX, this.MouseData.PosY);

    private AtkEventData* AtkEventData => (AtkEventData*)this.AtkEventDataPointer;

    private AtkMouseData MouseData => this.AtkEventData->MouseData;
}
