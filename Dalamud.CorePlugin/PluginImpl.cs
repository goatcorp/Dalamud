using System;

using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace Dalamud.CorePlugin
{
    /// <summary>
    /// This class is a a plugin testbed for developing new Dalamud features with easy access to Dalamud itself.
    /// Be careful to not commit anything extra.
    /// </summary>
    public sealed class PluginImpl : IDalamudPlugin
    {
        private readonly CommandManager commandManager;
        private readonly WindowSystem windowSystem = new("Dalamud.CorePlugin");
        private Localization localizationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginImpl"/> class.
        /// </summary>
#pragma warning disable SA1611
        public PluginImpl([RequiredVersion("1.0")] CommandManager commandManager)
#pragma warning restore SA1611
        {
            // your constructor only gets called in the event that dalamud can satisfy all of its dependencies through the constructor
            this.commandManager = commandManager;
        }

        /// <inheritdoc/>
        public string Name => "Dalamud.CorePlugin";

        /// <summary>
        /// Gets the plugin interface.
        /// </summary>
        internal DalamudPluginInterface Interface { get; private set; }

        /// <inheritdoc/>
        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            try
            {
                this.InitLoc();

                this.Interface = pluginInterface;

                this.windowSystem.AddWindow(new PluginWindow());

                this.Interface.UiBuilder.OnBuildUi += this.OnDraw;
                this.Interface.UiBuilder.OnOpenConfigUi += this.OnOpenConfigUi;

                this.commandManager.AddHandler("/di", new(this.OnCommand) { HelpMessage = $"Access the {this.Name} plugin." });
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "kaboom");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Interface.CommandManager.RemoveHandler("/di");

            this.Interface.UiBuilder.OnBuildUi -= this.OnDraw;

            this.windowSystem.RemoveAllWindows();

            this.Interface.Dispose();
        }

        private void InitLoc()
        {
            // CheapLoc needs to be reinitialized here because it tracks the setup by assembly name. New assembly, new setup.
            // this.localizationManager = new Localization(Path.Combine(Dalamud.Instance.AssetDirectory.FullName, "UIRes", "loc", "dalamud"), "dalamud_");
            // if (!string.IsNullOrEmpty(Dalamud.Instance.Configuration.LanguageOverride))
            // {
            //     this.localizationManager.SetupWithLangCode(Dalamud.Instance.Configuration.LanguageOverride);
            // }
            // else
            // {
            //     this.localizationManager.SetupWithUiCulture();
            // }
        }

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
            // this.window.IsOpen = true;
        }

        private void OnOpenConfigUi(object sender, EventArgs e)
        {
            // this.window.IsOpen = true;
        }
    }
}
