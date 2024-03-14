using System.Threading.Tasks;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Represents a reference counting handle for fonts.<br />
/// Not intended for plugins to implement.
/// </summary>
public interface IFontHandle : IDisposable
{
    /// <summary>
    /// Delegate for <see cref="IFontHandle.ImFontChanged"/>.
    /// </summary>
    /// <param name="fontHandle">The relevant font handle.</param>
    /// <param name="lockedFont">The locked font for this font handle, locked during the call of this delegate.</param>
    public delegate void ImFontChangedDelegate(IFontHandle fontHandle, ILockedImFont lockedFont);

    /// <summary>
    /// Called when the built instance of <see cref="ImFontPtr"/> has been changed.<br />
    /// This event can be invoked outside the main thread.
    /// </summary>
    event ImFontChangedDelegate ImFontChanged;

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
    /// <returns>An instance of <see cref="ILockedImFont"/> that <b>must</b> be disposed after use.</returns>
    /// <remarks>
    /// Calling <see cref="IFontHandle"/>.<see cref="IDisposable.Dispose"/> will not unlock the <see cref="ImFontPtr"/>
    /// locked by this function.
    /// </remarks>
    /// <exception cref="InvalidOperationException">If <see cref="Available"/> is <c>false</c>.</exception>
    ILockedImFont Lock();

    /// <summary>
    /// Pushes the current font into ImGui font stack, if available.<br />
    /// Use <see cref="ImGui.GetFont"/> to access the current font.<br />
    /// You may not access the font once you dispose this object.
    /// </summary>
    /// <returns>A disposable object that will pop the font on dispose.</returns>
    /// <exception cref="InvalidOperationException">If called outside of the main thread.</exception>
    /// <remarks>
    /// <para>This function uses <see cref="ImGui.PushFont"/>, and may do extra things. 
    /// Use <see cref="IDisposable.Dispose"/> or <see cref="Pop"/> to undo this operation.
    /// Do not use <see cref="ImGui.PopFont"/>.</para>
    /// </remarks>
    /// <example>
    /// <b>Push a font with `using` clause.</b>
    /// <code>
    /// using (fontHandle.Push())
    ///     ImGui.TextUnformatted("Test");
    /// </code>
    /// <b>Push a font with a matching call to <see cref="Pop"/>.</b>
    /// <code>
    /// fontHandle.Push();
    /// ImGui.TextUnformatted("Test 2");
    /// </code>
    /// <b>Push a font between two choices.</b>
    /// <code>
    /// using ((someCondition ? myFontHandle : dalamudPluginInterface.UiBuilder.MonoFontHandle).Push())
    ///     ImGui.TextUnformatted("Test 3"); 
    /// </code>
    /// </example>
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
}
