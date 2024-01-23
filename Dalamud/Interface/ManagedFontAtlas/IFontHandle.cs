using System.Threading.Tasks;

using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Represents a reference counting handle for fonts.
/// </summary>
public interface IFontHandle : IDisposable
{
    /// <summary>
    /// Called when the built instance of <see cref="ImFontPtr"/> has been changed.<br />
    /// This event will be invoked on the same thread with
    /// <see cref="IFontAtlas"/>.<see cref="IFontAtlas.BuildStepChange"/>,
    /// when the build step is <see cref="FontAtlasBuildStep.PostPromotion"/>.<br />
    /// See <see cref="IFontAtlas.BuildFontsOnNextFrame"/>, <see cref="IFontAtlas.BuildFontsImmediately"/>, and
    /// <see cref="IFontAtlas.BuildFontsAsync"/>.
    /// </summary>
    event Action<IFontHandle> ImFontChanged;

    /// <summary>
    /// Gets the load exception, if it failed to load. Otherwise, it is null.
    /// </summary>
    Exception? LoadException { get; }

    /// <summary>
    /// Gets a value indicating whether this font is ready for use.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Push"/> directly if you want to keep the current ImGui font if the font is not ready.<br />
    /// Alternatively, use <see cref="WaitAsync"/> to wait for this property to become <c>true</c>.
    /// </remarks>
    bool Available { get; }

    /// <summary>
    /// Locks the fully constructed instance of <see cref="ImFontPtr"/> corresponding to the this
    /// <see cref="IFontHandle"/>, for use in any thread.<br />
    /// Modification of the font will exhibit undefined behavior if some other thread also uses the font.
    /// </summary>
    /// <returns>An instance of <see cref="ImFontLocked"/> that <b>must</b> be disposed after use.</returns>
    /// <remarks>
    /// Calling <see cref="IFontHandle"/>.<see cref="IDisposable.Dispose"/> will not unlock the <see cref="ImFontPtr"/>
    /// locked by this function.
    /// </remarks>
    /// <exception cref="InvalidOperationException">If <see cref="Available"/> is <c>false</c>.</exception>
    ImFontLocked Lock();

    /// <summary>
    /// Pushes the current font into ImGui font stack, if available.<br />
    /// Use <see cref="ImGui.GetFont"/> to access the current font.<br />
    /// You may not access the font once you dispose this object.
    /// </summary>
    /// <returns>A disposable object that will pop the font on dispose.</returns>
    /// <exception cref="InvalidOperationException">If called outside of the main thread.</exception>
    /// <remarks>
    /// This function uses <see cref="ImGui.PushFont"/>, and may do extra things.
    /// Use <see cref="IDisposable.Dispose"/> or <see cref="Pop"/> to undo this operation.
    /// Do not use <see cref="ImGui.PopFont"/>.
    /// </remarks>
    IDisposable Push();

    /// <summary>
    /// Pops the font pushed to ImGui using <see cref="Push"/>, cleaning up any extra information as needed.
    /// </summary>
    void Pop();

    /// <summary>
    /// Waits for <see cref="Available"/> to become <c>true</c>.
    /// </summary>
    /// <returns>A task containing this <see cref="IFontHandle"/>.</returns>
    Task<IFontHandle> WaitAsync();

    /// <summary>
    /// The wrapper for <see cref="ImFontPtr"/>, guaranteeing that the associated data will be available as long as
    /// this struct is not disposed.
    /// </summary>
    public struct ImFontLocked : IDisposable
    {
        /// <summary>
        /// The associated <see cref="ImFontPtr"/>.
        /// </summary>
        public ImFontPtr ImFont;

        private IRefCountable? owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImFontLocked"/> struct.
        /// Ownership of reference of <paramref name="owner"/> is transferred.
        /// </summary>
        /// <param name="imFont">The contained font.</param>
        /// <param name="owner">The owner.</param>
        internal ImFontLocked(ImFontPtr imFont, IRefCountable owner)
        {
            this.ImFont = imFont;
            this.owner = owner;
        }

        public static implicit operator ImFontPtr(ImFontLocked l) => l.ImFont;

        public static unsafe implicit operator ImFont*(ImFontLocked l) => l.ImFont.NativePtr;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.owner is null)
                return;

            this.owner.Release();
            this.owner = null;
            this.ImFont = default;
        }
    }
}
