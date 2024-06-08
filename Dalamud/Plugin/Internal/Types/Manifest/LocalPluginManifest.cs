using System.IO;

using Dalamud.Utility;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Plugin.Internal.Types.Manifest;

/// <summary>
/// Information about a plugin, packaged in a json file with the DLL. This variant includes additional information such as
/// if the plugin is disabled and if it was installed from a testing URL. This is designed for use with manifests on disk.
/// </summary>
internal record LocalPluginManifest : PluginManifest, ILocalPluginManifest
{
    /// <summary>
    /// Gets or sets a value indicating whether the plugin is disabled and should not be loaded.
    /// This value supersedes the ".disabled" file functionality and should not be included in the plugin master.
    /// </summary>
    [Obsolete("This is merely used for migrations now. Use the profile manager to check if a plugin shall be enabled.")]
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the plugin should only be loaded when testing is enabled.
    /// This value supersedes the ".testing" file functionality and should not be included in the plugin master.
    /// </summary>
    public bool Testing { get; set; }

    /// <inheritdoc/>
    public bool ScheduledForDeletion { get; set; }

    /// <inheritdoc/>
    public string InstalledFromUrl { get; set; } = string.Empty;

    /// <inheritdoc/>
    public Guid WorkingPluginId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets a value indicating whether this manifest is associated with a plugin that was installed from a third party
    /// repo.
    /// </summary>
    public bool IsThirdParty => !this.InstalledFromUrl.IsNullOrEmpty() && this.InstalledFromUrl != SpecialPluginSource.MainRepo;

    /// <summary>
    /// Gets the effective version of this plugin.
    /// </summary>
    public Version EffectiveVersion => this.Testing && this.TestingAssemblyVersion != null ? this.TestingAssemblyVersion : this.AssemblyVersion;

    /// <summary>
    /// Gets the effective API level of this plugin.
    /// </summary>
    public int EffectiveApiLevel => this.Testing && this.TestingDalamudApiLevel != null ? this.TestingDalamudApiLevel.Value : this.DalamudApiLevel;

    /// <summary>
    /// Save a plugin manifest to file.
    /// </summary>
    /// <param name="manifestFile">Path to save at.</param>
    /// <param name="reason">The reason the manifest was saved.</param>
    public void Save(FileInfo manifestFile, string reason)
    {
        Log.Verbose("Saving manifest for '{PluginName}' because '{Reason}'", this.InternalName, reason);

        try
        {
            Util.WriteAllTextSafe(manifestFile.FullName, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch
        {
            Log.Error("Could not write out manifest for '{PluginName}' because '{Reason}'", this.InternalName, reason);
            throw;
        }
    }

    /// <summary>
    /// Loads a plugin manifest from file.
    /// </summary>
    /// <param name="manifestFile">Path to the manifest.</param>
    /// <returns>A <see cref="PluginManifest"/> object.</returns>
    public static LocalPluginManifest? Load(FileInfo manifestFile) => JsonConvert.DeserializeObject<LocalPluginManifest>(File.ReadAllText(manifestFile.FullName));

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
