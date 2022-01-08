using System;

using Dalamud.Hooking;
using ImGuiNET;
using Serilog;

namespace Dalamud.Game.ClientState.GamePad
{
    /// <summary>
    /// Exposes the game gamepad state to dalamud.
    ///
    /// Will block game's gamepad input if <see cref="ImGuiConfigFlags.NavEnableGamepad"/> is set.
    /// </summary>
    public unsafe class GamepadState : IDisposable
    {
        private readonly Hook<ControllerPoll> gamepadPoll;

        private bool isDisposed;

        private int leftStickX;
        private int leftStickY;
        private int rightStickX;
        private int rightStickY;

        /// <summary>
        /// Initializes a new instance of the <see cref="GamepadState" /> class.
        /// </summary>
        /// <param name="resolver">Resolver knowing the pointer to the GamepadPoll function.</param>
        public GamepadState(ClientStateAddressResolver resolver)
        {
            Log.Verbose($"GamepadPoll address 0x{resolver.GamepadPoll.ToInt64():X}");
            this.gamepadPoll = new Hook<ControllerPoll>(resolver.GamepadPoll, this.GamepadPollDetour);
        }

        /// <summary>
        ///     Finalizes an instance of the <see cref="GamepadState" /> class.
        /// </summary>
        ~GamepadState()
        {
            this.Dispose(false);
        }

        private delegate int ControllerPoll(IntPtr controllerInput);

        /// <summary>
        /// Gets the pointer to the current instance of the GamepadInput struct.
        /// </summary>
        public IntPtr GamepadInputAddress { get; private set; }

        /// <summary>
        ///     Gets the state of the left analogue stick in the left direction between 0 (not tilted) and 1 (max tilt).
        /// </summary>
        public float LeftStickLeft => this.leftStickX < 0 ? -this.leftStickX / 100f : 0;

        /// <summary>
        ///     Gets the state of the left analogue stick in the right direction between 0 (not tilted) and 1 (max tilt).
        /// </summary>
        public float LeftStickRight => this.leftStickX > 0 ? this.leftStickX / 100f : 0;

        /// <summary>
        ///     Gets the state of the left analogue stick in the up direction between 0 (not tilted) and 1 (max tilt).
        /// </summary>
        public float LeftStickUp => this.leftStickY > 0 ? this.leftStickY / 100f : 0;

        /// <summary>
        ///     Gets the state of the left analogue stick in the down direction between 0 (not tilted) and 1 (max tilt).
        /// </summary>
        public float LeftStickDown => this.leftStickY < 0 ? -this.leftStickY / 100f : 0;

        /// <summary>
        ///     Gets the state of the right analogue stick in the left direction between 0 (not tilted) and 1 (max tilt).
        /// </summary>
        public float RightStickLeft => this.rightStickX < 0 ? -this.rightStickX / 100f : 0;

        /// <summary>
        ///     Gets the state of the right analogue stick in the right direction between 0 (not tilted) and 1 (max tilt).
        /// </summary>
        public float RightStickRight => this.rightStickX > 0 ? this.rightStickX / 100f : 0;

        /// <summary>
        ///     Gets the state of the right analogue stick in the up direction between 0 (not tilted) and 1 (max tilt).
        /// </summary>
        public float RightStickUp => this.rightStickY > 0 ? this.rightStickY / 100f : 0;

        /// <summary>
        ///     Gets the state of the right analogue stick in the down direction between 0 (not tilted) and 1 (max tilt).
        /// </summary>
        public float RightStickDown => this.rightStickY < 0 ? -this.rightStickY / 100f : 0;

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

        /// <summary>
        /// Gets whether <paramref name="button"/> has been pressed.
        ///
        /// Only true on first frame of the press.
        /// If ImGuiConfigFlags.NavEnableGamepad is set, this is unreliable.
        /// </summary>
        /// <param name="button">The button to check for.</param>
        /// <returns>1 if pressed, 0 otherwise.</returns>
        public float Pressed(GamepadButtons button) => (this.ButtonsPressed & (ushort)button) > 0 ? 1 : 0;

        /// <summary>
        /// Gets whether <paramref name="button"/> is being pressed.
        ///
        /// True in intervals if button is held down.
        /// If ImGuiConfigFlags.NavEnableGamepad is set, this is unreliable.
        /// </summary>
        /// <param name="button">The button to check for.</param>
        /// <returns>1 if still pressed during interval, 0 otherwise or in between intervals.</returns>
        public float Repeat(GamepadButtons button) => (this.ButtonsRepeat & (ushort)button) > 0 ? 1 : 0;

        /// <summary>
        /// Gets whether <paramref name="button"/> has been released.
        ///
        /// Only true the frame after release.
        /// If ImGuiConfigFlags.NavEnableGamepad is set, this is unreliable.
        /// </summary>
        /// <param name="button">The button to check for.</param>
        /// <returns>1 if released, 0 otherwise.</returns>
        public float Released(GamepadButtons button) => (this.ButtonsReleased & (ushort)button) > 0 ? 1 : 0;

        /// <summary>
        /// Gets the raw state of <paramref name="button"/>.
        ///
        /// Is set the entire time a button is pressed down.
        /// </summary>
        /// <param name="button">The button to check for.</param>
        /// <returns>1 the whole time button is pressed, 0 otherwise.</returns>
        public float Raw(GamepadButtons button) => (this.ButtonsRaw & (ushort)button) > 0 ? 1 : 0;

        /// <summary>
        /// Enables the hook of the GamepadPoll function.
        /// </summary>
        public void Enable()
        {
            this.gamepadPoll.Enable();
        }

        /// <summary>
        /// Disposes this instance, alongside its hooks.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private int GamepadPollDetour(IntPtr gamepadInput)
        {
            var original = this.gamepadPoll.Original(gamepadInput);
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
}
