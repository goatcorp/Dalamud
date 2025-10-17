using System.Collections.Generic;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Plugin.SelfTest.Internal;

/// <summary>
/// Plugin-scoped version of SelfTestRegistry.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
[ResolveVia<ISelfTestRegistry>]
internal class SelfTestRegistryPluginScoped : ISelfTestRegistry, IInternalDisposableService
{
    [ServiceManager.ServiceDependency]
    private readonly SelfTestRegistry selfTestRegistry = Service<SelfTestRegistry>.Get();

    private readonly LocalPlugin plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelfTestRegistryPluginScoped"/> class.
    /// </summary>
    /// <param name="plugin">The plugin this service belongs to.</param>
    [ServiceManager.ServiceConstructor]
    public SelfTestRegistryPluginScoped(LocalPlugin plugin)
    {
        this.plugin = plugin;
    }

    /// <summary>
    /// Gets the plugin name.
    /// </summary>
    public string PluginName { get; private set; }

    /// <summary>
    /// Registers test steps for this plugin.
    /// </summary>
    /// <param name="steps">The test steps to register.</param>
    public void RegisterTestSteps(IEnumerable<ISelfTestStep> steps)
    {
        this.selfTestRegistry.RegisterPluginSelfTestSteps(this.plugin, steps);
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.selfTestRegistry.UnregisterPluginSelfTestSteps(this.plugin);
    }
}
