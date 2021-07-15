using System;

namespace Dalamud.Plugin.Internal.Types
{
    /// <summary>
    /// Plugin update status.
    /// </summary>
    internal class PluginUpdateStatus
    {
        /// <summary>
        /// Gets or sets the plugin internal name.
        /// </summary>
        public string InternalName { get; set; }

        /// <summary>
        /// Gets or sets the plugin name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the plugin version.
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the plugin was updated.
        /// </summary>
        public bool WasUpdated { get; set; }
    }
}
