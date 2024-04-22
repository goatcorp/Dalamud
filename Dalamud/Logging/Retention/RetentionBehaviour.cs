using System.IO;

using Serilog;

namespace Dalamud.Logging.Retention;

/// <summary>
/// Class implementing retention behaviour for log files.
/// </summary>
internal abstract class RetentionBehaviour
{
    /// <summary>
    /// Apply the specified retention behaviour to log files.
    /// </summary>
    /// <param name="logFile">The regular log file path.</param>
    /// <param name="rolloverFile">The rollover "old" log file path.</param>
    public abstract void Apply(FileInfo logFile, FileInfo rolloverFile);
    
    /// <summary>
    /// Trim existing log file to a specified length, and optionally move the excess data to another file.
    /// </summary>
    /// <param name="logFile">Target log file to trim.</param>
    /// <param name="logMaxSize">Maximum size of target log file.</param>
    /// <param name="oldFile">.old file to move excess data to.</param>
    /// <param name="oldMaxSize">Maximum size of .old file.</param>
    protected static void CullLogFile(FileInfo logFile, int logMaxSize, FileInfo oldFile, int oldMaxSize)
    {
        var targetFiles = new[]
        {
            (logFile, logMaxSize),
            (oldFile, oldMaxSize),
        };
        var buffer = new byte[4096];

        try
        {
            if (!logFile.Exists)
                logFile.Create().Close();

            // 1. Move excess data from logFile to oldFile
            if (logFile.Length > logMaxSize)
            {
                using var reader = logFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var writer = oldFile.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                var amountToMove = (int)Math.Min(logFile.Length - logMaxSize, oldMaxSize);
                reader.Seek(-(logMaxSize + amountToMove), SeekOrigin.End);

                for (var i = 0; i < amountToMove; i += buffer.Length)
                    writer.Write(buffer, 0, reader.Read(buffer, 0, Math.Min(buffer.Length, amountToMove - i)));
            }

            // 2. Cull each of .log and .old files
            foreach (var (file, maxSize) in targetFiles)
            {
                if (!file.Exists || file.Length <= maxSize)
                    continue;

                using var reader = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var writer = file.Open(FileMode.Open, FileAccess.Write, FileShare.ReadWrite);

                reader.Seek(file.Length - maxSize, SeekOrigin.Begin);
                for (int read; (read = reader.Read(buffer, 0, buffer.Length)) > 0;)
                    writer.Write(buffer, 0, read);

                writer.SetLength(maxSize);
            }
        }
        catch (Exception ex)
        {
            if (ex is IOException)
            {
                foreach (var (file, _) in targetFiles)
                {
                    try
                    {
                        if (file.Exists)
                            file.Delete();
                    }
                    catch (Exception ex2)
                    {
                        Log.Error(ex2, "Failed to delete {file}", file.FullName);
                    }
                }
            }

            Log.Error(ex, "Log cull failed");
        }
    }
}
