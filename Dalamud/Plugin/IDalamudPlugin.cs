using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Plugin
{
    /// <summary>
    /// This interface represents a basic Dalamud plugin. All plugins have to implement this interface.
    /// </summary>
    public interface IDalamudPlugin : IDisposable
    {
        /// <summary>
        /// The name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Initializes a Dalamud plugin.
        /// </summary>
        /// <param name="pluginInterface">The <see cref="DalamudPluginInterface"/> needed to access various Dalamud objects.</param>
        void Initialize(DalamudPluginInterface pluginInterface);
    }
}
