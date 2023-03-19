using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Fools.Plugins;
using Dalamud.Interface;
using Dalamud.Logging.Internal;

namespace Dalamud.Fools;

/// <summary>
/// Manager for all the IFoolsPlugin instances.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService]
internal class FoolsManager : IDisposable, IServiceType
{
    public readonly List<FoolsPluginMetadata> FoolsPlugins = new();
    public readonly Dictionary<string, IFoolsPlugin> ActivatedPlugins = new();

    private static readonly ModuleLog Log = new("FOOLS");

    private UiBuilder uiBuilder;

    [ServiceManager.ServiceConstructor]
    private FoolsManager()
    {
        this.uiBuilder = new UiBuilder("fools");
        this.uiBuilder.Draw += this.DrawUI;

        // reflect over all IFoolsPlugin implementations sometime(?)
        this.FoolsPlugins = new List<FoolsPluginMetadata>
        {
            new("Pixel Imperfect", "PixelImperfectPlugin", "Whoops... we messed up the math on that one.", "Halpo",
                typeof(PixelImperfectPlugin)),
            new("DailyLifeDuty", "DailyLifeDutyPlugin", "Easily Track Daily and Weekly tasks... in real life", "MidoriKami", typeof(DailyLifeDutyPlugin)),
        };
    }

    public void ActivatePlugin(string plugin)
    {
        if (this.ActivatedPlugins.ContainsKey(plugin))
        {
            Log.Warning("Trying to activate plugin {0} that is already activated", plugin);
            return;
        }

        var pluginMetadata = this.FoolsPlugins.FirstOrDefault(x => x.InternalName == plugin);
        if (pluginMetadata == null)
        {
            Log.Warning("Trying to activate plugin {0} that does not exist", plugin);
            return;
        }

        var pluginInstance = (IFoolsPlugin)Activator.CreateInstance(pluginMetadata.Type);
        this.ActivatedPlugins.Add(plugin, pluginInstance);
    }

    public void Dispose()
    {
        foreach (var plugin in this.ActivatedPlugins.Values)
        {
            plugin.Dispose();
        }

        this.ActivatedPlugins.Clear();

        ((IDisposable)this.uiBuilder).Dispose();
    }

    public bool IsPluginActivated(string plugin)
    {
        return this.ActivatedPlugins.ContainsKey(plugin);
    }

    public void DeactivatePlugin(string plugin)
    {
        if (!this.ActivatedPlugins.ContainsKey(plugin))
        {
            Log.Warning("Trying to deactivate plugin {0} that is not activated", plugin);
            return;
        }

        var pluginInstance = this.ActivatedPlugins[plugin];
        pluginInstance.Dispose();
        this.ActivatedPlugins.Remove(plugin);
    }

    private void DrawUI()
    {
        foreach (var plugin in this.ActivatedPlugins.Values)
        {
            plugin.DrawUi();
        }
    }
}
