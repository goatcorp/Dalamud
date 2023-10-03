using System.IO;

using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// Plugin Definition.
/// </summary>
internal struct PluginDef
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginDef"/> struct.
    /// </summary>
    /// <param name="dllFile">plugin dll file.</param>
    /// <param name="manifest">plugin manifest.</param>
    /// <param name="isDev">plugin dev indicator.</param>
    public PluginDef(FileInfo dllFile, LocalPluginManifest manifest, bool isDev)
    {
        this.DllFile = dllFile;
        this.Manifest = manifest;
        this.IsDev = isDev;
    }

    /// <summary>
    /// Gets plugin DLL File.
    /// </summary>
    public FileInfo DllFile { get; init; }

    /// <summary>
    /// Gets plugin manifest.
    /// </summary>
    public LocalPluginManifest Manifest { get; init; }

    /// <summary>
    /// Gets a value indicating whether plugin is a dev plugin.
    /// </summary>
    public bool IsDev { get; init; }

    /// <summary>
    /// Sort plugin definitions by priority.
    /// </summary>
    /// <param name="def1">plugin definition 1 to compare.</param>
    /// <param name="def2">plugin definition 2 to compare.</param>
    /// <returns>sort order.</returns>
    public static int Sorter(PluginDef def1, PluginDef def2)
    {
        var priority1 = def1.Manifest?.LoadPriority ?? 0;
        var priority2 = def2.Manifest?.LoadPriority ?? 0;
        return priority2.CompareTo(priority1);
    }
}
