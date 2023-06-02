using System;

namespace Dalamud.Game.ClientState.Keys;

partial class KeyCapture
{
    [Flags]
    private enum State : byte
    {
        CaptureAll = 0x01,
        CaptureAllSingleFrame = 0x02,
        RestoreAllOnNextFrame = 0x04,
    }
}
