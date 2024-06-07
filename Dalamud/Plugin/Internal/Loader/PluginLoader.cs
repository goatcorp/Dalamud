// Copyright (c) Nate McMaster, Dalamud team.
// Licensed under the Apache License, Version 2.0. See License.txt in the Loader root for license information.

using System.Reflection;
using System.Runtime.Loader;

namespace Dalamud.Plugin.Internal.Loader;

/// <summary>
/// This loader attempts to load binaries for execution (both managed assemblies and native libraries)
/// in the same way that .NET Core would if they were originally part of the .NET Core application.
/// <para>
/// This loader reads configuration files produced by .NET Core (.deps.json and runtimeconfig.json)
/// as well as a custom file (*.config files). These files describe a list of .dlls and a set of dependencies.
/// The loader searches the plugin path, as well as any additionally specified paths, for binaries
/// which satisfy the plugin's requirements.
/// </para>
/// </summary>
internal class PluginLoader : IDisposable
{
    private readonly LoaderConfig config;
    private readonly AssemblyLoadContextBuilder contextBuilder;
    private ManagedLoadContext context;
    private volatile bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoader"/> class.
    /// </summary>
    /// <param name="config">The configuration for the plugin.</param>
    public PluginLoader(LoaderConfig config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.contextBuilder = CreateLoadContextBuilder(config);
        this.context = (ManagedLoadContext)this.contextBuilder.Build();
    }

    /// <summary>
    /// Gets a value indicating whether this plugin is capable of being unloaded.
    /// </summary>
    public bool IsUnloadable
        => this.context.IsCollectible;

    /// <summary>
    /// Gets the assembly load context.
    /// </summary>
    public AssemblyLoadContext LoadContext => this.context;

    /// <summary>
    /// Create a plugin loader for an assembly file.
    /// </summary>
    /// <param name="assemblyFile">The file path to the main assembly for the plugin.</param>
    /// <param name="configure">A function which can be used to configure advanced options for the plugin loader.</param>
    /// <returns>A loader.</returns>
    public static PluginLoader CreateFromAssemblyFile(string assemblyFile, Action<LoaderConfig> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var config = new LoaderConfig(assemblyFile);
        configure(config);
        return new PluginLoader(config);
    }

    /// <summary>
    /// The unloads and reloads the plugin assemblies.
    /// This method throws if <see cref="IsUnloadable" /> is <c>false</c>.
    /// </summary>
    public void Reload()
    {
        this.EnsureNotDisposed();

        if (!this.IsUnloadable)
        {
            throw new InvalidOperationException("Reload cannot be used because IsUnloadable is false");
        }

        this.context.Unload();
        this.context = (ManagedLoadContext)this.contextBuilder.Build();

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// Load the main assembly for the plugin.
    /// </summary>
    /// <returns>The assembly.</returns>
    public Assembly LoadDefaultAssembly()
    {
        this.EnsureNotDisposed();
        return this.context.LoadAssemblyFromFilePath(this.config.MainAssemblyPath);
    }

    /// <summary>
    /// Sets the scope used by some System.Reflection APIs which might trigger assembly loading.
    /// <para>
    /// See https://github.com/dotnet/coreclr/blob/v3.0.0/Documentation/design-docs/AssemblyLoadContext.ContextualReflection.md for more details.
    /// </para>
    /// </summary>
    /// <returns>A contextual reflection scope.</returns>
    public AssemblyLoadContext.ContextualReflectionScope EnterContextualReflection()
        => this.context.EnterContextualReflection();

    /// <summary>
    /// Disposes the plugin loader. This only does something if <see cref="IsUnloadable" /> is true.
    /// When true, this will unload assemblies which which were loaded during the lifetime
    /// of the plugin.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;

        if (this.context.IsCollectible)
            this.context.Unload();
    }

    private static AssemblyLoadContextBuilder CreateLoadContextBuilder(LoaderConfig config)
    {
        var builder = new AssemblyLoadContextBuilder();

        builder.SetMainAssemblyPath(config.MainAssemblyPath);
        builder.SetDefaultContext(config.DefaultContext);

        foreach (var ext in config.PrivateAssemblies)
        {
            builder.PreferLoadContextAssembly(ext);
        }

        if (config.PreferSharedTypes)
        {
            builder.PreferDefaultLoadContext(true);
        }

        if (config.IsUnloadable)
        {
            builder.EnableUnloading();
        }

        if (config.LoadInMemory)
        {
            builder.PreloadAssembliesIntoMemory();
            builder.ShadowCopyNativeLibraries();
        }

        foreach (var (assemblyName, recursive) in config.SharedAssemblies)
        {
            builder.PreferDefaultLoadContextAssembly(assemblyName, recursive);
        }

        // Note: not adding Dalamud path here as a probing path.
        // It will be dealt as the last resort from ManagedLoadContext.Load.
        // See there for more details.

        return builder;
    }

    private void EnsureNotDisposed()
    {
        if (this.disposed)
            throw new ObjectDisposedException(nameof(PluginLoader));
    }
}
