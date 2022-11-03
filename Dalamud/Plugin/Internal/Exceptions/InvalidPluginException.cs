using System.IO;

namespace Dalamud.Plugin.Internal.Exceptions;

/// <summary>
/// This exception represents a file that does not implement IDalamudPlugin.
/// </summary>
internal class InvalidPluginException : PluginException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPluginException"/> class.
    /// </summary>
    /// <param name="dllFile">The invalid file.</param>
    public InvalidPluginException(FileInfo dllFile)
    {
        this.DllFile = dllFile;
    }

    /// <summary>
    /// Gets the invalid file.
    /// </summary>
    public FileInfo DllFile { get; init; }
}
