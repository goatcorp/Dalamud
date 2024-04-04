namespace Dalamud.Interface.Spannables;

/// <summary>A spannable that draws into a rectangular region.</summary>
public interface ISpannableTemplate : IDisposable
{
    /// <summary>Creates a new instance of <see cref="Spannable"/> from this instance of
    /// <see cref="ISpannableTemplate"/>.</summary>
    /// <returns>A new instance of <see cref="Spannable"/>.</returns>
    Spannable CreateSpannable();
}
