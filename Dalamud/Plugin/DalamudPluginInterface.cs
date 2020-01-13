using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Configuration;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
using Dalamud.Game.Internal.Gui;
using Dalamud.Interface;

namespace Dalamud.Plugin
{
    /// <summary>
    /// This class acts as an interface to various objects needed to interact with Dalamud and the game.
    /// </summary>
    public class DalamudPluginInterface : IDisposable {
        /// <summary>
        /// The CommandManager object that allows you to add and remove custom chat commands.
        /// </summary>
        public readonly CommandManager CommandManager;

        /// <summary>
        /// The ClientState object that allows you to access current client memory information like actors, territories, etc.
        /// </summary>
        public readonly ClientState ClientState;

        /// <summary>
        /// The Framework object that allows you to interact with the client.
        /// </summary>
        public readonly Framework Framework;

        /// <summary>
		/// A <see cref="UiBuilder">UiBuilder</see> instance which allows you to draw UI into the game via ImGui draw calls.
        /// </summary>
        public readonly UiBuilder UiBuilder;

        /// A <see cref="SigScanner">SigScanner</see> instance targeting the main module of the FFXIV process.
        /// </summary>
        public readonly SigScanner TargetModuleScanner;

        private readonly Dalamud dalamud;
        private readonly string pluginName;

        /// <summary>
        /// Set up the interface and populate all fields needed.
        /// </summary>
        /// <param name="dalamud"></param>
        public DalamudPluginInterface(Dalamud dalamud, string pluginName) {
            this.CommandManager = dalamud.CommandManager;
            this.Framework = dalamud.Framework;
            this.ClientState = dalamud.ClientState;
            this.UiBuilder = new UiBuilder(dalamud.InterfaceManager, pluginName);
            this.TargetModuleScanner = new SigScanner(dalamud.TargetModule);

            this.dalamud = dalamud;
            this.pluginName = pluginName;
        }

        public void Dispose() {
            this.UiBuilder.Dispose();
        }

        /// <summary>
        /// Save a plugin configuration(inheriting IPluginConfiguration).
        /// </summary>
        /// <param name="currentConfig">The current configuration.</param>
        public void SavePluginConfig(IPluginConfiguration currentConfig) {
            if (this.dalamud.Configuration.PluginConfigurations == null)
                this.dalamud.Configuration.PluginConfigurations = new Dictionary<string, IPluginConfiguration>();

            if (this.dalamud.Configuration.PluginConfigurations.ContainsKey(this.pluginName)) {
                this.dalamud.Configuration.PluginConfigurations[this.pluginName] = currentConfig;
                return;
            }

            if (currentConfig == null)
                return;

            this.dalamud.Configuration.PluginConfigurations.Add(this.pluginName, currentConfig);
            this.dalamud.Configuration.Save(this.dalamud.StartInfo.ConfigurationPath);
        }

        /// <summary>
        /// Get a previously saved plugin configuration or null if none was saved before.
        /// </summary>
        /// <returns>A previously saved config or null if none was saved before.</returns>
        public IPluginConfiguration GetPluginConfig() {
            if (this.dalamud.Configuration.PluginConfigurations == null)
                this.dalamud.Configuration.PluginConfigurations = new Dictionary<string, IPluginConfiguration>();

            if (!this.dalamud.Configuration.PluginConfigurations.ContainsKey(this.pluginName))
                return null;

            return this.dalamud.Configuration.PluginConfigurations[this.pluginName];
        }
    }
}
