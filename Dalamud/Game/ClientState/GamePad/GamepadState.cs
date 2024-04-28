using System.Numerics;

using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using ImGuiNET;
using Serilog;

namespace Dalamud.Game.ClientState.GamePad;

/// <summary>
/// Exposes the game gamepad state to dalamud.
///
/// Will block game's gamepad input if <see cref="ImGuiConfigFlags.NavEnableGamepad"/> is set.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IGamepadState>]
#pragma warning restore SA1015
internal unsafe class GamepadState : IInternalDisposableService, IGamepadState
{
    private readonly Hook<ControllerPoll>? gamepadPoll;

    private bool isDisposed;

    private int leftStickX;
    private int leftStickY;
    private int rightStickX;
    private int rightStickY;

    [ServiceManager.ServiceConstructor]
    private GamepadState(ClientState clientState)
    {
        var resolver = clientState.AddressResolver;
        Log.Verbose($"GamepadPoll address 0x{resolver.GamepadPoll.ToInt64():X}");
        this.gamepadPoll = Hook<ControllerPoll>.FromAddress(resolver.GamepadPoll, this.GamepadPollDetour);
        this.gamepadPoll?.Enable();
    }

    private delegate int ControllerPoll(IntPtr controllerInput);

    /// <summary>
    /// Gets the pointer to the current instance of the GamepadInput struct.
    /// </summary>
    public IntPtr GamepadInputAddress { get; private set; }

    /// <inheritdoc/>
    public Vector2 LeftStick => 
        new(this.leftStickX, this.leftStickY);
    
    /// <inheritdoc/>
    public Vector2 RightStick => 
        new(this.rightStickX, this.rightStickY);

    /// <summary>
    /// Gets buttons pressed bitmask, set once when the button is pressed. See <see cref="GamepadButtons"/> for the mapping.
    ///
    /// Exposed internally for Debug Data window.
    /// </summary>
    internal ushort ButtonsPressed { get; private set; }

    /// <summary>
    /// Gets raw button bitmask, set the whole time while a button is held. See <see cref="GamepadButtons"/> for the mapping.
    ///
    /// Exposed internally for Debug Data window.
    /// </summary>
    internal ushort ButtonsRaw { get; private set; }

    /// <summary>
    /// Gets button released bitmask, set once right after the button is not hold anymore. See <see cref="GamepadButtons"/> for the mapping.
    ///
    /// Exposed internally for Debug Data window.
    /// </summary>
    internal ushort ButtonsReleased { get; private set; }

    /// <summary>
    /// Gets button repeat bitmask, emits the held button input in fixed intervals. See <see cref="GamepadButtons"/> for the mapping.
    ///
    /// Exposed internally for Debug Data window.
    /// </summary>
    internal ushort ButtonsRepeat { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether detour should block gamepad input for game.
    ///
    /// Ideally, we would use
    /// (ImGui.GetIO().ConfigFlags &amp; ImGuiConfigFlags.NavEnableGamepad) > 0
    /// but this has a race condition during load with the detour which sets up ImGui
    /// and throws if our detour gets called before the other.
    /// </summary>
    internal bool NavEnableGamepad { get; set; }

    /// <inheritdoc/>
    public float Pressed(GamepadButtons button) => (this.ButtonsPressed & (ushort)button) > 0 ? 1 : 0;

    /// <inheritdoc/>
    public float Repeat(GamepadButtons button) => (this.ButtonsRepeat & (ushort)button) > 0 ? 1 : 0;

    /// <inheritdoc/>
    public float Released(GamepadButtons button) => (this.ButtonsReleased & (ushort)button) > 0 ? 1 : 0;

    /// <inheritdoc/>
    public float Raw(GamepadButtons button) => (this.ButtonsRaw & (ushort)button) > 0 ? 1 : 0;

    /// <summary>
    /// Disposes this instance, alongside its hooks.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    private int GamepadPollDetour(IntPtr gamepadInput)
    {
        var original = this.gamepadPoll!.Original(gamepadInput);
        try
        {
            this.GamepadInputAddress = gamepadInput;
            var input = (GamepadInput*)gamepadInput;
            this.leftStickX = input->LeftStickX;
            this.leftStickY = input->LeftStickY;
            this.rightStickX = input->RightStickX;
            this.rightStickY = input->RightStickY;
            this.ButtonsRaw = input->ButtonsRaw;
            this.ButtonsPressed = input->ButtonsPressed;
            this.ButtonsReleased = input->ButtonsReleased;
            this.ButtonsRepeat = input->ButtonsRepeat;

            if (this.NavEnableGamepad)
            {
                input->LeftStickX = 0;
                input->LeftStickY = 0;
                input->RightStickX = 0;
                input->RightStickY = 0;

                // NOTE (Chiv) Zeroing `ButtonsRaw` destroys `ButtonPressed`, `ButtonReleased`
                // and `ButtonRepeat` as the game uses the RAW input to determine those (apparently).
                // It does block, however, all input to the game.
                // Leaving `ButtonsRaw` as it is and only zeroing the other leaves e.g. long-hold L2/R2
                // and the digipad (in some situations, but thankfully not in menus) functional.
                // We can either:
                // (a) Explicitly only set L2/R2/Digipad to 0 (and destroy their `ButtonPressed` field) => Needs to be documented, or
                // (b) ignore it as so far it seems only a 'visual' error
                //      (L2/R2 being held down activates CrossHotBar but activating an ability is impossible because of the others blocked input,
                //      Digipad is ignored in menus but without any menu's  one still switches target or party members, but cannot interact with them
                //      because of the other blocked input)
                // `ButtonPressed` is pretty useful but its hella confusing to the user, so we do (a) and advise plugins do not rely on
                // `ButtonPressed` while ImGuiConfigFlags.NavEnableGamepad is set.
                // This is debatable.
                // ImGui itself does not care either way as it uses the Raw values and does its own state handling.
                const ushort deletionMask = (ushort)(~GamepadButtons.L2
                                                     & ~GamepadButtons.R2
                                                     & ~GamepadButtons.DpadDown
                                                     & ~GamepadButtons.DpadLeft
                                                     & ~GamepadButtons.DpadUp
                                                     & ~GamepadButtons.DpadRight);
                input->ButtonsRaw &= deletionMask;
                input->ButtonsPressed = 0;
                input->ButtonsReleased = 0;
                input->ButtonsRepeat = 0;
                return 0;
            }

            // NOTE (Chiv) Not so sure about the return value, does not seem to matter if we return the
            // original, zero or do the work adjusting the bits.
            return original;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Gamepad Poll detour critical error! Gamepad navigation will not work!");

            // NOTE (Chiv) Explicitly deactivate on error
            ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.NavEnableGamepad;
            return original;
        }
    }

    private void Dispose(bool disposing)
    {
        if (this.isDisposed) return;
        if (disposing)
        {
            this.gamepadPoll?.Disable();
            this.gamepadPoll?.Dispose();
        }

        this.isDisposed = true;
    }
}
