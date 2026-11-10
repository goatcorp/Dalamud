using System.IO;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Configuration;
using Dalamud.Storage;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Service to interact with the file system, as a replacement for standard C# file I/O.
/// Writes and reads using this service are, to the best of our ability, atomic and reliable.
///
/// All data is synced to disk immediately and written to a database, additionally to files on disk. This means
/// that in case of file corruption, data can likely be recovered from the database.
///
/// However, this also means that operations using this service duplicate data on disk, so we don't recommend
/// performing large file operations. The service will not permit files larger than <see cref="MaxFileSizeBytes"/>
/// (64MB) to be written.
///
/// Saved configuration data using the <see cref="PluginConfigurations"/> class uses this functionality implicitly.
/// </summary>
public interface IReliableFileStorage : IDalamudService
{
    /// <summary>
    /// Gets the maximum file size, in bytes, that can be written using this service.
    /// </summary>
    /// <remarks>
    /// The service enforces this limit when writing files and fails with an appropriate exception
    /// (for example <see cref="ArgumentException"/> or a custom exception) when a caller attempts to write
    /// more than this number of bytes.
    /// </remarks>
    long MaxFileSizeBytes { get; }

    /// <summary>
    /// Check whether a file exists either on the local filesystem or in the transparent backup database.
    /// </summary>
    /// <param name="path">The file system path to check. Must not be null or empty.</param>
    /// <returns>
    /// True if the file exists on disk or a backup copy exists in the storage's internal journal/backup database;
    /// otherwise false.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    bool Exists(string path);

    /// <summary>
    /// Write the given text into a file using UTF-8 encoding. The write is performed atomically and is persisted to
    /// both the filesystem and the internal backup database used by this service.
    /// </summary>
    /// <param name="path">The file path to write to. Must not be null or empty.</param>
    /// <param name="contents">The string contents to write. May be null, in which case an empty file is written.</param>
    /// <returns>A <see cref="Task"/> that completes when the write has finished and been flushed to disk and the backup.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    Task WriteAllTextAsync(string path, string? contents);

    /// <summary>
    /// Write the given text into a file using the provided <paramref name="encoding"/>. The write is performed
    /// atomically (to the extent possible) and is persisted to both the filesystem and the internal backup database
    /// used by this service.
    /// </summary>
    /// <param name="path">The file path to write to. Must not be null or empty.</param>
    /// <param name="contents">The string contents to write. May be null, in which case an empty file is written.</param>
    /// <param name="encoding">The text encoding to use when serializing the string to bytes. Must not be null.</param>
    /// <returns>A <see cref="Task"/> that completes when the write has finished and been flushed to disk and the backup.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is null.</exception>
    Task WriteAllTextAsync(string path, string? contents, Encoding encoding);

    /// <summary>
    /// Write the given bytes to a file. The write is persisted to both the filesystem and the service's internal
    /// backup database. Avoid writing extremely large byte arrays because this service duplicates data on disk.
    /// </summary>
    /// <param name="path">The file path to write to. Must not be null or empty.</param>
    /// <param name="bytes">The raw bytes to write. Must not be null.</param>
    /// <returns>A <see cref="Task"/> that completes when the write has finished and been flushed to disk and the backup.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is null.</exception>
    Task WriteAllBytesAsync(string path, byte[] bytes);

    /// <summary>
    /// Read all text from a file using UTF-8 encoding. If the file is unreadable or missing on disk, the service
    /// attempts to return a backed-up copy from its internal journal/backup database.
    /// </summary>
    /// <param name="path">The file path to read. Must not be null or empty.</param>
    /// <param name="forceBackup">
    /// When true the service prefers the internal backup database and returns backed-up contents if available. When
    /// false the service tries the filesystem first and falls back to the backup only on error or when the file is missing.
    /// </param>
    /// <returns>The textual contents of the file, decoded using UTF-8.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist on disk and no backup copy is available.</exception>
    Task<string> ReadAllTextAsync(string path, bool forceBackup = false);

    /// <summary>
    /// Read all text from a file using the specified <paramref name="encoding"/>. If the file is unreadable or
    /// missing on disk, the service attempts to return a backed-up copy from its internal journal/backup database.
    /// </summary>
    /// <param name="path">The file path to read. Must not be null or empty.</param>
    /// <param name="encoding">The encoding to use when decoding the stored bytes into text. Must not be null.</param>
    /// <param name="forceBackup">
    /// When true the service prefers the internal backup database and returns backed-up contents if available. When
    /// false the service tries the filesystem first and falls back to the backup only on error or when the file is missing.
    /// </param>
    /// <returns>The textual contents of the file decoded using the provided <paramref name="encoding"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist on disk and no backup copy is available.</exception>
    Task<string> ReadAllTextAsync(string path, Encoding encoding, bool forceBackup = false);

    /// <summary>
    /// Read all text from a file and invoke the provided <paramref name="reader"/> callback with the string
    /// contents. If the reader throws or the initial read fails, the service attempts a backup read and invokes the
    /// reader again with the backup contents. If both reads fail the service surfaces an exception to the caller.
    /// </summary>
    /// <param name="path">The file path to read. Must not be null or empty.</param>
    /// <param name="reader">
    /// A callback invoked with the file's textual contents. Must not be null.
    /// If the callback throws an exception the service treats that as a signal to retry the read using the
    /// internal backup database and will invoke the callback again with the backup contents when available.
    /// For example, the callback can throw when JSON deserialization fails to request the backup copy instead of
    /// silently accepting corrupt data.
    /// </param>
    /// <returns>A <see cref="Task"/> that completes when the read (and any attempted fallback) and callback invocation have finished.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reader"/> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist on disk and no backup copy is available.</exception>
    /// <exception cref="FileReadException">Thrown when both the filesystem read and the backup read fail for other reasons.</exception>
    Task ReadAllTextAsync(string path, Action<string> reader);

    /// <summary>
    /// Read all text from a file using the specified <paramref name="encoding"/> and invoke the provided
    /// <paramref name="reader"/> callback with the decoded string contents. If the reader throws or the initial
    /// read fails, the service attempts a backup read and invokes the reader again with the backup contents. If
    /// both reads fail the service surfaces an exception to the caller.
    /// </summary>
    /// <param name="path">The file path to read. Must not be null or empty.</param>
    /// <param name="encoding">The encoding to use when decoding the stored bytes into text. Must not be null.</param>
    /// <param name="reader">
    /// A callback invoked with the file's textual contents. Must not be null.
    /// If the callback throws an exception the service treats that as a signal to retry the read using the
    /// internal backup database and will invoke the callback again with the backup contents when available.
    /// For example, the callback can throw when JSON deserialization fails to request the backup copy instead of
    /// silently accepting corrupt data.
    /// </param>
    /// <returns>A <see cref="Task"/> that completes when the read (and any attempted fallback) and callback invocation have finished.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> or <paramref name="reader"/> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist on disk and no backup copy is available.</exception>
    /// <exception cref="FileReadException">Thrown when both the filesystem read and the backup read fail for other reasons.</exception>
    Task ReadAllTextAsync(string path, Encoding encoding, Action<string> reader);

    /// <summary>
    /// Read all bytes from a file. If the file is unreadable or missing on disk, the service may try to return a
    /// backed-up copy from its internal journal/backup database.
    /// </summary>
    /// <param name="path">The file path to read. Must not be null or empty.</param>
    /// <param name="forceBackup">
    /// When true the service prefers the internal backup database and returns the backed-up contents
    /// if available. When false the service tries the filesystem first and falls back to the backup only
    /// on error or when the file is missing.
    /// </param>
    /// <returns>The raw bytes stored in the file.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist on disk and no backup copy is available.</exception>
    Task<byte[]> ReadAllBytesAsync(string path, bool forceBackup = false);
}
