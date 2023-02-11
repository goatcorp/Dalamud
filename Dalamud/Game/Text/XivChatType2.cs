using System;

namespace Dalamud.Game.Text;

/// <summary>
/// The FFXIV chat types, the channels of which are seen in the LogKind sheet.
/// This enum also includes masks for source and target.
/// A complete XivChatType as used by the game is a bitwise or of channel, target mask, and source mask.  The masks can be zero.
/// </summary>
public enum XivChatType2 : ushort
{
    // Channels start at 0x0 and go as high as 0x7F (bits 0-6).
    #region Channels

    /// <summary>
    /// No chat type.
    /// </summary>
    None = 0,

    /// <summary>
    /// The debug chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    Debug = 1,

    /// <summary>
    /// The urgent chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Urgent", "urgent", 0xFF9400D3)]
    Urgent = 2,

    /// <summary>
    /// The notice chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Notice", "notice", 0xFF9400D3)]
    Notice = 3,

    /// <summary>
    /// The say chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Say", "say", 0xFFFFFFFF)]
    Say = 10,

    /// <summary>
    /// The shout chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Shout", "shout", 0xFFFF4500)]
    Shout = 11,

    /// <summary>
    /// The outgoing tell chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    TellOutgoing = 12,

    /// <summary>
    /// The incoming tell chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Tell", "tell", 0xFFFF69B4)]
    TellIncoming = 13,

    /// <summary>
    /// The party chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Party", "party", 0xFF1E90FF)]
    Party = 14,

    /// <summary>
    /// The alliance chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Alliance", "alliance", 0xFFFF4500)]
    Alliance = 15,

    /// <summary>
    /// The linkshell 1 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Linkshell 1", "ls1", 0xFF228B22)]
    Ls1 = 16,

    /// <summary>
    /// The linkshell 2 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Linkshell 2", "ls2", 0xFF228B22)]
    Ls2 = 17,

    /// <summary>
    /// The linkshell 3 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Linkshell 3", "ls3", 0xFF228B22)]
    Ls3 = 18,

    /// <summary>
    /// The linkshell 4 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Linkshell 4", "ls4", 0xFF228B22)]
    Ls4 = 19,

    /// <summary>
    /// The linkshell 5 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Linkshell 5", "ls5", 0xFF228B22)]
    Ls5 = 20,

    /// <summary>
    /// The linkshell 6 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Linkshell 6", "ls6", 0xFF228B22)]
    Ls6 = 21,

    /// <summary>
    /// The linkshell 7 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Linkshell 7", "ls7", 0xFF228B22)]
    Ls7 = 22,

    /// <summary>
    /// The linkshell 8 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Linkshell 8", "ls8", 0xFF228B22)]
    Ls8 = 23,

    /// <summary>
    /// The free company chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Free Company", "fc", 0xFF00BFFF)]
    FreeCompany = 24,

    /// <summary>
    /// The novice network chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Novice Network", "nn", 0xFF8B4513)]
    NoviceNetwork = 27,

    /// <summary>
    /// The custom emotes chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Custom Emotes", "emote", 0xFF8B4513)]
    CustomEmote = 28,

    /// <summary>
    /// The standard emotes chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Standard Emotes", "emote", 0xFF8B4513)]
    StandardEmote = 29,

    /// <summary>
    /// The yell chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Yell", "yell", 0xFFFFFF00)]
    Yell = 30,

    /// <summary>
    /// The cross-world party chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Party", "party", 0xFF1E90FF)]
    CrossParty = 32,

    /// <summary>
    /// The PvP team chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("PvP Team", "pvpt", 0xFFF4A460)]
    PvPTeam = 36,

    /// <summary>
    /// The cross-world linkshell chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Crossworld Linkshell 1", "cw1", 0xFF1E90FF)]
    CrossLinkShell1 = 37,

    /// <summary>
    /// The battle log (damage) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    BattleLog_Damage = 41,

    /// <summary>
    /// The battle log (misses) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    BattleLog_Miss = 42,

    /// <summary>
    /// The battle log (actions) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    BattleLog_Action = 43,

    /// <summary>
    /// The battle log (items) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    BattleLog_Item = 44,

    /// <summary>
    /// The battle log (healing) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    BattleLog_Healing = 45,

    /// <summary>
    /// The battle log (buff) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    BattleLog_Buff = 46,

    /// <summary>
    /// The battle log (debuff) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    BattleLog_Debuff = 47,

    /// <summary>
    /// The battle log (buff expiring) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    BattleLog_BuffExpiration = 48,

    /// <summary>
    /// The battle log (debuff expiring) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    BattleLog_DebuffExpiration = 49,

    /// <summary>
    /// The alarm notification chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    AlarmNotification = 55,

    /// <summary>
    /// The echo chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Echo", "echo", 0xFF808080)]
    Echo = 56,

    /// <summary>
    /// The system message chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    SystemMessage = 57,

    /// <summary>
    /// The system message (battle) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    BattleSystemMessage = 58,

    /// <summary>
    /// The system error chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [Obsolete("This value is improperly named, use \"BattleSystemMessage\" instead for the same channel with the proper name.", true)]
    SystemError = 58,

    /// <summary>
    /// The system message (gathering) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    GatheringSystemMessage = 59,

    /// <summary>
    /// The error message chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    ErrorMessage = 60,

    /// <summary>
    /// The NPC Dialogue chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    NPCDialogue = 61,

    /// <summary>
    /// The Loot Notice (i.e., obtaining an item) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    LootNotice = 62,

    /// <summary>
    /// The Progression Message (i.e., level up) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    ProgressionMessage = 64,

    /// <summary>
    /// The Loot Message (i.e., need/greed roll numbers) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    LootMessage = 65,

    /// <summary>
    /// The synthesis messages chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    SynthesisMessage = 66,

    /// <summary>
    /// The gathering messages chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    GatheringMessage = 67,

    /// <summary>
    /// The NPC Dialogue (Announcements) chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    NPCDialogueAnnouncements = 68,

    /// <summary>
    /// The free company announcements chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    FreeCompanyAnnouncement = 69,

    /// <summary>
    /// The free company login chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    FreeCompanyLogin = 70,

    /// <summary>
    /// The retainer sale chat type.
    /// </summary>
    /// <remarks>
    /// This might be used for other purposes.
    /// </remarks>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    RetainerSale = 71,

    /// <summary>
    /// The Periodic Recruitment Notifications (i.e., "Of the X parties currently recruiting...") chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    RecruitmentNotification = 72,

    /// <summary>
    /// The PC Sign (i.e, "Attack 1") chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    SignMessage = 73,

    /// <summary>
    /// The Random Number Messages chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    RandomNumberMessage = 74,

    /// <summary>
    /// The Novice Network Notifications chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    NoviceNetworkNotification = 75,

    /// <summary>
    /// The current orchestrion track chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    OrchestrionTrackMessage = 76,

    /// <summary>
    /// The PvP team announcements chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    PvPTeamAnnouncement = 77,

    /// <summary>
    /// The PvP team logins chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    PvPTeamLogin = 78,

    /// <summary>
    /// The message book alerts chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    MessageBookAlert = 79,

    /// <summary>
    /// The cross-world linkshell 2 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Crossworld Linkshell 2", "cw2", 0xFF1E90FF)]
    CrossLinkShell2 = 101,

    /// <summary>
    /// The cross-world linkshell 3 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Crossworld Linkshell 3", "cw3", 0xFF1E90FF)]
    CrossLinkShell3 = 102,

    /// <summary>
    /// The cross-world linkshell 4 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Crossworld Linkshell 4", "cw4", 0xFF1E90FF)]
    CrossLinkShell4 = 103,

    /// <summary>
    /// The cross-world linkshell 5 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Crossworld Linkshell 5", "cw5", 0xFF1E90FF)]
    CrossLinkShell5 = 104,

    /// <summary>
    /// The cross-world linkshell 6 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Crossworld Linkshell 6", "cw6", 0xFF1E90FF)]
    CrossLinkShell6 = 105,

    /// <summary>
    /// The cross-world linkshell 7 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Crossworld Linkshell 7", "cw7", 0xFF1E90FF)]
    CrossLinkShell7 = 106,

    /// <summary>
    /// The cross-world linkshell 8 chat type.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Channel)]
    [XivChatTypeInfo("Crossworld Linkshell 8", "cw8", 0xFF1E90FF)]
    CrossLinkShell8 = 107,

    #endregion

    // ***** TODO: Possible additional masks to look for:
    // Enemy Pets
    // Friendly NPC Pets
    // Hostile players and pets
    // These seem unlikely to exist though, considering the log filter settings in the game.

    // Target masks start at 0x80 and go as high as 0x780 (bits 7-10).
    #region Target Masks

    /// <summary>
    /// The target mask for the player.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Target)]
    TargetMask_You = 0x80,

    /// <summary>
    /// The target mask for party members.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Target)]
    TargetMask_PartyMember = 0x100,

    /// <summary>
    /// The target mask for alliance members.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Target)]
    TargetMask_AllianceMember = 0x180,

    /// <summary>
    /// The target mask for other players.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Target)]
    TargetMask_OtherPlayers = 0x200,

    /// <summary>
    /// The target mask for enemies.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Target)]
    TargetMask_Enemy = 0x280,

    /// <summary>
    /// The target mask for unengaged enemies.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Target)]
    TargetMask_UnengagedEnemy = 0x300,

    /// <summary>
    /// The target mask for friendly NPCs.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Target)]
    TargetMask_FriendlyNPC = 0x380,

    /// <summary>
    /// The target mask for the player's pet.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Target)]
    TargetMask_YourPet = 0x400,

    /// <summary>
    /// The target mask for party members' pets.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Target)]
    TargetMask_PartyPet = 0x480,

    /// <summary>
    /// The target mask for alliance members' pets.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Target)]
    TargetMask_AlliancePet = 0x500,

    /// <summary>
    /// The target mask for other players' pets.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Target)]
    TargetMask_OtherPlayerPet = 0x580,

    #endregion

    // Source masks start at 0x800 and go as high as 0x7800 (bits 11-14).
    #region Source Masks

    /// <summary>
    /// The source mask for the player.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Source)]
    SourceMask_You = 0x800,

    /// <summary>
    /// The source mask for party members.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Source)]
    SourceMask_PartyMember = 0x1000,

    /// <summary>
    /// The source mask for alliance members.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Source)]
    SourceMask_AllianceMember = 0x1800,

    /// <summary>
    /// The source mask for other players.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Source)]
    SourceMask_OtherPlayers = 0x2000,

    /// <summary>
    /// The source mask for enemies.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Source)]
    SourceMask_Enemy = 0x2800,

    /// <summary>
    /// The source mask for unengaged enemies.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Source)]
    SourceMask_UnengagedEnemy = 0x3000,

    /// <summary>
    /// The source mask for friendly NPCs.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Source)]
    SourceMask_FriendlyNPC = 0x3800,

    /// <summary>
    /// The source mask for the player's pet.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Source)]
    SourceMask_YourPet = 0x4000,

    /// <summary>
    /// The source mask for party members' pets.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Source)]
    SourceMask_PartyPet = 0x4800,

    /// <summary>
    /// The source mask for alliance members' pets.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Source)]
    SourceMask_AlliancePet = 0x5000,

    /// <summary>
    /// The source mask for other players' pets.
    /// </summary>
    [XivChatType2Mask(XivChatType2EntryKind.Source)]
    SourceMask_OtherPlayerPet = 0x5800,

    #endregion
}
