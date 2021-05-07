using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Serilog;

namespace Dalamud.Interface.Scratchpad
{
    class ScratchExecutionManager
    {
        private readonly Dalamud dalamud;
        private Dictionary<Guid, IDalamudPlugin> loadedScratches = new Dictionary<Guid, IDalamudPlugin>();

        public ScratchMacroProcessor MacroProcessor { get; private set; } = new ScratchMacroProcessor();

        public ScratchExecutionManager(Dalamud dalamud)
        {
            this.dalamud = dalamud;
        }

        public void DisposeAllScratches()
        {
            foreach (var dalamudPlugin in this.loadedScratches)
            {
                dalamudPlugin.Value.Dispose();
            }

            this.loadedScratches.Clear();
        }

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
                                       .AddReferences(typeof(ImGui).Assembly)
                                       .AddReferences(typeof(Dalamud).Assembly)
                                       .AddReferences(typeof(FFXIVClientStructs.Attributes.Addon)
                                                          .Assembly)                    // FFXIVClientStructs
                                       .AddReferences(typeof(Lumina.GameData).Assembly) // Lumina
                                       .AddReferences(typeof(TerritoryType).Assembly) // Lumina.Excel
                                       //.WithReferences(MetadataReference.CreateFromFile(typeof(ScratchExecutionManager).Assembly.Location))
                                       .AddImports("System")
                                       .AddImports("System.IO")
                                       .AddImports("System.Reflection")
                                       .AddImports("System.Runtime.InteropServices")
                                       .AddImports("Dalamud")
                                       .AddImports("Dalamud.Plugin")
                                       .AddImports("Dalamud.Game.Command")
                                       .AddImports("Dalamud.Hooking")
                                       .AddImports("ImGuiNET");

            try
            {
                var script = CSharpScript.Create(code, options);

                var pi = new DalamudPluginInterface(this.dalamud, "Scratch-" + doc.Id, null, PluginLoadReason.Installer);
                var plugin = script.ContinueWith<IDalamudPlugin>("return new ScratchPlugin() as IDalamudPlugin;").RunAsync().GetAwaiter().GetResult()
                                   .ReturnValue;

                plugin.Initialize(pi);

                this.loadedScratches[doc.Id] = plugin;
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
