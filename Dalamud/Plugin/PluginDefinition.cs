using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Plugin
{
    public class PluginDefinition
    {
        /// <summary>
        /// The author/s of the plugin.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The public name of the plugin.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The internal name of the plugin, which should match the assembly name of the plugin.
        /// </summary>
        public string InternalName { get; set; }

        /// <summary>
        /// The current assembly version of the plugin.
        /// </summary>
        public string AssemblyVersion { get; set; }

        /// <summary>
        /// The current testing assembly version of the plugin.
        /// </summary>
        public string TestingAssemblyVersion { get; set; }

        /// <summary>
        /// Defines if the plugin is only available for testing.
        /// </summary>
        public bool IsTestingExclusive { get; set; }

        /// <summary>
        /// A description of the plugins functions.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The version of the game this plugin works with.
        /// </summary>
        public string ApplicableVersion { get; set; }

        /// <summary>
        /// An URL to the website or source code of the plugin.
        /// </summary>
        public string RepoUrl { get; set; }

        /// <summary>
        /// List of tags defined on the plugin.
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Whether or not the plugin is hidden in the plugin installer.
        /// </summary>
        public bool IsHide { get; set; }

        /// <summary>
        /// The API level of this plugin. For the current API level, please see <see cref="PluginManager.DALAMUD_API_LEVEL"/> for the currently used API level.
        /// </summary>
        public int DalamudApiLevel { get; set; }

        /// <summary>
        /// The number of downloads this plugin has.
        /// </summary>
        public long DownloadCount { get; set; }

        /// <summary>
        /// The last time this plugin was updated.
        /// </summary>
        public long LastUpdate { get; set; }

        /// <summary>
        /// Number of the repo.
        /// </summary>
        public int RepoNumber { get; set; }
        
        /// <summary>
        /// Download link used to install the plugin.
        /// </summary>
        public string DownloadLinkInstall { get; set; }

        /// <summary>
        /// Download link used to update the plugin.
        /// </summary>
        public string DownloadLinkUpdate { get; set; }

        /// <summary>
        /// Download link used to get testing versions of the plugin.
        /// </summary>
        public string DownloadLinkTesting { get; set; }

        /// <summary>
        /// Load priority for this plugin. Higher values means higher priority. 0 is default priority.
        /// </summary>
        public int LoadPriority { get; set; }
    }
}
