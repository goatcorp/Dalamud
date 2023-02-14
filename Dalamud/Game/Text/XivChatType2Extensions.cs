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
    // Property to cache the relevant types to avoid repeat queries.
    private static List<XivChatType2> AllChannelsList { get; set; } = null;

    // Property to cache the relevant types to avoid repeat queries.
    private static List<XivChatType2> AllTargetMasksList { get; set; } = null;

    // Property to cache the relevant types to avoid repeat queries.
    private static List<XivChatType2> AllSourceMasksList { get; set; } = null;

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
    /// <returns>The chat type's "kind".</returns>
    public static XivChatTypeKind GetKind(this XivChatType2 chatType)
    {
        return chatType.GetAttribute<XivChatTypeKindAttribute>()?.Kind ?? XivChatTypeKind.Unknown;
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
    /// Get the current-language name of the chat type, if there is one.
    /// </summary>
    /// <param name="chatType">The chat type.</param>
    /// <returns>A string containing the XivChatType's name.</returns>
    public static string GetTranslatedName(this XivChatType2 chatType)
    {
        // ***** TODO: Stubbed for now.
        return $"{chatType}";
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
                .Where(chatType => chatType.GetKind() == XivChatTypeKind.Channel)
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
                .Where(chatType => chatType == XivChatType2.None || chatType.GetKind() == XivChatTypeKind.Target)
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
                .Where(chatType => chatType == XivChatType2.None || chatType.GetKind() == XivChatTypeKind.Source)
                .ToList();
        }

        // Return a copy since we're caching this.
        return AllSourceMasksList.ToArray();
    }
}
