using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.NamePlate;

/// <summary>
/// A part builder for constructing and setting quoted nameplate fields (i.e. free company tag and title).
/// </summary>
/// <param name="field">The field type which should be set.</param>
/// <param name="isFreeCompany">Whether or not this is a Free Company part.</param>
/// <remarks>
/// This class works as a lazy writer initialized with empty parts, where an empty part signifies no change should be
/// performed. Only after all handler processing is complete does it write out any parts which were set to the
/// associated field. Reading fields from this class is usually not what you want to do, as you'll only be reading the
/// contents of parts which other plugins have written to. Prefer reading from the base handler's properties or using
/// <see cref="NamePlateInfoView"/>.
/// </remarks>
public class NamePlateQuotedParts(NamePlateStringField field, bool isFreeCompany)
{
    /// <summary>
    /// Gets or sets the opening and closing SeStrings which will wrap the entire contents, which can be used to apply
    /// colors or styling to the entire field.
    /// </summary>
    public (SeString, SeString)? OuterWrap { get; set; }

    /// <summary>
    /// Gets or sets the opening quote string which appears before the text and opening text-wrap.
    /// </summary>
    public SeString? LeftQuote { get; set; }

    /// <summary>
    /// Gets or sets the closing quote string which appears after the text and closing text-wrap.
    /// </summary>
    public SeString? RightQuote { get; set; }

    /// <summary>
    /// Gets or sets the opening and closing SeStrings which will wrap the text, which can be used to apply colors or
    /// styling to the field's text.
    /// </summary>
    public (SeString, SeString)? TextWrap { get; set; }

    /// <summary>
    /// Gets or sets this field's text.
    /// </summary>
    public SeString? Text { get; set; }

    /// <summary>
    /// Applies the changes from this builder to the actual field.
    /// </summary>
    /// <param name="handler">The handler to perform the changes on.</param>
    internal unsafe void Apply(NamePlateUpdateHandler handler)
    {
        if ((nint)handler.GetFieldAsPointer(field) == NamePlateGui.EmptyStringPointer)
            return;

        var sb = new SeStringBuilder();
        if (this.OuterWrap is { Item1: { } outerLeft })
        {
            sb.Append(outerLeft);
        }

        if (this.LeftQuote is not null)
        {
            sb.Append(this.LeftQuote);
        }
        else
        {
            sb.Append(isFreeCompany ? " «" : "《");
        }

        if (this.TextWrap is { Item1: { } left, Item2: { } right })
        {
            sb.Append(left);
            sb.Append(this.Text ?? this.GetStrippedField(handler));
            sb.Append(right);
        }
        else
        {
            sb.Append(this.Text ?? this.GetStrippedField(handler));
        }

        if (this.RightQuote is not null)
        {
            sb.Append(this.RightQuote);
        }
        else
        {
            sb.Append(isFreeCompany ? "»" : "》");
        }

        if (this.OuterWrap is { Item2: { } outerRight })
        {
            sb.Append(outerRight);
        }

        handler.SetField(field, sb.Build());
    }

    private SeString GetStrippedField(NamePlateUpdateHandler handler)
    {
        return SeString.Parse(
            isFreeCompany
                ? NamePlateGui.StripFreeCompanyTagQuotes(handler.GetFieldAsSpan(field))
                : NamePlateGui.StripTitleQuotes(handler.GetFieldAsSpan(field)));
    }
}
