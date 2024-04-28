using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dalamud.Interface.ImGuiFileDialog;

/// <summary>
/// A file or folder picker.
/// </summary>
public partial class FileDialog
{
    private readonly object filesLock = new();

    private readonly DriveListLoader driveListLoader = new();

    private List<FileStruct> files = new();
    private List<FileStruct> filteredFiles = new();

    private SortingField currentSortingField = SortingField.FileName;
    private bool[] sortDescending = { false, false, false, false };

    private enum FileStructType
    {
        File,
        Directory,
    }

    private enum SortingField
    {
        None,
        FileName,
        Type,
        Size,
        Date,
    }

    private static string ComposeNewPath(List<string> decomp)
    {
        if (decomp.Count == 1)
        {
            var drivePath = decomp[0];
            if (drivePath[^1] != Path.DirectorySeparatorChar)
            { // turn C: into C:\
                drivePath += Path.DirectorySeparatorChar;
            }

            return drivePath;
        }

        return Path.Combine(decomp.ToArray());
    }

    private static FileStruct GetFile(FileInfo file, string path)
    {
        return new FileStruct
        {
            FileName = file.Name,
            FilePath = path,
            FileModifiedDate = FormatModifiedDate(file.LastWriteTime),
            FileSize = file.Length,
            FormattedFileSize = BytesToString(file.Length),
            Type = FileStructType.File,
            Ext = file.Extension.Trim('.'),
        };
    }

    private static FileStruct GetDir(DirectoryInfo dir, string path)
    {
        return new FileStruct
        {
            FileName = dir.Name,
            FilePath = path,
            FileModifiedDate = FormatModifiedDate(dir.LastWriteTime),
            FileSize = 0,
            FormattedFileSize = string.Empty,
            Type = FileStructType.Directory,
            Ext = string.Empty,
        };
    }

    private static int SortByFileNameDesc(FileStruct a, FileStruct b)
    {
        if (a.FileName[0] == '.' && b.FileName[0] != '.')
        {
            return 1;
        }

        if (a.FileName[0] != '.' && b.FileName[0] == '.')
        {
            return -1;
        }

        if (a.FileName[0] == '.' && b.FileName[0] == '.')
        {
            if (a.FileName.Length == 1)
            {
                return -1;
            }

            if (b.FileName.Length == 1)
            {
                return 1;
            }

            return -1 * string.Compare(a.FileName[1..], b.FileName[1..]);
        }

        if (a.Type != b.Type)
        {
            return a.Type == FileStructType.Directory ? 1 : -1;
        }

        return -1 * string.Compare(a.FileName, b.FileName);
    }

    private static int SortByFileNameAsc(FileStruct a, FileStruct b)
    {
        if (a.FileName[0] == '.' && b.FileName[0] != '.')
        {
            return -1;
        }

        if (a.FileName[0] != '.' && b.FileName[0] == '.')
        {
            return 1;
        }

        if (a.FileName[0] == '.' && b.FileName[0] == '.')
        {
            if (a.FileName.Length == 1)
            {
                return 1;
            }

            if (b.FileName.Length == 1)
            {
                return -1;
            }

            return string.Compare(a.FileName[1..], b.FileName[1..]);
        }

        if (a.Type != b.Type)
        {
            return a.Type == FileStructType.Directory ? -1 : 1;
        }

        return string.Compare(a.FileName, b.FileName);
    }

    private static int SortByTypeDesc(FileStruct a, FileStruct b)
    {
        if (a.Type != b.Type)
        {
            return (a.Type == FileStructType.Directory) ? 1 : -1;
        }

        return string.Compare(a.Ext, b.Ext);
    }

    private static int SortByTypeAsc(FileStruct a, FileStruct b)
    {
        if (a.Type != b.Type)
        {
            return (a.Type == FileStructType.Directory) ? -1 : 1;
        }

        return -1 * string.Compare(a.Ext, b.Ext);
    }

    private static int SortBySizeDesc(FileStruct a, FileStruct b)
    {
        if (a.Type != b.Type)
        {
            return (a.Type == FileStructType.Directory) ? 1 : -1;
        }

        return (a.FileSize > b.FileSize) ? 1 : -1;
    }

    private static int SortBySizeAsc(FileStruct a, FileStruct b)
    {
        if (a.Type != b.Type)
        {
            return (a.Type == FileStructType.Directory) ? -1 : 1;
        }

        return (a.FileSize > b.FileSize) ? -1 : 1;
    }

    private static int SortByDateDesc(FileStruct a, FileStruct b)
    {
        if (a.Type != b.Type)
        {
            return (a.Type == FileStructType.Directory) ? 1 : -1;
        }

        return string.Compare(a.FileModifiedDate, b.FileModifiedDate);
    }

    private static int SortByDateAsc(FileStruct a, FileStruct b)
    {
        if (a.Type != b.Type)
        {
            return (a.Type == FileStructType.Directory) ? -1 : 1;
        }

        return -1 * string.Compare(a.FileModifiedDate, b.FileModifiedDate);
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

    private void SortFields(SortingField sortingField, bool canChangeOrder = false)
    {
        switch (sortingField)
        {
            case SortingField.FileName:
                if (canChangeOrder && sortingField == this.currentSortingField)
                {
                    this.sortDescending[0] = !this.sortDescending[0];
                }

                this.files.Sort(this.sortDescending[0] ? SortByFileNameDesc : SortByFileNameAsc);
                break;

            case SortingField.Type:
                if (canChangeOrder && sortingField == this.currentSortingField)
                {
                    this.sortDescending[1] = !this.sortDescending[1];
                }

                this.files.Sort(this.sortDescending[1] ? SortByTypeDesc : SortByTypeAsc);
                break;

            case SortingField.Size:
                if (canChangeOrder && sortingField == this.currentSortingField)
                {
                    this.sortDescending[2] = !this.sortDescending[2];
                }

                this.files.Sort(this.sortDescending[2] ? SortBySizeDesc : SortBySizeAsc);
                break;

            case SortingField.Date:
                if (canChangeOrder && sortingField == this.currentSortingField)
                {
                    this.sortDescending[3] = !this.sortDescending[3];
                }

                this.files.Sort(this.sortDescending[3] ? SortByDateDesc : SortByDateAsc);
                break;
        }

        if (sortingField != SortingField.None)
        {
            this.currentSortingField = sortingField;
        }

        this.ApplyFilteringOnFileList();
    }
}
