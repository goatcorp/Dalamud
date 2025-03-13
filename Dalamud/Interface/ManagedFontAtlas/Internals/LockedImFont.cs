using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// The implementation for <see cref="ILockedImFont"/>.
/// </summary>
internal class LockedImFont : ILockedImFont
{
    private IRefCountable? owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="LockedImFont"/> class.
    /// Ownership of reference of <paramref name="owner"/> is transferred.
    /// </summary>
    /// <param name="font">The contained font.</param>
    /// <param name="owner">The owner.</param>
    /// <returns>The rented instance of <see cref="LockedImFont"/>.</returns>
    internal LockedImFont(ImFontPtr font, IRefCountable owner)
    {
        this.ImFont = font;
        this.owner = owner;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="LockedImFont"/> class.
    /// </summary>
    ~LockedImFont() => this.FreeOwner();

    /// <inheritdoc/>
    public ImFontPtr ImFont { get; private set; }

    /// <inheritdoc/>
    public ILockedImFont NewRef()
    {
        if (this.owner is null)
            throw new ObjectDisposedException(nameof(LockedImFont));

        var newRef = new LockedImFont(this.ImFont, this.owner);
        this.owner.AddRef();
        return newRef;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.FreeOwner();
        GC.SuppressFinalize(this);
    }

    private void FreeOwner()
    {
        if (this.owner is null)
            return;

        this.owner.Release();
        this.owner = null;
        this.ImFont = default;
    }
}
