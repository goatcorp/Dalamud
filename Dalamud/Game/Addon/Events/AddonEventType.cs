namespace Dalamud.Game.Addon.Events;

/// <summary>
/// Reimplementation of AtkEventType.
/// </summary>
public enum AddonEventType : byte
{
    /// <summary>
    /// Mouse Down.
    /// </summary>
    MouseDown = 3,

    /// <summary>
    /// Mouse Up.
    /// </summary>
    MouseUp = 4,

    /// <summary>
    /// Mouse Move.
    /// </summary>
    MouseMove = 5,

    /// <summary>
    /// Mouse Over.
    /// </summary>
    MouseOver = 6,

    /// <summary>
    /// Mouse Out.
    /// </summary>
    MouseOut = 7,

    /// <summary>
    /// Mouse Wheel.
    /// </summary>
    MouseWheel = 8,

    /// <summary>
    /// Mouse Click.
    /// </summary>
    MouseClick = 9,

    /// <summary>
    /// Mouse Double Click.
    /// </summary>
    MouseDoubleClick = 10,

    /// <summary>
    /// Input Received.
    /// </summary>
    InputReceived = 12,

    /// <summary>
    /// Focus Start.
    /// </summary>
    FocusStart = 18,

    /// <summary>
    /// Focus Stop.
    /// </summary>
    FocusStop = 19,

    /// <summary>
    /// Resize (ChatLogPanel).
    /// </summary>
    Resize = 19,

    /// <summary>
    /// AtkComponentButton Press, sent on MouseDown on Button.
    /// </summary>
    ButtonPress = 23,

    /// <summary>
    /// AtkComponentButton Release, sent on MouseUp and MouseOut.
    /// </summary>
    ButtonRelease = 24,

    /// <summary>
    /// AtkComponentButton Click, sent on MouseUp and MouseClick on button.
    /// </summary>
    ButtonClick = 25,

    /// <summary>
    /// Value Update (NumericInput, ScrollBar, etc.)
    /// </summary>
    ValueUpdate = 27,

    /// <summary>
    /// AtkComponentSlider Value Update.
    /// </summary>
    SliderValueUpdate = 29,

    /// <summary>
    /// AtkComponentSlider Released.
    /// </summary>
    SliderReleased = 30,

    /// <summary>
    /// AtkComponentList RollOver.
    /// </summary>
    ListItemRollOver = 33,

    /// <summary>
    /// AtkComponentList Roll Out.
    /// </summary>
    ListItemRollOut = 34,

    /// <summary>
    /// AtkComponentList Click.
    /// </summary>
    ListItemClick = 35,

    /// <summary>
    /// AtkComponentList Toggle.
    /// </summary>
    [Obsolete("Use ListItemClick")]
    ListItemToggle = 35,

    /// <summary>
    /// AtkComponentList Double Click.
    /// </summary>
    ListItemDoubleClick = 36,

    /// <summary>
    /// AtkComponentList Select.
    /// </summary>
    ListItemSelect = 38,

    /// <summary>
    /// AtkComponentDragDrop Begin.
    /// Sent on MouseDown over a draggable icon (will NOT send for a locked icon).
    /// </summary>
    DragDropBegin = 50,

    /// <summary>
    /// AtkComponentDragDrop End.
    /// </summary>
    DragDropEnd = 51,

    /// <summary>
    /// AtkComponentDragDrop Insert.
    /// Sent when dropping an icon into a hotbar/inventory slot or similar.
    /// </summary>
    DragDropInsert = 53,

    /// <summary>
    /// AtkComponentDragDrop Roll Over.
    /// </summary>
    DragDropRollOver = 55,

    /// <summary>
    /// AtkComponentDragDrop Roll Out.
    /// </summary>
    DragDropRollOut = 56,

    /// <summary>
    /// AtkComponentDragDrop Discard.
    /// Sent when dropping an icon into empty screenspace, eg to remove an action from a hotBar.
    /// </summary>
    DragDropDiscard = 57,

    /// <summary>
    /// Drag Drop Unknown.
    /// </summary>
    [Obsolete("Use DragDropDiscard", true)]
    DragDropUnk54 = 54,

    /// <summary>
    /// AtkComponentDragDrop Cancel.
    /// Sent on MouseUp if the cursor has not moved since DragDropBegin, OR on MouseDown over a locked icon.
    /// </summary>
    DragDropCancel = 58,

    /// <summary>
    /// Drag Drop Unknown.
    /// </summary>
    [Obsolete("Use DragDropCancel", true)]
    DragDropUnk55 = 55,

    /// <summary>
    /// AtkComponentIconText Roll Over.
    /// </summary>
    IconTextRollOver = 59,

    /// <summary>
    /// AtkComponentIconText Roll Out.
    /// </summary>
    IconTextRollOut = 60,

    /// <summary>
    /// AtkComponentIconText Click.
    /// </summary>
    IconTextClick = 61,

    /// <summary>
    /// AtkDialogue Close.
    /// </summary>
    DialogueClose = 62,

    /// <summary>
    /// AtkDialogue Submit.
    /// </summary>
    DialogueSubmit = 63,

    /// <summary>
    /// AtkTimer Tick.
    /// </summary>
    TimerTick = 64,

    /// <summary>
    /// AtkTimer End.
    /// </summary>
    TimerEnd = 65,

    /// <summary>
    /// AtkSimpleTween Progress.
    /// </summary>
    TweenProgress = 67,

    /// <summary>
    /// AtkSimpleTween Complete.
    /// </summary>
    TweenComplete = 68,

    /// <summary>
    /// AtkAddonControl Child Addon Attached.
    /// </summary>
    ChildAddonAttached = 69,

    /// <summary>
    /// AtkComponentWindow Roll Over.
    /// </summary>
    WindowRollOver = 70,

    /// <summary>
    /// AtkComponentWindow Roll Out.
    /// </summary>
    WindowRollOut = 71,

    /// <summary>
    /// AtkComponentWindow Change Scale.
    /// </summary>
    WindowChangeScale = 72,

    /// <summary>
    /// AtkTextNode Link Mouse Click.
    /// </summary>
    LinkMouseClick = 75,

    /// <summary>
    /// AtkTextNode Link Mouse Over.
    /// </summary>
    LinkMouseOver = 76,

    /// <summary>
    /// AtkTextNode Link Mouse Out.
    /// </summary>
    LinkMouseOut = 77,
}
