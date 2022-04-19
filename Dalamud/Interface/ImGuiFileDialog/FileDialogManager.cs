using System;
using System.Collections.Generic;

namespace Dalamud.Interface.ImGuiFileDialog
{
    /// <summary>
    /// A manager for the <see cref="FileDialog"/> class.
    /// </summary>
    public class FileDialogManager
    {
        private FileDialog? dialog;
        private Action<bool, string>? callback;
        private Action<bool, List<string>>? multiCallback;
        private string savedPath = ".";

        /// <summary>
        /// Create a dialog which selects an already existing folder.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        public void OpenFolderDialog(string title, Action<bool, string> callback)
        {
            this.SetCallback(callback);
            this.SetDialog("OpenFolderDialog", title, string.Empty, this.savedPath, ".", string.Empty, 1, false, ImGuiFileDialogFlags.SelectOnly);
        }

        /// <summary>
        /// Create a dialog which selects an already existing folder.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        /// <param name="startPath">The directory which the dialog should start inside of. The last path this manager was in is used if this is null.</param>
        /// <param name="isModal">Whether the dialog should be a modal popup.</param>
        public void OpenFolderDialog(string title, Action<bool, string> callback, string? startPath, bool isModal = false)
        {
            this.SetCallback(callback);
            this.SetDialog("OpenFolderDialog", title, string.Empty, startPath ?? this.savedPath, ".", string.Empty, 1, isModal, ImGuiFileDialogFlags.SelectOnly);
        }

        /// <summary>
        /// Create a dialog which selects an already existing folder or new folder.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="defaultFolderName">The default name to use when creating a new folder.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        public void SaveFolderDialog(string title, string defaultFolderName, Action<bool, string> callback)
        {
            this.SetCallback(callback);
            this.SetDialog("SaveFolderDialog", title, string.Empty, this.savedPath, defaultFolderName, string.Empty, 1, false, ImGuiFileDialogFlags.None);
        }

        /// <summary>
        /// Create a dialog which selects an already existing folder or new folder.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="defaultFolderName">The default name to use when creating a new folder.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        /// <param name="startPath">The directory which the dialog should start inside of. The last path this manager was in is used if this is null.</param>
        /// <param name="isModal">Whether the dialog should be a modal popup.</param>
        public void SaveFolderDialog(string title, string defaultFolderName, Action<bool, string> callback, string? startPath, bool isModal = false)
        {
            this.SetCallback(callback);
            this.SetDialog("SaveFolderDialog", title, string.Empty, startPath ?? this.savedPath, defaultFolderName, string.Empty, 1, isModal, ImGuiFileDialogFlags.None);
        }

        /// <summary>
        /// Create a dialog which selects a single, already existing file.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="filters">Which files to show in the dialog.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        public void OpenFileDialog(string title, string filters, Action<bool, string> callback)
        {
            this.SetCallback(callback);
            this.SetDialog("OpenFileDialog", title, filters, this.savedPath, ".", string.Empty, 1, false, ImGuiFileDialogFlags.SelectOnly);
        }

        /// <summary>
        /// Create a dialog which selects already existing files.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="filters">Which files to show in the dialog.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        /// <param name="startPath">The directory which the dialog should start inside of. The last path this manager was in is used if this is null.</param>
        /// <param name="selectionCountMax">The maximum amount of files or directories which can be selected. Set to 0 for an infinite number.</param>
        /// <param name="isModal">Whether the dialog should be a modal popup.</param>
        public void OpenFileDialog(
            string title,
            string filters,
            Action<bool, List<string>> callback,
            string? startPath = null,
            int selectionCountMax = 1,
            bool isModal = false)
        {
            this.SetCallback(callback);
            this.SetDialog("OpenFileDialog", title, filters, startPath ?? this.savedPath, ".", string.Empty, selectionCountMax, isModal, ImGuiFileDialogFlags.SelectOnly);
        }

        /// <summary>
        /// Create a dialog which selects an already existing folder or new file.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="filters">Which files to show in the dialog.</param>
        /// <param name="defaultFileName">The default name to use when creating a new file.</param>
        /// <param name="defaultExtension">The extension to use when creating a new file.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        public void SaveFileDialog(
            string title,
            string filters,
            string defaultFileName,
            string defaultExtension,
            Action<bool, string> callback)
        {
            this.SetCallback(callback);
            this.SetDialog("SaveFileDialog", title, filters, this.savedPath, defaultFileName, defaultExtension, 1, false, ImGuiFileDialogFlags.None);
        }

        /// <summary>
        /// Create a dialog which selects an already existing folder or new file.
        /// </summary>
        /// <param name="title">The header title of the dialog.</param>
        /// <param name="filters">Which files to show in the dialog.</param>
        /// <param name="defaultFileName">The default name to use when creating a new file.</param>
        /// <param name="defaultExtension">The extension to use when creating a new file.</param>
        /// <param name="callback">The action to execute when the dialog is finished.</param>
        /// <param name="startPath">The directory which the dialog should start inside of. The last path this manager was in is used if this is null.</param>
        /// <param name="isModal">Whether the dialog should be a modal popup.</param>
        public void SaveFileDialog(
            string title,
            string filters,
            string defaultFileName,
            string defaultExtension,
            Action<bool, string> callback,
            string? startPath,
            bool isModal = false)
        {
            this.SetCallback(callback);
            this.SetDialog("SaveFileDialog", title, filters, startPath ?? this.savedPath, defaultFileName, defaultExtension, 1, isModal, ImGuiFileDialogFlags.None);
        }

        /// <summary>
        /// Draws the current dialog, if any, and executes the callback if it is finished.
        /// </summary>
        public void Draw()
        {
            if (this.dialog == null) return;
            if (this.dialog.Draw())
            {
                var isOk = this.dialog.GetIsOk();
                var results = this.dialog.GetResults();
                this.callback?.Invoke(isOk, results.Count > 0 ? results[0] : string.Empty);
                this.multiCallback?.Invoke(isOk, results);
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
            this.multiCallback = null;
        }

        private void SetCallback(Action<bool, string> action)
        {
            this.callback = action;
            this.multiCallback = null;
        }

        private void SetCallback(Action<bool, List<string>> action)
        {
            this.multiCallback = action;
            this.callback = null;
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
            ImGuiFileDialogFlags flags)
        {
            this.Reset();
            this.dialog = new FileDialog(id, title, filters, path, defaultFileName, defaultExtension, selectionCountMax, isModal, flags);
            this.dialog.Show();
        }
    }
}
