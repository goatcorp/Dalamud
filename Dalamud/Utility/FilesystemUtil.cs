using System.ComponentModel;
using System.IO;
using System.Text;

using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace Dalamud.Utility;

/// <summary>
/// Helper functions for filesystem operations.
/// </summary>
public static class FilesystemUtil
{
    /// <summary>
    /// Overwrite text in a file by first writing it to a temporary file, and then
    /// moving that file to the path specified.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="text">The text to write.</param>
    public static void WriteAllTextSafe(string path, string text)
    {
        WriteAllTextSafe(path, text, Encoding.UTF8);
    }

    /// <summary>
    /// Overwrite text in a file by first writing it to a temporary file, and then
    /// moving that file to the path specified.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="text">The text to write.</param>
    /// <param name="encoding">Encoding to use.</param>
    public static void WriteAllTextSafe(string path, string text, Encoding encoding)
    {
        WriteAllBytesSafe(path, encoding.GetBytes(text));
    }

    /// <summary>
    /// Overwrite data in a file by first writing it to a temporary file, and then
    /// moving that file to the path specified.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="bytes">The data to write.</param>
    public static unsafe void WriteAllBytesSafe(string path, byte[] bytes)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        // Open the temp file
        var tempPath = path + ".tmp";

        var tempFile = Windows.Win32.PInvoke.CreateFile(
            tempPath,
            (uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE),
            FILE_SHARE_MODE.FILE_SHARE_NONE,
            null,
            FILE_CREATION_DISPOSITION.CREATE_ALWAYS,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
            HANDLE.Null);

        if (tempFile.IsNull)
            throw new Win32Exception();

        // Write the data
        uint bytesWritten = 0;
        fixed (byte* ptr = bytes)
        {
            if (!Windows.Win32.PInvoke.WriteFile(tempFile, ptr, (uint)bytes.Length, &bytesWritten, null))
                throw new Win32Exception();
        }

        if (bytesWritten != bytes.Length)
        {
            Windows.Win32.PInvoke.CloseHandle(tempFile);
            throw new Exception($"Could not write all bytes to temp file ({bytesWritten} of {bytes.Length})");
        }

        if (!Windows.Win32.PInvoke.FlushFileBuffers(tempFile))
        {
            Windows.Win32.PInvoke.CloseHandle(tempFile);
            throw new Win32Exception();
        }

        Windows.Win32.PInvoke.CloseHandle(tempFile);

        if (!Windows.Win32.PInvoke.MoveFileEx(tempPath, path, MOVE_FILE_FLAGS.MOVEFILE_REPLACE_EXISTING | MOVE_FILE_FLAGS.MOVEFILE_WRITE_THROUGH))
            throw new Win32Exception();
    }

    /// <summary>
    /// Generates a temporary file name.
    /// </summary>
    /// <returns>A temporary file name that is almost guaranteed to be unique.</returns>
    internal static string GetTempFileName()
    {
        // https://stackoverflow.com/a/50413126
        return Path.Combine(Path.GetTempPath(), "dalamud_" + Guid.NewGuid());
    }

    /// <summary>
    /// Copy files recursively from one directory to another.
    /// </summary>
    /// <param name="source">The source directory.</param>
    /// <param name="target">The target directory.</param>
    internal static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (var dir in source.GetDirectories())
            CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));

        foreach (var file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name));
    }

    /// <summary>
    /// Delete and recreate a directory.
    /// </summary>
    /// <param name="dir">The directory to delete and recreate.</param>
    internal static void DeleteAndRecreateDirectory(DirectoryInfo dir)
    {
        if (dir.Exists)
        {
            dir.Delete(true);
        }

        dir.Create();
    }
}
