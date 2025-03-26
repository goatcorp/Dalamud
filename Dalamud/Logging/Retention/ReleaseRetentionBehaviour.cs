using System.IO;

namespace Dalamud.Logging.Retention;

/// <summary>
/// Class implementing log retention behaviour for release builds.
/// </summary>
internal class ReleaseRetentionBehaviour : RetentionBehaviour
{
    /// <inheritdoc/>
    public override void Apply(FileInfo logFile, FileInfo rolloverFile)
    {
        CullLogFile(logFile, 0, rolloverFile, 10 * 1024 * 1024);
    }
}
