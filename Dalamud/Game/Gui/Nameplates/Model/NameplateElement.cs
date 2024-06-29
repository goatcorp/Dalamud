using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Represents a nameplate text element.
/// </summary>
/// <param name="address">The pointer for the nameplate element.</param>
/// <param name="type">The type for the nameplate element.</param>
internal class NameplateElement(nint address, NameplateElementType type) : INameplateElement
{
    private SeString? text;

    /// <summary>
    /// Gets or sets the pointer for the text.
    /// </summary>
    public nint Address { get; set; } = address;

    /// <inheritdoc/>
    public SeString Text
    {
        get => this.text ??= MemoryHelper.ReadSeStringNullTerminated(this.Address);
        set => this.text = value;
    }

    /// <inheritdoc/>
    public bool HasChanged { get; set; }

    /// <inheritdoc/>
    public NameplateElementType Type { get; } = type;
}
