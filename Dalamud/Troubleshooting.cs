using System;
using System.Linq;
using Dalamud.Plugin;
using Newtonsoft.Json;
using Serilog;
using Encoding = System.Text.Encoding;

namespace Dalamud
{
    internal static class Troubleshooting
    {
        private class TroubleshootingPayload {
            public PluginDefinition[] LoadedPlugins { get; set; }
            public string DalamudVersion { get; set; }
            public string GameVersion { get; set; }
            public string Language { get; set; }
            public bool DoDalamudTest { get; set; }
            public bool DoPluginTest { get; set; }
            public bool InterfaceLoaded { get; set; }
        }

        public static void LogTroubleshooting(Dalamud dalamud, bool isInterfaceLoaded) {
            try {
                var payload = new TroubleshootingPayload {
                    LoadedPlugins = dalamud.PluginManager.Plugins.Select(x => x.Definition).ToArray(),
                    DalamudVersion = Util.AssemblyVersion,
                    GameVersion = dalamud.StartInfo.GameVersion,
                    Language = dalamud.StartInfo.Language.ToString(),
                    DoDalamudTest = dalamud.Configuration.DoDalamudTest,
                    DoPluginTest = dalamud.Configuration.DoPluginTest,
                    InterfaceLoaded = isInterfaceLoaded
                };


                Log.Information("TROUBLESHOOTING:" +
                                System.Convert.ToBase64String(
                                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload))));
            } catch (Exception ex) {
                Log.Error(ex, "Could not print troubleshooting.");
            }
        }
    }
}
