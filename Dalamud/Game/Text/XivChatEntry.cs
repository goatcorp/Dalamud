using System;
using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Text
{
    public sealed class XivChatEntry {
        public XivChatType Type { get; set; } = XivChatType.Debug;

        public uint SenderId { get; set; }

        public SeString Name { get; set; } = string.Empty;

        public SeString Message { get; set; }

        public IntPtr Parameters { get; set; }
    }
}
