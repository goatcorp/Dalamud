using System.Numerics;

using Dalamud.Game.ClientState.GamePad;
using ImGuiNET;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Exposes the game gamepad state to dalamud.
///
/// Will block game's gamepad input if <see cref="ImGuiConfigFlags.NavEnableGamepad"/> is set.
/// </summary>
public interface IGamepadState
{
    /// <summary>
    /// Gets the pointer to the current instance of the GamepadInput struct.
    /// </summary>
    public nint GamepadInputAddress { get; }
    
    /// <summary>
    /// Gets the left analogue sticks tilt vector.
    /// </summary>
    public Vector2 LeftStick { get; }
    
    /// <summary>
    /// Gets the right analogue sticks tilt vector.
    /// </summary>
    public Vector2 RightStick { get; }

    /// <summary>
    /// Gets whether <paramref name="button"/> has been pressed.
    ///
    /// Only true on first frame of the press.
    /// If ImGuiConfigFlags.NavEnableGamepad is set, this is unreliable.
    /// </summary>
    /// <param name="button">The button to check for.</param>
    /// <returns>1 if pressed, 0 otherwise.</returns>
    public float Pressed(GamepadButtons button);

    /// <summary>
    /// Gets whether <paramref name="button"/> is being pressed.
    ///
    /// True in intervals if button is held down.
    /// If ImGuiConfigFlags.NavEnableGamepad is set, this is unreliable.
    /// </summary>
    /// <param name="button">The button to check for.</param>
    /// <returns>1 if still pressed during interval, 0 otherwise or in between intervals.</returns>
    public float Repeat(GamepadButtons button);

    /// <summary>
    /// Gets whether <paramref name="button"/> has been released.
    ///
    /// Only true the frame after release.
    /// If ImGuiConfigFlags.NavEnableGamepad is set, this is unreliable.
    /// </summary>
    /// <param name="button">The button to check for.</param>
    /// <returns>1 if released, 0 otherwise.</returns>
    public float Released(GamepadButtons button);

    /// <summary>
    /// Gets the raw state of <paramref name="button"/>.
    ///
    /// Is set the entire time a button is pressed down.
    /// </summary>
    /// <param name="button">The button to check for.</param>
    /// <returns>1 the whole time button is pressed, 0 otherwise.</returns>
    public float Raw(GamepadButtons button);
}
