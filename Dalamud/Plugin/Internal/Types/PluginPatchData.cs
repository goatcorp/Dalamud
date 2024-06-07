using System.IO;

namespace Dalamud.Plugin.Internal.Types;

internal record PluginPatchData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginPatchData"/> class.
    /// </summary>
    /// <param name="dllFile">DLL file being loaded.</param>
    public PluginPatchData(FileSystemInfo dllFile)
    {
        this.Location = dllFile.FullName;
        this.CodeBase = new Uri(dllFile.FullName).AbsoluteUri;
    }

    /// <summary>
    /// Gets simulated Assembly.Location output.
    /// </summary>
    public string Location { get; }

    /// <summary>
    /// Gets simulated Assembly.CodeBase output.
    /// </summary>
    public string CodeBase { get; }
}
