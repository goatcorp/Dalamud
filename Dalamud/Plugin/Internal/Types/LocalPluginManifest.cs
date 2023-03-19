using System;
using System.IO;

using Dalamud.Utility;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// Information about a plugin, packaged in a json file with the DLL. This variant includes additional information such as
/// if the plugin is disabled and if it was installed from a testing URL. This is designed for use with manifests on disk.
/// </summary>
internal record LocalPluginManifest : PluginManifest
{
    /// <summary>
    /// Flag indicating that a plugin was installed from the official repo.
    /// </summary>
    [JsonIgnore]
    public const string FlagMainRepo = "OFFICIAL";

    /// <summary>
    /// Flag indicating that a plugin is a dev plugin..
    /// </summary>
    [JsonIgnore]
    public const string FlagDevPlugin = "DEVPLUGIN";

    /// <summary>
    /// Gets or sets a value indicating whether the plugin is disabled and should not be loaded.
    /// This value supersedes the ".disabled" file functionality and should not be included in the plugin master.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the plugin should only be loaded when testing is enabled.
    /// This value supersedes the ".testing" file functionality and should not be included in the plugin master.
    /// </summary>
    public bool Testing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the plugin should be deleted during the next cleanup.
    /// </summary>
    public bool ScheduledForDeletion { get; set; }

    /// <summary>
    /// Gets or sets the 3rd party repo URL that this plugin was installed from. Used to display where the plugin was
    /// sourced from on the installed plugin view. This should not be included in the plugin master. This value is null
    /// when installed from the main repo.
    /// </summary>
    public string InstalledFromUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this manifest is associated with a plugin that was installed from a third party
    /// repo. Unless the manifest has been manually modified, this is determined by the InstalledFromUrl being null.
    /// </summary>
    public bool IsThirdParty => !this.InstalledFromUrl.IsNullOrEmpty() && this.InstalledFromUrl != FlagMainRepo;

    /// <summary>
    /// Gets the effective version of this plugin.
    /// </summary>
    public Version EffectiveVersion => this.Testing && this.TestingAssemblyVersion != null ? this.TestingAssemblyVersion : this.AssemblyVersion;

    /// <summary>
    /// Gets a value indicating whether this plugin is eligible for testing.
    /// </summary>
    public bool IsAvailableForTesting => this.TestingAssemblyVersion != null && this.TestingAssemblyVersion > this.AssemblyVersion;

    /// <summary>
    /// Save a plugin manifest to file.
    /// </summary>
    /// <param name="manifestFile">Path to save at.</param>
    public void Save(FileInfo manifestFile) => Util.WriteAllTextSafe(manifestFile.FullName, JsonConvert.SerializeObject(this, Formatting.Indented));

    /// <summary>
    /// Loads a plugin manifest from file.
    /// </summary>
    /// <param name="manifestFile">Path to the manifest.</param>
    /// <returns>A <see cref="PluginManifest"/> object.</returns>
    public static LocalPluginManifest Load(FileInfo manifestFile) => JsonConvert.DeserializeObject<LocalPluginManifest>(File.ReadAllText(manifestFile.FullName))!;

    /// <summary>
    /// A standardized way to get the plugin DLL name that should accompany a manifest file. May not exist.
    /// </summary>
    /// <param name="dir">Manifest directory.</param>
    /// <param name="manifest">The manifest.</param>
    /// <returns>The <see cref="LocalPlugin"/> file.</returns>
    public static FileInfo GetPluginFile(DirectoryInfo dir, PluginManifest manifest) => new(Path.Combine(dir.FullName, $"{manifest.InternalName}.dll"));

    /// <summary>
    /// A standardized way to get the manifest file that should accompany a plugin DLL. May not exist.
    /// </summary>
    /// <param name="dllFile">The plugin DLL.</param>
    /// <returns>The <see cref="PluginManifest"/> file.</returns>
    public static FileInfo GetManifestFile(FileInfo dllFile) => new(Path.Combine(dllFile.DirectoryName!, Path.GetFileNameWithoutExtension(dllFile.Name) + ".json"));

    /// <summary>
    /// A standardized way to get the obsolete .disabled file that should accompany a plugin DLL. May not exist.
    /// </summary>
    /// <param name="dllFile">The plugin DLL.</param>
    /// <returns>The <see cref="PluginManifest"/> .disabled file.</returns>
    public static FileInfo GetDisabledFile(FileInfo dllFile) => new(Path.Combine(dllFile.DirectoryName!, ".disabled"));

    /// <summary>
    /// A standardized way to get the obsolete .testing file that should accompany a plugin DLL. May not exist.
    /// </summary>
    /// <param name="dllFile">The plugin DLL.</param>
    /// <returns>The <see cref="PluginManifest"/> .testing file.</returns>
    public static FileInfo GetTestingFile(FileInfo dllFile) => new(Path.Combine(dllFile.DirectoryName!, ".testing"));
}
