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

    /// <summary>Invoked after snapshot copying, under write exclusion, to retire previous-frame resources.</summary>
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
    /// <remarks>
    /// Call on the game thread. A resize can cause the step to be skipped or its capture to be discarded.
    /// Rendered ImGui frames complete platform updates under write exclusion even when capture is discarded.
    /// </remarks>
    void Step();

    /// <summary>Renders the latest main snapshot and attempts secondary presentation at most once per captured step.</summary>
    /// <remarks>
    /// May run repeatedly on presentation threads without invoking <see cref="BuildUi"/>. Skips rendering during resize
    /// and until a complete capture built after that resize is available.
    /// </remarks>
    void Render();

    /// <summary>Reads a viewport's back buffer while excluding renderer mutations and swap-chain reallocation.</summary>
    /// <remarks>
    /// Call within the serialized presentation transaction, which protects live ImGui viewport metadata.
    /// The callback borrows an ID3D11Texture2D pointer and must not retain it or re-enter rendering, capture,
    /// or resize. The temporary buffer reference is released before snapshot exclusion ends.
    /// </remarks>
    /// <param name="viewportId">The live ImGui viewport ID.</param>
    /// <param name="reader">The callback that reads the borrowed texture pointer.</param>
    /// <returns>False if resize has suspended capture; otherwise true after invoking the callback.</returns>
    bool TryReadViewportBackBuffer(uint viewportId, Action<nint> reader);

    /// <summary>Suspends rendering and snapshot capture for swap-chain reallocation, draining active readers.</summary>
    /// <remarks>
    /// Dispose the scope after reallocation and resource updates. Scopes may nest, overlap, or finish on another
    /// thread; capture and replay remain suspended until every scope completes. No thread-owned lock is retained.
    /// Do not begin or dispose a scope inside a snapshot lock. In-progress UI construction completes its platform
    /// updates but discards capture. Replay requires a fresh capture; deferred resources await normal retirement.
    /// </remarks>
    /// <returns>An independently owned, idempotent resize scope.</returns>
    IDisposable BeginResize();

    /// <summary>Releases renderer references to the main viewport buffers before reallocation.</summary>
    /// <remarks>
    /// Keep a resize scope across this call, buffer reallocation, and <see cref="OnPostResize"/>.
    /// Renderer mutations acquire snapshot write exclusion internally.
    /// </remarks>
    void OnPreResize();

    /// <summary>Updates main viewport dimensions after reallocation; back-buffer acquisition remains lazy.</summary>
    /// <remarks>Call before completing the resize scope opened before <see cref="OnPreResize"/>.</remarks>
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
