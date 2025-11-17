using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dalamud.Utility;

/// <summary>Integrity utilities.</summary>
public static class IntegrityUtils
{
    private const int BufferSize = 1048576; // 1 MB
    private const char NormalizationPathSeparator = '/';

    /// <summary>Computes the hash of a <paramref name="directory"/> recursively using <see cref="HMACSHA256"/> with <paramref name="key"/>.</summary>
    /// <param name="directory">Directory to check integrity for.</param>
    /// <param name="key">Key for <see cref="HMACSHA256"/>.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns><see cref="HMACSHA256"/> result with <paramref name="key"/>.</returns>
    public static Task<byte[]> ComputeDirectoryHmacSha256Async(string directory, byte[] key, CancellationToken cancellationToken = default) => ComputeDirectoryHmacSha256Async(new DirectoryInfo(directory), key, cancellationToken);

    /// <summary>Computes the hash of a <paramref name="directory"/> recursively using <see cref="HMACSHA256"/> with <paramref name="key"/>.</summary>
    /// <param name="directory">Directory to check integrity for.</param>
    /// <param name="key">Key for <see cref="HMACSHA256"/>.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns><see cref="HMACSHA256"/> result with <paramref name="key"/>.</returns>
    public static async Task<byte[]> ComputeDirectoryHmacSha256Async(DirectoryInfo directory, byte[] key, CancellationToken cancellationToken = default)
    {
        // Read the entire directory structure recursively
        var structure = ReadDirectoryStructure(directory, cancellationToken);

        // Sort directories and files
        structure.Directories.Sort(Comparer<DirectoryInfo>.Create((left, right) => StringComparer.Ordinal.Compare(GetNormalizedPath(Path.GetRelativePath(directory.FullName, left.FullName)), GetNormalizedPath(Path.GetRelativePath(directory.FullName, right.FullName)))));
        structure.Files.Sort(Comparer<FileInfo>.Create((left, right) => StringComparer.Ordinal.Compare(GetNormalizedPath(Path.GetRelativePath(directory.FullName, left.FullName)), GetNormalizedPath(Path.GetRelativePath(directory.FullName, right.FullName)))));

        // Create HMACSHA256
        using var hmac = new HMACSHA256(key);

        // Hash directory paths
        foreach (var innerDirectory in structure.Directories)
        {
            var path = GetNormalizedPath(Path.GetRelativePath(directory.FullName, innerDirectory.FullName));
            var bytes = Encoding.UTF8.GetBytes(path);
            hmac.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        // Rent a buffer
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            // Hash file paths, sizes and contents
            foreach (var file in structure.Files)
            {
                var path = GetNormalizedPath(Path.GetRelativePath(directory.FullName, file.FullName));
                var bytes = (byte[])[.. Encoding.UTF8.GetBytes(path), .. BitConverter.GetBytes(file.Length)];
                hmac.TransformBlock(bytes, 0, bytes.Length, null, 0);

                await using var stream = file.OpenRead();
                while (true)
                {
                    var result = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (result is 0) break;
                    hmac.TransformBlock(buffer, 0, result, null, 0);
                }
            }
        }
        finally
        {
            // Return the buffer
            ArrayPool<byte>.Shared.Return(buffer);
        }

        // Compute HMACSHA256 and returns the hash
        _ = hmac.TransformFinalBlock([], 0, 0);
        var hash = hmac.Hash;
        Debug.Assert(hash is not null, "Result hash should not be null");
        return hash;
    }

    /// <summary>Reads <paramref name="directory"/>'s structure.</summary>
    /// <param name="directory">Directory to read the structure from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="DirectoryStructure"/>.</returns>
    private static DirectoryStructure ReadDirectoryStructure(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        // This method doesn't do anything by itself, it simply creates a new empty
        // structure then call ReadDirectoryStructureCore to do all the work recursively.
        var result = new DirectoryStructure();
        ReadDirectoryStructureCore(directory, result, cancellationToken);
        return result;
    }

    /// <summary>Inner functionality for <see cref="ReadDirectoryStructure(DirectoryInfo, CancellationToken)"/>.</summary>
    /// <param name="directory">Directory to read the structure from.</param>
    /// <param name="structure">Directory structure to add into.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private static void ReadDirectoryStructureCore(DirectoryInfo directory, DirectoryStructure structure, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Add all files into the structure
        structure.Files.AddRange(directory.GetFiles());

        // Add all directories into the structure
        var directories = directory.GetDirectories();
        structure.Directories.AddRange(directories);

        // Recurse into all directories
        foreach (var innerDirectory in directories)
        {
            ReadDirectoryStructureCore(innerDirectory, structure, cancellationToken);
        }
    }

    /// <summary>Normalize <paramref name="path"/> for hashing purposes.</summary>
    /// <param name="path">Path to normalize.</param>
    /// <returns>Normalized <paramref name="path"/>.</returns>
    private static string GetNormalizedPath(string path) => path.Replace(Path.DirectorySeparatorChar, NormalizationPathSeparator);

    /// <summary>Contains the directory structure.</summary>
    /// <remarks>Used internally only.</remarks>
    private sealed class DirectoryStructure
    {
        /// <summary>Gets all directories found recursively.</summary>
        public List<DirectoryInfo> Directories { get; } = [];

        /// <summary>Gets all files found recursively.</summary>
        public List<FileInfo> Files { get; } = [];
    }
}
