using System;

namespace Dalamud.Interface.ImGuiFileDialog;

/// <summary>
/// A manager for the <see cref="FileDialog"/> class.
/// </summary>
public class FileDialogManager
{
    private FileDialog dialog;
    private string savedPath = ".";
    private Action<bool, string> callback;

    /// <summary>
    /// Create a dialog which selects an already existing folder.
    /// </summary>
    /// <param name="title">The header title of the dialog.</param>
    /// <param name="callback">The action to execute when the dialog is finished.</param>
    public void OpenFolderDialog(string title, Action<bool, string> callback)
    {
        this.SetDialog("OpenFolderDialog", title, string.Empty, this.savedPath, ".", string.Empty, 1, false, ImGuiFileDialogFlags.SelectOnly, callback);
    }

    /// <summary>
    /// Create a dialog which selects an already existing folder or new folder.
    /// </summary>
    /// <param name="title">The header title of the dialog.</param>
    /// <param name="defaultFolderName">The default name to use when creating a new folder.</param>
    /// <param name="callback">The action to execute when the dialog is finished.</param>
    public void SaveFolderDialog(string title, string defaultFolderName, Action<bool, string> callback)
    {
        this.SetDialog("SaveFolderDialog", title, string.Empty, this.savedPath, defaultFolderName, string.Empty, 1, false, ImGuiFileDialogFlags.None, callback);
    }

    /// <summary>
    /// Create a dialog which selects an already existing file.
    /// </summary>
    /// <param name="title">The header title of the dialog.</param>
    /// <param name="filters">Which files to show in the dialog.</param>
    /// <param name="callback">The action to execute when the dialog is finished.</param>
    public void OpenFileDialog(string title, string filters, Action<bool, string> callback)
    {
        this.SetDialog("OpenFileDialog", title, filters, this.savedPath, ".", string.Empty, 1, false, ImGuiFileDialogFlags.SelectOnly, callback);
    }

    /// <summary>
    /// Create a dialog which selects an already existing folder or new file.
    /// </summary>
    /// <param name="title">The header title of the dialog.</param>
    /// <param name="filters">Which files to show in the dialog.</param>
    /// <param name="defaultFileName">The default name to use when creating a new file.</param>
    /// <param name="defaultExtension">The extension to use when creating a new file.</param>
    /// <param name="callback">The action to execute when the dialog is finished.</param>
    public void SaveFileDialog(string title, string filters, string defaultFileName, string defaultExtension, Action<bool, string> callback)
    {
        this.SetDialog("SaveFileDialog", title, filters, this.savedPath, defaultFileName, defaultExtension, 1, false, ImGuiFileDialogFlags.None, callback);
    }

    /// <summary>
    /// Draws the current dialog, if any, and executes the callback if it is finished.
    /// </summary>
    public void Draw()
    {
        if (this.dialog == null) return;
        if (this.dialog.Draw())
        {
            this.callback(this.dialog.GetIsOk(), this.dialog.GetResult());
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
