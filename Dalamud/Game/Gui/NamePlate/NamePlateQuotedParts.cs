using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.NamePlate;

/// <summary>
/// A part builder for constructing and setting quoted nameplate fields (i.e. free company tag and title).
/// </summary>
/// <param name="field">The field type which should be set.</param>
public class NamePlateQuotedParts(NamePlateStringField field, bool isFreeCompany)
{
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
        if (this.LeftQuote is not null)
        {
            sb.Append(this.LeftQuote);
        }
        else
        {
            sb.Append(isFreeCompany ? " «" : "《");
        }

        if (this.TextWrap is { Item1: var left, Item2: var right })
        {
            sb.Append(left);
            sb.Append(this.Text ?? handler.GetFieldAsSeString(field));
            sb.Append(right);
        }
        else
        {
            sb.Append(this.Text ?? handler.GetFieldAsSeString(field));
        }

        if (this.RightQuote is not null)
        {
            sb.Append(this.RightQuote);
        }
        else
        {
            sb.Append(isFreeCompany ? "»" : "》");
        }

        handler.SetField(field, sb.Build());
    }
}
