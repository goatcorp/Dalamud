using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Dalamud.Interface.ImGuiFileDialog;

/// <summary>
/// A file or folder picker.
/// </summary>
public partial class FileDialog
{
    private static Regex filterRegex = new(@"[^,{}]+(\{([^{}]*?)\})?", RegexOptions.Compiled);

    private List<FilterStruct> filters = new();
    private FilterStruct selectedFilter;

    private void ParseFilters(string filters)
    {
        // ".*,.cpp,.h,.hpp"
        // "Source files{.cpp,.h,.hpp},Image files{.png,.gif,.jpg,.jpeg},.md"

        this.filters.Clear();
        if (filters.Length == 0) return;

        var currentFilterFound = false;
        var matches = filterRegex.Matches(filters);
        foreach (Match m in matches)
        {
            var match = m.Value;
            var filter = default(FilterStruct);

            if (match.Contains("{"))
            {
                var exts = m.Groups[2].Value;
                filter = new FilterStruct
                {
                    Filter = match.Split('{')[0],
                    CollectionFilters = new HashSet<string>(exts.Split(',')),
                };
            }
            else
            {
                filter = new FilterStruct
                {
                    Filter = match,
                    CollectionFilters = new(),
                };
            }

            this.filters.Add(filter);

            if (!currentFilterFound && filter.Filter == this.selectedFilter.Filter)
            {
                currentFilterFound = true;
                this.selectedFilter = filter;
            }
        }

        if (!currentFilterFound && !(this.filters.Count == 0))
        {
            this.selectedFilter = this.filters[0];
        }
    }

    private void SetSelectedFilterWithExt(string ext)
    {
        if (this.filters.Count == 0) return;
        if (string.IsNullOrEmpty(ext)) return;

        foreach (var filter in this.filters)
        {
            if (filter.FilterExists(ext))
            {
                this.selectedFilter = filter;
            }
        }

        if (this.selectedFilter.Empty())
        {
            this.selectedFilter = this.filters[0];
        }
    }

    private void ApplyFilteringOnFileList()
    {
        lock (this.filesLock)
        {
            this.filteredFiles.Clear();

            foreach (var file in this.files)
            {
                var show = true;
                if (!string.IsNullOrEmpty(this.searchBuffer) && !file.FileName.ToLower().Contains(this.searchBuffer.ToLower()))
                {
                    show = false;
                }

                if (this.IsDirectoryMode() && file.Type != FileStructType.Directory)
                {
                    show = false;
                }

                if (show)
                {
                    this.filteredFiles.Add(file);
                }
            }
        }
    }
}
