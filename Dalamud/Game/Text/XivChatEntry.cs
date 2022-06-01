using System;

using Dalamud.Game.Text.SeStringHandling;

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
        /// Gets or sets the Unix timestamp of the log entry.
        /// This property was incorrectly named, and is not related to the message sender.
        /// When printing this chat entry to the log, a value of zero will use the current time.
        /// </summary>
        [Obsolete("This is actually the Unix timestamp of the log entry, and not the sender's ID.  Use with caution.")]
        public uint SenderId { get; set; }

        /// <summary>
        /// Gets or sets the sender name.
        /// </summary>
        public SeString Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public SeString Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message parameters.
        /// </summary>
        public IntPtr Parameters { get; set; }
    }
}
