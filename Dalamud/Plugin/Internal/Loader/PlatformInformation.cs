// Copyright (c) Nate McMaster, Dalamud team.
// Licensed under the Apache License, Version 2.0. See License.txt in the Loader root for license information.

namespace Dalamud.Plugin.Internal.Loader;

/// <summary>
/// Platform specific information.
/// </summary>
internal class PlatformInformation
{
    /// <summary>
    /// Gets a list of native OS specific library extensions.
    /// </summary>
    public static string[] NativeLibraryExtensions => [".dll"];

    /// <summary>
    /// Gets a list of native OS specific library prefixes.
    /// </summary>
    public static string[] NativeLibraryPrefixes => [string.Empty];

    /// <summary>
    /// Gets a list of native OS specific managed assembly extensions.
    /// </summary>
    public static string[] ManagedAssemblyExtensions =>
    [
        ".dll",
        ".ni.dll",
        ".exe",
        ".ni.exe",
    ];
}
