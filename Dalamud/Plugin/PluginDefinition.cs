using System.Collections.Generic;

namespace Dalamud.Plugin
{
    /// <summary>
    /// Class containing information about a plugin.
    /// </summary>
    public class PluginDefinition
    {
        /// <summary>
        /// Gets or sets the author/s of the plugin.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the public name of the plugin.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the internal name of the plugin, which should match the assembly name of the plugin.
        /// </summary>
        public string InternalName { get; set; }

        /// <summary>
        /// Gets or sets the current assembly version of the plugin.
        /// </summary>
        public string AssemblyVersion { get; set; }

        /// <summary>
        /// Gets or sets the current testing assembly version of the plugin.
        /// </summary>
        public string TestingAssemblyVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the plugin is only available for testing.
        /// </summary>
        public bool IsTestingExclusive { get; set; }

        /// <summary>
        /// Gets or sets a description of the plugins functions.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the version of the game this plugin works with.
        /// </summary>
        public string ApplicableVersion { get; set; }

        /// <summary>
        /// Gets or sets an URL to the website or source code of the plugin.
        /// </summary>
        public string RepoUrl { get; set; }

        /// <summary>
        /// Gets or sets a list of tags defined on the plugin.
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the plugin is hidden in the plugin installer.
        /// </summary>
        public bool IsHide { get; set; }

        /// <summary>
        /// Gets or sets the API level of this plugin. For the current API level, please see <see cref="PluginManager.DalamudApiLevel"/> for the currently used API level.
        /// </summary>
        public int DalamudApiLevel { get; set; }

        /// <summary>
        /// Gets or sets the number of downloads this plugin has.
        /// </summary>
        public long DownloadCount { get; set; }

        /// <summary>
        /// Gets or sets the last time this plugin was updated.
        /// </summary>
        public long LastUpdate { get; set; }

        /// <summary>
        /// Gets or sets the index of the third party repo.
        /// </summary>
        public int RepoNumber { get; set; }

        /// <summary>
        /// Gets or sets the download link used to install the plugin.
        /// </summary>
        public string DownloadLinkInstall { get; set; }

        /// <summary>
        /// Gets or sets the download link used to update the plugin.
        /// </summary>
        public string DownloadLinkUpdate { get; set; }

        /// <summary>
        /// Gets or sets the download link used to get testing versions of the plugin.
        /// </summary>
        public string DownloadLinkTesting { get; set; }

        /// <summary>
        /// Gets or sets the load priority for this plugin. Higher values means higher priority. 0 is default priority.
        /// </summary>
        public int LoadPriority { get; set; }
    }
}
