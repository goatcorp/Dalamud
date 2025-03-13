namespace Dalamud.Game.Gui.NamePlate;

/// <summary>
/// A container for parts.
/// </summary>
internal class NamePlatePartsContainer
{
    private NamePlateSimpleParts? nameParts;
    private NamePlateQuotedParts? titleParts;
    private NamePlateQuotedParts? freeCompanyTagParts;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamePlatePartsContainer"/> class.
    /// </summary>
    /// <param name="context">The currently executing update context.</param>
    public NamePlatePartsContainer(NamePlateUpdateContext context)
    {
        context.HasParts = true;
    }

    /// <summary>
    /// Gets a parts object for constructing a nameplate name.
    /// </summary>
    internal NamePlateSimpleParts Name => this.nameParts ??= new NamePlateSimpleParts(NamePlateStringField.Name);

    /// <summary>
    /// Gets a parts object for constructing a nameplate title.
    /// </summary>
    internal NamePlateQuotedParts Title => this.titleParts ??= new NamePlateQuotedParts(NamePlateStringField.Title, false);

    /// <summary>
    /// Gets a parts object for constructing a nameplate free company tag.
    /// </summary>
    internal NamePlateQuotedParts FreeCompanyTag => this.freeCompanyTagParts ??= new NamePlateQuotedParts(NamePlateStringField.FreeCompanyTag, true);

    /// <summary>
    /// Applies all container parts.
    /// </summary>
    /// <param name="handler">The handler to apply the builders to.</param>
    internal void ApplyBuilders(NamePlateUpdateHandler handler)
    {
        this.nameParts?.Apply(handler);
        this.freeCompanyTagParts?.Apply(handler);
        this.titleParts?.Apply(handler);
    }
}
