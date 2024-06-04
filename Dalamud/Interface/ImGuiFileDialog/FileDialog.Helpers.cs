namespace Dalamud.Interface.ImGuiFileDialog;

/// <summary>
/// A file or folder picker.
/// </summary>
public partial class FileDialog
{
    private static string FormatModifiedDate(DateTime date)
    {
        return date.ToString("yyyy/MM/dd HH:mm");
    }

    private static string BytesToString(long byteCount)
    {
        string[] suf = { " B", " KB", " MB", " GB", " TB" };
        if (byteCount == 0)
            return "0" + suf[0];
        var bytes = Math.Abs(byteCount);
        var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        var num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString() + suf[place];
    }
}
