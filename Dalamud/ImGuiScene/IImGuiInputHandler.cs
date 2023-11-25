namespace Dalamud.ImGuiScene;

/// <summary>
/// A simple shared public interface that all ImGui input implementations follows.
/// </summary>
internal interface IImGuiInputHandler : IDisposable
{
    /// <summary>
    /// Gets or sets a value indicating whether or not the cursor should be overridden with the ImGui cursor.
    /// </summary>
    public bool UpdateCursor { get; set; }

    /// <summary>
    /// Gets or sets the path of ImGui configuration .ini file.
    /// </summary>
    string? IniPath { get; set; }

    /// <summary>
    /// Determines if <paramref name="cursorHandle"/> is owned by this.
    /// </summary>
    /// <param name="cursorHandle">The cursor.</param>
    /// <returns>Whether it is the case.</returns>
    public bool IsImGuiCursor(nint cursorHandle);

    /// <summary>
    /// Marks the beginning of a new frame.
    /// </summary>
    /// <param name="width">The width of the new frame.</param>
    /// <param name="height">The height of the new frame.</param>
    void NewFrame(int width, int height);
}
