namespace Dalamud.Game.ClientState
{
    public enum GamepadButtons : ushort
    {
        XINPUT_GAMEPAD_DPAD_UP = 0x0001,
        XINPUT_GAMEPAD_DPAD_DOWN = 0x0002,
        XINPUT_GAMEPAD_DPAD_LEFT = 0x0004,
        XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008,
        XINPUT_GAMEPAD_Y = 0x0010,
        XINPUT_GAMEPAD_A = 0x0020,
        XINPUT_GAMEPAD_X = 0x0040,
        XINPUT_GAMEPAD_B = 0x0080,
        XINPUT_GAMEPAD_LEFT_1 = 0x0100,
        XINPUT_GAMEPAD_LEFT_2 = 0x0200, // The back one
        XINPUT_GAMEPAD_LEFT_3 = 0x0400,
        XINPUT_GAMEPAD_RIGHT_1 = 0x0800,
        XINPUT_GAMEPAD_RIGHT_2 = 0x1000, // The back one
        XINPUT_GAMEPAD_RIGHT_3 = 0x2000,
        XINPUT_GAMEPAD_START = 0x8000,
        XINPUT_GAMEPAD_SELECT = 0x4000,


    }
}
