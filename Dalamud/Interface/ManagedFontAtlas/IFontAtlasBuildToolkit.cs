using System.Runtime.InteropServices;

using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Common stuff for <see cref="IFontAtlasBuildToolkitPreBuild"/> and <see cref="IFontAtlasBuildToolkitPostBuild"/>.
/// </summary>
public interface IFontAtlasBuildToolkit
{
    /// <summary>
    /// Gets or sets the font relevant to the call.
    /// </summary>
    ImFontPtr Font { get; set; }

    /// <summary>
    /// Gets the current scale this font atlas is being built with.
    /// </summary>
    float Scale { get; }

    /// <summary>
    /// Gets a value indicating whether the current build operation is asynchronous.
    /// </summary>
    bool IsAsyncBuildOperation { get; }

    /// <summary>
    /// Gets the current build step.
    /// </summary>
    FontAtlasBuildStep BuildStep { get; }

    /// <summary>
    /// Gets the font atlas being built.
    /// </summary>
    ImFontAtlasPtr NewImAtlas { get; }

    /// <summary>
    /// Gets the wrapper for <see cref="ImFontAtlas.Fonts"/> of <see cref="NewImAtlas"/>.<br />
    /// This does not need to be disposed. Calling <see cref="IDisposable.Dispose"/> does nothing.-
    /// <br />
    /// Modification of this vector may result in undefined behaviors.
    /// </summary>
    ImVectorWrapper<ImFontPtr> Fonts { get; }

    /// <summary>
    /// Queues an item to be disposed after the native atlas gets disposed, successful or not.
    /// </summary>
    /// <typeparam name="T">Disposable type.</typeparam>
    /// <param name="disposable">The disposable.</param>
    /// <returns>The same <paramref name="disposable"/>.</returns>
    T DisposeWithAtlas<T>(T disposable) where T : IDisposable;

    /// <summary>
    /// Queues an item to be disposed after the native atlas gets disposed, successful or not.
    /// </summary>
    /// <param name="gcHandle">The gc handle.</param>
    /// <returns>The same <paramref name="gcHandle"/>.</returns>
    GCHandle DisposeWithAtlas(GCHandle gcHandle);

    /// <summary>
    /// Queues an item to be disposed after the native atlas gets disposed, successful or not.
    /// </summary>
    /// <param name="action">The action to run on dispose.</param>
    void DisposeWithAtlas(Action action);
}
