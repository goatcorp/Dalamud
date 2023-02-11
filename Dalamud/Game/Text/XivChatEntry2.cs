using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Text;

/// <summary>
/// This class represents a single chat log entry.
/// </summary>
public sealed class XivChatEntry2
{
    // / <summary>
    // / Initializes a new instance of the <see cref="XivChatEntry2"/> class.
    // / </summary>
    // [MethodImpl(MethodImplOptions.NoInlining)] // Make sure that the constructor doesn't get inlined so that GetCallingAssembly() resolves correctly.
    // public XivChatEntry2()
    // {
    //    // ***** TODO: Probably delete this and have the caller of the print chat stuff pass in the plugin interface and get the name from that.
    //    this.SourceName = Assembly.GetCallingAssembly()?.GetName()?.Name ?? string.Empty;   //***** TODO: Profile the performance of this.
    // }

    /// <summary>
    /// Gets or sets the type of entry.  This can be a channel, or a bitwise combination of channel, target, and source values.
    /// </summary>
    public XivChatType2 Type { get; set; } = XivChatType2.Debug;

    /// <summary>
    /// Gets or sets the Unix timestamp of the log entry.  A value of zero will cause the current time to be used.
    /// </summary>
    public uint Timestamp { get; set; }

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

    /// <summary>
    /// Gets or sets the message source.  This is something that is set by Dalamud itself, and should not be exposed to plugins in this struct.
    /// </summary>
    internal XivChatMessageSource MessageSource { get; set; } = XivChatMessageSource.Unknown;

    /// <summary>
    /// Gets or sets the source plugin name for messages originating from plugins.  This is something that is set by Dalamud itself, and should not be exposed to plugins in this struct.
    /// </summary>
    internal string SourceName { get; set; } = string.Empty;
}
