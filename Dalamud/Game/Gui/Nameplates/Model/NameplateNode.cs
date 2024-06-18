using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Represents a nameplate text node.
/// </summary>
/// <param name="pointer">The pointer for the nameplate node.</param>
/// <param name="name">The name for the nameplate node.</param>
internal class NameplateNode(nint pointer, NameplateNodeName name) : INameplateNode
{
    private SeString? text;

    /// <inheritdoc/>
    public nint Pointer { get; set; } = pointer;

    /// <inheritdoc/>
    public SeString Text
    {
        get => this.text ??= MemoryHelper.ReadSeStringNullTerminated(this.PointerOrig);
        set => this.text = value;
    }

    /// <inheritdoc/>
    public bool HasChanged { get; set; }

    /// <inheritdoc/>
    public NameplateNodeName Name { get; } = name;

    /// <summary>
    /// Gets the original pointer.
    /// </summary>
    internal nint PointerOrig { get; } = pointer;
}
