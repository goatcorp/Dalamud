// Copyright (c) Nate McMaster, Dalamud contributors.
// Licensed under the Apache License, Version 2.0. See License.txt in the Loader root for license information.

using System.Diagnostics;
using System.IO;

namespace Dalamud.Plugin.Internal.Loader.LibraryModel;

/// <summary>
/// Represents an unmanaged library, such as `libsqlite3`, which may need to be loaded
/// for P/Invoke to work.
/// </summary>
[DebuggerDisplay("{Name} = {AdditionalProbingPath}")]
internal class NativeLibrary
{
    private NativeLibrary(string name, string appLocalPath, string additionalProbingPath)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.AppLocalPath = appLocalPath ?? throw new ArgumentNullException(nameof(appLocalPath));
        this.AdditionalProbingPath = additionalProbingPath ?? throw new ArgumentNullException(nameof(additionalProbingPath));
    }

    /// <summary>
    /// Gets the name of the native library. This should match the name of the P/Invoke call.
    /// <para>
    /// For example, if specifying `[DllImport("sqlite3")]`, <see cref="Name" /> should be <c>sqlite3</c>.
    /// This may not match the exact file name as loading will attempt variations on the name according
    /// to OS convention. On Windows, P/Invoke will attempt to load `sqlite3.dll`. On macOS, it will
    /// attempt to find `sqlite3.dylib` and `libsqlite3.dylib`. On Linux, it will attempt to find
    /// `sqlite3.so` and `libsqlite3.so`.
    /// </para>
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the path to file within a deployed, framework-dependent application.
    /// <para>
    /// For example, <c>runtimes/linux-x64/native/libsqlite.so</c>.
    /// </para>
    /// </summary>
    public string AppLocalPath { get; }

    /// <summary>
    /// Gets the path to file within an additional probing path root. This is typically a combination
    /// of the NuGet package ID (lowercased), version, and path within the package.
    /// <para>
    /// For example, <c>sqlite/3.13.3/runtimes/linux-x64/native/libsqlite.so</c>.
    /// </para>
    /// </summary>
    public string AdditionalProbingPath { get; }

    /// <summary>
    /// Create an instance of <see cref="NativeLibrary" /> from a NuGet package.
    /// </summary>
    /// <param name="packageId">The name of the package.</param>
    /// <param name="packageVersion">The version of the package.</param>
    /// <param name="assetPath">The path within the NuGet package.</param>
    /// <returns>A native library.</returns>
    public static NativeLibrary CreateFromPackage(string packageId, string packageVersion, string assetPath)
    {
        return new NativeLibrary(
            Path.GetFileNameWithoutExtension(assetPath),
            assetPath,
            Path.Combine(packageId.ToLowerInvariant(), packageVersion, assetPath));
    }
}
