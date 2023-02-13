using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Utility;

namespace Dalamud.Game.Text;

/// <summary>
/// Extension methods for the <see cref="XivChatType2"/> type.
/// </summary>
public static class XivChatType2Extensions
{
    /// <summary>
    /// Get the InfoAttribute associated with this chat type.
    /// </summary>
    /// <param name="chatType">The chat type.</param>
    /// <returns>The info attribute.</returns>
    public static XivChatTypeInfoAttribute GetDetails(this XivChatType2 chatType)
    {
        return chatType.GetAttribute<XivChatTypeInfoAttribute>();
    }

    /// <summary>
    /// Get the MaskAttribute associated with this chat type.
    /// </summary>
    /// <param name="chatType">The chat type.</param>
    /// <returns>The mask kind attribute.</returns>
    public static XivChatTypeKindAttribute GetMaskKind(this XivChatType2 chatType)
    {
        return chatType.GetAttribute<XivChatTypeKindAttribute>();
    }

    /// <summary>
    /// Get the unmasked channel of the chat type (say, tell, shout, etc.).
    /// </summary>
    /// <param name="chatType">The chat type.</param>
    /// <returns>The XivChatType entry for the unmasked channel.</returns>
    public static XivChatType2 GetChatChannel(this XivChatType2 chatType)
    {
        return (XivChatType2)((ushort)chatType & 0x7F);
    }

    /// <summary>
    /// Get the target mask of the chat type (you, party member, pet, etc.).
    /// </summary>
    /// <param name="chatType">The chat type.</param>
    /// <returns>The XivChatType entry for the target mask.</returns>
    public static XivChatType2 GetTargetMask(this XivChatType2 chatType)
    {
        return (XivChatType2)((ushort)chatType & 0x780);
    }

    /// <summary>
    /// Get the source mask of the chat type (you, party member, pet, etc.).
    /// </summary>
    /// <param name="chatType">The chat type.</param>
    /// <returns>The XivChatType entry for the source mask.</returns>
    public static XivChatType2 GetSourceMask(this XivChatType2 chatType)
    {
        return (XivChatType2)((ushort)chatType & 0x7800);
    }

    /// <summary>
    /// Gets a collection of all known chat channels (say, tell, shout, etc.).
    /// </summary>
    /// <returns>A collection of <see cref="XivChatType2"/>.</returns>
    public static XivChatType2[] GetAllChatChannels()
    {
        if (AllChannelsList == null)
        {
            AllChannelsList = Enum.GetValues(typeof(XivChatType2))
                .Cast<XivChatType2>()
                .ToList() // Supposedly a potential efficiency gain by preventing repeated boxing if linq query is deferred.
                .Where(chatType => chatType.GetMaskKind()?.Kind == XivChatTypeKind.Channel)
                .ToList();
        }

        // Return a copy since we're caching this.
        return AllChannelsList.ToArray();
    }

    /// <summary>
    /// Gets a collection of all known target masks (you, party member, pet, etc.).
    /// </summary>
    /// <returns>A collection of <see cref="XivChatType2"/>.</returns>
    public static XivChatType2[] GetAllTargetMasks()
    {
        if (AllTargetMasksList == null)
        {
            AllTargetMasksList = Enum.GetValues(typeof(XivChatType2))
                .Cast<XivChatType2>()
                .ToList() // Supposedly a potential efficiency gain by preventing repeated boxing if linq query is deferred.
                .Where(chatType => chatType == XivChatType2.None || chatType.GetMaskKind()?.Kind == XivChatTypeKind.Target)
                .ToList();
        }

        // Return a copy since we're caching this.
        return AllTargetMasksList.ToArray();
    }

    /// <summary>
    /// Gets a collection of all known source masks (you, party member, pet, etc.).
    /// </summary>
    /// <returns>A collection of <see cref="XivChatType2"/>.</returns>
    public static XivChatType2[] GetAllSourceMasks()
    {
        if (AllSourceMasksList == null)
        {
            AllSourceMasksList = Enum.GetValues(typeof(XivChatType2))
                .Cast<XivChatType2>()
                .ToList() // Supposedly a potential efficiency gain by preventing repeated boxing if linq query is deferred.
                .Where(chatType => chatType == XivChatType2.None || chatType.GetMaskKind()?.Kind == XivChatTypeKind.Source)
                .ToList();
        }

        // Return a copy since we're caching this.
        return AllSourceMasksList.ToArray();
    }

    private static List<XivChatType2> AllChannelsList { get; set; } = null;

    private static List<XivChatType2> AllTargetMasksList { get; set; } = null;

    private static List<XivChatType2> AllSourceMasksList { get; set; } = null;

    //***** TODO: Can/should we override the equality operator to only compare the channel portion?

    //***** TODO: Can we make it so that enumerating the enum values (not using the extensions above) implicitly returns only channels?  Would it even be good to do this?

    //***** TODO: Method to get localized name for chat channel.  Include masks in the text if present?  Should we do this by resource file, by sheets at runtime (it's almost impossible for this to be exhaustive without manual intervention), or put it right in the code with an attribute?

    //***** TODO: Is it better to initialize lists early on (can we do this at compile time?) and just return them instead of creating them every time for the GetAll...() Methods?  How much does it affect performance?
}
