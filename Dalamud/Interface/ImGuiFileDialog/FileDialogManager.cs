using System;

namespace Dalamud.Interface.ImGuiFileDialog
{
    /// <summary>
    /// A manager for the <see cref="FileDialog"/> class.
    /// </summary>
    public class FileDialogManager
    {
        private FileDialog dialog;
        private string savedPath = ".";
        private Action<bool, string> callback;
        private char selectionSeparator;

        /// <summary>
        /// Create a dialog which selects an already existing folder.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        /// <param name="startPath">The directory which the dialog should start inside of.</param>
        /// <param name="isModal">Whether the dialog should be a modal popup.</param>
        public void OpenFolderDialog(string title, Action<bool, string> callback, string? startPath = null, bool isModal = false)
        {
            this.SetDialog("OpenFolderDialog", title, string.Empty, startPath ?? this.savedPath, ".", string.Empty, 1, isModal, ImGuiFileDialogFlags.SelectOnly, callback);
        }

        /// <summary>
        /// Create a dialog which selects an already existing folder or new folder.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="defaultFolderName">The default name to use when creating a new folder.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        /// <param name="startPath">The directory which the dialog should start inside of.</param>
        /// <param name="isModal">Whether the dialog should be a modal popup.</param>
        public void SaveFolderDialog(string title, string defaultFolderName, Action<bool, string> callback, string? startPath = null, bool isModal = false)
        {
            this.SetDialog("SaveFolderDialog", title, string.Empty, startPath ?? this.savedPath, defaultFolderName, string.Empty, 1, isModal, ImGuiFileDialogFlags.None, callback);
        }

        /// <summary>
        /// Create a dialog which selects an already existing file.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="filters">Which files to show in the dialog.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        /// <param name="startPath">The directory which the dialog should start inside of.</param>
        /// <param name="selectionCountMax">The maximum amount of files or directories which can be selected. Set to 0 for an infinite number.</param>
        /// <param name="selectionSeparator">The separator to put between multiple selected entries.</param>
        /// <param name="isModal">Whether the dialog should be a modal popup.</param>
        public void OpenFileDialog(
            string title,
            string filters,
            Action<bool, string> callback,
            string? startPath = null,
            int selectionCountMax = 1,
            char selectionSeparator = '\0',
            bool isModal = false)
        {
            this.selectionSeparator = selectionSeparator;
            this.SetDialog("OpenFileDialog", title, filters, startPath ?? this.savedPath, ".", string.Empty, selectionCountMax, isModal, ImGuiFileDialogFlags.SelectOnly, callback);
        }

        /// <summary>
        /// Create a dialog which selects an already existing folder or new file.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="filters">Which files to show in the dialog.</param>
        /// <param name="defaultFileName">The default name to use when creating a new file.</param>
        /// <param name="defaultExtension">The extension to use when creating a new file.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        /// <param name="startPath">The directory which the dialog should start inside of.</param>
        /// <param name="isModal">Whether the dialog should be a modal popup.</param>
        public void SaveFileDialog(
            string title,
            string filters,
            string defaultFileName,
            string defaultExtension,
            Action<bool, string> callback,
            string? startPath = null,
            bool isModal = false)
        {
            this.SetDialog("SaveFileDialog", title, filters, startPath ?? this.savedPath, defaultFileName, defaultExtension, 1, isModal, ImGuiFileDialogFlags.None, callback);
        }

        /// <summary>
        /// Draws the current dialog, if any, and executes the callback if it is finished.
        /// </summary>
        public void Draw()
        {
            if (this.dialog == null) return;
            if (this.dialog.Draw())
            {
                this.callback(this.dialog.GetIsOk(), this.dialog.GetResult(this.selectionSeparator));
                this.savedPath = this.dialog.GetCurrentPath();
                this.Reset();
            }
        }

        /// <summary>
        /// Removes the current dialog, if any.
        /// </summary>
        public void Reset()
        {
            this.dialog?.Hide();
            this.dialog = null;
            this.callback = null;
            this.selectionSeparator = '\0';
        }

        private void SetDialog(
            string id,
            string title,
            string filters,
            string path,
            string defaultFileName,
            string defaultExtension,
            int selectionCountMax,
            bool isModal,
            ImGuiFileDialogFlags flags,
            Action<bool, string> callback)
        {
            this.Reset();
            this.callback = callback;
            this.dialog = new FileDialog(id, title, filters, path, defaultFileName, defaultExtension, selectionCountMax, isModal, flags);
            this.dialog.Show();
        }
    }
}
