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
    /// Represents a reference counting handle for fonts. Dalamud internal use only.
    /// </summary>
    internal interface IInternal : IFontHandle
    {
        /// <summary>
        /// Gets the font.<br />
        /// Use of this properly is safe only from the UI thread.<br />
        /// Use <see cref="IFontHandle.Push"/> if the intended purpose of this property is <see cref="ImGui.PushFont"/>.<br />
        /// Futures changes may make simple <see cref="ImGui.PushFont"/> not enough.<br />
        /// If you need to access a font outside the UI thread, consider using <see cref="IFontHandle.Lock"/>.
        /// </summary>
        ImFontPtr ImFont { get; }
    }

    /// <summary>
    /// Gets the load exception, if it failed to load. Otherwise, it is null.
    /// </summary>
    Exception? LoadException { get; }

    /// <summary>
    /// Gets a value indicating whether this font is ready for use.
    /// </summary>
    /// <remarks>
    /// Once set to <c>true</c>, it will remain <c>true</c>.<br />
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
    /// Pushes the current font into ImGui font stack using <see cref="ImGui.PushFont"/>, if available.<br />
    /// Use <see cref="ImGui.GetFont"/> to access the current font.<br />
    /// You may not access the font once you dispose this object.
    /// </summary>
    /// <returns>A disposable object that will call <see cref="ImGui.PopFont"/>(1) on dispose.</returns>
    /// <exception cref="InvalidOperationException">If called outside of the main thread.</exception>
    /// <remarks>
    /// Only intended for use with <c>using</c> keywords, such as <c>using (handle.Push())</c>.<br />
    /// Should you store or transfer the return value to somewhere else, use <see cref="IDisposable"/> as the type.
    /// </remarks>
    FontPopper Push();

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
        /// Initializes a new instance of the <see cref="ImFontLocked"/> struct,
        /// and incrase the reference count of <paramref name="owner"/>.
        /// </summary>
        /// <param name="imFont">The contained font.</param>
        /// <param name="owner">The owner.</param>
        internal ImFontLocked(ImFontPtr imFont, IRefCountable owner)
        {
            owner.AddRef();
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

    /// <summary>
    /// The wrapper for popping fonts.
    /// </summary>
    public struct FontPopper : IDisposable
    {
        private int count;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontPopper"/> struct.
        /// </summary>
        /// <param name="fontPtr">The font to push.</param>
        /// <param name="push">Whether to push.</param>
        internal FontPopper(ImFontPtr fontPtr, bool push)
        {
            if (!push)
                return;

            ThreadSafety.AssertMainThread();

            this.count = 1;
            ImGui.PushFont(fontPtr);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ThreadSafety.AssertMainThread();

            while (this.count-- > 0)
                ImGui.PopFont();
        }
    }
}
