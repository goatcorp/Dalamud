using System;

namespace Dalamud.Game.ClientState.Keys;

[Flags]
public enum KeyStateFlag
{
    None = 0x00,
    Down = 0x01,
    JustPressed = 0x02,
    JustReleased = 0x04,
}
