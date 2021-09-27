using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Support
{
    /// <summary>
    /// Class responsible for printing troubleshooting information to the log.
    /// </summary>
    public static class Troubleshooting
    {
        /// <summary>
        /// Gets the most recent exception to occur.
        /// </summary>
        public static Exception? LastException { get; private set; }

        /// <summary>
        /// Log the last exception in a parseable format to serilog.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="context">Additional context.</param>
        public static void LogException(Exception exception, string context)
        {
            LastException = exception;

            try
            {
                var payload = new ExceptionPayload
                {
                    Context = context,
                    When = DateTime.Now,
                    Info = exception.ToString(),
                };

                var encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
                Log.Information($"LASTEXCEPTION:{encodedPayload}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not print exception.");
            }
        }

        /// <summary>
        /// Log troubleshooting information in a parseable format to Serilog.
        /// </summary>
        internal static void LogTroubleshooting()
        {
            var startInfo = Service<DalamudStartInfo>.Get();
            var configuration = Service<DalamudConfiguration>.Get();
            var interfaceManager = Service<InterfaceManager>.GetNullable();
            var pluginManager = Service<PluginManager>.GetNullable();

            try
            {
                var payload = new TroubleshootingPayload
                {
                    LoadedPlugins = pluginManager?.InstalledPlugins?.Select(x => x.Manifest)?.ToArray(),
                    DalamudVersion = Util.AssemblyVersion,
                    DalamudGitHash = Util.GetGitHash(),
                    GameVersion = startInfo.GameVersion.ToString(),
                    Language = startInfo.Language.ToString(),
                    DoDalamudTest = configuration.DoDalamudTest,
                    DoPluginTest = configuration.DoPluginTest,
                    InterfaceLoaded = interfaceManager?.IsReady ?? false,
                    ThirdRepo = configuration.ThirdRepoList,
                };

                var encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
                Log.Information($"TROUBLESHOOTING:{encodedPayload}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not print troubleshooting.");
            }
        }

        private class ExceptionPayload
        {
            public DateTime When { get; set; }

            public string Info { get; set; }

            public string Context { get; set; }
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
