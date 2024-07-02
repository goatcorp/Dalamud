using Dalamud.Game.ClientState.Objects.Types;

namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Represents a nameplate object (mostly like player or NPC).
/// </summary>
/// <param name="address">The address for the nameplate object.</param>
/// <param name="info">The nameplate info for this nameplate object.</param>
internal class NameplateObject(IntPtr address, INameplateInfo info) : INameplateObject
{
    private long? nameplateIndex = null;
    private GameObject? gameObject = null;

    /// <inheritdoc/>
    public nint Address { get; } = address;

    /// <inheritdoc/>
    public INameplateInfo Nameplate { get; } = info;

    /// <inheritdoc/>
    public long NameplateIndex
    {
        get => this.nameplateIndex ??= Service<NameplateGui>.Get().GetNameplateIndex(this.Address);
    }

    /// <inheritdoc/>
    public GameObject? GameObject
    {
        get => this.gameObject ??= Service<NameplateGui>.Get().GetNameplateGameObject(this.NameplateIndex);
    }
}
