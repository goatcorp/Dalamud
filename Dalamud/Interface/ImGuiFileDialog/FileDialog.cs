using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dalamud.Interface.ImGuiFileDialog
{
    /// <summary>
    /// A file or folder picker.
    /// </summary>
    public partial class FileDialog
    {
        private readonly string title;
        private readonly int selectionCountMax;
        private readonly ImGuiFileDialogFlags flags;
        private readonly string id;
        private readonly string defaultExtension;
        private readonly string defaultFileName;

        private bool visible;

        private string currentPath;
        private string fileNameBuffer = string.Empty;

        private List<string> pathDecomposition = new();
        private bool pathClicked = true;
        private bool pathInputActivated = false;
        private string pathInputBuffer = string.Empty;

        private bool isModal = false;
        private bool okResultToConfirm = false;
        private bool isOk;
        private bool wantsToQuit;

        private bool createDirectoryMode = false;
        private string createDirectoryBuffer = string.Empty;

        private string searchBuffer = string.Empty;

        private string lastSelectedFileName = string.Empty;
        private List<string> selectedFileNames = new();

        private float footerHeight = 0;

        private string selectedSideBar = string.Empty;
        private List<SideBarItem> drives = new();
        private List<SideBarItem> quickAccess = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDialog"/> class.
        /// </summary>
        /// <param name="id">A unique id for the dialog.</param>
        /// <param name="title">The text which is shown at the top of the dialog.</param>
        /// <param name="filters">Which file extension filters to apply. This should be left blank to select directories.</param>
        /// <param name="path">The directory which the dialog should start inside of.</param>
        /// <param name="defaultFileName">The default file or directory name.</param>
        /// <param name="defaultExtension">The default extension when creating new files.</param>
        /// <param name="selectionCountMax">The maximum amount of files or directories which can be selected. Set to 0 for an infinite number.</param>
        /// <param name="isModal">Whether the dialog should be a modal popup.</param>
        /// <param name="flags">Settings flags for the dialog, see <see cref="ImGuiFileDialogFlags"/>.</param>
        public FileDialog(
            string id,
            string title,
            string filters,
            string path,
            string defaultFileName,
            string defaultExtension,
            int selectionCountMax,
            bool isModal,
            ImGuiFileDialogFlags flags)
        {
            this.id = id;
            this.title = title;
            this.flags = flags;
            this.selectionCountMax = selectionCountMax;
            this.isModal = isModal;

            this.currentPath = path;
            this.defaultExtension = defaultExtension;
            this.defaultFileName = defaultFileName;

            this.ParseFilters(filters);
            this.SetSelectedFilterWithExt(this.defaultExtension);
            this.SetDefaultFileName();
            this.SetPath(this.currentPath);

            this.SetupSideBar();
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        public void Show()
        {
            this.visible = true;
        }

        /// <summary>
        /// Hides the dialog.
        /// </summary>
        public void Hide()
        {
            this.visible = false;
        }

        /// <summary>
        /// Gets whether a file or folder was successfully selected.
        /// </summary>
        /// <returns>The success state. Will be false if the selection was canceled or was otherwise unsuccessful.</returns>
        public bool GetIsOk()
        {
            return this.isOk;
        }

        /// <summary>
        /// Gets the result of the selection.
        /// </summary>
        /// <param name="separator">The separator to put between multiple selected entries.</param>
        /// <returns>The result of the selection (file or folder path). If multiple entries were selected, they are separated with the given separator, which is a comma by default.</returns>
        public string GetResult(char separator = ',')
        {
            if (!this.flags.HasFlag(ImGuiFileDialogFlags.SelectOnly))
            {
                return this.GetFilePathName();
            }

            if (this.IsDirectoryMode() && this.selectedFileNames.Count == 0)
            {
                return this.GetFilePathName(); // current directory
            }

            var fullPaths = this.selectedFileNames.Where(x => !string.IsNullOrEmpty(x)).Select(x => Path.Combine(this.currentPath, x));
            return string.Join(separator, fullPaths.ToArray());
        }

        /// <summary>
        /// Gets the current path of the dialog.
        /// </summary>
        /// <returns>The path of the directory which the dialog is current viewing.</returns>
        public string GetCurrentPath()
        {
            if (this.IsDirectoryMode())
            {
                // combine path file with directory input
                var selectedDirectory = this.fileNameBuffer;
                if (!string.IsNullOrEmpty(selectedDirectory) && selectedDirectory != ".")
                {
                    return string.IsNullOrEmpty(this.currentPath) ? selectedDirectory : Path.Combine(this.currentPath, selectedDirectory);
                }
            }

            return this.currentPath;
        }

        private string GetFilePathName()
        {
            var path = this.GetCurrentPath();
            var fileName = this.GetCurrentFileName();

            if (!string.IsNullOrEmpty(fileName))
            {
                return Path.Combine(path, fileName);
            }

            return path;
        }

        private string GetCurrentFileName()
        {
            if (this.IsDirectoryMode())
            {
                return string.Empty;
            }

            var result = this.fileNameBuffer;

            // a collection like {.cpp, .h}, so can't decide on an extension
            if (this.selectedFilter.CollectionFilters != null && this.selectedFilter.CollectionFilters.Count > 0)
            {
                return result;
            }

            // a single one, like .cpp
            if (!this.selectedFilter.Filter.Contains('*') && result != this.selectedFilter.Filter)
            {
                var lastPoint = result.LastIndexOf('.');
                if (lastPoint != -1)
                {
                    result = result.Substring(0, lastPoint);
                }

                result += this.selectedFilter.Filter;
            }

            return result;
        }

        private void SetDefaultFileName()
        {
            this.fileNameBuffer = this.defaultFileName;
        }

        private void SetPath(string path)
        {
            this.selectedSideBar = string.Empty;
            this.currentPath = path;
            this.files.Clear();
            this.pathDecomposition.Clear();
            this.selectedFileNames.Clear();
            if (this.IsDirectoryMode())
            {
                this.SetDefaultFileName();
            }

            this.ScanDir(this.currentPath);
        }

        private void SetCurrentDir(string path)
        {
            var dir = new DirectoryInfo(path);
            this.currentPath = dir.FullName;
            if (this.currentPath[^1] == Path.DirectorySeparatorChar)
            { // handle selecting a drive, like C: -> C:\
                this.currentPath = this.currentPath[0..^1];
            }

            this.pathInputBuffer = this.currentPath;
            this.pathDecomposition = new List<string>(this.currentPath.Split(Path.DirectorySeparatorChar));
        }

        private bool IsDirectoryMode()
        {
            return this.filters.Count == 0;
        }

        private void ResetEvents()
        {
            this.pathClicked = false;
        }
    }
}
