using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Memory;

using FFXIVClientStructs.FFXIV.Client.UI.Info;

using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// Dalamud wrapper around a ClientStructs CharacterData.
/// </summary>
public unsafe class CharacterData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterData"/> class.
    /// </summary>
    /// <param name="data">Character data to wrap.</param>
    internal CharacterData(InfoProxyCommonList.CharacterData data)
    {
        this.ContentId = data.ContentId;
        this.StatusMask = (ulong)data.State;

        var statuses = new List<ExcelResolver<OnlineStatus>>();
        for (var i = 0; i < 64; i++)
        {
            if ((this.StatusMask & (1UL << i)) != 0)
                statuses.Add(new((uint)i));
        }

        this.Statuses = statuses;
        this.DisplayGroup = (DisplayGroup)data.Group;
        this.IsFromOtherServer = data.IsOtherServer;
        this.Sort = data.Sort;
        this.CurrentWorld = new(data.CurrentWorld);
        this.HomeWorld = new(data.HomeWorld);
        this.Location = new(data.Location);
        this.GrandCompany = new((uint)data.GrandCompany);
        this.ClientLanguage = (ClientLanguage)data.ClientLanguage;
        this.LanguageMask = (byte)data.Languages;

        var languages = new List<ClientLanguage>();
        for (var i = 0; i < 4; i++)
        {
            if ((this.LanguageMask & (1 << i)) != 0)
                languages.Add((ClientLanguage)i);
        }

        this.Languages = languages;

        this.Name = MemoryHelper.ReadString((nint)Unsafe.AsPointer(ref Unsafe.AsRef(in data.Name[0])), 32);
        this.FCTag = MemoryHelper.ReadString((nint)Unsafe.AsPointer(ref Unsafe.AsRef(in data.FCTag[0])), 6);
    }

    /// <summary>
    /// Gets the content id of the character.
    /// </summary>
    public ulong ContentId { get; }

    /// <summary>
    /// Gets the status mask of the character.
    /// </summary>
    public ulong StatusMask { get; }

    /// <summary>
    /// Gets the applicable statues of the character.
    /// </summary>
    public IReadOnlyList<ExcelResolver<OnlineStatus>> Statuses { get; }

    /// <summary>
    /// Gets the display group of the character.
    /// </summary>
    public DisplayGroup DisplayGroup { get; }

    /// <summary>
    /// Gets a value indicating whether the character's home world is different from the current world.
    /// </summary>
    public bool IsFromOtherServer { get; }

    /// <summary>
    /// Gets the sort order of the character.
    /// </summary>
    public byte Sort { get; }

    /// <summary>
    /// Gets the current world of the character.
    /// </summary>
    public ExcelResolver<World> CurrentWorld { get; }

    /// <summary>
    /// Gets the home world of the character.
    /// </summary>
    public ExcelResolver<World> HomeWorld { get; }

    /// <summary>
    /// Gets the location of the character.
    /// </summary>
    public ExcelResolver<TerritoryType> Location { get; }

    /// <summary>
    /// Gets the grand company of the character.
    /// </summary>
    public ExcelResolver<GrandCompany> GrandCompany { get; }

    /// <summary>
    /// Gets the primary client language of the character.
    /// </summary>
    public ClientLanguage ClientLanguage { get; }

    /// <summary>
    /// Gets the supported language mask of the character.
    /// </summary>
    public byte LanguageMask { get; }

    /// <summary>
    /// Gets the supported languages the character supports.
    /// </summary>
    public IReadOnlyList<ClientLanguage> Languages { get; }

    /// <summary>
    /// Gets the gender of the character.
    /// </summary>
    public byte Gender { get; }

    /// <summary>
    /// Gets the job of the character.
    /// </summary>
    public ExcelResolver<ClassJob> Job { get; }

    /// <summary>
    /// Gets the name of the character.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the free company tag of the character.
    /// </summary>
    public string FCTag { get; }
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
