using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs
{
    // It is bigger, but I dunno how big in real
    [StructLayout(LayoutKind.Explicit)]
    public struct GamepadInput
    {
        // Each stick is -99 till 99
        [FieldOffset(0x88)]
        public int LeftStickX;

        [FieldOffset(0x8C)]
        public int LeftStickY;

        [FieldOffset(0x90)]
        public int RightStickX;

        [FieldOffset(0x94)]
        public int RightStickY;

        // Seems to be source of true, instant population, keeps value while hold.
        [FieldOffset(0x98)]
        public ushort ButtonFlag; // bitfield

        // Gets populated only if released after a short tick
        [FieldOffset(0x9C)]
        public ushort ButtonFlag_Tap; // bitfield

        // Gets populated on button release
        [FieldOffset(0xA0)]
        public ushort ButtonFlag_Release; // bitfield

        // Gets populated after a tick and keeps being set while button is held
        [FieldOffset(0xA4)]
        public ushort ButtonFlag_Holding; // bitfield
    }
}
