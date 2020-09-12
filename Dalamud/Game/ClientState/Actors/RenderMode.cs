#pragma warning disable CS1591
using System;

namespace Dalamud.Game.ClientState.Actors {
    public enum RenderMode : byte {
        // There's a couple of things here that hide weapon (1<<4?) and create a green nametag / health bar (1<<2?)
        None = 0x00,
        Invisible = 0x02,
    }
}
