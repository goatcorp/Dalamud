using System.Threading.Tasks;

using Dalamud.Plugin.Internal.Loader;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>Specify how to dispose <see cref="PluginLoader"/>.</summary>
internal enum PluginLoaderDisposalMode
{
    /// <summary>Do not dispose the plugin loader.</summary>
    None,
    
    /// <summary>Whether to wait a few before disposing the loader, just in case there are <see cref="Task{TResult}"/>s
    /// from the plugin that are still running.</summary>
    WaitBeforeDispose,

    /// <summary>Immediately dispose the plugin loader.</summary>
    ImmediateDispose,
}
