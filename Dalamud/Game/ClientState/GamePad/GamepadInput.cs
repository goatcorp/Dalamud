using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.GamePad;

/// <summary>
/// Struct which gets populated by polling the gamepads.
///
/// Has an array of gamepads, among many other things (here not mapped).
/// All we really care about is the final data which the game uses to determine input.
///
/// The size is definitely bigger than only the following fields but I do not know how big.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct GamepadInput
{
    /// <summary>
    /// Left analogue stick's horizontal value, -99 for left, 99 for right.
    /// </summary>
    [FieldOffset(0x88)]
    public int LeftStickX;

    /// <summary>
    /// Left analogue stick's vertical value, -99 for down, 99 for up.
    /// </summary>
    [FieldOffset(0x8C)]
    public int LeftStickY;

    /// <summary>
    /// Right analogue stick's horizontal value, -99 for left, 99 for right.
    /// </summary>
    [FieldOffset(0x90)]
    public int RightStickX;

    /// <summary>
    /// Right analogue stick's vertical value, -99 for down, 99 for up.
    /// </summary>
    [FieldOffset(0x94)]
    public int RightStickY;

    /// <summary>
    /// Raw input, set the whole time while a button is held. See <see cref="GamepadButtons"/> for the mapping.
    /// </summary>
    /// <remarks>
    /// This is a bitfield.
    /// </remarks>
    [FieldOffset(0x98)]
    public ushort ButtonsRaw;

    /// <summary>
    /// Button pressed, set once when the button is pressed. See <see cref="GamepadButtons"/> for the mapping.
    /// </summary>
    /// <remarks>
    /// This is a bitfield.
    /// </remarks>
    [FieldOffset(0x9C)]
    public ushort ButtonsPressed;

    /// <summary>
    /// Button released input, set once right after the button is not hold anymore. See <see cref="GamepadButtons"/> for the mapping.
    /// </summary>
    /// <remarks>
    /// This is a bitfield.
    /// </remarks>
    [FieldOffset(0xA0)]
    public ushort ButtonsReleased;

    /// <summary>
    /// Repeatedly emits the held button input in fixed intervals. See <see cref="GamepadButtons"/> for the mapping.
    /// </summary>
    /// <remarks>
    /// This is a bitfield.
    /// </remarks>
    [FieldOffset(0xA4)]
    public ushort ButtonsRepeat;
}
