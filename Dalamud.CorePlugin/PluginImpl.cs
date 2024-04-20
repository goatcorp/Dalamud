using System;
using System.IO;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Serilog;

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
        public void Dispose()
        {
        }

#else

        private readonly WindowSystem windowSystem = new("Dalamud.CorePlugin");
        private Localization localization;

        private IPluginLog pluginLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginImpl"/> class.
        /// </summary>
        /// <param name="pluginInterface">Dalamud plugin interface.</param>
        /// <param name="log">Logging service.</param>
        public PluginImpl(DalamudPluginInterface pluginInterface, IPluginLog log)
        {
            try
            {
                // this.InitLoc();
                this.Interface = pluginInterface;
                this.pluginLog = log;

                this.windowSystem.AddWindow(new PluginWindow());

                this.Interface.UiBuilder.Draw += this.OnDraw;
                this.Interface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;
                this.Interface.UiBuilder.OpenMainUi += this.OnOpenMainUi;
                this.Interface.UiBuilder.DefaultFontHandle.ImFontChanged += (fc, _) =>
                {
                    Log.Information($"CorePlugin : DefaultFontHandle.ImFontChanged called {fc}");
                };

                Service<CommandManager>.Get().AddHandler("/coreplug", new(this.OnCommand) { HelpMessage = "Access the plugin." });

                log.Information("CorePlugin ctor!");
            }
            catch (Exception ex)
            {
                log.Error(ex, "kaboom");
            }
        }

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
                this.pluginLog.Error(ex, "Boom");
            }
        }

        private void OnCommand(string command, string args)
        {
            this.pluginLog.Information("Command called!");

            // this.window.IsOpen = true;
        }

        private void OnOpenConfigUi()
        {
            // this.window.IsOpen = true;
        }

        private void OnOpenMainUi()
        {
            Log.Verbose("Opened main UI");
        }

#endif
    }
}
