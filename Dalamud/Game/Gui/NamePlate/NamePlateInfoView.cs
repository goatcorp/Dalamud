using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.UI;

using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Gui.NamePlate;

/// <summary>
/// Provides a read-only view of the nameplate info object data for a nameplate. Modifications to
/// <see cref="NamePlateUpdateHandler"/> fields do not affect this data.
/// </summary>
public interface INamePlateInfoView
{
    /// <summary>
    /// Gets the displayed name for this nameplate according to the nameplate info object.
    /// </summary>
    ReadOnlySeStringSpan Name { get; }

    /// <summary>
    /// Gets the displayed free company tag for this nameplate according to the nameplate info object. For this field,
    /// the quote characters which appear on either side of the title are NOT included.
    /// </summary>
    ReadOnlySeStringSpan FreeCompanyTag { get; }

    /// <summary>
    /// Gets the displayed free company tag for this nameplate according to the nameplate info object. For this field,
    /// the quote characters which appear on either side of the title ARE included.
    /// </summary>
    ReadOnlySeStringSpan QuotedFreeCompanyTag { get; }

    /// <summary>
    /// Gets the displayed title for this nameplate according to the nameplate info object. For this field, the quote
    /// characters which appear on either side of the title are NOT included.
    /// </summary>
    ReadOnlySeStringSpan Title { get; }

    /// <summary>
    /// Gets the displayed title for this nameplate according to the nameplate info object. For this field, the quote
    /// characters which appear on either side of the title ARE included.
    /// </summary>
    ReadOnlySeStringSpan QuotedTitle { get; }

    /// <summary>
    /// Gets the displayed level text for this nameplate according to the nameplate info object.
    /// </summary>
    ReadOnlySeStringSpan LevelText { get; }

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
    /// <inheritdoc/>
    public ReadOnlySeStringSpan Name => info->Name.AsReadOnlySeStringSpan();

    /// <inheritdoc/>
    public ReadOnlySeStringSpan FreeCompanyTag => NamePlateGui.StripFreeCompanyTagQuotes(info->FcName.AsReadOnlySeString()).AsSpan();

    /// <inheritdoc/>
    public ReadOnlySeStringSpan QuotedFreeCompanyTag => info->FcName.AsReadOnlySeStringSpan();

    /// <inheritdoc/>
    public ReadOnlySeStringSpan Title => info->Title.AsReadOnlySeStringSpan();

    /// <inheritdoc/>
    public ReadOnlySeStringSpan QuotedTitle => info->DisplayTitle.AsReadOnlySeStringSpan();

    /// <inheritdoc/>
    public ReadOnlySeStringSpan LevelText => info->LevelText.AsReadOnlySeStringSpan();

    /// <inheritdoc/>
    public int Flags => info->Flags;

    /// <inheritdoc/>
    public bool IsDirty => info->IsDirty;

    /// <inheritdoc/>
    public bool IsPrefixTitle => ((info->Flags >> (8 * 3)) & 0xFF) == 1;
}
