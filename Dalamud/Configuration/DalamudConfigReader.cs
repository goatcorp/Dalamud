using Dalamud.Configuration.Internal;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

namespace Dalamud.Configuration;

/// <summary>
/// An implementation of <see cref="IDalamudConfigReader"/>.
/// </summary>
[ServiceManager.ScopedService]
[PluginInterface]
#pragma warning disable SA1015
[ResolveVia<IDalamudConfigReader>]
#pragma warning restore SA1015
internal class DalamudConfigReader : IServiceType, IDalamudConfigReader
{
    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration dalamudConfiguration = Service<DalamudConfiguration>.Get();

    private readonly LocalPlugin plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudConfigReader"/> class.
    /// </summary>
    /// <param name="plugin">The LocalPlugin this config reader is for. Used for certain per-plugin settings managed
    /// by Dalamud.</param>
    internal DalamudConfigReader(LocalPlugin plugin)
    {
        this.plugin = plugin;
    }
    
    /// <inheritdoc/>
    public bool IsMbCollect => this.dalamudConfiguration.IsMbCollect;

    /// <inheritdoc/>
    public bool DisableRmtFiltering => this.dalamudConfiguration.DisableRmtFiltering;
    
    /// <inheritdoc/>
    public bool IsAntiAntiDebugEnabled => this.dalamudConfiguration.IsAntiAntiDebugEnabled;

    /// <inheritdoc />
    public bool EnablePluginUISoundEffects => this.dalamudConfiguration.EnablePluginUISoundEffects;

    /// <inheritdoc />
    public bool AutoUpdatePlugins => this.dalamudConfiguration.AutoUpdatePlugins;

    /// <inheritdoc/>
    public bool DoButtonsSystemMenu => this.dalamudConfiguration.DoButtonsSystemMenu;
}
