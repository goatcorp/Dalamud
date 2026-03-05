using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.UI;

using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Gui.NamePlate;

// TODO: should we use ReadOnlySeStringSpan here?

/// <summary>
/// Provides a read-only view of the nameplate info object data for a nameplate. Modifications to
/// <see cref="NamePlateUpdateHandler"/> fields do not affect this data.
/// </summary>
public interface INamePlateInfoView
{
    /// <summary>
    /// Gets the displayed name for this nameplate according to the nameplate info object.
    /// </summary>
    ReadOnlySeString Name { get; }

    /// <summary>
    /// Gets the displayed free company tag for this nameplate according to the nameplate info object. For this field,
    /// the quote characters which appear on either side of the title are NOT included.
    /// </summary>
    ReadOnlySeString FreeCompanyTag { get; }

    /// <summary>
    /// Gets the displayed free company tag for this nameplate according to the nameplate info object. For this field,
    /// the quote characters which appear on either side of the title ARE included.
    /// </summary>
    ReadOnlySeString QuotedFreeCompanyTag { get; }

    /// <summary>
    /// Gets the displayed title for this nameplate according to the nameplate info object. For this field, the quote
    /// characters which appear on either side of the title are NOT included.
    /// </summary>
    ReadOnlySeString Title { get; }

    /// <summary>
    /// Gets the displayed title for this nameplate according to the nameplate info object. For this field, the quote
    /// characters which appear on either side of the title ARE included.
    /// </summary>
    ReadOnlySeString QuotedTitle { get; }

    /// <summary>
    /// Gets the displayed level text for this nameplate according to the nameplate info object.
    /// </summary>
    ReadOnlySeString LevelText { get; }

    /// <summary>
    /// Gets the flags for this nameplate according to the nameplate info object.
    /// </summary>
    int Flags { get; }

    /// <summary>
    /// Gets a value indicating whether this nameplate is considered 'dirty' or not according to the nameplate
    /// info object.
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Gets a value indicating whether the title for this nameplate is a prefix title or not according to the nameplate
    /// info object. This value is derived from the <see cref="Flags"/> field.
    /// </summary>
    bool IsPrefixTitle { get; }
}

/// <summary>
/// Provides a read-only view of the nameplate info object data for a nameplate. Modifications to
/// <see cref="NamePlateUpdateHandler"/> fields do not affect this data.
/// </summary>
internal unsafe class NamePlateInfoView(RaptureAtkModule.NamePlateInfo* info) : INamePlateInfoView
{
    private ReadOnlySeString? name;
    private ReadOnlySeString? freeCompanyTag;
    private ReadOnlySeString? quotedFreeCompanyTag;
    private ReadOnlySeString? title;
    private ReadOnlySeString? quotedTitle;
    private ReadOnlySeString? levelText;

    /// <inheritdoc/>
    public ReadOnlySeString Name => this.name ??= info->Name.AsReadOnlySeString();

    /// <inheritdoc/>
    public ReadOnlySeString FreeCompanyTag => this.freeCompanyTag ??= NamePlateGui.StripFreeCompanyTagQuotes(info->FcName);

    /// <inheritdoc/>
    public ReadOnlySeString QuotedFreeCompanyTag => this.quotedFreeCompanyTag ??= info->FcName.AsReadOnlySeString();

    /// <inheritdoc/>
    public ReadOnlySeString Title => this.title ??= info->Title.AsReadOnlySeString();

    /// <inheritdoc/>
    public ReadOnlySeString QuotedTitle => this.quotedTitle ??= info->DisplayTitle.AsReadOnlySeString();

    /// <inheritdoc/>
    public ReadOnlySeString LevelText => this.levelText ??= info->LevelText.AsReadOnlySeString();

    /// <inheritdoc/>
    public int Flags => info->Flags;

    /// <inheritdoc/>
    public bool IsDirty => info->IsDirty;

    /// <inheritdoc/>
    public bool IsPrefixTitle => ((info->Flags >> (8 * 3)) & 0xFF) == 1;
}
