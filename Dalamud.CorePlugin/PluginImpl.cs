using System;
using System.Diagnostics.CodeAnalysis;
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
        private readonly IChatGui chatGui;
#if !DEBUG

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginImpl"/> class.
        /// </summary>
        /// <param name="pluginInterface">Dalamud plugin interface.</param>
        public PluginImpl(IDalamudPluginInterface pluginInterface)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

#else

        private readonly WindowSystem windowSystem = new("Dalamud.CorePlugin");

        private IPluginLog pluginLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginImpl"/> class.
        /// </summary>
        /// <param name="pluginInterface">Dalamud plugin interface.</param>
        /// <param name="log">Logging service.</param>
        /// <param name="commandManager">Command manager.</param>
        /// <param name="chatGui">Chat GUI.</param>
        [Experimental("Dalamud001")]
        public PluginImpl(IDalamudPluginInterface pluginInterface, IPluginLog log, ICommandManager commandManager, IChatGui chatGui)
        {
            this.chatGui = chatGui;
            this.Interface = pluginInterface;
            this.pluginLog = log;

            try
            {
                this.windowSystem.AddWindow(new PluginWindow());

                this.Interface.UiBuilder.Draw += this.OnDraw;
                this.Interface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;
                this.Interface.UiBuilder.OpenMainUi += this.OnOpenMainUi;
                this.Interface.UiBuilder.DefaultFontHandle.ImFontChanged += (fc, _) =>
                {
                    Log.Information($"CorePlugin : DefaultFontHandle.ImFontChanged called {fc}");
                };

                commandManager.AddHandler("/coreplug", new CommandInfo(this.OnCommand) { HelpMessage = "Access the plugin." });
                commandManager.AddCommand("/coreplugnew", "Access the plugin.", this.OnCommandNew);

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
        internal IDalamudPluginInterface Interface { get; private set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            Service<CommandManager>.Get().RemoveHandler("/coreplug");

            this.Interface.UiBuilder.Draw -= this.OnDraw;

            this.windowSystem.RemoveAllWindows();
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
            this.chatGui.Print("Command called!");

            // this.window.IsOpen = true;
        }

        private bool OnCommandNew(bool var1, int var2, string? var3)
        {
            this.chatGui.Print($"CorePlugin: Command called! var1: {var1}, var2: {var2}, var3: {var3}");
            return true;
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
