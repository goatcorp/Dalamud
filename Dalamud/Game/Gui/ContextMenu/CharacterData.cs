using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// Dalamud wrapper around a ClientStructs CharacterData.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = StructSizeInBytes)]
public unsafe struct CharacterData
{
    /// <summary>
    /// The actual data.
    /// </summary>
    [FieldOffset(0)]
    internal readonly InfoProxyCommonList.CharacterData InternalData;

    private const int StructSizeInBytes = 0x68;

    /// <summary>
    /// The view of the backing data, in <see cref="ulong"/>.
    /// </summary>
    [FieldOffset(0)]
    private fixed ulong dataUInt64[StructSizeInBytes / 0x8];

    static CharacterData()
    {
        Debug.Assert(
            sizeof(InfoProxyCommonList.CharacterData) == StructSizeInBytes,
            $"Definition of {nameof(InfoProxyCommonList.CharacterData)} has been changed. " +
            $"Update {nameof(StructSizeInBytes)} to {sizeof(InfoProxyCommonList.CharacterData)} to accommodate for the size change.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterData"/> struct.
    /// </summary>
    /// <param name="data">Character data to wrap.</param>
    internal CharacterData(InfoProxyCommonList.CharacterData data)
    {
        this.InternalData = data;
    }

    /// <summary>
    /// Gets the content id of the character.
    /// </summary>
    public ulong ContentId => this.InternalData.ContentId;

    /// <summary>
    /// Gets the online status of the character.
    /// </summary>
    public OnlineStatus OnlineStatus => (OnlineStatus)this.InternalData.State;

    /// <summary>
    /// Gets the display group of the character.
    /// </summary>
    public DisplayGroup DisplayGroup => (DisplayGroup)this.InternalData.Group;

    /// <summary>
    /// Gets a value indicating whether the character's home world is different from the current world.
    /// </summary>
    public bool IsFromOtherServer => this.InternalData.IsOtherServer;

    /// <summary>
    /// Gets the sort order of the character.
    /// </summary>
    public byte Sort => this.InternalData.Sort;

    /// <summary>
    /// Gets the current world id of the character.
    /// </summary>
    public ushort CurrentWorld => this.InternalData.CurrentWorld;

    /// <summary>
    /// Gets the home world id of the character.
    /// </summary>
    public ushort HomeWorld => this.InternalData.HomeWorld;

    /// <summary>
    /// Gets the location of the character.
    /// </summary>
    public ushort Location => this.InternalData.Location;

    /// <summary>
    /// Gets the grand company of the character.
    /// </summary>
    public GrandCompany GrandCompany => (GrandCompany)this.InternalData.GrandCompany;

    /// <summary>
    /// Gets the primary client language of the character.
    /// </summary>
    public ClientLanguage ClientLanguage => (ClientLanguage)this.InternalData.ClientLanguage;

    /// <summary>
    /// Gets the supported languages of the character.
    /// </summary>
    public ClientLanguageMask Languages => (ClientLanguageMask)this.InternalData.Languages;

    /// <summary>
    /// Gets the gender of the character.
    /// </summary>
    public byte Gender => this.InternalData.Sex;

    /// <summary>
    /// Gets the job id of the character.
    /// </summary>
    public byte Job => this.InternalData.Job;

    /// <summary>
    /// Gets the name of the character.
    /// </summary>
    public string Name => MemoryHelper.ReadString((nint)Unsafe.AsPointer(ref Unsafe.AsRef(in this.InternalData.Name[0])), 32);

    /// <summary>
    /// Gets the free company tag of the character.
    /// </summary>
    public string FCTag => MemoryHelper.ReadString((nint)Unsafe.AsPointer(ref Unsafe.AsRef(in this.InternalData.FCTag[0])), 6);
}

/// <summary>
/// Display group of a character. Used for friends.
/// </summary>
public enum DisplayGroup : sbyte
{
    /// <summary>
    /// All display groups.
    /// </summary>
    All = -1,

    /// <summary>
    /// No display group.
    /// </summary>
    None,

    /// <summary>
    /// Star display group.
    /// </summary>
    Star,

    /// <summary>
    /// Circle display group.
    /// </summary>
    Circle,

    /// <summary>
    /// Triangle display group.
    /// </summary>
    Triangle,

    /// <summary>
    /// Diamond display group.
    /// </summary>
    Diamond,

    /// <summary>
    /// Heart display group.
    /// </summary>
    Heart,

    /// <summary>
    /// Spade display group.
    /// </summary>
    Spade,

    /// <summary>
    /// Club display group.
    /// </summary>
    Club,
}

/// <summary>
/// Grand company of a character.
/// </summary>
public enum GrandCompany : byte
{
    /// <summary>
    /// No grand company.
    /// </summary>
    None = 0,

    /// <summary>
    /// The Maelstrom (Limsa Lominsa).
    /// </summary>
    Maelstrom = 1,

    /// <summary>
    /// The Order of the Twin Adder (Gridania).
    /// </summary>
    TwinAdder = 2,

    /// <summary>
    /// The Immortal Flames (Ul'dah).
    /// </summary>
    ImmortalFlames = 3,
}

/// <summary>
/// Online status of a character.
/// </summary>
[Flags]
public enum OnlineStatus : ulong
{
    /// <summary>
    /// They're offline.
    /// </summary>
    Offline = 0,

    /// <summary>
    /// They're a Game QA Tester.
    /// </summary>
    GameQA = 1ul << 1,

    /// <summary>
    /// They're a GM.
    /// </summary>
    GameMaster = 1ul << 2,

    /// <summary>
    /// They're a blue GM.
    /// </summary>
    GameMasterBlue = 1ul << 3,

    /// <summary>
    /// They're an event participant.
    /// </summary>
    EventParticipant = 1ul << 4,

    /// <summary>
    /// They're disconnected (pokeball).
    /// </summary>
    Disconnected = 1ul << 5,

    /// <summary>
    /// They're waiting for friend list approval.
    /// </summary>
    WaitingForFriendListApproval = 1ul << 6,

    /// <summary>
    /// They're waiting for linkshell approval.
    /// </summary>
    WaitingForLinkshellApproval = 1ul << 7,

    /// <summary>
    /// They're waiting for free company approval.
    /// </summary>
    WaitingForFreeCompanyApproval = 1ul << 8,

    /// <summary>
    /// Character not found (?).
    /// </summary>
    NotFound = 1ul << 9,

    /// <summary>
    /// They're offline (?).
    /// </summary>
    OfflineExd = 1ul << 10,

    /// <summary>
    /// They're a battle mentor.
    /// </summary>
    BattleMentor = 1ul << 11,

    /// <summary>
    /// They're busy.
    /// </summary>
    Busy = 1ul << 12,

    /// <summary>
    /// They're in a PvP area.
    /// </summary>
    PvP = 1ul << 13,

    /// <summary>
    /// They're playing Triple Triad.
    /// </summary>
    PlayingTripleTriad = 1ul << 14,

    /// <summary>
    /// They're viewing a cutscene.
    /// </summary>
    ViewingCutscene = 1ul << 15,

    /// <summary>
    /// They're using a chocobo porter.
    /// </summary>
    UsingAChocoboPorter = 1ul << 16,

    /// <summary>
    /// They're away from keyboard.
    /// </summary>
    AwayFromKeyboard = 1ul << 17,

    /// <summary>
    /// They're in gpose.
    /// </summary>
    CameraMode = 1ul << 18,

    /// <summary>
    /// They're looking for repairs.
    /// </summary>
    LookingForRepairs = 1ul << 19,

    /// <summary>
    /// They're looking to repair.
    /// </summary>
    LookingToRepair = 1ul << 20,

    /// <summary>
    /// They're looking to meld materia.
    /// </summary>
    LookingToMeldMateria = 1ul << 21,

    /// <summary>
    /// They're roleplaying.
    /// </summary>
    RolePlaying = 1ul << 22,

    /// <summary>
    /// They're looking for a party.
    /// </summary>
    LookingForParty = 1ul << 23,

    /// <summary>
    /// They're a sword for hire.
    /// </summary>
    SwordForHire = 1ul << 24,

    /// <summary>
    /// They're waiting for duty finder.
    /// </summary>
    WaitingForDutyFinder = 1ul << 25,

    /// <summary>
    /// They're recruiting party members.
    /// </summary>
    RecruitingPartyMembers = 1ul << 26,

    /// <summary>
    /// They're a mentor.
    /// </summary>
    Mentor = 1ul << 27,

    /// <summary>
    /// They're a PvE mentor.
    /// </summary>
    PvEMentor = 1ul << 28,

    /// <summary>
    /// They're a trade mentor.
    /// </summary>
    TradeMentor = 1ul << 29,

    /// <summary>
    /// They're a PvP mentor.
    /// </summary>
    PvPMentor = 1ul << 30,

    /// <summary>
    /// They're a returner.
    /// </summary>
    Returner = 1ul << 31,

    /// <summary>
    /// They're a new adventurer.
    /// </summary>
    NewAdventurer = 1ul << 32,

    /// <summary>
    /// They're the leader of an alliance.
    /// </summary>
    AllianceLeader = 1ul << 33,

    /// <summary>
    /// They're the leader of an alliance party.
    /// </summary>
    AlliancePartyLeader = 1ul << 34,

    /// <summary>
    /// They're a member of an alliance party.
    /// </summary>
    AlliancePartyMember = 1ul << 35,

    /// <summary>
    /// They're the leader of a party.
    /// </summary>
    PartyLeader = 1ul << 36,

    /// <summary>
    /// They're a member of a party.
    /// </summary>
    PartyMember = 1ul << 37,

    /// <summary>
    /// They're the leader of a cross-world party.
    /// </summary>
    PartyLeaderCrossWorld = 1ul << 38,

    /// <summary>
    /// They're a member of a cross-world party.
    /// </summary>
    PartyMemberCrossWorld = 1ul << 39,

    /// <summary>
    /// They're in another world.
    /// </summary>
    AnotherWorld = 1ul << 40,

    /// <summary>
    /// They're in a duty with you.
    /// </summary>
    SharingDuty = 1ul << 41,

    /// <summary>
    /// They're in a similar duty to you.
    /// </summary>
    SimilarDuty = 1ul << 42,

    /// <summary>
    /// They're in duty.
    /// </summary>
    InDuty = 1ul << 43,

    /// <summary>
    /// They're a trail adventurer.
    /// </summary>
    TrailAdventurer = 1ul << 44,

    /// <summary>
    /// They're in your free company.
    /// </summary>
    FreeCompany = 1ul << 45,

    /// <summary>
    /// They're in your grand company.
    /// </summary>
    GrandCompany = 1ul << 46,

    /// <summary>
    /// They're online.
    /// </summary>
    Online = 1ul << 47,
}

/// <summary>
/// Flag enum describing the language a character supports. Primarily for duty finder.
/// </summary>
[Flags]
public enum ClientLanguageMask
{
    /// <summary>
    /// Indicating a Japanese game client.
    /// </summary>
    Japanese = 1,

    /// <summary>
    /// Indicating an English game client.
    /// </summary>
    English = 2,

    /// <summary>
    /// Indicating a German game client.
    /// </summary>
    German = 4,

    /// <summary>
    /// Indicating a French game client.
    /// </summary>
    French = 8,
}
