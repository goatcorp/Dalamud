using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.IoC.Internal;
using Dalamud.Plugin;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Serilog;

namespace Dalamud.Interface.Internal.Scratchpad
{
    /// <summary>
    /// This class manages the execution of <see cref="ScratchpadDocument"/> classes.
    /// </summary>
    internal class ScratchExecutionManager
    {
        private Dictionary<Guid, IDalamudPlugin> loadedScratches = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScratchExecutionManager"/> class.
        /// </summary>
        public ScratchExecutionManager()
        {
        }

        /// <summary>
        /// Gets the ScratchPad macro processor.
        /// </summary>
        public ScratchMacroProcessor MacroProcessor { get; private set; } = new();

        /// <summary>
        /// Dispose of all currently loaded ScratchPads.
        /// </summary>
        public void DisposeAllScratches()
        {
            foreach (var dalamudPlugin in this.loadedScratches)
            {
                dalamudPlugin.Value.Dispose();
            }

            this.loadedScratches.Clear();
        }

        /// <summary>
        /// Renew a given ScratchPadDocument.
        /// </summary>
        /// <param name="doc">The document to renew.</param>
        /// <returns>The new load status.</returns>
        public ScratchLoadStatus RenewScratch(ScratchpadDocument doc)
        {
            var existingScratch = this.loadedScratches.FirstOrDefault(x => x.Key == doc.Id);
            if (existingScratch.Value != null)
            {
                existingScratch.Value.Dispose();
                this.loadedScratches[existingScratch.Key] = null;
            }

            var code = doc.IsMacro ? this.MacroProcessor.Process(doc.Content) : doc.Content;

            var options = ScriptOptions.Default
                // Dalamud
                .AddReferences(typeof(Dalamud).Assembly)
                // ImGui
                .AddReferences(typeof(ImGuiNET.ImGui).Assembly)
                // ImGuiScene
                .AddReferences(typeof(ImGuiScene.RawDX11Scene).Assembly)
                // FFXIVClientStructs
                .AddReferences(typeof(FFXIVClientStructs.Resolver).Assembly)
                // Lumina
                .AddReferences(typeof(Lumina.GameData).Assembly)
                // Lumina.Excel
                .AddReferences(typeof(Lumina.Excel.GeneratedSheets.TerritoryType).Assembly)
                .AddImports("System")
                .AddImports("System.IO")
                .AddImports("System.Reflection")
                .AddImports("System.Runtime.InteropServices")
                .AddImports("Dalamud")
                .AddImports("Dalamud.Data")
                .AddImports("Dalamud.Game")
                .AddImports("Dalamud.Game.ClientState")
                .AddImports("Dalamud.Game.ClientState.Buddy")
                .AddImports("Dalamud.Game.ClientState.Conditions")
                .AddImports("Dalamud.Game.ClientState.Fates")
                .AddImports("Dalamud.Game.ClientState.JobGauge")
                .AddImports("Dalamud.Game.ClientState.Keys")
                .AddImports("Dalamud.Game.ClientState.Objects")
                .AddImports("Dalamud.Game.ClientState.Party")
                .AddImports("Dalamud.Game.Command")
                .AddImports("Dalamud.Game.Gui")
                .AddImports("Dalamud.Game.Gui.FlyText")
                .AddImports("Dalamud.Game.Gui.PartyFinder")
                .AddImports("Dalamud.Game.Gui.Toast")
                .AddImports("Dalamud.Hooking")
                .AddImports("Dalamud.Game.Libc")
                .AddImports("Dalamud.Game.Network")
                .AddImports("Dalamud.Game.Text.SeStringHandling")
                .AddImports("Dalamud.Logging")
                .AddImports("Dalamud.Plugin")
                .AddImports("Dalamud.Utility")
                .AddImports("ImGuiNET");

            try
            {
                var script = CSharpScript.Create(code, options);

                var pluginType = script.ContinueWith<Type>("return typeof(ScratchPlugin);")
                    .RunAsync().GetAwaiter().GetResult().ReturnValue;

                var pi = new DalamudPluginInterface($"Scratch-{doc.Id}", PluginLoadReason.Unknown, false);

                var ioc = Service<ServiceContainer>.Get();
                var plugin = ioc.Create(pluginType, pi);

                if (plugin == null)
                    throw new Exception("Could not initialize scratch plugin");

                this.loadedScratches[doc.Id] = (IDalamudPlugin)plugin;
                return ScratchLoadStatus.Success;
            }
            catch (CompilationErrorException ex)
            {
                Log.Error(ex, "Compilation error occurred!\n" + string.Join(Environment.NewLine, ex.Diagnostics));
                return ScratchLoadStatus.FailureCompile;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Initialization error occured!\n");

                return ScratchLoadStatus.FailureInit;
            }
        }
    }
}
