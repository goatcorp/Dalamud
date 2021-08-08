using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dalamud.Configuration;
using Dalamud.Interface.Internal;
using Dalamud.Plugin;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;
using Newtonsoft.Json;
using Serilog;

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
        public static void LogTroubleshooting()
        {
            var dalamud = Service<Dalamud>.Get();
            var interfaceManager = Service<InterfaceManager>.Get();
            var startInfo = Service<DalamudStartInfo>.Get();
            var plguinManager = Service<PluginManager>.Get();
            
            try
            {
                var payload = new TroubleshootingPayload
                {
                    LoadedPlugins = Service<PluginManager>.Get().InstalledPlugins.Select(x => x.Manifest).ToArray(),
                    DalamudVersion = Util.AssemblyVersion,
                    DalamudGitHash = Util.GetGitHash(),
                    GameVersion = startInfo.GameVersion.ToString(),
                    Language = startInfo.Language.ToString(),
                    DoDalamudTest = dalamud.Configuration.DoDalamudTest,
                    DoPluginTest = dalamud.Configuration.DoPluginTest,
                    InterfaceLoaded = interfaceManager.IsReady,
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
            public PluginManifest[] LoadedPlugins { get; set; }

            public string DalamudVersion { get; set; }

            public string DalamudGitHash { get; set; }

            public string GameVersion { get; set; }

            public string Language { get; set; }

            public bool DoDalamudTest { get; set; }

            public bool DoPluginTest { get; set; }

            public bool InterfaceLoaded { get; set; }

            public List<ThirdPartyRepoSettings> ThirdRepo { get; set; }
        }
    }
}
