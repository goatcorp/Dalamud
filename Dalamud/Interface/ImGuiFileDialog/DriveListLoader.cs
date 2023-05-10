using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace Dalamud.Interface.ImGuiFileDialog;

/// <summary>
/// A drive list loader. Thread-safety guaranteed.
/// </summary>
internal class DriveListLoader
{
    private bool initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="DriveListLoader"/> class.
    /// </summary>
    public DriveListLoader()
    {
        this.Drives = ImmutableArray<DriveInfo>.Empty;
    }

    /// <summary>
    /// Gets the drive list. This may be incomplete if the loader is still loading.
    /// </summary>
    public IReadOnlyList<DriveInfo> Drives { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not the loader is loading.
    /// </summary>
    public bool Loading { get; private set; }

    /// <summary>
    /// Loads the drive list, asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task LoadDrivesAsync()
    {
        this.Loading = true;
        try
        {
            await this.InitDrives();
        }
        finally
        {
            this.Loading = false;
        }
    }

    private async Task InitDrives()
    {
        // Force async to avoid this being invoked synchronously unless it's awaited.
        // Putting this after the GetDrives call (inside the loop) keeps this on the
        // UI thread sometimes.
        await Task.Yield();

        var drives = ImmutableArray<DriveInfo>.Empty;
        foreach (var drive in DriveInfo.GetDrives())
        {
            drives = drives.Add(drive);
            if (!this.initialized)
            {
                // Show results as soon as they load initially, but otherwise keep
                // the existing drive list
                this.Drives = drives;
            }
        }

        // Replace the whole drive list
        this.Drives = drives;
        this.initialized = true;
    }
}
