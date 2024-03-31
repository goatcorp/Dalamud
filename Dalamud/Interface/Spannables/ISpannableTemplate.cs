namespace Dalamud.Interface.Spannables;

/// <summary>A spannable that draws into a rectangular region.</summary>
public interface ISpannableTemplate : IDisposable
{
    /// <summary>Creates a new instance of <see cref="Spannable"/> from this instance of
    /// <see cref="ISpannableTemplate"/>.</summary>
    /// <returns>A new instance of <see cref="Spannable"/>.</returns>
    Spannable CreateSpannable();

    /// <summary>Recycles the spannable finished using, if possible. Disposes it otherwise.</summary>
    /// <param name="spannable">The spannable to dispose or recycle.</param>
    /// <remarks>Passing a <c>null</c> is a no-op.</remarks>
    void RecycleSpannable(Spannable? spannable) => spannable?.Dispose();
}
