using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.NamePlate;

/// <summary>
/// A part builder for constructing and setting a simple (unquoted) nameplate field.
/// </summary>
/// <param name="field">The field type which should be set.</param>
public class NamePlateSimpleParts(NamePlateStringField field)
{
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

        if (this.TextWrap is { Item1: var left, Item2: var right })
        {
            var sb = new SeStringBuilder();
            sb.Append(left);
            sb.Append(this.Text ?? handler.GetFieldAsSeString(field));
            sb.Append(right);
            handler.SetField(field, sb.Build());
        }
        else if (this.Text is not null)
        {
            handler.SetField(field, this.Text);
        }
    }
}
