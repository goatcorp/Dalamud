using System.Collections.Generic;
using System.Linq;

using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Plugin.SelfTest.Internal;

/// <summary>
/// Registry for self-tests that can be run in the SelfTest window.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class SelfTestRegistry : IServiceType
{
    /// <summary>
    /// The name of the Dalamud test group.
    /// </summary>
    public const string DalamudTestGroup = "Dalamud";

    private static readonly ModuleLog Log = new("SelfTestRegistry");

    private List<SelfTestWithResults> dalamudSelfTests = new();
    private List<SelfTestWithResults> pluginSelfTests = new();
    private Dictionary<string, SelfTestGroup> allGroups = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SelfTestRegistry"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    public SelfTestRegistry()
    {
    }

    /// <summary>
    /// Gets all available self test groups.
    /// </summary>
    public IEnumerable<SelfTestGroup> SelfTestGroups
    {
        get
        {
            // Always return Dalamud group first, then plugin groups
            if (this.allGroups.TryGetValue(DalamudTestGroup, out var dalamudGroup))
            {
                yield return dalamudGroup;
            }

            foreach (var group in this.allGroups.Values)
            {
                if (group.Name != DalamudTestGroup)
                {
                    yield return group;
                }
            }
        }
    }

    /// <summary>
    /// Gets all self tests from all groups.
    /// </summary>
    public IEnumerable<SelfTestWithResults> SelfTests => this.dalamudSelfTests.Concat(this.pluginSelfTests);

    /// <summary>
    /// Registers Dalamud self test steps.
    /// </summary>
    /// <param name="steps">The steps to register.</param>
    public void RegisterDalamudSelfTestSteps(IEnumerable<ISelfTestStep> steps)
    {
        // Ensure Dalamud group exists and is loaded
        if (!this.allGroups.ContainsKey(DalamudTestGroup))
        {
            this.allGroups[DalamudTestGroup] = new SelfTestGroup(DalamudTestGroup, loaded: true);
        }
        else
        {
            this.allGroups[DalamudTestGroup].Loaded = true;
        }

        this.dalamudSelfTests.AddRange(steps.Select(step => SelfTestWithResults.FromDalamudStep(step)));
    }

    /// <summary>
    /// Registers plugin self test steps.
    /// </summary>
    /// <param name="plugin">The plugin registering the tests.</param>
    /// <param name="steps">The steps to register.</param>
    public void RegisterPluginSelfTestSteps(LocalPlugin plugin, IEnumerable<ISelfTestStep> steps)
    {
        // Ensure plugin group exists and is loaded
        if (!this.allGroups.ContainsKey(plugin.InternalName))
        {
            this.allGroups[plugin.InternalName] = new SelfTestGroup(plugin.InternalName, loaded: true);
        }
        else
        {
            this.allGroups[plugin.InternalName].Loaded = true;
        }

        this.pluginSelfTests.AddRange(steps.Select(step => SelfTestWithResults.FromPluginStep(plugin, step)));
    }

    /// <summary>
    /// Unregisters all self test steps for a plugin.
    /// </summary>
    /// <param name="plugin">The plugin to unregister tests for.</param>
    public void UnregisterPluginSelfTestSteps(LocalPlugin plugin)
    {
        // Clean up existing tests for this plugin
        this.pluginSelfTests.ForEach(test =>
        {
            if (test.Plugin == plugin)
            {
                test.Unload();
            }
        });

        this.pluginSelfTests.RemoveAll(test => test.Plugin == plugin);

        // Mark group as unloaded if it exists
        if (this.allGroups.ContainsKey(plugin.InternalName))
        {
            this.allGroups[plugin.InternalName].Loaded = false;
        }
    }
}
