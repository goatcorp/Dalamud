namespace Dalamud.Game.ClientState.GamePad;

/// <summary>
/// Bitmask of the Button ushort used by the game.
/// </summary>
[Flags]
public enum GamepadButtons : ushort
{
    /// <summary>
    /// No buttons pressed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Digipad up.
    /// </summary>
    DpadUp = 0x0001,

    /// <summary>
    /// Digipad down.
    /// </summary>
    DpadDown = 0x0002,

    /// <summary>
    /// Digipad left.
    /// </summary>
    DpadLeft = 0x0004,

    /// <summary>
    /// Digipad right.
    /// </summary>
    DpadRight = 0x0008,

    /// <summary>
    /// North action button. Triangle on PS, Y on Xbox.
    /// </summary>
    North = 0x0010,

    /// <summary>
    /// South action button. Cross on PS, A on Xbox.
    /// </summary>
    South = 0x0020,

    /// <summary>
    /// West action button. Square on PS, X on Xbos.
    /// </summary>
    West = 0x0040,

    /// <summary>
    /// East action button. Circle on PS, B on Xbox.
    /// </summary>
    East = 0x0080,

    /// <summary>
    /// First button on left shoulder side.
    /// </summary>
    L1 = 0x0100,

    /// <summary>
    /// Second button on left shoulder side. Analog input lost in this bitmask.
    /// </summary>
    L2 = 0x0200,

    /// <summary>
    /// Press on left analogue stick.
    /// </summary>
    L3 = 0x0400,

    /// <summary>
    /// First button on right shoulder.
    /// </summary>
    R1 = 0x0800,

    /// <summary>
    /// Second button on right shoulder. Analog input lost in this bitmask.
    /// </summary>
    R2 = 0x1000,

    /// <summary>
    /// Press on right analogue stick.
    /// </summary>
    R3 = 0x2000,

    /// <summary>
    /// Button on the right inner side of the controller. Options on PS, Start on Xbox.
    /// </summary>
    Start = 0x8000,

    /// <summary>
    /// Button on the left inner side of the controller. ??? on PS, Back on Xbox.
    /// </summary>
    Select = 0x4000,
}
