using Dalamud.Utility;

namespace Dalamud.Game.Text;

/// <summary>
/// Extension methods for the <see cref="XivChatType"/> type.
/// </summary>
public static class XivChatTypeExtensions
{
    /// <summary>
    /// Get the InfoAttribute associated with this chat type.
    /// </summary>
    /// <param name="chatType">The chat type.</param>
    /// <returns>The info attribute.</returns>
    public static XivChatTypeInfoAttribute GetDetails(this XivChatType chatType)
    {
        return chatType.GetAttribute<XivChatTypeInfoAttribute>();
    }
}
