// Copyright (c) Nate McMaster, Dalamud contributors.
// Licensed under the Apache License, Version 2.0. See License.txt in the Loader root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Dalamud.Plugin.Internal.Loader.LibraryModel;

namespace Dalamud.Plugin.Internal.Loader;

/// <summary>
/// An implementation of <see cref="AssemblyLoadContext" /> which attempts to load managed and native
/// binaries at runtime immitating some of the behaviors of corehost.
/// </summary>
[DebuggerDisplay("'{Name}' ({_mainAssemblyPath})")]
internal class ManagedLoadContext : AssemblyLoadContext
{
    private readonly string basePath;
    private readonly string mainAssemblyPath;
    private readonly IReadOnlyDictionary<string, ManagedLibrary> managedAssemblies;
    private readonly IReadOnlyDictionary<string, NativeLibrary> nativeLibraries;
    private readonly IReadOnlyCollection<string> privateAssemblies;
    private readonly ICollection<string> defaultAssemblies;
    private readonly IReadOnlyCollection<string> additionalProbingPaths;
    private readonly bool preferDefaultLoadContext;
    private readonly string[] resourceRoots;
    private readonly bool loadInMemory;
    private readonly AssemblyLoadContext defaultLoadContext;
    private readonly AssemblyDependencyResolver dependencyResolver;
    private readonly bool shadowCopyNativeLibraries;
    private readonly string unmanagedDllShadowCopyDirectoryPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedLoadContext"/> class.
    /// </summary>
    /// <param name="mainAssemblyPath">Main assembly path.</param>
    /// <param name="managedAssemblies">Managed assemblies.</param>
    /// <param name="nativeLibraries">Native assemblies.</param>
    /// <param name="privateAssemblies">Private assemblies.</param>
    /// <param name="defaultAssemblies">Default assemblies.</param>
    /// <param name="additionalProbingPaths">Additional probing paths.</param>
    /// <param name="resourceProbingPaths">Resource probing paths.</param>
    /// <param name="defaultLoadContext">Default load context.</param>
    /// <param name="preferDefaultLoadContext">If the default load context should be prefered.</param>
    /// <param name="isCollectible">If the dll is collectible.</param>
    /// <param name="loadInMemory">If the dll should  be loaded in memory.</param>
    /// <param name="shadowCopyNativeLibraries">If native libraries should be shadow copied.</param>
    public ManagedLoadContext(
        string mainAssemblyPath,
        IReadOnlyDictionary<string, ManagedLibrary> managedAssemblies,
        IReadOnlyDictionary<string, NativeLibrary> nativeLibraries,
        IReadOnlyCollection<string> privateAssemblies,
        IReadOnlyCollection<string> defaultAssemblies,
        IReadOnlyCollection<string> additionalProbingPaths,
        IReadOnlyCollection<string> resourceProbingPaths,
        AssemblyLoadContext defaultLoadContext,
        bool preferDefaultLoadContext,
        bool isCollectible,
        bool loadInMemory,
        bool shadowCopyNativeLibraries)
        : base(Path.GetFileNameWithoutExtension(mainAssemblyPath), isCollectible)
    {
        if (resourceProbingPaths == null)
            throw new ArgumentNullException(nameof(resourceProbingPaths));

        this.mainAssemblyPath = mainAssemblyPath ?? throw new ArgumentNullException(nameof(mainAssemblyPath));
        this.dependencyResolver = new AssemblyDependencyResolver(mainAssemblyPath);
        this.basePath = Path.GetDirectoryName(mainAssemblyPath) ?? throw new ArgumentException("Invalid assembly path", nameof(mainAssemblyPath));
        this.managedAssemblies = managedAssemblies ?? throw new ArgumentNullException(nameof(managedAssemblies));
        this.privateAssemblies = privateAssemblies ?? throw new ArgumentNullException(nameof(privateAssemblies));
        this.defaultAssemblies = defaultAssemblies != null ? defaultAssemblies.ToList() : throw new ArgumentNullException(nameof(defaultAssemblies));
        this.nativeLibraries = nativeLibraries ?? throw new ArgumentNullException(nameof(nativeLibraries));
        this.additionalProbingPaths = additionalProbingPaths ?? throw new ArgumentNullException(nameof(additionalProbingPaths));
        this.defaultLoadContext = defaultLoadContext;
        this.preferDefaultLoadContext = preferDefaultLoadContext;
        this.loadInMemory = loadInMemory;

        this.resourceRoots = new[] { this.basePath }
                             .Concat(resourceProbingPaths)
                             .ToArray();

        this.shadowCopyNativeLibraries = shadowCopyNativeLibraries;
        this.unmanagedDllShadowCopyDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        if (shadowCopyNativeLibraries)
        {
            this.Unloading += _ => this.OnUnloaded();
        }
    }

    /// <summary>
    /// Load an assembly from a filepath.
    /// </summary>
    /// <param name="path">Assembly path.</param>
    /// <returns>A loaded assembly.</returns>
    public Assembly LoadAssemblyFromFilePath(string path)
    {
        if (!this.loadInMemory)
            return this.LoadFromAssemblyPath(path);

        using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        var pdbPath = Path.ChangeExtension(path, ".pdb");
        if (File.Exists(pdbPath))
        {
            using var pdbFile = File.Open(pdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return this.LoadFromStream(file, pdbFile);
        }

        return this.LoadFromStream(file);
    }

    /// <summary>
    /// Load an assembly.
    /// </summary>
    /// <param name="assemblyName">Name of the assembly.</param>
    /// <returns>Loaded assembly.</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == null)
        {
            // not sure how to handle this case. It's technically possible.
            return null;
        }

        if ((this.preferDefaultLoadContext || this.defaultAssemblies.Contains(assemblyName.Name)) && !this.privateAssemblies.Contains(assemblyName.Name))
        {
            // If default context is preferred, check first for types in the default context unless the dependency has been declared as private
            try
            {
                var defaultAssembly = this.defaultLoadContext.LoadFromAssemblyName(assemblyName);
                if (defaultAssembly != null)
                {
                    // Older versions used to return null here such that returned assembly would be resolved from the default ALC.
                    // However, with the addition of custom default ALCs, the Default ALC may not be the user's chosen ALC when
                    // this context was built. As such, we simply return the Assembly from the user's chosen default load context.
                    return defaultAssembly;
                }
            }
            catch
            {
                // Swallow errors in loading from the default context
            }
        }

        var resolvedPath = this.dependencyResolver.ResolveAssemblyToPath(assemblyName);
        if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
        {
            return this.LoadAssemblyFromFilePath(resolvedPath);
        }

        // Resource assembly binding does not use the TPA. Instead, it probes PLATFORM_RESOURCE_ROOTS (a list of folders)
        // for $folder/$culture/$assemblyName.dll
        // See https://github.com/dotnet/coreclr/blob/3fca50a36e62a7433d7601d805d38de6baee7951/src/binder/assemblybinder.cpp#L1232-L1290

        if (!string.IsNullOrEmpty(assemblyName.CultureName) && !string.Equals(assemblyName.CultureName, "neutral"))
        {
            foreach (var resourceRoot in this.resourceRoots)
            {
                var resourcePath = Path.Combine(resourceRoot, assemblyName.CultureName, assemblyName.Name + ".dll");
                if (File.Exists(resourcePath))
                {
                    return this.LoadAssemblyFromFilePath(resourcePath);
                }
            }

            return null;
        }

        if (this.managedAssemblies.TryGetValue(assemblyName.Name, out var library) && library != null)
        {
            if (this.SearchForLibrary(library, out var path) && path != null)
            {
                return this.LoadAssemblyFromFilePath(path);
            }
        }
        else
        {
            // if an assembly was not listed in the list of known assemblies,
            // fallback to the load context base directory
            var dllName = assemblyName.Name + ".dll";
            foreach (var probingPath in this.additionalProbingPaths.Prepend(this.basePath))
            {
                var localFile = Path.Combine(probingPath, dllName);
                if (File.Exists(localFile))
                {
                    return this.LoadAssemblyFromFilePath(localFile);
                }
            }
        }

        // https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/loading-managed#algorithm
        // > These assemblies are loaded (load-by-name) as needed by the runtime.
        // For load-by-name assembiles, the following will happen in order:
        // (1) this.Load will be called.
        // (2) AssemblyLoadContext.Default's cache will be referred for lookup.
        // (3) Default probing will be done from PLATFORM_RESOURCE_ROOTS and APP_PATHS.
        // https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing#managed-assembly-default-probing
        // > TRUSTED_PLATFORM_ASSEMBLIES: List of platform and application assembly file paths.
        // > APP_PATHS: is not populated by default and is omitted for most applications.
        // If we return null here, if the assembly has not been already loaded, the resolution will fail.
        // Therefore as the final attempt, we try loading from the default load context.
        return this.defaultLoadContext.LoadFromAssemblyName(assemblyName);
    }

    /// <summary>
    /// Loads the unmanaged binary using configured list of native libraries.
    /// </summary>
    /// <param name="unmanagedDllName">Unmanaged DLL name.</param>
    /// <returns>The unmanaged dll handle.</returns>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var resolvedPath = this.dependencyResolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
        {
            return this.LoadUnmanagedDllFromResolvedPath(resolvedPath, normalizePath: false);
        }

        foreach (var prefix in PlatformInformation.NativeLibraryPrefixes)
        {
            if (this.nativeLibraries.TryGetValue(prefix + unmanagedDllName, out var library))
            {
                if (this.SearchForLibrary(library, prefix, out var path) && path != null)
                {
                    return this.LoadUnmanagedDllFromResolvedPath(path);
                }
            }
            else
            {
                // coreclr allows code to use [DllImport("sni")] or [DllImport("sni.dll")]
                // This library treats the file name without the extension as the lookup name,
                // so this loop is necessary to check if the unmanaged name matches a library
                // when the file extension has been trimmed.
                foreach (var suffix in PlatformInformation.NativeLibraryExtensions)
                {
                    if (!unmanagedDllName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // check to see if there is a library entry for the library without the file extension
                    var trimmedName = unmanagedDllName.Substring(0, unmanagedDllName.Length - suffix.Length);

                    if (this.nativeLibraries.TryGetValue(prefix + trimmedName, out library))
                    {
                        if (this.SearchForLibrary(library, prefix, out var path) && path != null)
                        {
                            return this.LoadUnmanagedDllFromResolvedPath(path);
                        }
                    }
                    else
                    {
                        // fallback to native assets which match the file name in the plugin base directory
                        var prefixSuffixDllName = prefix + unmanagedDllName + suffix;
                        var prefixDllName = prefix + unmanagedDllName;

                        foreach (var probingPath in this.additionalProbingPaths.Prepend(this.basePath))
                        {
                            var localFile = Path.Combine(probingPath, prefixSuffixDllName);
                            if (File.Exists(localFile))
                            {
                                return this.LoadUnmanagedDllFromResolvedPath(localFile);
                            }

                            var localFileWithoutSuffix = Path.Combine(probingPath, prefixDllName);
                            if (File.Exists(localFileWithoutSuffix))
                            {
                                return this.LoadUnmanagedDllFromResolvedPath(localFileWithoutSuffix);
                            }
                        }
                    }
                }
            }
        }

        return base.LoadUnmanagedDll(unmanagedDllName);
    }

    private bool SearchForLibrary(ManagedLibrary library, out string? path)
    {
        // 1. Check for in _basePath + app local path
        var localFile = Path.Combine(this.basePath, library.AppLocalPath);
        if (File.Exists(localFile))
        {
            path = localFile;
            return true;
        }

        // 2. Search additional probing paths
        foreach (var searchPath in this.additionalProbingPaths)
        {
            var candidate = Path.Combine(searchPath, library.AdditionalProbingPath);
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        // 3. Search in base path
        foreach (var ext in PlatformInformation.ManagedAssemblyExtensions)
        {
            var local = Path.Combine(this.basePath, library.Name.Name + ext);
            if (File.Exists(local))
            {
                path = local;
                return true;
            }
        }

        path = null;
        return false;
    }

    private bool SearchForLibrary(NativeLibrary library, string prefix, out string? path)
    {
        // 1. Search in base path
        foreach (var ext in PlatformInformation.NativeLibraryExtensions)
        {
            var candidate = Path.Combine(this.basePath, $"{prefix}{library.Name}{ext}");
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        // 2. Search in base path + app local (for portable deployments of netcoreapp)
        var local = Path.Combine(this.basePath, library.AppLocalPath);
        if (File.Exists(local))
        {
            path = local;
            return true;
        }

        // 3. Search additional probing paths
        foreach (var searchPath in this.additionalProbingPaths)
        {
            var candidate = Path.Combine(searchPath, library.AdditionalProbingPath);
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        path = null;
        return false;
    }

    private IntPtr LoadUnmanagedDllFromResolvedPath(string unmanagedDllPath, bool normalizePath = true)
    {
        if (normalizePath)
        {
            unmanagedDllPath = Path.GetFullPath(unmanagedDllPath);
        }

        return this.shadowCopyNativeLibraries
                   ? this.LoadUnmanagedDllFromShadowCopy(unmanagedDllPath)
                   : this.LoadUnmanagedDllFromPath(unmanagedDllPath);
    }

    private IntPtr LoadUnmanagedDllFromShadowCopy(string unmanagedDllPath)
    {
        var shadowCopyDllPath = this.CreateShadowCopy(unmanagedDllPath);

        return this.LoadUnmanagedDllFromPath(shadowCopyDllPath);
    }

    private string CreateShadowCopy(string dllPath)
    {
        Directory.CreateDirectory(this.unmanagedDllShadowCopyDirectoryPath);

        var dllFileName = Path.GetFileName(dllPath);
        var shadowCopyPath = Path.Combine(this.unmanagedDllShadowCopyDirectoryPath, dllFileName);

        if (!File.Exists(shadowCopyPath))
        {
            File.Copy(dllPath, shadowCopyPath);
        }

        return shadowCopyPath;
    }

    private void OnUnloaded()
    {
        if (!this.shadowCopyNativeLibraries || !Directory.Exists(this.unmanagedDllShadowCopyDirectoryPath))
        {
            return;
        }

        // Attempt to delete shadow copies
        try
        {
            Directory.Delete(this.unmanagedDllShadowCopyDirectoryPath, recursive: true);
        }
        catch (Exception)
        {
            // Files might be locked by host process. Nothing we can do about it, I guess.
        }
    }
}
