using System;
using System.Threading.Tasks;
using Dalamud.Logging.Internal;
using Dalamud.Support;
using Dalamud.Utility.Timing;

namespace Dalamud.Plugin.Internal;

/// <summary>
/// Class responsible for loading plugins on startup.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService]
public class StartupPluginLoader : IServiceType
{
    private static readonly ModuleLog Log = new("SPL");

    [ServiceManager.ServiceConstructor]
    private StartupPluginLoader(PluginManager pluginManager)
    {
        try
        {
            using (Timings.Start("PM Load Plugin Repos"))
            {
                _ = pluginManager.SetPluginReposFromConfigAsync(false);
                pluginManager.OnInstalledPluginsChanged += () => Task.Run(Troubleshooting.LogTroubleshooting);

                Log.Information("[T3] PM repos OK!");
            }

            using (Timings.Start("PM Cleanup Plugins"))
            {
                pluginManager.CleanupPlugins();
                Log.Information("[T3] PMC OK!");
            }

            using (Timings.Start("PM Load Sync Plugins"))
            {
                pluginManager.LoadAllPlugins();
                Log.Information("[T3] PML OK!");
            }

            Task.Run(Troubleshooting.LogTroubleshooting);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Plugin load failed");
        }
    }
}
