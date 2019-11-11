using System;

namespace Dalamud.Game.Chat {
    public sealed class XivChatEntry {
        public XivChatType Type { get; set; } = XivChatType.Debug;

        public uint SenderId { get; set; }

        public string Name { get; set; } = string.Empty;

        public byte[] MessageBytes { get; set; }

        public IntPtr Parameters { get; set; }
    }
}
