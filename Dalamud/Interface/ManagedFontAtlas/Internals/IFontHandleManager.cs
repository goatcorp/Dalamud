using Dalamud.Utility;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Manager for <see cref="IFontHandle"/>.
/// </summary>
internal interface IFontHandleManager : IDisposable
{
    /// <inheritdoc cref="IFontAtlas.RebuildRecommend"/>
    event Action? RebuildRecommend;

    /// <summary>
    /// Gets the name of the font handle manager. For logging and debugging purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets the active font handle substance.
    /// </summary>
    IFontHandleSubstance? Substance { get; set; }

    /// <summary>
    /// Decrease font reference counter.
    /// </summary>
    /// <param name="handle">Handle being released.</param>
    void FreeFontHandle(IFontHandle handle);

    /// <summary>
    /// Creates a new substance of the font atlas.
    /// </summary>
    /// <param name="dataRoot">The data root.</param>
    /// <returns>The new substance.</returns>
    IFontHandleSubstance NewSubstance(IRefCountable dataRoot);
}
