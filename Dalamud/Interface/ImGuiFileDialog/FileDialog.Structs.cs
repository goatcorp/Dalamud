using System.Collections.Generic;
using System.Numerics;

namespace Dalamud.Interface.ImGuiFileDialog
{
    /// <summary>
    /// A file or folder picker.
    /// </summary>
    public partial class FileDialog
    {
        private struct FileStruct
        {
            public FileStructType Type;
            public string FilePath;
            public string FileName;
            public string Ext;
            public long FileSize;
            public string FormattedFileSize;
            public string FileModifiedDate;
        }

        private struct SideBarItem
        {
            public char Icon;
            public string Text;
            public string Location;
        }

        private struct FilterStruct
        {
            public string Filter;
            public HashSet<string> CollectionFilters;

            public void Clear()
            {
                this.Filter = string.Empty;
                this.CollectionFilters.Clear();
            }

            public bool Empty()
            {
                return string.IsNullOrEmpty(this.Filter) && ((this.CollectionFilters == null) || (this.CollectionFilters.Count == 0));
            }

            public bool FilterExists(string filter)
            {
                return (this.Filter == filter) || (this.CollectionFilters != null && this.CollectionFilters.Contains(filter));
            }
        }

        private struct IconColorItem
        {
            public char Icon;
            public Vector4 Color;
        }
    }
}
