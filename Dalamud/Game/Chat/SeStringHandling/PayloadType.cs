using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Chat.SeStringHandling
{
    /// <summary>
    /// All parsed types of SeString payloads.
    /// </summary>
    public enum PayloadType
    {
        /// <summary>
        /// An SeString payload representing a player link.
        /// </summary>
        Player,
        /// <summary>
        /// An SeString payload representing an Item link.
        /// </summary>
        Item,
        /// <summary>
        /// An SeString payload representing an Status Effect link.
        /// </summary>
        Status,
        /// <summary>
        /// An SeString payload representing raw, typed text.
        /// </summary>
        RawText,
        /// <summary>
        /// An SeString payload representing a text foreground color.
        /// </summary>
        UIForeground,
        /// <summary>
        /// An SeString payload representing a text glow color.
        /// </summary>
        UIGlow,
        /// <summary>
        /// An SeString payload representing any data we don't handle.
        /// </summary>
        Unknown
    }
}
