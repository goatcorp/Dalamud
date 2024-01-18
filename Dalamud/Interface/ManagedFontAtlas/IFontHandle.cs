using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Represents a reference counting handle for fonts.
/// </summary>
public interface IFontHandle : IDisposable
{
    /// <summary>
    /// Represents a reference counting handle for fonts. Dalamud internal use only.
    /// </summary>
    internal interface IInternal : IFontHandle
    {
        /// <summary>
        /// Gets the font.<br />
        /// Use of this properly is safe only from the UI thread.<br />
        /// Use <see cref="IFontHandle.Push"/> if the intended purpose of this property is <see cref="ImGui.PushFont"/>.<br />
        /// Futures changes may make simple <see cref="ImGui.PushFont"/> not enough.
        /// </summary>
        ImFontPtr ImFont { get; }
    }

    /// <summary>
    /// Gets the load exception, if it failed to load. Otherwise, it is null.
    /// </summary>
    Exception? LoadException { get; }

    /// <summary>
    /// Gets a value indicating whether this font is ready for use.<br />
    /// Use <see cref="Push"/> directly if you want to keep the current ImGui font if the font is not ready.
    /// </summary>
    bool Available { get; }

    /// <summary>
    /// Pushes the current font into ImGui font stack using <see cref="ImGui.PushFont"/>, if available.<br />
    /// Use <see cref="ImGui.GetFont"/> to access the current font.<br />
    /// You may not access the font once you dispose this object.
    /// </summary>
    /// <returns>A disposable object that will call <see cref="ImGui.PopFont"/>(1) on dispose.</returns>
    IDisposable Push();
}
