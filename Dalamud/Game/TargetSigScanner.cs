using System.Diagnostics;
using System.IO;

using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game;

/// <summary>
/// A SigScanner facilitates searching for memory signatures in a given ProcessModule.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ProvidedService]
#pragma warning disable SA1015
[ResolveVia<ISigScanner>]
#pragma warning restore SA1015
internal class TargetSigScanner : SigScanner, IPublicDisposableService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TargetSigScanner"/> class.
    /// </summary>
    /// <param name="doCopy">Whether or not to copy the module upon initialization for search operations to use, as to not get disturbed by possible hooks.</param>
    /// <param name="cacheFile">File used to cached signatures.</param>
    public TargetSigScanner(bool doCopy = false, FileInfo? cacheFile = null)
        : base(Process.GetCurrentProcess().MainModule!, doCopy, cacheFile)
    {
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        if (this.IsService)
            this.DisposeCore();
    }

    /// <inheritdoc/>
    void IPublicDisposableService.MarkDisposeOnlyFromService() => this.IsService = true;
}
