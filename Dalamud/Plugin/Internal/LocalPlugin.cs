using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Exceptions;
using Dalamud.Plugin.Internal.Loader;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

namespace Dalamud.Plugin.Internal
{
    /// <summary>
    /// This class represents a plugin and all facets of its lifecycle.
    /// The DLL on disk, dependencies, loaded assembly, etc.
    /// </summary>
    internal class LocalPlugin : IDisposable
    {
        private static readonly ModuleLog Log = new("LOCALPLUGIN");

        private readonly FileInfo manifestFile;
        private readonly FileInfo disabledFile;
        private readonly FileInfo testingFile;

        private PluginLoader? loader;
        private Assembly? pluginAssembly;
        private Type? pluginType;
        private IDalamudPlugin? instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalPlugin"/> class.
        /// </summary>
        /// <param name="dllFile">Path to the DLL file.</param>
        /// <param name="manifest">The plugin manifest.</param>
        public LocalPlugin(FileInfo dllFile, LocalPluginManifest? manifest)
        {
            if (dllFile.Name == "FFXIVClientStructs.Generators.dll")
            {
                // Could this be done another way? Sure. It is an extremely common source
                // of errors in the log through, and should never be loaded as a plugin.
                Log.Error($"Not a plugin: {dllFile.FullName}");
                throw new InvalidPluginException(dllFile);
            }

            this.DllFile = dllFile;
            this.State = PluginState.Unloaded;

            this.loader = PluginLoader.CreateFromAssemblyFile(this.DllFile.FullName, this.SetupLoaderConfig);

            try
            {
                this.pluginAssembly = this.loader.LoadDefaultAssembly();
            }
            catch (Exception ex)
            {
                this.pluginAssembly = null;
                this.pluginType = null;
                this.loader.Dispose();

                Log.Error(ex, $"Not a plugin: {this.DllFile.FullName}");
                throw new InvalidPluginException(this.DllFile);
            }

            try
            {
                this.pluginType = this.pluginAssembly.GetTypes().FirstOrDefault(type => type.IsAssignableTo(typeof(IDalamudPlugin)));
            }
            catch (ReflectionTypeLoadException ex)
            {
                Log.Error(ex, $"Could not load one or more types when searching for IDalamudPlugin: {this.DllFile.FullName}");
                // Something blew up when parsing types, but we still want to look for IDalamudPlugin. Let Load() handle the error.
                this.pluginType = ex.Types.FirstOrDefault(type => type.IsAssignableTo(typeof(IDalamudPlugin)));
            }

            if (this.pluginType == default)
            {
                this.pluginAssembly = null;
                this.pluginType = null;
                this.loader.Dispose();

                Log.Error($"Nothing inherits from IDalamudPlugin: {this.DllFile.FullName}");
                throw new InvalidPluginException(this.DllFile);
            }

            var assemblyVersion = this.pluginAssembly.GetName().Version;

            // Although it is conditionally used here, we need to set the initial value regardless.
            this.manifestFile = LocalPluginManifest.GetManifestFile(this.DllFile);

            // If the parameter manifest was null
            if (manifest == null)
            {
                this.Manifest = new LocalPluginManifest()
                {
                    Author = "developer",
                    Name = Path.GetFileNameWithoutExtension(this.DllFile.Name),
                    InternalName = Path.GetFileNameWithoutExtension(this.DllFile.Name),
                    AssemblyVersion = assemblyVersion,
                    Description = string.Empty,
                    ApplicableVersion = GameVersion.Any,
                    DalamudApiLevel = PluginManager.DalamudApiLevel,
                    IsHide = false,
                };

                // Save the manifest to disk so there won't be any problems later.
                // We'll update the name property after it can be retrieved from the instance.
                this.Manifest.Save(this.manifestFile);
            }
            else
            {
                this.Manifest = manifest;
            }

            // This converts from the ".disabled" file feature to the manifest instead.
            this.disabledFile = LocalPluginManifest.GetDisabledFile(this.DllFile);
            if (this.disabledFile.Exists)
            {
                this.Manifest.Disabled = true;
                this.disabledFile.Delete();
            }

            // This converts from the ".testing" file feature to the manifest instead.
            this.testingFile = LocalPluginManifest.GetTestingFile(this.DllFile);
            if (this.testingFile.Exists)
            {
                this.Manifest.Testing = true;
                this.testingFile.Delete();
            }

            var pluginManager = Service<PluginManager>.Get();
            this.IsBanned = pluginManager.IsManifestBanned(this.Manifest);
            this.BanReason = pluginManager.GetBanReason(this.Manifest);

            this.SaveManifest();
        }

        /// <summary>
        /// Gets the <see cref="DalamudPluginInterface"/> associated with this plugin.
        /// </summary>
        public DalamudPluginInterface? DalamudInterface { get; private set; }

        /// <summary>
        /// Gets the path to the plugin DLL.
        /// </summary>
        public FileInfo DllFile { get; }

        /// <summary>
        /// Gets the plugin manifest, if one exists.
        /// </summary>
        public LocalPluginManifest Manifest { get; private set; }

        /// <summary>
        /// Gets or sets the current state of the plugin.
        /// </summary>
        public PluginState State { get; protected set; }

        /// <summary>
        /// Gets the AssemblyName plugin, populated during <see cref="Load(PluginLoadReason, bool)"/>.
        /// </summary>
        /// <returns>Plugin type.</returns>
        public AssemblyName? AssemblyName { get; private set; }

        /// <summary>
        /// Gets the plugin name, directly from the plugin or if it is not loaded from the manifest.
        /// </summary>
        public string Name => this.instance?.Name ?? this.Manifest.Name;

        /// <summary>
        /// Gets an optional reason, if the plugin is banned.
        /// </summary>
        public string BanReason { get; }

        /// <summary>
        /// Gets a value indicating whether the plugin is loaded and running.
        /// </summary>
        public bool IsLoaded => this.State == PluginState.Loaded;

        /// <summary>
        /// Gets a value indicating whether the plugin is disabled.
        /// </summary>
        public bool IsDisabled => this.Manifest.Disabled;

        /// <summary>
        /// Gets a value indicating whether this plugin's API level is out of date.
        /// </summary>
        public bool IsOutdated => this.Manifest.DalamudApiLevel < PluginManager.DalamudApiLevel;

        /// <summary>
        /// Gets a value indicating whether the plugin is for testing use only.
        /// </summary>
        public bool IsTesting => this.Manifest.IsTestingExclusive || this.Manifest.Testing;

        /// <summary>
        /// Gets a value indicating whether this plugin has been banned.
        /// </summary>
        public bool IsBanned { get; }

        /// <summary>
        /// Gets a value indicating whether this plugin is dev plugin.
        /// </summary>
        public bool IsDev => this is LocalDevPlugin;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.instance?.Dispose();
            this.instance = null;

            this.DalamudInterface?.ExplicitDispose();
            this.DalamudInterface = null;

            this.pluginType = null;
            this.pluginAssembly = null;

            this.loader?.Dispose();
        }

        /// <summary>
        /// Load this plugin.
        /// </summary>
        /// <param name="reason">The reason why this plugin is being loaded.</param>
        /// <param name="reloading">Load while reloading.</param>
        public void Load(PluginLoadReason reason, bool reloading = false)
        {
            var startInfo = Service<DalamudStartInfo>.Get();
            var configuration = Service<DalamudConfiguration>.Get();
            var pluginManager = Service<PluginManager>.Get();

            // Allowed: Unloaded
            switch (this.State)
            {
                case PluginState.InProgress:
                    throw new InvalidPluginOperationException($"Unable to load {this.Name}, already working");
                case PluginState.Loaded:
                    throw new InvalidPluginOperationException($"Unable to load {this.Name}, already loaded");
                case PluginState.LoadError:
                    throw new InvalidPluginOperationException($"Unable to load {this.Name}, load previously faulted, unload first");
                case PluginState.UnloadError:
                    throw new InvalidPluginOperationException($"Unable to load {this.Name}, unload previously faulted, restart Dalamud");
            }

            if (pluginManager.IsManifestBanned(this.Manifest))
                throw new BannedPluginException($"Unable to load {this.Name}, banned");

            if (this.Manifest.ApplicableVersion < startInfo.GameVersion)
                throw new InvalidPluginOperationException($"Unable to load {this.Name}, no applicable version");

            if (this.Manifest.DalamudApiLevel < PluginManager.DalamudApiLevel && !configuration.LoadAllApiLevels)
                throw new InvalidPluginOperationException($"Unable to load {this.Name}, incompatible API level");

            if (this.Manifest.Disabled)
                throw new InvalidPluginOperationException($"Unable to load {this.Name}, disabled");

            this.State = PluginState.InProgress;
            Log.Information($"Loading {this.DllFile.Name}");

            if (this.DllFile.DirectoryName != null && File.Exists(Path.Combine(this.DllFile.DirectoryName, "Dalamud.dll")))
            {
                Log.Error("==== IMPORTANT MESSAGE TO {0}, THE DEVELOPER OF {1} ====", this.Manifest.Author!, this.Manifest.InternalName);
                Log.Error("YOU ARE INCLUDING DALAMUD DEPENDENCIES IN YOUR BUILDS!!!");
                Log.Error("You may not be able to load your plugin. \"<Private>False</Private>\" needs to be set in your csproj.");
                Log.Error("If you are using ILMerge, do not merge anything other than your direct dependencies.");
                Log.Error("Do not merge FFXIVClientStructs.Generators.dll.");
                Log.Error("Please refer to https://github.com/goatcorp/Dalamud/discussions/603 for more information.");
            }

            try
            {
                this.loader ??= PluginLoader.CreateFromAssemblyFile(this.DllFile.FullName, this.SetupLoaderConfig);

                if (reloading || this.IsDev)
                {
                    if (this.IsDev)
                    {
                        // If a dev plugin is set to not autoload on boot, but we want to reload it at the arbitrary load
                        // time, we need to essentially "Unload" the plugin, but we can't call plugin.Unload because of the
                        // load state checks. Null any references to the assembly and types, then proceed with regular reload
                        // operations.
                        this.pluginAssembly = null;
                        this.pluginType = null;
                    }

                    this.loader.Reload();

                    if (this.IsDev)
                    {
                        // Reload the manifest in-case there were changes here too.
                        var manifestFile = LocalPluginManifest.GetManifestFile(this.DllFile);
                        if (manifestFile.Exists)
                        {
                            this.Manifest = LocalPluginManifest.Load(manifestFile);
                        }
                    }
                }

                // Load the assembly
                this.pluginAssembly ??= this.loader.LoadDefaultAssembly();

                this.AssemblyName = this.pluginAssembly.GetName();

                // Find the plugin interface implementation. It is guaranteed to exist after checking in the ctor.
                this.pluginType ??= this.pluginAssembly.GetTypes().First(type => type.IsAssignableTo(typeof(IDalamudPlugin)));

                // Check for any loaded plugins with the same assembly name
                var assemblyName = this.pluginAssembly.GetName().Name;
                foreach (var otherPlugin in pluginManager.InstalledPlugins)
                {
                    // During hot-reloading, this plugin will be in the plugin list, and the instance will have been disposed
                    if (otherPlugin == this || otherPlugin.instance == null)
                        continue;

                    var otherPluginAssemblyName = otherPlugin.instance.GetType().Assembly.GetName().Name;
                    if (otherPluginAssemblyName == assemblyName)
                    {
                        this.State = PluginState.Unloaded;
                        Log.Debug($"Duplicate assembly: {this.Name}");

                        throw new DuplicatePluginException(assemblyName);
                    }
                }

                // Update the location for the Location and CodeBase patches
                PluginManager.PluginLocations[this.pluginType.Assembly.FullName] = new(this.DllFile);

                this.DalamudInterface = new DalamudPluginInterface(this.pluginAssembly.GetName().Name!, this.DllFile, reason, this.IsDev);

                var ioc = Service<ServiceContainer>.Get();
                this.instance = ioc.Create(this.pluginType, this.DalamudInterface) as IDalamudPlugin;
                if (this.instance == null)
                {
                    this.State = PluginState.LoadError;
                    this.DalamudInterface.ExplicitDispose();
                    Log.Error($"Error while loading {this.Name}, failed to bind and call the plugin constructor");
                    return;
                }

                // In-case the manifest name was a placeholder. Can occur when no manifest was included.
                if (this.instance.Name != this.Manifest.Name)
                {
                    this.Manifest.Name = this.instance.Name;
                    this.Manifest.Save(this.manifestFile);
                }

                this.State = PluginState.Loaded;
                Log.Information($"Finished loading {this.DllFile.Name}");
            }
            catch (Exception ex)
            {
                this.State = PluginState.LoadError;
                Log.Error(ex, $"Error while loading {this.Name}");

                throw;
            }
        }

        /// <summary>
        /// Unload this plugin. This is the same as dispose, but without the "disposed" connotations. This object should stay
        /// in the plugin list until it has been actually disposed.
        /// </summary>
        /// <param name="reloading">Unload while reloading.</param>
        public void Unload(bool reloading = false)
        {
            // Allowed: Loaded, LoadError(we are cleaning this up while we're at it)
            switch (this.State)
            {
                case PluginState.InProgress:
                    throw new InvalidPluginOperationException($"Unable to unload {this.Name}, already working");
                case PluginState.Unloaded:
                    throw new InvalidPluginOperationException($"Unable to unload {this.Name}, already unloaded");
                case PluginState.UnloadError:
                    throw new InvalidPluginOperationException($"Unable to unload {this.Name}, unload previously faulted, restart Dalamud");
            }

            try
            {
                this.State = PluginState.InProgress;
                Log.Information($"Unloading {this.DllFile.Name}");

                this.instance?.Dispose();
                this.instance = null;

                this.DalamudInterface?.ExplicitDispose();
                this.DalamudInterface = null;

                this.pluginType = null;
                this.pluginAssembly = null;

                if (!reloading)
                {
                    this.loader?.Dispose();
                    this.loader = null;
                }

                this.State = PluginState.Unloaded;
                Log.Information($"Finished unloading {this.DllFile.Name}");
            }
            catch (Exception ex)
            {
                this.State = PluginState.UnloadError;
                Log.Error(ex, $"Error while unloading {this.Name}");

                throw;
            }
        }

        /// <summary>
        /// Reload this plugin.
        /// </summary>
        public void Reload()
        {
            this.Unload(true);
            this.Load(PluginLoadReason.Reload, true);
        }

        /// <summary>
        /// Revert a disable. Must be unloaded first, does not load.
        /// </summary>
        public void Enable()
        {
            // Allowed: Unloaded, UnloadError
            switch (this.State)
            {
                case PluginState.InProgress:
                case PluginState.Loaded:
                case PluginState.LoadError:
                    throw new InvalidPluginOperationException($"Unable to enable {this.Name}, still loaded");
            }

            if (!this.Manifest.Disabled)
                throw new InvalidPluginOperationException($"Unable to enable {this.Name}, not disabled");

            this.Manifest.Disabled = false;
            this.SaveManifest();
        }

        /// <summary>
        /// Disable this plugin, must be unloaded first.
        /// </summary>
        public void Disable()
        {
            // Allowed: Unloaded, UnloadError
            switch (this.State)
            {
                case PluginState.InProgress:
                case PluginState.Loaded:
                case PluginState.LoadError:
                    throw new InvalidPluginOperationException($"Unable to disable {this.Name}, still loaded");
            }

            if (this.Manifest.Disabled)
                throw new InvalidPluginOperationException($"Unable to disable {this.Name}, already disabled");

            this.Manifest.Disabled = true;
            this.SaveManifest();
        }

        private void SetupLoaderConfig(LoaderConfig config)
        {
            config.IsUnloadable = true;
            config.LoadInMemory = true;
            config.PreferSharedTypes = false;
            config.SharedAssemblies.Add(typeof(Lumina.GameData).Assembly.GetName());
            config.SharedAssemblies.Add(typeof(Lumina.Excel.ExcelSheetImpl).Assembly.GetName());
        }

        private void SaveManifest() => this.Manifest.Save(this.manifestFile);
    }
}
