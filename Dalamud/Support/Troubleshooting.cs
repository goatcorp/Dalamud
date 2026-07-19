using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Utility;

using Newtonsoft.Json;

using Serilog;

namespace Dalamud.Support;

/// <summary>
/// Class responsible for printing troubleshooting information to the log.
/// </summary>
public static class Troubleshooting
{
    private static PendingTroubleshootingWrite? pendingTroubleshootingWrite;
    private static int isTroubleshootingWriteRunning;

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

        var fixedContext = context?.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        try
        {
            var payload = new ExceptionPayload
            {
                Context = fixedContext,
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
        var startInfo = Service<Dalamud>.Get().StartInfo;
        var configuration = Service<DalamudConfiguration>.Get();
        var interfaceManager = Service<InterfaceManager>.GetNullable();
        var pluginManager = Service<PluginManager>.GetNullable();

        try
        {
            var payload = new TroubleshootingPayload
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                LoadedPlugins = pluginManager?.InstalledPlugins?.Select(x => x.Manifest as LocalPluginManifest)?.OrderByDescending(x => x.InternalName).ToArray(),
                PluginStates = pluginManager?.InstalledPlugins?.Where(x => !x.IsDev).ToDictionary(x => x.Manifest.InternalName, x => x.IsBanned ? "Banned" : x.State.ToString()),
                EverStartedLoadingPlugins = pluginManager?.InstalledPlugins.Where(x => x.HasEverStartedLoad).Select(x => x.InternalName).ToList(),
                DalamudVersion = Versioning.GetScmVersion(),
                DalamudGitHash = Versioning.GetGitHash() ?? "Unknown",
                GameVersion = startInfo.GameVersion?.ToString() ?? "Unknown",
                Language = startInfo.Language.ToString(),
                BetaKey = Versioning.GetActiveTrack(),
                DoPluginTest = configuration.DoPluginTest,
                LoadAllApiLevels = false,
                InterfaceLoaded = interfaceManager?.IsReady ?? false,
                HasThirdRepo = configuration.ThirdRepoList is { Count: > 0 },
                ForcedMinHook = EnvironmentConfiguration.DalamudForceMinHook,
            };

            var encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
            Log.Information($"TROUBLESHOOTING:{encodedPayload}");

            // Queue the latest payload for async file writing; only one writer task runs at a time.
            Interlocked.Exchange(
                ref pendingTroubleshootingWrite,
                new PendingTroubleshootingWrite(
                    Path.Join(startInfo.LogPath, "dalamud.troubleshooting.json"),
                    JsonConvert.SerializeObject(payload, Formatting.Indented)));

            if (Interlocked.CompareExchange(ref isTroubleshootingWriteRunning, 1, 0) == 0)
            {
                _ = Task.Run(WriteTroubleshootingFileAsync);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not print troubleshooting.");
        }
    }

    private static async Task WriteTroubleshootingFileAsync()
    {
        while (true)
        {
            var write = Interlocked.Exchange(ref pendingTroubleshootingWrite, null);
            if (write is null)
                break;

            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await File.WriteAllTextAsync(write.Path, (string)write.Json, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not write troubleshooting file.");
            }
        }

        Interlocked.Exchange(ref isTroubleshootingWriteRunning, 0);

        if (Volatile.Read(ref pendingTroubleshootingWrite) is not null &&
            Interlocked.CompareExchange(ref isTroubleshootingWriteRunning, 1, 0) == 0)
        {
            await WriteTroubleshootingFileAsync();
        }
    }

    private class ExceptionPayload
    {
        public DateTime When { get; set; }

        public string Info { get; set; }

        public string? Context { get; set; }
    }

    private class TroubleshootingPayload
    {
        public long Timestamp { get; set; }

        public LocalPluginManifest[]? LoadedPlugins { get; set; }

        public Dictionary<string, string>? PluginStates { get; set; }

        public List<string>? EverStartedLoadingPlugins { get; set; }

        public string DalamudVersion { get; set; }

        public string DalamudGitHash { get; set; }

        public string GameVersion { get; set; }

        public string Language { get; set; }

        public bool DoDalamudTest => false;

        public string? BetaKey { get; set; }

        public bool DoPluginTest { get; set; }

        public bool LoadAllApiLevels { get; set; }

        public bool InterfaceLoaded { get; set; }

        public bool ForcedMinHook { get; set; }

        public List<ThirdPartyRepoSettings> ThirdRepo => [];

        public bool HasThirdRepo { get; set; }
    }

    private record PendingTroubleshootingWrite(string Path, string Json);
}
