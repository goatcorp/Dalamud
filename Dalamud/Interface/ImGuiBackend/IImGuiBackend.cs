using Dalamud.Interface.ImGuiBackend.InputHandler;
using Dalamud.Interface.ImGuiBackend.Renderers;

namespace Dalamud.Interface.ImGuiBackend;

/// <summary>Backend for ImGui.</summary>
internal interface IImGuiBackend : IDisposable
{
    /// <summary>Delegate to be called when ImGui should be used to layout now.</summary>
    public delegate void BuildUiDelegate();

    /// <summary>Delegate to be called on new input frame.</summary>
    public delegate void NewInputFrameDelegate();

    /// <summary>Delegaet to be called on new render frame.</summary>
    public delegate void NewRenderFrameDelegate();

    /// <summary>User methods invoked every ImGui frame to construct custom UIs.</summary>
    event BuildUiDelegate? BuildUi;

    /// <summary>User methods invoked every ImGui frame on handling inputs.</summary>
    event NewInputFrameDelegate? NewInputFrame;

    /// <summary>User methods invoked every ImGui frame on handling renders.</summary>
    event NewRenderFrameDelegate? NewRenderFrame;

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

    /// <summary>Performs a render cycle.</summary>
    void Render();

    /// <summary>Handles stuff before resizing happens.</summary>
    void OnPreResize();
    
    /// <summary>Handles stuff after resizing happens.</summary>
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
