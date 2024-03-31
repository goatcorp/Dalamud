namespace Dalamud.Interface.Spannables.EventHandlers;

/// <summary>Arguments for child controls related events.</summary>
public record SpannableChildEventArgs : SpannableEventArgs
{
    /// <summary>Gets the index of <see cref="Child"/> within <see cref="SpannableEventArgs.Sender"/>.</summary>
    public int Index { get; private set; }

    /// <summary>Gets the relevant child.</summary>
    public Spannable Child { get; private set; } = null!;

    /// <summary>Gets the previous child, if the child at <see cref="Index"/> is changing.</summary>
    public Spannable? OldChild { get; private set; }

    /// <inheritdoc/>
    public override bool TryReset()
    {
        this.Index = 0;
        this.Child = null!;
        this.OldChild = null;
        return base.TryReset();
    }

    /// <summary>Initializes mouse related properties of this instance of <see cref="SpannableChildEventArgs"/>.
    /// </summary>
    /// <param name="index">Index of child within the sender.</param>
    /// <param name="child">Relevant child.</param>
    /// <param name="oldChild">Previous child.</param>
    public void InitializeChildProperties(int index, Spannable child, Spannable? oldChild)
    {
        this.Index = index;
        this.Child = child;
        this.OldChild = oldChild;
    }
}
