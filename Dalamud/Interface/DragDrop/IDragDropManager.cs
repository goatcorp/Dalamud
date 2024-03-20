using System.Collections.Generic;

namespace Dalamud.Interface.DragDrop;

/// <summary>
/// A service to handle external drag and drop from WinAPI.
/// </summary>
public interface IDragDropManager
{
    /// <summary> Gets a value indicating whether Drag and Drop functionality is available at all. </summary>
    public bool ServiceAvailable { get; }

    /// <summary> Gets a value indicating whether anything is being dragged from an external application and over any of the games viewports. </summary>
    public bool IsDragging { get; }

    /// <summary> Gets the list of files currently being dragged from an external application over any of the games viewports. </summary>
    public IReadOnlyList<string> Files { get; }

    /// <summary> Gets the set of file types by extension currently being dragged from an external application over any of the games viewports. </summary>
    public IReadOnlySet<string> Extensions { get; }

    /// <summary> Gets the list of directories currently being dragged from an external application over any of the games viewports. </summary>
    public IReadOnlyList<string> Directories { get; }

    /// <summary> Create an ImGui drag and drop source that is active only if anything is being dragged from an external source. </summary>
    /// <param name="label"> The label used for the drag and drop payload. </param>
    /// <param name="validityCheck">A function returning whether the current status is relevant for this source. Checked before creating the source but only if something is being dragged.</param>
    public void CreateImGuiSource(string label, Func<IDragDropManager, bool> validityCheck)
        => this.CreateImGuiSource(label, validityCheck, _ => false);

    /// <summary> Create an ImGui drag and drop source that is active only if anything is being dragged from an external source. </summary>
    /// <param name="label"> The label used for the drag and drop payload. </param>
    /// <param name="validityCheck">A function returning whether the current status is relevant for this source. Checked before creating the source but only if something is being dragged.</param>
    /// <param name="tooltipBuilder">Executes ImGui functions to build a tooltip. Should return true if it creates any tooltip and false otherwise. If multiple sources are active, only the first non-empty tooltip type drawn in a frame will be used.</param>
    public void CreateImGuiSource(string label, Func<IDragDropManager, bool> validityCheck, Func<IDragDropManager, bool> tooltipBuilder);

    /// <summary> Create an ImGui drag and drop target on the last ImGui object. </summary>
    /// <param name="label">The label used for the drag and drop payload.</param>
    /// <param name="files">On success, contains the list of file paths dropped onto the target.</param>
    /// <param name="directories">On success, contains the list of directory paths dropped onto the target.</param>
    /// <returns>True if items were dropped onto the target this frame, false otherwise.</returns>
    public bool CreateImGuiTarget(string label, out IReadOnlyList<string> files, out IReadOnlyList<string> directories);
}
