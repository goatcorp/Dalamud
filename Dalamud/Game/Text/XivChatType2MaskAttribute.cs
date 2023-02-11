using System;

namespace Dalamud.Game.Text;

/// <summary>
/// Storage for whether a chat type value of type <see cref="XivChatType2"/> is a channel, mask, etc.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class XivChatType2MaskAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XivChatType2MaskAttribute"/> class.
    /// </summary>
    /// <param name="kind">The mask kind of the XivChatType value (channel, source mask, destination mask, etc.)</param>
    internal XivChatType2MaskAttribute(XivChatType2EntryKind kind)
    {
        this.Kind = kind;
    }

    /// <summary>
    /// Gets the "kind" of the XivChatType value (channel, source mask, destination mask, etc.).
    /// </summary>
    public XivChatType2EntryKind Kind { get; }
}
