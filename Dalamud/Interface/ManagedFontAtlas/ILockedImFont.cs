using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// The wrapper for <see cref="ImFontPtr"/>, guaranteeing that the associated data will be available as long as
/// this struct is not disposed.<br />
/// Not intended for plugins to implement.
/// </summary>
public interface ILockedImFont : IDisposable
{
    /// <summary>
    /// Gets the associated <see cref="ImFontPtr"/>.
    /// </summary>
    ImFontPtr ImFont { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ILockedImFont"/> with an additional reference to the owner.
    /// </summary>
    /// <returns>The new locked instance.</returns>
    ILockedImFont NewRef();
}
