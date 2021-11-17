using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    ConfirmOverwrite = 1,

    /// <summary>
    /// Only allow selection of files or folders which currently exist.
    /// </summary>
    SelectOnly = 2,

    /// <summary>
    /// Hide files or folders which start with a period.
    /// </summary>
    DontShowHiddenFiles = 3,

    /// <summary>
    /// Disable the creation of new folders within the dialog.
    /// </summary>
    DisableCreateDirectoryButton = 4,

    /// <summary>
    /// Hide the type column.
    /// </summary>
    HideColumnType = 5,

    /// <summary>
    /// Hide the file size column.
    /// </summary>
    HideColumnSize = 6,

    /// <summary>
    /// Hide the last modified date column.
    /// </summary>
    HideColumnDate = 7,

    /// <summary>
    /// Hide the quick access sidebar.
    /// </summary>
    HideSideBar = 8,
}
