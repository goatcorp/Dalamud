using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Plugin
{
    /// <summary>
    /// This enum reflects reasons for loading a plugin.
    /// </summary>
    [Flags]
    public enum PluginLoadReason 
    {
        None = 0,

        /// <summary>
        /// We don't know why this plugin was loaded.
        /// </summary>
        Unknown = 1,

        /// <summary>
        /// This plugin was loaded because it was installed with the plugin installer.
        /// </summary>
        Installer = 2,

        /// <summary>
        /// This plugin was loaded because the game was started or Dalamud was reinjected.
        /// </summary>
        Boot = 4,

        /// <summary>
        /// This plugin was loaded from the installedPlugins folder.
        /// </summary>
        Installed = 8,

        /// <summary>
        /// This plugin was loaded from the devPlugins folder.
        /// </summary>
        Dev = 16
    }
}
