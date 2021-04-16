using System;
using System.Windows.Forms;
using Dalamud.Game.ClientState.Structs;
using Dalamud.Hooking;
using OpenGL;

namespace Dalamud.Game.ClientState
{
    public unsafe class GamepadState
    {

        public float ButtonSouth => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_A) > 0 ? 1 : 0;
        public float ButtonEast => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_B) > 0 ? 1 : 0;
        public float ButtonWest => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_X) > 0 ? 1 : 0;
        public float ButtonNorth => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_Y) > 0 ? 1 : 0;
        public float DPadDown => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_DPAD_DOWN) > 0 ? 1 : 0;
        public float DPadRight => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_DPAD_RIGHT) > 0 ? 1 : 0;
        public float DPadUp => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_DPAD_UP) > 0 ? 1 : 0;
        public float DPadLeft => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_DPAD_LEFT) > 0 ? 1 : 0;
        public float L1 => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_LEFT_1) > 0 ? 1 : 0;
        public float L2 => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_LEFT_2) > 0 ? 1 : 0;
        public float L3 => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_LEFT_3) > 0 ? 1 : 0;
        public float R1 => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_RIGHT_1) > 0 ? 1 : 0;
        public float R2 => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_RIGHT_2) > 0 ? 1 : 0;
        public float R3 => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_RIGHT_3) > 0 ? 1 : 0;
        public float Start => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_START) > 0 ? 1 : 0;
        public float Select => (this.buttonsTapped & (ushort)GamepadButtons.XINPUT_GAMEPAD_SELECT) > 0 ? 1 : 0;
        public float LeftStickLeft => this.leftStickX < 0 ? -this.leftStickX / 100f : 0;
        public float LeftStickRight => this.leftStickX > 0 ? this.leftStickX / 100f : 0;
        public float LeftStickUp => this.leftStickY > 0 ? this.leftStickY / 100f : 0;
        public float LeftStickDown => this.leftStickY < 0 ? -this.leftStickY / 100f : 0;
        public float RightStickLeft => this.rightStickX < 0 ? -this.rightStickX / 100f : 0;
        public float RightStickRight => this.rightStickX > 0 ? this.rightStickX / 100f : 0;
        public float RightStickUp => this.rightStickY > 0 ? this.rightStickY / 100f : 0;
        public float RightStickDown => this.rightStickY < 0 ? -this.rightStickY / 100f : 0;
        
        public float Tapped(GamepadButtons button)
            => (this.buttonsTapped & (ushort)button) > 0 ? 1 : 0;
        
        public float Holding(GamepadButtons button)
            => (this.buttonsHolding & (ushort)button) > 0 ? 1 : 0;
        
        public float Released(GamepadButtons button)
            => (this.buttonsReleased & (ushort)button) > 0 ? 1 : 0;
        
        private delegate int ControllerPoll(IntPtr controllerInput);

        private Hook<ControllerPoll> controllerPoll;
        //private GamepadInput* _gamePadInput;
        private bool isDisposed;

        private int leftStickX;
        private int leftStickY;
        private int rightStickX;
        private int rightStickY;
        private ushort buttons;
        private ushort buttonsTapped;
        private ushort buttonsReleased;
        private ushort buttonsHolding;
        private bool imGuiMode;
        

        public GamepadState(SigScanner scanner)
        {
            const string controllerPollSignature =
                "40 ?? 57 41 ?? 48 81 EC ?? ?? ?? ?? 44 0F ?? ?? ?? ?? ?? ?? ?? 48 8B";
            this.controllerPoll = new Hook<ControllerPoll>(
                scanner.ScanText(controllerPollSignature),
                (ControllerPoll) ControllerPollDetour);
        }


        private unsafe int ControllerPollDetour(IntPtr gamepadInput)
        {
            //this._gamePadInput =(GamepadInput*) gamepadInput;
            var original = this.controllerPoll.Original(gamepadInput);
            var input = (GamepadInput*)gamepadInput;
            if (
                (input->ButtonFlag_Holding & (ushort)GamepadButtons.XINPUT_GAMEPAD_RIGHT_1) > 0
                && (input->ButtonFlag & (ushort)GamepadButtons.XINPUT_GAMEPAD_LEFT_1) > 0)
            {
                this.imGuiMode = !this.imGuiMode;
                if (!this.imGuiMode)
                {
                    this.leftStickX = 0;
                    this.leftStickY = 0;
                    this.rightStickY = 0;
                    this.rightStickX = 0;
                    this.buttons = 0;
                    this.buttonsTapped = 0;
                    this.buttonsReleased = 0;
                    this.buttonsHolding = 0;
                }
            }

            if (this.imGuiMode)
            {
                this.leftStickX = input->LeftStickX;
                this.leftStickY = input->LeftStickY;
                this.rightStickX = input->RightStickX;
                this.rightStickY = input->RightStickY;
                this.buttons = input->ButtonFlag;
                this.buttonsTapped = input->ButtonFlag_Tap;
                this.buttonsReleased = input->ButtonFlag_Release;
                this.buttonsHolding = input->ButtonFlag_Holding;

                input->LeftStickX = 0;
                input->LeftStickY = 0;
                input->RightStickX = 0;
                input->RightStickY = 0;
                input->ButtonFlag = 0;
                input->ButtonFlag_Tap = 0;
                input->ButtonFlag_Release = 0;
                input->ButtonFlag_Holding = 0;
            }

            // Not so sure about the return value, does not seem to matter if we return the
            // original, zero or do the work adjusting the bits.
            return original;
        }

        public void Enable()
        {
            this.controllerPoll.Enable();
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (this.isDisposed) return;
            if (disposing)
            {
                this.controllerPoll?.Disable();
                this.controllerPoll?.Dispose();
            }

            this.isDisposed = true;
        }

        ~GamepadState()
        {
            Dispose(false);
        }
    }
}
