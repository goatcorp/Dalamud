using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using Serilog;

using Encoding = System.Text.Encoding;

namespace Dalamud
{
    /// <summary>
    /// Class responsible for printing troubleshooting information to the log.
    /// </summary>
    internal static class Troubleshooting
    {
        /// <summary>
        /// Log troubleshooting information to Serilog.
        /// </summary>
        /// <param name="dalamud">The <see cref="Dalamud"/> instance to read information from.</param>
        /// <param name="isInterfaceLoaded">Whether or not the interface was loaded.</param>
        public static void LogTroubleshooting(Dalamud dalamud, bool isInterfaceLoaded)
        {
            try
            {
                var payload = new TroubleshootingPayload
                {
                    LoadedPlugins = dalamud.PluginManager.Plugins.Select(x => x.Definition).ToArray(),
                    DalamudVersion = Util.AssemblyVersion,
                    GameVersion = dalamud.StartInfo.GameVersion,
                    Language = dalamud.StartInfo.Language.ToString(),
                    DoDalamudTest = dalamud.Configuration.DoDalamudTest,
                    DoPluginTest = dalamud.Configuration.DoPluginTest,
                    InterfaceLoaded = isInterfaceLoaded,
                    ThirdRepo = dalamud.Configuration.ThirdRepoList,
                };

                var encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
                Log.Information($"TROUBLESHOOTING:{encodedPayload}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not print troubleshooting.");
            }
        }

        private class TroubleshootingPayload
        {
            public PluginDefinition[] LoadedPlugins { get; set; }

            public string DalamudVersion { get; set; }

            public string GameVersion { get; set; }

            public string Language { get; set; }

            public bool DoDalamudTest { get; set; }

            public bool DoPluginTest { get; set; }

            public bool InterfaceLoaded { get; set; }

            public List<ThirdRepoSetting> ThirdRepo { get; set; }
        }
    }
}
