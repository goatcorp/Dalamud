using System;

namespace Dalamud.Game.Text
{
    /// <summary>
    /// This class represents a single chat log entry.
    /// </summary>
    public sealed class XivChatEntry
    {
        /// <summary>
        /// Gets or sets the type of entry.
        /// </summary>
        public XivChatType Type { get; set; } = XivChatType.Debug;

        /// <summary>
        /// Gets or sets the sender ID.
        /// </summary>
        public uint SenderId { get; set; }

        /// <summary>
        /// Gets or sets the sender name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message bytes.
        /// </summary>
        public byte[] MessageBytes { get; set; }

        /// <summary>
        /// Gets or sets the message parameters.
        /// </summary>
        public IntPtr Parameters { get; set; }
    }
}
