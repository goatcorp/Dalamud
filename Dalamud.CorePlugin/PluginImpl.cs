using System;
using System.IO;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;

namespace Dalamud.CorePlugin
{
    /// <summary>
    /// This class is a a plugin testbed for developing new Dalamud features with easy access to Dalamud itself.
    /// Be careful to not commit anything extra.
    /// </summary>
    /// <remarks>
    /// ██████╗ ███████╗ █████╗ ██████╗     ████████╗██╗  ██╗██╗███████╗
    /// ██╔══██╗██╔════╝██╔══██╗██╔══██╗    ╚══██╔══╝██║  ██║██║██╔════╝
    /// ██████╔╝█████╗  ███████║██║  ██║       ██║   ███████║██║███████╗
    /// ██╔══██╗██╔══╝  ██╔══██║██║  ██║       ██║   ██╔══██║██║╚════██║
    /// ██║  ██║███████╗██║  ██║██████╔╝       ██║   ██║  ██║██║███████║
    /// ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚═════╝        ╚═╝   ╚═╝  ╚═╝╚═╝╚══════╝
    /// CorePlugin should not be used as a base for new plugins. Use this instead https://github.com/goatcorp/SamplePlugin.
    /// While it may have similarities, it is compiled with access to Dalamud internals, which may cause confusion when
    /// some things work and others don't in normal operations.
    /// </remarks>
    public sealed class PluginImpl : IDalamudPlugin
    {
#if !DEBUG

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginImpl"/> class.
        /// </summary>
        /// <param name="pluginInterface">Dalamud plugin interface.</param>
        public PluginImpl(DalamudPluginInterface pluginInterface)
        {
        }

        /// <inheritdoc/>
        public string Name => "Dalamud.CorePlugin";

        /// <inheritdoc/>
        public void Dispose()
        {
        }

#else

        private readonly WindowSystem windowSystem = new("Dalamud.CorePlugin");
        private Localization localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginImpl"/> class.
        /// </summary>
        /// <param name="pluginInterface">Dalamud plugin interface.</param>
        public PluginImpl(DalamudPluginInterface pluginInterface)
        {
            try
            {
                // this.InitLoc();
                this.Interface = pluginInterface;

                this.windowSystem.AddWindow(new PluginWindow());

                this.Interface.UiBuilder.Draw += this.OnDraw;
                this.Interface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;

                Service<CommandManager>.Get().AddHandler("/coreplug", new(this.OnCommand) { HelpMessage = $"Access the {this.Name} plugin." });

                PluginLog.Information("CorePlugin ctor!");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "kaboom");
            }
        }

        /// <inheritdoc/>
        public string Name => "Dalamud.CorePlugin";

        /// <summary>
        /// Gets the plugin interface.
        /// </summary>
        internal DalamudPluginInterface Interface { get; private set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            Service<CommandManager>.Get().RemoveHandler("/coreplug");

            this.Interface.UiBuilder.Draw -= this.OnDraw;

            this.windowSystem.RemoveAllWindows();

            this.Interface.ExplicitDispose();
        }

        /// <summary>
        /// CheapLoc needs to be reinitialized here because it tracks the setup by assembly name. New assembly, new setup.
        /// </summary>
        public void InitLoc()
        {
            var dalamud = Service<Dalamud>.Get();
            var dalamudConfig = Service<DalamudConfiguration>.Get();

            this.localization = new Localization(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "loc", "dalamud"), "dalamud_");
            if (!dalamudConfig.LanguageOverride.IsNullOrEmpty())
            {
                this.localization.SetupWithLangCode(dalamudConfig.LanguageOverride);
            }
            else
            {
                this.localization.SetupWithUiCulture();
            }
        }

        /// <summary>
        /// Draw the window system.
        /// </summary>
        private void OnDraw()
        {
            try
            {
                this.windowSystem.Draw();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Boom");
            }
        }

        private void OnCommand(string command, string args)
        {
            PluginLog.Information("Command called!");

            // this.window.IsOpen = true;
        }

        private void OnOpenConfigUi()
        {
            // this.window.IsOpen = true;
        }

#endif
    }
}
