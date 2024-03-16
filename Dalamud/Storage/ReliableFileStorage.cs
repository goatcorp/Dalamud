using System.IO;
using System.Text;

using Dalamud.Logging.Internal;
using Dalamud.Utility;
using SQLite;

namespace Dalamud.Storage;

/*
 * TODO: A file that is read frequently, but written very rarely, might not have offline changes by users persisted
 * into the backup database, since it is only written to the backup database when it is written to the filesystem.
 */

/// <summary>
/// A service that provides a reliable file storage.
/// Implements a VFS that writes files to the disk, and additionally keeps files in a SQLite database
/// for journaling/backup purposes.
/// Consumers can choose to receive a backup if they think that the file is corrupt.
/// </summary>
/// <remarks>
/// This is not an early-loaded service, as it is needed before they are initialized.
/// </remarks>
[ServiceManager.ProvidedService]
[Api10ToDo("Make internal and IInternalDisposableService, and remove #pragma guard from the caller.")]
public class ReliableFileStorage : IPublicDisposableService
{
    private static readonly ModuleLog Log = new("VFS");

    private readonly object syncRoot = new();

    private SQLiteConnection? db;
    private bool isService;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ReliableFileStorage"/> class.
    /// </summary>
    /// <param name="vfsDbPath">Path to the VFS.</param>
    [Obsolete("Dalamud internal use only.", false)]
    [Api10ToDo("Make internal, and remove #pragma guard from the caller.")]
    public ReliableFileStorage(string vfsDbPath)
    {
        var databasePath = Path.Combine(vfsDbPath, "dalamudVfs.db");
        
        Log.Verbose("Initializing VFS database at {Path}", databasePath);

        try
        {
            this.SetupDb(databasePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load VFS database, starting fresh");

            try
            {
                if (File.Exists(databasePath))
                    File.Delete(databasePath);
                
                this.SetupDb(databasePath);
            }
            catch (Exception)
            {
                // ignored, we can run without one
            }
        }
    }

    /// <summary>
    /// Check if a file exists.
    /// This will return true if the file does not exist on the filesystem, but in the transparent backup.
    /// You must then use this instance to read the file to ensure consistency.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="containerId">The container to check in.</param>
    /// <returns>True if the file exists.</returns>
    public bool Exists(string path, Guid containerId = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (File.Exists(path))
            return true;

        if (this.db == null)
            return false;
        
        // If the file doesn't actually exist on the FS, but it does in the DB, we can say YES and read operations will read from the DB instead
        var normalizedPath = NormalizePath(path);
        var file = this.db.Table<DbFile>().FirstOrDefault(f => f.Path == normalizedPath && f.ContainerId == containerId);
        return file != null;
    }
    
    /// <summary>
    /// Write all text to a file.
    /// </summary>
    /// <param name="path">Path to write to.</param>
    /// <param name="contents">The contents of the file.</param>
    /// <param name="containerId">Container to write to.</param>
    public void WriteAllText(string path, string? contents, Guid containerId = default)
        => this.WriteAllText(path, contents, Encoding.UTF8, containerId);
    
    /// <summary>
    /// Write all text to a file.
    /// </summary>
    /// <param name="path">Path to write to.</param>
    /// <param name="contents">The contents of the file.</param>
    /// <param name="encoding">The encoding to write with.</param>
    /// <param name="containerId">Container to write to.</param>
    public void WriteAllText(string path, string? contents, Encoding encoding, Guid containerId = default)
    {
        var bytes = encoding.GetBytes(contents ?? string.Empty);
        this.WriteAllBytes(path, bytes, containerId);
    }
    
    /// <summary>
    /// Write all bytes to a file.
    /// </summary>
    /// <param name="path">Path to write to.</param>
    /// <param name="bytes">The contents of the file.</param>
    /// <param name="containerId">Container to write to.</param>
    public void WriteAllBytes(string path, byte[] bytes, Guid containerId = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        lock (this.syncRoot)
        {
            if (this.db == null)
            {
                Util.WriteAllBytesSafe(path, bytes);
                return;
            }
        
            this.db.RunInTransaction(() =>
            {
                var normalizedPath = NormalizePath(path);
                var file = this.db.Table<DbFile>().FirstOrDefault(f => f.Path == normalizedPath && f.ContainerId == containerId);
                if (file == null)
                {
                    file = new DbFile
                    {
                        ContainerId = containerId,
                        Path = normalizedPath,
                        Data = bytes,
                    };
                    this.db.Insert(file);
                }
                else
                {
                    file.Data = bytes;
                    this.db.Update(file);
                }
        
                Util.WriteAllBytesSafe(path, bytes);
            });
        }
    }

    /// <summary>
    /// Read all text from a file.
    /// If the file does not exist on the filesystem, a read is attempted from the backup. The backup is not
    /// automatically written back to disk, however.
    /// </summary>
    /// <param name="path">The path to read from.</param>
    /// <param name="forceBackup">Whether or not the backup of the file should take priority.</param>
    /// <param name="containerId">The container to read from.</param>
    /// <returns>All text stored in this file.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist on the filesystem or in the backup.</exception>
    public string ReadAllText(string path, bool forceBackup = false, Guid containerId = default)
        => this.ReadAllText(path, Encoding.UTF8, forceBackup, containerId);

    /// <summary>
    /// Read all text from a file.
    /// If the file does not exist on the filesystem, a read is attempted from the backup. The backup is not
    /// automatically written back to disk, however.
    /// </summary>
    /// <param name="path">The path to read from.</param>
    /// <param name="encoding">The encoding to read with.</param>
    /// <param name="forceBackup">Whether or not the backup of the file should take priority.</param>
    /// <param name="containerId">The container to read from.</param>
    /// <returns>All text stored in this file.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist on the filesystem or in the backup.</exception>
    public string ReadAllText(string path, Encoding encoding, bool forceBackup = false, Guid containerId = default)
    {
        var bytes = this.ReadAllBytes(path, forceBackup, containerId);
        return encoding.GetString(bytes);
    }
    
    /// <summary>
    /// Read all text from a file, and automatically try again with the backup if the file does not exist or
    /// the <paramref name="reader"/> function throws an exception. If the backup read also throws an exception,
    /// or the file does not exist in the backup, a <see cref="FileReadException"/> is thrown.
    /// </summary>
    /// <param name="path">The path to read from.</param>
    /// <param name="reader">Lambda that reads the file. Throw here to automatically attempt a read from the backup.</param>
    /// <param name="containerId">The container to read from.</param>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist on the filesystem or in the backup.</exception>
    /// <exception cref="FileReadException">Thrown here if the file and the backup fail their read.</exception>
    public void ReadAllText(string path, Action<string> reader, Guid containerId = default)
        => this.ReadAllText(path, Encoding.UTF8, reader, containerId);

    /// <summary>
    /// Read all text from a file, and automatically try again with the backup if the file does not exist or
    /// the <paramref name="reader"/> function throws an exception. If the backup read also throws an exception,
    /// or the file does not exist in the backup, a <see cref="FileReadException"/> is thrown.
    /// </summary>
    /// <param name="path">The path to read from.</param>
    /// <param name="encoding">The encoding to read with.</param>
    /// <param name="reader">Lambda that reads the file. Throw here to automatically attempt a read from the backup.</param>
    /// <param name="containerId">The container to read from.</param>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist on the filesystem or in the backup.</exception>
    /// <exception cref="FileReadException">Thrown here if the file and the backup fail their read.</exception>
    public void ReadAllText(string path, Encoding encoding, Action<string> reader, Guid containerId = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        
        // TODO: We are technically reading one time too many here, if the file does not exist on the FS, ReadAllText
        // fails over to the backup, and then the backup fails to read in the lambda. We should do something about that,
        // but it's not a big deal. Would be nice if ReadAllText could indicate if it did fail over.
        
        // 1.) Try without using the backup
        try
        {
            var text = this.ReadAllText(path, encoding, false, containerId);
            reader(text);
            return;
        }
        catch (FileNotFoundException)
        {
            // We can't do anything about this.
            throw;
        }
        catch (Exception ex)
        {
            Log.Verbose(ex, "First chance read from {Path} failed, trying backup", path);
        }
        
        // 2.) Try using the backup
        try
        {
            var text = this.ReadAllText(path, encoding, true, containerId);
            reader(text);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Second chance read from {Path} failed, giving up", path);
            throw new FileReadException(ex);
        }
    }

    /// <summary>
    /// Read all bytes from a file.
    /// If the file does not exist on the filesystem, a read is attempted from the backup. The backup is not
    /// automatically written back to disk, however.
    /// </summary>
    /// <param name="path">The path to read from.</param>
    /// <param name="forceBackup">Whether or not the backup of the file should take priority.</param>
    /// <param name="containerId">The container to read from.</param>
    /// <returns>All bytes stored in this file.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist on the filesystem or in the backup.</exception>
    public byte[] ReadAllBytes(string path, bool forceBackup = false, Guid containerId = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        
        if (forceBackup)
        {
            // If the db failed to load, act as if the file does not exist
            if (this.db == null)
                throw new FileNotFoundException("Backup database was not available");
                
            var normalizedPath = NormalizePath(path);
            var file = this.db.Table<DbFile>().FirstOrDefault(f => f.Path == normalizedPath && f.ContainerId == containerId);
            if (file == null)
                throw new FileNotFoundException();

            return file.Data;
        }

        // If the file doesn't exist, immediately check the backup db
        if (!File.Exists(path))
            return this.ReadAllBytes(path, true, containerId);
        
        try
        {
            return File.ReadAllBytes(path);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to read file from disk, falling back to database");
            return this.ReadAllBytes(path, true, containerId);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!this.isService)
            this.DisposeCore();
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        if (this.isService)
            this.DisposeCore();
    }

    /// <inheritdoc/>
    void IPublicDisposableService.MarkDisposeOnlyFromService() => this.isService = true;

    /// <summary>
    /// Replace possible non-portable parts of a path with portable versions.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    private static string NormalizePath(string path)
    {
        // Replace users folder
        var usersFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        path = path.Replace(usersFolder, "%USERPROFILE%");
        
        return path;
    }
    
    private void SetupDb(string path)
    {
        this.db = new SQLiteConnection(path,
                                       SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
        this.db.CreateTable<DbFile>();
    }

    private void DisposeCore() => this.db?.Dispose();

    private class DbFile
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        
        public Guid ContainerId { get; set; }
        
        public string Path { get; set; } = null!;

        public byte[] Data { get; set; } = null!;
    }
}
