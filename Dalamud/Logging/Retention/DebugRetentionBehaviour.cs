using System.IO;

namespace Dalamud.Logging.Retention;

/// <summary>
/// Class implementing log retention behaviour for debug builds.
/// </summary>
internal class DebugRetentionBehaviour : RetentionBehaviour
{
    /// <inheritdoc/>
    public override void Apply(FileInfo logFile, FileInfo rolloverFile)
    {
        CullLogFile(logFile, 1 * 1024 * 1024, rolloverFile, 10 * 1024 * 1024);
    }
}
