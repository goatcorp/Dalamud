// Copyright (c) Nate McMaster, Dalamud contributors.
// Licensed under the Apache License, Version 2.0. See License.txt in the Loader root for license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Dalamud.Plugin.Internal.Loader;

/// <summary>
/// Represents the configuration for a plugin loader.
/// </summary>
internal class LoaderConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoaderConfig"/> class.
    /// </summary>
    /// <param name="mainAssemblyPath">The full file path to the main assembly for the plugin.</param>
    public LoaderConfig(string mainAssemblyPath)
    {
        if (string.IsNullOrEmpty(mainAssemblyPath))
            throw new ArgumentException("Value must be null or not empty", nameof(mainAssemblyPath));

        if (!Path.IsPathRooted(mainAssemblyPath))
            throw new ArgumentException("Value must be an absolute file path", nameof(mainAssemblyPath));

        if (!File.Exists(mainAssemblyPath))
            throw new ArgumentException("Value must exist", nameof(mainAssemblyPath));

        this.MainAssemblyPath = mainAssemblyPath;
    }

    /// <summary>
    /// Gets the file path to the main assembly.
    /// </summary>
    public string MainAssemblyPath { get; }

    /// <summary>
    /// Gets a list of assemblies which should be treated as private.
    /// </summary>
    public ICollection<AssemblyName> PrivateAssemblies { get; } = new List<AssemblyName>();

    /// <summary>
    /// Gets a list of assemblies which should be unified between the host and the plugin.
    /// </summary>
    /// <seealso href="https://github.com/natemcmaster/DotNetCorePlugins/blob/main/docs/what-are-shared-types.md">what-are-shared-types</seealso>
    public ICollection<(AssemblyName Name, bool Recursive)> SharedAssemblies { get; } = new List<(AssemblyName Name, bool Recursive)>();

    /// <summary>
    /// Gets or sets a value indicating whether attempt to unify all types from a plugin with the host.
    /// <para>
    /// This does not guarantee types will unify.
    /// </para>
    /// <seealso href="https://github.com/natemcmaster/DotNetCorePlugins/blob/main/docs/what-are-shared-types.md">what-are-shared-types</seealso>
    /// </summary>
    public bool PreferSharedTypes { get; set; }

    /// <summary>
    /// Gets or sets the default <see cref="AssemblyLoadContext"/> used by the <see cref="PluginLoader"/>.
    /// Use this feature if the <see cref="AssemblyLoadContext"/> of the <see cref="Assembly"/> is not the Runtime's default load context.
    /// i.e. (AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly) != <see cref="AssemblyLoadContext.Default"/>.
    /// </summary>
    public AssemblyLoadContext DefaultContext { get; set; } = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) ?? AssemblyLoadContext.Default;

    /// <summary>
    /// Gets or sets a value indicating whether the plugin can be unloaded from memory.
    /// </summary>
    public bool IsUnloadable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to load assemblies into memory in order to not lock files.
    /// </summary>
    public bool LoadInMemory { get; set; }
}
