namespace Dalamud.Interface.Spannables;

/// <summary>Base class for <see cref="Spannable"/>s.</summary>
/// <typeparam name="TOptions">Type of options.</typeparam>
public abstract class Spannable<TOptions> : Spannable
    where TOptions : SpannableOptions, new()
{
    /// <summary>Initializes a new instance of the <see cref="Spannable{TOptions}"/> class.</summary>
    /// <param name="options">Initial options to copy data from.</param>
    protected Spannable(TOptions? options = null)
        : base((TOptions)options?.Clone() ?? new TOptions())
    {
    }

    /// <inheritdoc cref="Spannable.Options"/>
    public new TOptions Options => (TOptions)base.Options;
}
