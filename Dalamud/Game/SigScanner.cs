using System.Diagnostics;
using System.IO;

using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game;

/// <summary>
/// A SigScanner service specifically targeting the FFXIV game process.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
#pragma warning disable SA1015
[ResolveVia<ISigScanner>]
#pragma warning restore SA1015
internal class SigScanner : GenericSigScanner, IServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SigScanner"/> class using the main module of the current process.
    /// </summary>
    /// <param name="doCopy">Whether or not to copy the module upon initialization for search operations to use, as to not get disturbed by possible hooks.</param>
    /// <param name="cacheFile">File used to cached signatures.</param>
    internal SigScanner(bool doCopy = false, FileInfo? cacheFile = null)
        : base(Process.GetCurrentProcess().MainModule!, doCopy, cacheFile)
    {
    }
}
