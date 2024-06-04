namespace Dalamud.Interface.ImGuiFileDialog;

/// <summary>
/// Settings flags for the <see cref="FileDialog"/> class.
/// </summary>
[Flags]
public enum ImGuiFileDialogFlags
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// Confirm the selection when choosing a file which already exists.
    /// </summary>
    ConfirmOverwrite = 0x01,

    /// <summary>
    /// Only allow selection of files or folders which currently exist.
    /// </summary>
    SelectOnly = 0x02,

    /// <summary>
    /// Hide files or folders which start with a period.
    /// </summary>
    DontShowHiddenFiles = 0x04,

    /// <summary>
    /// Disable the creation of new folders within the dialog.
    /// </summary>
    DisableCreateDirectoryButton = 0x08,

    /// <summary>
    /// Hide the type column.
    /// </summary>
    HideColumnType = 0x10,

    /// <summary>
    /// Hide the file size column.
    /// </summary>
    HideColumnSize = 0x20,

    /// <summary>
    /// Hide the last modified date column.
    /// </summary>
    HideColumnDate = 0x40,

    /// <summary>
    /// Hide the quick access sidebar.
    /// </summary>
    HideSideBar = 0x80,
}
