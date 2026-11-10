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
    public static XivChatTypeInfoAttribute? GetDetails(this XivChatType chatType)
    {
        return chatType.GetAttribute<XivChatTypeInfoAttribute>();
    }

    /// <summary>
    /// Gets whether the chat type is used by a GM to talk.
    /// </summary>
    /// <param name="type">The chat type.</param>
    /// <returns>True if used by GM.</returns>
    public static bool IsUsedByGm(this XivChatType type) => type switch
    {
        XivChatType.GmTell => true,
        XivChatType.GmSay => true,
        XivChatType.GmShout => true,
        XivChatType.GmYell => true,
        XivChatType.GmParty => true,
        XivChatType.GmFreeCompany => true,
        XivChatType.GmLinkshell1 => true,
        XivChatType.GmLinkshell2 => true,
        XivChatType.GmLinkshell3 => true,
        XivChatType.GmLinkshell4 => true,
        XivChatType.GmLinkshell5 => true,
        XivChatType.GmLinkshell6 => true,
        XivChatType.GmLinkshell7 => true,
        XivChatType.GmLinkshell8 => true,
        XivChatType.GmNoviceNetwork => true,
        _ => false,
    };

    /// <summary>
    /// Gets whether the chat type utilizes <see cref="XivChatRelationKind"/>.
    /// </summary>
    /// <param name="type">The chat type.</param>
    /// <returns>True if <see cref="XivChatRelationKind"/> are used.</returns>
    public static bool AppliesRelationKind(this XivChatType type) => type switch
    {
        // Battle
        XivChatType.Damage => true,
        XivChatType.Miss => true,
        XivChatType.Action => true,
        XivChatType.Item => true,
        XivChatType.Healing => true,
        XivChatType.GainBuff => true,
        XivChatType.LoseBuff => true,
        XivChatType.GainDebuff => true,
        XivChatType.LoseDebuff => true,

        // Announcements
        XivChatType.SystemMessage => true,
        XivChatType.SystemError => true,
        XivChatType.ErrorMessage => true,
        XivChatType.LootNotice => true,
        XivChatType.Progress => true,
        XivChatType.LootRoll => true,
        XivChatType.Crafting => true,
        XivChatType.Gathering => true,
        XivChatType.FreeCompanyLoginLogout => true,
        XivChatType.PvpTeamLoginLogout => true,
        _ => false,
    };
}
