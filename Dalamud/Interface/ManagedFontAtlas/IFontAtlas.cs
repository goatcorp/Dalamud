using System.Threading.Tasks;

using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas.Internals;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Wrapper for <see cref="ImFontAtlasPtr"/>.
/// </summary>
public interface IFontAtlas : IDisposable
{
    /// <summary>
    /// Event to be called on build step changes.<br />
    /// <see cref="IFontAtlasBuildToolkit.Font"/> is meaningless for this event.
    /// </summary>
    event FontAtlasBuildStepDelegate? BuildStepChange;

    /// <summary>
    /// Event fired when a font rebuild operation is suggested.<br />
    /// This will be invoked from the main thread.
    /// </summary>
    event Action? RebuildRecommend;

    /// <summary>
    /// Gets the name of the atlas. For logging and debugging purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value how the atlas should be rebuilt when the relevant Dalamud Configuration changes. 
    /// </summary>
    FontAtlasAutoRebuildMode AutoRebuildMode { get; }

    /// <summary>
    /// Gets the font atlas. Might be empty.
    /// </summary>
    ImFontAtlasPtr ImAtlas { get; }

    /// <summary>
    /// Gets the task that represents the current font rebuild state.
    /// </summary>
    Task BuildTask { get; }

    /// <summary>
    /// Gets a value indicating whether there exists any built atlas, regardless of <see cref="BuildTask"/>.
    /// </summary>
    bool HasBuiltAtlas { get; }

    /// <summary>
    /// Gets a value indicating whether this font atlas is under the effect of global scale.
    /// </summary>
    bool IsGlobalScaled { get; }

    /// <inheritdoc cref="GamePrebakedFontHandle.HandleManager.NewFontHandle"/>
    public IFontHandle NewGameFontHandle(GameFontStyle style);

    /// <inheritdoc cref="DelegateFontHandle.HandleManager.NewFontHandle"/>
    public IFontHandle NewDelegateFontHandle(FontAtlasBuildStepDelegate buildStepDelegate);

    /// <inheritdoc cref="IFontHandleManager.FreeFontHandle"/>
    public void FreeFontHandle(IFontHandle handle);

    /// <summary>
    /// Queues rebuilding fonts, on the main thread.<br />
    /// Note that <see cref="BuildTask"/> would not necessarily get changed from calling this function.
    /// </summary>
    void BuildFontsOnNextFrame();

    /// <summary>
    /// Rebuilds fonts immediately, on the current thread.<br />
    /// Even the callback for <see cref="FontAtlasBuildStep.PostPromotion"/> will be called on the same thread.
    /// </summary>
    void BuildFontsImmediately();

    /// <summary>
    /// Rebuilds fonts asynchronously, on any thread. 
    /// </summary>
    /// <param name="callPostPromotionOnMainThread">Call <see cref="FontAtlasBuildStep.PostPromotion"/> on the main thread.</param>
    /// <returns>The task.</returns>
    Task BuildFontsAsync(bool callPostPromotionOnMainThread = true);
}
