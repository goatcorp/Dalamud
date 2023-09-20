namespace Dalamud.Game.Addon;

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
    /// Mouse Click.
    /// </summary>
    MouseClick = 9,
    
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
    /// Button Press, sent on MouseDown on Button.
    /// </summary>
    ButtonPress = 23,
    
    /// <summary>
    /// Button Release, sent on MouseUp and MouseOut.
    /// </summary>
    ButtonRelease = 24,
    
    /// <summary>
    /// Button Click, sent on MouseUp and MouseClick on button.
    /// </summary>
    ButtonClick = 25,
    
    /// <summary>
    /// List Item RollOver.
    /// </summary>
    ListItemRollOver = 33,
    
    /// <summary>
    /// List Item Roll Out.
    /// </summary>
    ListItemRollOut = 34,
    
    /// <summary>
    /// List Item Toggle.
    /// </summary>
    ListItemToggle = 35,
    
    /// <summary>
    /// Drag Drop Roll Over.
    /// </summary>
    DragDropRollOver = 52,
    
    /// <summary>
    /// Drag Drop Roll Out.
    /// </summary>
    DragDropRollOut = 53,
    
    /// <summary>
    /// Drag Drop Unknown.
    /// </summary>
    DragDropUnk54 = 54,
    
    /// <summary>
    /// Drag Drop Unknown.
    /// </summary>
    DragDropUnk55 = 55,
    
    /// <summary>
    /// Icon Text Roll Over.
    /// </summary>
    IconTextRollOver = 56,
    
    /// <summary>
    /// Icon Text Roll Out.
    /// </summary>
    IconTextRollOut = 57,
    
    /// <summary>
    /// Icon Text Click.
    /// </summary>
    IconTextClick = 58,
    
    /// <summary>
    /// Window Roll Over.
    /// </summary>
    WindowRollOver = 67,
    
    /// <summary>
    /// Window Roll Out.
    /// </summary>
    WindowRollOut = 68,
    
    /// <summary>
    /// Window Change Scale.
    /// </summary>
    WindowChangeScale = 69,
}
