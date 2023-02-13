using System;

namespace Dalamud.Game.Text;

/// <summary>
/// Storage for whether a chat type value of type <see cref="XivChatType2"/> is a channel, mask, etc.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class XivChatTypeKindAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XivChatTypeKindAttribute"/> class.
    /// </summary>
    /// <param name="kind">The mask kind of the XivChatType value (channel, source mask, destination mask, etc.)</param>
    internal XivChatTypeKindAttribute(XivChatTypeKind kind)
    {
        this.Kind = kind;
    }

    /// <summary>
    /// Gets the "kind" of the XivChatType value (channel, source mask, destination mask, etc.).
    /// </summary>
    public XivChatTypeKind Kind { get; }
}
