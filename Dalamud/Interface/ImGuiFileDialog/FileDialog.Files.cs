using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Dalamud.Utility;

namespace Dalamud.Interface.ImGuiFileDialog;

/// <summary>
/// A file or folder picker.
/// </summary>
public partial class FileDialog
{
    private readonly Lock filesLock = new();

    private readonly DriveListLoader driveListLoader = new();

    private readonly List<FileStruct> files = [];
    private readonly List<FileStruct> filteredFiles = [];

    private SortingField currentSortingField = SortingField.FileName;

    /// <summary> Fired whenever the sorting field changes. </summary>
    public event Action<SortingField>? SortOrderChanged;

    /// <summary> The sorting type of the file selector. </summary>
    public enum SortingField
    {
        /// <summary> No sorting specified. </summary>
        None = 0,

        /// <summary> Sort for ascending file names in culture-specific order. </summary>
        FileName = 1,

        /// <summary> Sort for ascending file types in culture-specific order. </summary>
        Type = 2,

        /// <summary> Sort for ascending file sizes. </summary>
        Size = 3,

        /// <summary> Sort for ascending last update dates. </summary>
        Date = 4,

        /// <summary> Sort for descending file names in culture-specific order. </summary>
        FileNameDescending = 5,

        /// <summary> Sort for descending file types in culture-specific order. </summary>
        TypeDescending = 6,

        /// <summary> Sort for descending file sizes. </summary>
        SizeDescending = 7,

        /// <summary> Sort for descending last update dates. </summary>
        DateDescending = 8,
    }

    private enum FileStructType
    {
        File,
        Directory,
    }

    /// <summary> Specify the current and subsequent sort order. </summary>
    /// <param name="sortingField"> The new sort order. None is invalid and will not have any effect. </param>
    public void SortFields(SortingField sortingField)
    {
        Comparison<FileStruct>? sortFunc = sortingField switch
        {
            SortingField.FileName => SortByFileNameAsc,
            SortingField.FileNameDescending => SortByFileNameDesc,
            SortingField.Type => SortByTypeAsc,
            SortingField.TypeDescending => SortByTypeDesc,
            SortingField.Size => SortBySizeAsc,
            SortingField.SizeDescending => SortBySizeDesc,
            SortingField.Date => SortByDateAsc,
            SortingField.DateDescending => SortByDateDesc,
            _ => null,
        };

        if (sortFunc is null)
        {
            return;
        }

        this.files.Sort(sortFunc);
        this.currentSortingField = sortingField;
        this.ApplyFilteringOnFileList();
        this.SortOrderChanged?.InvokeSafely(this.currentSortingField);
    }

    private static string ComposeNewPath(List<string> decomposition)
    {
        switch (decomposition.Count)
        {
            // Handle UNC paths (network paths)
            case >= 2 when string.IsNullOrEmpty(decomposition[0]) && string.IsNullOrEmpty(decomposition[1]):
                var pathParts = new List<string>(decomposition);
                pathParts.RemoveRange(0, 2);

                // Can not access server level or UNC root
                if (pathParts.Count <= 1)
                {
                    return string.Empty;
                }

                return $@"\\{string.Join('\\', pathParts)}";
            case 1:
                var drivePath = decomposition[0];
                if (drivePath[^1] != Path.DirectorySeparatorChar)
                { // turn C: into C:\
                    drivePath += Path.DirectorySeparatorChar;
                }

                return drivePath;
            default: return Path.Combine(decomposition.ToArray());
        }
    }

    private static FileStruct GetFile(FileInfo file, string path)
        => new()
        {
            FileName = file.Name,
            FilePath = path,
            FileModifiedDate = FormatModifiedDate(file.LastWriteTime),
            FileSize = file.Length,
            FormattedFileSize = BytesToString(file.Length),
            Type = FileStructType.File,
            Ext = file.Extension.Trim('.'),
        };

    private static FileStruct GetDir(DirectoryInfo dir, string path)
        => new()
        {
            FileName = dir.Name,
            FilePath = path,
            FileModifiedDate = FormatModifiedDate(dir.LastWriteTime),
            FileSize = 0,
            FormattedFileSize = string.Empty,
            Type = FileStructType.Directory,
            Ext = string.Empty,
        };

    private static int SortByFileNameDesc(FileStruct a, FileStruct b)
    {
        switch (a.FileName, b.FileName)
        {
            case ("..", ".."): return 0;
            case ("..", _): return -1;
            case (_, ".."): return 1;
        }

        if (a.FileName[0] is '.')
        {
            if (b.FileName[0] is not '.')
            {
                return 1;
            }

            if (a.FileName.Length is 1)
            {
                return -1;
            }

            if (b.FileName.Length is 1)
            {
                return 1;
            }

            return -1 * string.Compare(a.FileName[1..], b.FileName[1..], StringComparison.CurrentCulture);
        }

        if (b.FileName[0] is '.')
        {
            return -1;
        }

        if (a.Type != b.Type)
        {
            return a.Type is FileStructType.Directory ? 1 : -1;
        }

        return -string.Compare(a.FileName, b.FileName, StringComparison.CurrentCulture);
    }

    private static int SortByFileNameAsc(FileStruct a, FileStruct b)
    {
        switch (a.FileName, b.FileName)
        {
            case ("..", ".."): return 0;
            case ("..", _): return -1;
            case (_, ".."): return 1;
        }

        if (a.FileName[0] is '.')
        {
            if (b.FileName[0] is not '.')
            {
                return -1;
            }

            if (a.FileName.Length is 1)
            {
                return 1;
            }

            if (b.FileName.Length is 1)
            {
                return -1;
            }

            return string.Compare(a.FileName[1..], b.FileName[1..], StringComparison.CurrentCulture);
        }

        if (b.FileName[0] is '.')
        {
            return 1;
        }

        if (a.Type != b.Type)
        {
            return a.Type is FileStructType.Directory ? -1 : 1;
        }

        return string.Compare(a.FileName, b.FileName, StringComparison.CurrentCulture);
    }

    private static int SortByTypeDesc(FileStruct a, FileStruct b)
    {
        switch (a.FileName, b.FileName)
        {
            case ("..", ".."): return 0;
            case ("..", _): return -1;
            case (_, ".."): return 1;
        }

        if (a.Type != b.Type)
        {
            return (a.Type == FileStructType.Directory) ? 1 : -1;
        }

        return string.Compare(a.Ext, b.Ext, StringComparison.CurrentCulture);
    }

    private static int SortByTypeAsc(FileStruct a, FileStruct b)
    {
        switch (a.FileName, b.FileName)
        {
            case ("..", ".."): return 0;
            case ("..", _): return -1;
            case (_, ".."): return 1;
        }

        if (a.Type != b.Type)
        {
            return (a.Type == FileStructType.Directory) ? -1 : 1;
        }

        return -string.Compare(a.Ext, b.Ext, StringComparison.CurrentCulture);
    }

    private static int SortBySizeDesc(FileStruct a, FileStruct b)
    {
        switch (a.FileName, b.FileName)
        {
            case ("..", ".."): return 0;
            case ("..", _): return -1;
            case (_, ".."): return 1;
        }

        if (a.Type != b.Type)
        {
            return (a.Type is FileStructType.Directory) ? 1 : -1;
        }

        return a.FileSize.CompareTo(b.FileSize);
    }

    private static int SortBySizeAsc(FileStruct a, FileStruct b)
    {
        switch (a.FileName, b.FileName)
        {
            case ("..", ".."): return 0;
            case ("..", _): return -1;
            case (_, ".."): return 1;
        }

        if (a.Type != b.Type)
        {
            return (a.Type is FileStructType.Directory) ? -1 : 1;
        }

        return -a.FileSize.CompareTo(b.FileSize);
    }

    private static int SortByDateDesc(FileStruct a, FileStruct b)
    {
        switch (a.FileName, b.FileName)
        {
            case ("..", ".."): return 0;
            case ("..", _): return -1;
            case (_, ".."): return 1;
        }

        if (a.Type != b.Type)
        {
            return (a.Type == FileStructType.Directory) ? 1 : -1;
        }

        return string.Compare(a.FileModifiedDate, b.FileModifiedDate, StringComparison.CurrentCulture);
    }

    private static int SortByDateAsc(FileStruct a, FileStruct b)
    {
        switch (a.FileName, b.FileName)
        {
            case ("..", ".."): return 0;
            case ("..", _): return -1;
            case (_, ".."): return 1;
        }

        if (a.Type != b.Type)
        {
            return (a.Type == FileStructType.Directory) ? -1 : 1;
        }

        return -string.Compare(a.FileModifiedDate, b.FileModifiedDate, StringComparison.CurrentCulture);
    }

    private bool CreateDir(string dirPath)
    {
        var newPath = Path.Combine(this.currentPath, dirPath);
        if (string.IsNullOrEmpty(newPath))
        {
            return false;
        }

        Directory.CreateDirectory(newPath);
        return true;
    }

    private void ScanDir(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        if (this.pathDecomposition.Count == 0)
        {
            this.SetCurrentDir(path);
        }

        if (this.pathDecomposition.Count > 0)
        {
            this.files.Clear();

            if (this.pathDecomposition.Count > 1)
            {
                this.files.Add(new FileStruct
                {
                    Type = FileStructType.Directory,
                    FilePath = path,
                    FileName = "..",
                    FileSize = 0,
                    FileModifiedDate = string.Empty,
                    FormattedFileSize = string.Empty,
                    Ext = string.Empty,
                });
            }

            var dirInfo = new DirectoryInfo(path);

            var dontShowHidden = this.flags.HasFlag(ImGuiFileDialogFlags.DontShowHiddenFiles);

            foreach (var dir in dirInfo.EnumerateDirectories().OrderBy(d => d.Name))
            {
                if (string.IsNullOrEmpty(dir.Name))
                {
                    continue;
                }

                if (dontShowHidden && dir.Name[0] == '.')
                {
                    continue;
                }

                this.files.Add(GetDir(dir, path));
            }

            foreach (var file in dirInfo.EnumerateFiles().OrderBy(f => f.Name))
            {
                if (string.IsNullOrEmpty(file.Name))
                {
                    continue;
                }

                if (dontShowHidden && file.Name[0] == '.')
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(file.Extension))
                {
                    var ext = file.Extension;
                    if (this.filters.Count > 0 && !this.selectedFilter.Empty() && !this.selectedFilter.FilterExists(ext) && this.selectedFilter.Filter != ".*")
                    {
                        continue;
                    }
                }

                this.files.Add(GetFile(file, path));
            }

            this.SortFields(this.currentSortingField);
        }
    }

    private IEnumerable<SideBarItem> GetDrives()
    {
        return this.driveListLoader.Drives.Select(drive => new SideBarItem(drive.Name, drive.Name, FontAwesomeIcon.Server));
    }

    private void SetupSideBar()
    {
        _ = this.driveListLoader.LoadDrivesAsync();

        var personal = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal));

        this.quickAccess.Add(new SideBarItem("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), FontAwesomeIcon.Desktop));
        this.quickAccess.Add(new SideBarItem("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), FontAwesomeIcon.File));

        if (!string.IsNullOrEmpty(personal))
        {
            this.quickAccess.Add(new SideBarItem("Downloads", Path.Combine(personal, "Downloads"), FontAwesomeIcon.Download));
        }

        this.quickAccess.Add(new SideBarItem("Favorites", Environment.GetFolderPath(Environment.SpecialFolder.Favorites), FontAwesomeIcon.Star));
        this.quickAccess.Add(new SideBarItem("Music", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), FontAwesomeIcon.Music));
        this.quickAccess.Add(new SideBarItem("Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), FontAwesomeIcon.Image));
        this.quickAccess.Add(new SideBarItem("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), FontAwesomeIcon.Video));
    }

    private SortingField GetNewSorting(int column)
        => column switch
        {
            0 when this.currentSortingField is SortingField.FileName => SortingField.FileNameDescending,
            0 => SortingField.FileName,
            1 when this.currentSortingField is SortingField.Type => SortingField.TypeDescending,
            1 => SortingField.Type,
            2 when this.currentSortingField is SortingField.Size => SortingField.SizeDescending,
            2 => SortingField.Size,
            3 when this.currentSortingField is SortingField.Date => SortingField.DateDescending,
            3 => SortingField.Date,
            _ => SortingField.None,
        };
}
