using System.Diagnostics;
using System.IO;

using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game;

// TODO(v9): There are static functions here that we can't keep due to interfaces

/// <summary>
/// A SigScanner facilitates searching for memory signatures in a given ProcessModule.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
#pragma warning disable SA1015
[ResolveVia<ISigScanner>]
#pragma warning restore SA1015
internal class TargetSigScanner : IDisposable, IServiceType, ISigScanner
{
    private readonly SigScanner scanner;

    /// <summary>
    /// Initializes a new instance of the <see cref="TargetSigScanner"/> class.
    /// </summary>
    /// <param name="doCopy">Whether or not to copy the module upon initialization for search operations to use, as to not get disturbed by possible hooks.</param>
    /// <param name="cacheFile">File used to cached signatures.</param>
    public TargetSigScanner(bool doCopy = false, FileInfo? cacheFile = null)
    {
        this.scanner = new SigScanner(Process.GetCurrentProcess().MainModule!, doCopy, cacheFile);
    }

    /// <inheritdoc/>
    public bool IsCopy => this.scanner.IsCopy;
    
    /// <inheritdoc/>
    public bool Is32BitProcess => this.scanner.Is32BitProcess;
    
    /// <inheritdoc/>
    public IntPtr SearchBase => this.scanner.SearchBase;
    
    /// <inheritdoc/>
    public IntPtr TextSectionBase => this.scanner.TextSectionBase;
    
    /// <inheritdoc/>
    public long TextSectionOffset => this.scanner.TextSectionOffset;
    
    /// <inheritdoc/>
    public int TextSectionSize => this.scanner.TextSectionSize;
    
    /// <inheritdoc/>
    public IntPtr DataSectionBase => this.scanner.DataSectionBase;
    
    /// <inheritdoc/>
    public long DataSectionOffset => this.scanner.DataSectionOffset;
    
    /// <inheritdoc/>
    public int DataSectionSize => this.scanner.DataSectionSize;
    
    /// <inheritdoc/>
    public IntPtr RDataSectionBase => this.scanner.RDataSectionBase;
    
    /// <inheritdoc/>
    public long RDataSectionOffset => this.scanner.RDataSectionOffset;
    
    /// <inheritdoc/>
    public int RDataSectionSize => this.scanner.RDataSectionSize;
    
    /// <inheritdoc/>
    public ProcessModule Module => this.scanner.Module;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.scanner.Dispose();
    }

    /// <inheritdoc/>
    public IntPtr GetStaticAddressFromSig(string signature, int offset = 0) =>
        this.scanner.GetStaticAddressFromSig(signature, offset);

    /// <inheritdoc/>
    public bool TryGetStaticAddressFromSig(string signature, out IntPtr result, int offset = 0) =>
        this.scanner.TryGetStaticAddressFromSig(signature, out result, offset);

    /// <inheritdoc/>
    public IntPtr ScanData(string signature) =>
        this.scanner.ScanData(signature);

    /// <inheritdoc/>
    public bool TryScanData(string signature, out IntPtr result) =>
        this.scanner.TryScanData(signature, out result);

    /// <inheritdoc/>
    public IntPtr ScanModule(string signature) =>
        this.scanner.ScanModule(signature);

    /// <inheritdoc/>
    public bool TryScanModule(string signature, out IntPtr result) =>
        this.scanner.TryScanModule(signature, out result);

    /// <inheritdoc/>
    public IntPtr ResolveRelativeAddress(IntPtr nextInstAddr, int relOffset) =>
        this.scanner.ResolveRelativeAddress(nextInstAddr, relOffset);

    /// <inheritdoc/>
    public IntPtr ScanText(string signature) =>
        this.scanner.ScanText(signature);

    /// <inheritdoc/>
    public bool TryScanText(string signature, out IntPtr result) =>
        this.scanner.TryScanText(signature, out result);

    /// <inheritdoc cref="SigScanner.Save()"/>
    internal void Save() =>
        this.scanner.Save();
}
