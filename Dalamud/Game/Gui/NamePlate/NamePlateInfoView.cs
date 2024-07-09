using Dalamud.Game.Text.SeStringHandling;

using FFXIVClientStructs.FFXIV.Client.UI;

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
    SeString Name { get; }

    /// <summary>
    /// Gets the displayed free company tag for this nameplate according to the nameplate info object.
    /// </summary>
    SeString FreeCompanyTag { get; }

    /// <summary>
    /// Gets the displayed title for this nameplate according to the nameplate info object. In this field, the quote
    /// characters which appear on either side of the title are NOT included.
    /// </summary>
    SeString Title { get; }

    /// <summary>
    /// Gets the displayed title for this nameplate according to the nameplate info object. In this field, the quote
    /// characters which appear on either side of the title ARE included.
    /// </summary>
    SeString DisplayTitle { get; }

    /// <summary>
    /// Gets the displayed level text for this nameplate according to the nameplate info object.
    /// </summary>
    SeString LevelText { get; }

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
    private SeString? name;
    private SeString? freeCompanyTag;
    private SeString? title;
    private SeString? displayTitle;
    private SeString? levelText;

    /// <inheritdoc/>
    public SeString Name => this.name ??= SeString.Parse(info->Name);

    /// <inheritdoc/>
    public SeString FreeCompanyTag => this.freeCompanyTag ??= SeString.Parse(info->FcName);

    /// <inheritdoc/>
    public SeString Title => this.title ??= SeString.Parse(info->Title);

    /// <inheritdoc/>
    public SeString DisplayTitle => this.displayTitle ??= SeString.Parse(info->DisplayTitle);

    /// <inheritdoc/>
    public SeString LevelText => this.levelText ??= SeString.Parse(info->LevelText);

    /// <inheritdoc/>
    public int Flags => info->Flags;

    /// <inheritdoc/>
    public bool IsDirty => info->IsDirty;

    /// <inheritdoc/>
    public bool IsPrefixTitle => ((info->Flags >> (8 * 3)) & 0xFF) == 1;
}
