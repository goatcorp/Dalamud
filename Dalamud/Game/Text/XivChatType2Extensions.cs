using System;
using System.Collections.Generic;
using System.Linq;

using CheapLoc;

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
        // This isn't great, but right now, we can't do anything nicer without objectionable changes.
        return chatType switch
        {
            XivChatType2.None => Loc.Localize("XivChatType.None", "None"),
            XivChatType2.Debug => Loc.Localize("XivChatType.Debug", "Debug"),
            XivChatType2.Urgent => Loc.Localize("XivChatType.Urgent", "Urgent"),
            XivChatType2.Notice => Loc.Localize("XivChatType.Notice", "Notice"),
            XivChatType2.Say => Loc.Localize("XivChatType.Say", "Say"),
            XivChatType2.Shout => Loc.Localize("XivChatType.Shout", "Shout"),
            XivChatType2.TellOutgoing => Loc.Localize("XivChatType.TellOutgoing", "Tell (Outgoing)"),
            XivChatType2.TellIncoming => Loc.Localize("XivChatType.TellIncoming", "Tell (Incoming)"),
            XivChatType2.Party => Loc.Localize("XivChatType.Party", "Party"),
            XivChatType2.Alliance => Loc.Localize("XivChatType.Alliance", "Alliance"),
            XivChatType2.Ls1 => Loc.Localize("XivChatType.Ls1", "Linkshell 1"),
            XivChatType2.Ls2 => Loc.Localize("XivChatType.Ls2", "Linkshell 2"),
            XivChatType2.Ls3 => Loc.Localize("XivChatType.Ls3", "Linkshell 3"),
            XivChatType2.Ls4 => Loc.Localize("XivChatType.Ls4", "Linkshell 4"),
            XivChatType2.Ls5 => Loc.Localize("XivChatType.Ls5", "Linkshell 5"),
            XivChatType2.Ls6 => Loc.Localize("XivChatType.Ls6", "Linkshell 6"),
            XivChatType2.Ls7 => Loc.Localize("XivChatType.Ls7", "Linkshell 7"),
            XivChatType2.Ls8 => Loc.Localize("XivChatType.Ls8", "Linkshell 8"),
            XivChatType2.FreeCompany => Loc.Localize("XivChatType.FreeCompany", "Free Company"),
            XivChatType2.NoviceNetwork => Loc.Localize("XivChatType.NoviceNetwork", "Novice Network"),
            XivChatType2.CustomEmote => Loc.Localize("XivChatType.CustomEmote", "Custom Emote"),
            XivChatType2.StandardEmote => Loc.Localize("XivChatType.StandardEmote", "Standard Emote"),
            XivChatType2.Yell => Loc.Localize("XivChatType.Yell", "Yell"),
            XivChatType2.CrossParty => Loc.Localize("XivChatType.CrossParty", "Cross-World Party"),
            XivChatType2.PvPTeam => Loc.Localize("XivChatType.PvPTeam", "PvP Team"),
            XivChatType2.CrossLinkShell1 => Loc.Localize("XivChatType.CrossLinkShell1", "Cross-World Linkshell 1"),
            XivChatType2.BattleLog_Damage => Loc.Localize("XivChatType.BattleLog_Damage", "Battle Log (Damage)"),
            XivChatType2.BattleLog_Miss => Loc.Localize("XivChatType.BattleLog_Miss", "Battle Log (Misses)"),
            XivChatType2.BattleLog_Action => Loc.Localize("XivChatType.BattleLog_Action", "Battle Log (Actions)"),
            XivChatType2.BattleLog_Item => Loc.Localize("XivChatType.BattleLog_Item", "Battle Log (Items)"),
            XivChatType2.BattleLog_Healing => Loc.Localize("XivChatType.BattleLog_Healing", "Battle Log (Healing)"),
            XivChatType2.BattleLog_Buff => Loc.Localize("XivChatType.BattleLog_Buff", "Battle Log (Buffs)"),
            XivChatType2.BattleLog_Debuff => Loc.Localize("XivChatType.BattleLog_Debuff", "Battle Log (Debuffs)"),
            XivChatType2.BattleLog_BuffExpiration => Loc.Localize("XivChatType.BattleLog_BuffExpiration", "Battle Log (Buffs Expiring)"),
            XivChatType2.BattleLog_DebuffExpiration => Loc.Localize("XivChatType.BattleLog_DebuffExpiration", "Battle Log (Debuffs Expiring)"),
            XivChatType2.AlarmNotification => Loc.Localize("XivChatType.AlarmNotification", "Alarm Notifications"),
            XivChatType2.Echo => Loc.Localize("XivChatType.Echo", "Echo"),
            XivChatType2.SystemMessage => Loc.Localize("XivChatType.SystemMessage", "System Messages"),
            XivChatType2.BattleSystemMessage => Loc.Localize("XivChatType.BattleSystemMessage", "Battle System Messages"),
            XivChatType2.GatheringSystemMessage => Loc.Localize("XivChatType.GatheringSystemMessage", "Gathering System Messages"),
            XivChatType2.ErrorMessage => Loc.Localize("XivChatType.ErrorMessage", "Error Messages"),
            XivChatType2.NPCDialogue => Loc.Localize("XivChatType.NPCDialogue", "NPC Dialogue"),
            XivChatType2.LootNotice => Loc.Localize("XivChatType.LootNotice", "Loot Notices"),
            XivChatType2.ProgressionMessage => Loc.Localize("XivChatType.ProgressionMessage", "Progression Messages" ),
            XivChatType2.LootMessage => Loc.Localize("XivChatType.LootMessage", "Loot Messages"),
            XivChatType2.SynthesisMessage => Loc.Localize("XivChatType.SynthesisMessage", "Synthesis Messages"),
            XivChatType2.GatheringMessage => Loc.Localize("XivChatType.GatheringMessage", "Gathering Messages"),
            XivChatType2.NPCDialogueAnnouncements => Loc.Localize("XivChatType.NPCDialogueAnnouncements", "NPC Dialogue (Announcements)"),
            XivChatType2.FreeCompanyAnnouncement => Loc.Localize("XivChatType.FreeCompanyAnnouncement", "Free Company Announcements"),
            XivChatType2.FreeCompanyLogin => Loc.Localize("XivChatType.FreeCompanyLogin", "Free Company Logins"),
            XivChatType2.RetainerSale => Loc.Localize("XivChatType.RetainerSale", "Retainer Sales"),
            XivChatType2.RecruitmentNotification => Loc.Localize("XivChatType.RecruitmentNotification", "Recruitment Notifications"),
            XivChatType2.SignMessage => Loc.Localize("XivChatType.SignMessage", "Sign Messages"),
            XivChatType2.RandomNumberMessage => Loc.Localize("XivChatType.RandomNumberMessage", "Random Number Messages"),
            XivChatType2.NoviceNetworkNotification => Loc.Localize("XivChatType.NoviceNetworkNotification", "Novice Network Notifications"),
            XivChatType2.OrchestrionTrackMessage => Loc.Localize("XivChatType.OrchestrionTrackMessage", "Orchestrion Track Messages"),
            XivChatType2.PvPTeamAnnouncement => Loc.Localize("XivChatType.PvPTeamAnnouncement", "PvP Team Announcements"),
            XivChatType2.PvPTeamLogin => Loc.Localize("XivChatType.PvPTeamLogin", "PvP Team Logins"),
            XivChatType2.MessageBookAlert => Loc.Localize("XivChatType.MessageBookAlert", "Message Book Alerts"),
            XivChatType2.GM_TellIncoming => Loc.Localize("XivChatType.GM_TellIncoming", "Tell (From GM)"),
            XivChatType2.GM_Say => Loc.Localize("XivChatType.GM_Say", "Say (From GM)"),
            XivChatType2.GM_Shout => Loc.Localize("XivChatType.GM_Shout", "Shout (From GM)"),
            XivChatType2.GM_Yell => Loc.Localize("XivChatType.GM_Yell", "Yell (From GM)"),
            XivChatType2.GM_Party => Loc.Localize("XivChatType.GM_Party", "Party (From GM)"),
            XivChatType2.GM_FreeCompany => Loc.Localize("XivChatType.GM_FreeCompany", "Free Company (From GM)"),
            XivChatType2.GM_Ls1 => Loc.Localize("XivChatType.GM_Ls1", "Linkshell 1 (From GM)"),
            XivChatType2.GM_Ls2 => Loc.Localize("XivChatType.GM_Ls2", "Linkshell 2 (From GM)"),
            XivChatType2.GM_Ls3 => Loc.Localize("XivChatType.GM_Ls3", "Linkshell 3 (From GM)"),
            XivChatType2.GM_Ls4 => Loc.Localize("XivChatType.GM_Ls4", "Linkshell 4 (From GM)"),
            XivChatType2.GM_Ls5 => Loc.Localize("XivChatType.GM_Ls5", "Linkshell 5 (From GM)"),
            XivChatType2.GM_Ls6 => Loc.Localize("XivChatType.GM_Ls6", "Linkshell 6 (From GM)"),
            XivChatType2.GM_Ls7 => Loc.Localize("XivChatType.GM_Ls7", "Linkshell 7 (From GM)"),
            XivChatType2.GM_Ls8 => Loc.Localize("XivChatType.GM_Ls8", "Linkshell 8 (From GM)"),
            XivChatType2.GM_NoviceNetwork => Loc.Localize("XivChatType.GM_NoviceNetwork", "Novice Network (From GM)"),
            XivChatType2.CrossLinkShell2 => Loc.Localize("XivChatType.CrossLinkShell2", "Cross-World Linkshell 2"),
            XivChatType2.CrossLinkShell3 => Loc.Localize("XivChatType.CrossLinkShell3", "Cross-World Linkshell 3"),
            XivChatType2.CrossLinkShell4 => Loc.Localize("XivChatType.CrossLinkShell4", "Cross-World Linkshell 4"),
            XivChatType2.CrossLinkShell5 => Loc.Localize("XivChatType.CrossLinkShell5", "Cross-World Linkshell 5"),
            XivChatType2.CrossLinkShell6 => Loc.Localize("XivChatType.CrossLinkShell6", "Cross-World Linkshell 6"),
            XivChatType2.CrossLinkShell7 => Loc.Localize("XivChatType.CrossLinkShell7", "Cross-World Linkshell 7"),
            XivChatType2.CrossLinkShell8 => Loc.Localize("XivChatType.CrossLinkShell8", "Cross-World Linkshell 8"),

            XivChatType2.TargetMask_You => Loc.Localize("XivChatType.TargetMask_You", "Target Mask - You"),
            XivChatType2.TargetMask_PartyMember => Loc.Localize("XivChatType.TargetMask_PartyMember", "Target Mask - Party Members"),
            XivChatType2.TargetMask_AllianceMember => Loc.Localize("XivChatType.TargetMask_AllianceMember", "Target Mask - Alliance Members"),
            XivChatType2.TargetMask_OtherPlayers => Loc.Localize("XivChatType.TargetMask_OtherPlayers", "Target Mask - Other Players"),
            XivChatType2.TargetMask_EngagedEnemy => Loc.Localize("XivChatType.TargetMask_EngagedEnemy", "Target Mask - Engaged Enemies"),
            XivChatType2.TargetMask_UnengagedEnemy => Loc.Localize("XivChatType.TargetMask_UnengagedEnemy", "Target Mask - Unengaged Enemies"),
            XivChatType2.TargetMask_FriendlyNPC => Loc.Localize("XivChatType.TargetMask_FriendlyNPC", "Target Mask - Friendly NPCs"),
            XivChatType2.TargetMask_YourPet => Loc.Localize("XivChatType.TargetMask_YourPet", "Target Mask - Your Pets/Companions"),
            XivChatType2.TargetMask_PartyPet => Loc.Localize("XivChatType.TargetMask_PartyPet", "Target Mask - Party Members' Pets/Companions"),
            XivChatType2.TargetMask_AlliancePet => Loc.Localize("XivChatType.TargetMask_AlliancePet", "Target Mask - Alliance Members' Pets/Companions"),
            XivChatType2.TargetMask_OtherPlayerPet => Loc.Localize("XivChatType.TargetMask_OtherPlayerPet", "Target Mask - Other Players' Pets/Companions"),

            XivChatType2.SourceMask_You => Loc.Localize("XivChatType.SourceMask_You", "Source Mask - You"),
            XivChatType2.SourceMask_PartyMember => Loc.Localize("XivChatType.SourceMask_PartyMember", "Source Mask - Party Members"),
            XivChatType2.SourceMask_AllianceMember => Loc.Localize("XivChatType.SourceMask_AllianceMember", "Source Mask - Alliance Members"),
            XivChatType2.SourceMask_OtherPlayers => Loc.Localize("XivChatType.SourceMask_OtherPlayers", "Source Mask - Other Players"),
            XivChatType2.SourceMask_EngagedEnemy => Loc.Localize("XivChatType.SourceMask_EngagedEnemy", "Source Mask - Engaged Enemies"),
            XivChatType2.SourceMask_UnengagedEnemy => Loc.Localize("XivChatType.SourceMask_UnengagedEnemy", "Source Mask - Unengaged Enemies"),
            XivChatType2.SourceMask_FriendlyNPC => Loc.Localize("XivChatType.SourceMask_FriendlyNPC", "Source Mask - Friendly NPCs"),
            XivChatType2.SourceMask_YourPet => Loc.Localize("XivChatType.SourceMask_YourPet", "Source Mask - Your Pets/Companions"),
            XivChatType2.SourceMask_PartyPet => Loc.Localize("XivChatType.SourceMask_PartyPet", "Source Mask - Party Members' Pets/Companions"),
            XivChatType2.SourceMask_AlliancePet => Loc.Localize("XivChatType.SourceMask_AlliancePet", "Source Mask - Alliance Members' Pets/Companions"),
            XivChatType2.SourceMask_OtherPlayerPet => Loc.Localize("XivChatType.SourceMask_OtherPlayerPet", "Source Mask - Other Players' Pets/Companions"),

            _ => $"{chatType}",
        };
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
