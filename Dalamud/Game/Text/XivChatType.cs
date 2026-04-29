namespace Dalamud.Game.Text;

/// <summary>
/// The FFXIV chat types as seen in the LogKind excel sheet.
/// </summary>
public enum XivChatType : ushort
{
    /// <summary>
    /// No chat type.
    /// </summary>
    None = 0,

    /// <summary>
    /// The debug chat type.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// The urgent chat type.
    /// </summary>
    [XivChatTypeInfo("Urgent", "urgent", 0xFF9400D3)]
    Urgent = 2,

    /// <summary>
    /// The notice chat type.
    /// </summary>
    [XivChatTypeInfo("Notice", "notice", 0xFF9400D3)]
    Notice = 3,

    /// <summary>
    /// The say chat type.
    /// </summary>
    [XivChatTypeInfo("Say", "say", 0xFFFFFFFF)]
    Say = 10,

    /// <summary>
    /// The shout chat type.
    /// </summary>
    [XivChatTypeInfo("Shout", "shout", 0xFFFF4500)]
    Shout = 11,

    /// <summary>
    /// The outgoing tell chat type.
    /// </summary>
    TellOutgoing = 12,

    /// <summary>
    /// The incoming tell chat type.
    /// </summary>
    [XivChatTypeInfo("Tell", "tell", 0xFFFF69B4)]
    TellIncoming = 13,

    /// <summary>
    /// The party chat type.
    /// </summary>
    [XivChatTypeInfo("Party", "party", 0xFF1E90FF)]
    Party = 14,

    /// <summary>
    /// The alliance chat type.
    /// </summary>
    [XivChatTypeInfo("Alliance", "alliance", 0xFFFF4500)]
    Alliance = 15,

    /// <summary>
    /// The linkshell 1 chat type.
    /// </summary>
    [XivChatTypeInfo("Linkshell 1", "ls1", 0xFF228B22)]
    Ls1 = 16,

    /// <summary>
    /// The linkshell 2 chat type.
    /// </summary>
    [XivChatTypeInfo("Linkshell 2", "ls2", 0xFF228B22)]
    Ls2 = 17,

    /// <summary>
    /// The linkshell 3 chat type.
    /// </summary>
    [XivChatTypeInfo("Linkshell 3", "ls3", 0xFF228B22)]
    Ls3 = 18,

    /// <summary>
    /// The linkshell 4 chat type.
    /// </summary>
    [XivChatTypeInfo("Linkshell 4", "ls4", 0xFF228B22)]
    Ls4 = 19,

    /// <summary>
    /// The linkshell 5 chat type.
    /// </summary>
    [XivChatTypeInfo("Linkshell 5", "ls5", 0xFF228B22)]
    Ls5 = 20,

    /// <summary>
    /// The linkshell 6 chat type.
    /// </summary>
    [XivChatTypeInfo("Linkshell 6", "ls6", 0xFF228B22)]
    Ls6 = 21,

    /// <summary>
    /// The linkshell 7 chat type.
    /// </summary>
    [XivChatTypeInfo("Linkshell 7", "ls7", 0xFF228B22)]
    Ls7 = 22,

    /// <summary>
    /// The linkshell 8 chat type.
    /// </summary>
    [XivChatTypeInfo("Linkshell 8", "ls8", 0xFF228B22)]
    Ls8 = 23,

    /// <summary>
    /// The free company chat type.
    /// </summary>
    [XivChatTypeInfo("Free Company", "fc", 0xFF00BFFF)]
    FreeCompany = 24,

    /// <summary>
    /// The novice network chat type.
    /// </summary>
    [XivChatTypeInfo("Novice Network", "nn", 0xFF8B4513)]
    NoviceNetwork = 27,

    /// <summary>
    /// The custom emotes chat type.
    /// </summary>
    [XivChatTypeInfo("Custom Emotes", "emote", 0xFF8B4513)]
    CustomEmote = 28,

    /// <summary>
    /// The standard emotes chat type.
    /// </summary>
    [XivChatTypeInfo("Standard Emotes", "emote", 0xFF8B4513)]
    StandardEmote = 29,

    /// <summary>
    /// The yell chat type.
    /// </summary>
    [XivChatTypeInfo("Yell", "yell", 0xFFFFFF00)]
    Yell = 30,

    /// <summary>
    /// The cross-world party chat type.
    /// </summary>
    [XivChatTypeInfo("Party", "party", 0xFF1E90FF)]
    CrossParty = 32,

    /// <summary>
    /// The PvP team chat type.
    /// </summary>
    [XivChatTypeInfo("PvP Team", "pvpt", 0xFFF4A460)]
    PvPTeam = 36,

    /// <summary>
    /// The cross-world linkshell chat type.
    /// </summary>
    [XivChatTypeInfo("Crossworld Linkshell 1", "cw1", 0xFF1E90FF)]
    CrossLinkShell1 = 37,

    /// <summary>
    /// The action damage chat type.
    /// </summary>
    Damage = 41,

    /// <summary>
    /// The action miss chat type.
    /// </summary>
    Miss = 42,

    /// <summary>
    /// The action chat type.
    /// </summary>
    Action = 43,

    /// <summary>
    /// The item chat type.
    /// </summary>
    Item = 44,

    /// <summary>
    /// The action healing chat type.
    /// </summary>
    Healing = 45,

    /// <summary>
    /// The gain buff chat type.
    /// </summary>
    GainBuff = 46,

    /// <summary>
    /// The gain debuff chat type.
    /// </summary>
    GainDebuff = 47,

    /// <summary>
    /// The lose buff chat type.
    /// </summary>
    LoseBuff = 48,

    /// <summary>
    /// The lose debuff chat type.
    /// </summary>
    LoseDebuff = 49,

    /// <summary>
    /// The glamourer notification chat type.
    /// </summary>
    GlamourNotifications = 54,

    /// <summary>
    /// The alarm chat type.
    /// </summary>
    Alarm = 55,

    /// <summary>
    /// The echo chat type.
    /// </summary>
    [XivChatTypeInfo("Echo", "echo", 0xFF808080)]
    Echo = 56,

    /// <summary>
    /// The system message chat type.
    /// </summary>
    SystemMessage = 57,

    /// <summary>
    /// The system error chat type.
    /// </summary>
    SystemError = 58,

    /// <summary>
    /// The system message (gathering) chat type.
    /// </summary>
    GatheringSystemMessage = 59,

    /// <summary>
    /// The error message chat type.
    /// </summary>
    ErrorMessage = 60,

    /// <summary>
    /// The NPC Dialogue chat type.
    /// </summary>
    NPCDialogue = 61,

    /// <summary>
    /// The loot notice chat type.
    /// </summary>
    LootNotice = 62,

    /// <summary>
    /// The progress chat type.
    /// </summary>
    Progress = 64,

    /// <summary>
    /// The loot roll chat type.
    /// </summary>
    LootRoll = 65,

    /// <summary>
    /// The crafting chat type.
    /// </summary>
    Crafting = 66,

    /// <summary>
    /// The gathering chat type.
    /// </summary>
    Gathering = 67,

    /// <summary>
    /// The NPC Dialogue (Announcements) chat type.
    /// </summary>
    NPCDialogueAnnouncements = 68,

    /// <summary>
    /// The fc announcement chat type.
    /// </summary>
    FreeCompanyAnnouncement = 69,

    /// <summary>
    /// The fc login and logout chat type.
    /// </summary>
    FreeCompanyLoginLogout = 70,

    /// <summary>
    /// The retainer sale chat type.
    /// </summary>
    /// <remarks>
    /// This might be used for other purposes.
    /// </remarks>
    RetainerSale = 71,

    /// <summary>
    /// The periodic recruitment notification chat type.
    /// </summary>
    PeriodicRecruitmentNotification = 72,

    /// <summary>
    /// The sign chat type.
    /// </summary>
    Sign = 73,

    /// <summary>
    /// The random number chat type.
    /// </summary>
    RandomNumber = 74,

    /// <summary>
    /// The NN system chat type.
    /// </summary>
    NoviceNetworkSystem = 75,

    /// <summary>
    /// The orchestrion chat type.
    /// </summary>
    Orchestrion = 76,

    /// <summary>
    /// The pvp team announcement chat type.
    /// </summary>
    PvpTeamAnnouncement = 77,

    /// <summary>
    /// The pvp team login and logout chat type.
    /// </summary>
    PvpTeamLoginLogout = 78,

    /// <summary>
    /// The message book chat type.
    /// </summary>
    MessageBook = 79,

    /// <summary>
    /// The GM tell chat type.
    /// </summary>
    GmTell = 80,

    /// <summary>
    /// The GM say chat type.
    /// </summary>
    GmSay = 81,

    /// <summary>
    /// The GM shout chat type.
    /// </summary>
    GmShout = 82,

    /// <summary>
    /// The GM yell chat type.
    /// </summary>
    GmYell = 83,

    /// <summary>
    /// The GM party chat type.
    /// </summary>
    GmParty = 84,

    /// <summary>
    /// The GM fc chat type.
    /// </summary>
    GmFreeCompany = 85,

    /// <summary>
    /// The GM ls1 chat type.
    /// </summary>
    GmLinkshell1 = 86,

    /// <summary>
    /// The GM ls2 chat type.
    /// </summary>
    GmLinkshell2 = 87,

    /// <summary>
    /// The GM ls3 chat type.
    /// </summary>
    GmLinkshell3 = 88,

    /// <summary>
    /// The GM ls4 chat type.
    /// </summary>
    GmLinkshell4 = 89,

    /// <summary>
    /// The GM ls5 chat type.
    /// </summary>
    GmLinkshell5 = 90,

    /// <summary>
    /// The GM ls6 chat type.
    /// </summary>
    GmLinkshell6 = 91,

    /// <summary>
    /// The GM ls7 chat type.
    /// </summary>
    GmLinkshell7 = 92,

    /// <summary>
    /// The GM ls8 chat type.
    /// </summary>
    GmLinkshell8 = 93,

    /// <summary>
    /// The GM NN chat type.
    /// </summary>
    GmNoviceNetwork = 94,

    /// <summary>
    /// The cross-world linkshell 2 chat type.
    /// </summary>
    [XivChatTypeInfo("Crossworld Linkshell 2", "cw2", 0xFF1E90FF)]
    CrossLinkShell2 = 101,

    /// <summary>
    /// The cross-world linkshell 3 chat type.
    /// </summary>
    [XivChatTypeInfo("Crossworld Linkshell 3", "cw3", 0xFF1E90FF)]
    CrossLinkShell3 = 102,

    /// <summary>
    /// The cross-world linkshell 4 chat type.
    /// </summary>
    [XivChatTypeInfo("Crossworld Linkshell 4", "cw4", 0xFF1E90FF)]
    CrossLinkShell4 = 103,

    /// <summary>
    /// The cross-world linkshell 5 chat type.
    /// </summary>
    [XivChatTypeInfo("Crossworld Linkshell 5", "cw5", 0xFF1E90FF)]
    CrossLinkShell5 = 104,

    /// <summary>
    /// The cross-world linkshell 6 chat type.
    /// </summary>
    [XivChatTypeInfo("Crossworld Linkshell 6", "cw6", 0xFF1E90FF)]
    CrossLinkShell6 = 105,

    /// <summary>
    /// The cross-world linkshell 7 chat type.
    /// </summary>
    [XivChatTypeInfo("Crossworld Linkshell 7", "cw7", 0xFF1E90FF)]
    CrossLinkShell7 = 106,

    /// <summary>
    /// The cross-world linkshell 8 chat type.
    /// </summary>
    [XivChatTypeInfo("Crossworld Linkshell 8", "cw8", 0xFF1E90FF)]
    CrossLinkShell8 = 107,
}
