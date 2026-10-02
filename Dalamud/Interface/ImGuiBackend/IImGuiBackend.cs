using Dalamud.Interface.ImGuiBackend.Delegates;
using Dalamud.Interface.ImGuiBackend.InputHandler;
using Dalamud.Interface.ImGuiBackend.Renderers;

namespace Dalamud.Interface.ImGuiBackend;

/// <summary>Backend for ImGui.</summary>
internal interface IImGuiBackend : IDisposable
{
    /// <summary>User methods invoked every ImGui frame to construct custom UIs.</summary>
    event ImGuiBuildUiDelegate? BuildUi;

    /// <summary>User methods invoked every ImGui frame on handling inputs.</summary>
    event ImGuiNewInputFrameDelegate? NewInputFrame;

    /// <summary>Invoked during <see cref="Step"/> before input processing and UI construction.</summary>
    event ImGuiNewRenderFrameDelegate? NewRenderFrame;

    /// <summary>Invoked under snapshot write exclusion after the current step replaces the previous draw snapshot.</summary>
    /// <remarks>Handlers hold the non-recursive write lock and must not re-enter capture, rendering, or resize.</remarks>
    event Action? PostCopy;

    /// <summary>Gets or sets a value indicating whether the cursor should be overridden with the ImGui cursor.
    /// </summary>
    bool UpdateCursor { get; set; }

    /// <summary>Gets or sets the path of ImGui configuration .ini file.</summary>
    string? IniPath { get; set; }

    /// <summary>Gets the device handle.</summary>
    nint DeviceHandle { get; }

    /// <summary>Gets the input handler.</summary>
    IImGuiInputHandler InputHandler { get; }

    /// <summary>Gets the renderer.</summary>
    IImGuiRenderer Renderer { get; }

    /// <summary>Gets a value indicating whether a swap-chain resize has been announced or is in progress.</summary>
    /// <remarks>This advisory flag does not establish lock ownership.</remarks>
    bool IsResizeInProgress { get; }

    /// <summary>Builds the ImGui frame and captures main and secondary viewport draw data for subsequent rendering.</summary>
    /// <remarks>Call on the game thread. An announced resize can cause the step to be skipped.</remarks>
    void Step();

    /// <summary>Renders the latest main snapshot and attempts secondary presentation at most once per captured step.</summary>
    /// <remarks>
    /// May run repeatedly on presentation threads without invoking <see cref="BuildUi"/>. Skips rendering during resize.
    /// </remarks>
    void Render();

    /// <summary>Acquires exclusive access to snapshot storage for swap-chain reallocation.</summary>
    /// <remarks>
    /// Must be paired with <see cref="ExitResize"/> on the same thread. Acquisitions must not be nested with each
    /// other or with <see cref="Step"/>/<see cref="Render"/> on that thread. The write lock excludes snapshot capture
    /// and rendering; UI construction in an already-started step runs outside that lock.
    /// </remarks>
    void EnterResize();

    /// <summary>Leaves the resize-exclusive section opened by <see cref="EnterResize"/>.</summary>
    void ExitResize();

    /// <summary>Releases renderer references to the main viewport buffers before reallocation.</summary>
    /// <remarks>Hold resize exclusion across this call, buffer reallocation, and <see cref="OnPostResize"/>.</remarks>
    void OnPreResize();

    /// <summary>Recreates main viewport renderer resources and updates the target dimensions after reallocation.</summary>
    /// <remarks>Call while holding the resize exclusion acquired before <see cref="OnPreResize"/>.</remarks>
    /// <param name="newWidth">The new width.</param>
    /// <param name="newHeight">The new height.</param>
    void OnPostResize(int newWidth, int newHeight);

    /// <summary>Invalidates fonts immediately.</summary>
    /// <remarks>Call this while handling <see cref="NewRenderFrame"/>.</remarks>
    void InvalidateFonts();

    /// <summary>Determines if <paramref name="cursorHandle"/> is owned by this.</summary>
    /// <param name="cursorHandle">The cursor.</param>
    /// <returns>Whether it is the case.</returns>
    bool IsImGuiCursor(nint cursorHandle);

    /// <summary>Determines if this instance of <see cref="IImGuiBackend"/> is rendering to
    /// <paramref name="targetHandle"/>. </summary>
    /// <param name="targetHandle">The present target handle.</param>
    /// <returns>Whether it is the case.</returns>
    bool IsAttachedToPresentationTarget(nint targetHandle);

    /// <summary>Determines if the main viewport is full screen. </summary>
    /// <returns>Whether it is the case.</returns>
    bool IsMainViewportFullScreen();
}
