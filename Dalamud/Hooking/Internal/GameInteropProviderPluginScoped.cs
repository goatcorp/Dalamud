using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Serilog;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Plugin-scoped version of service used to create hooks.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IGameInteropProvider>]
#pragma warning restore SA1015
internal class GameInteropProviderPluginScoped : IGameInteropProvider, IServiceType, IDisposable
{
    private readonly LocalPlugin plugin;
    private readonly SigScanner scanner;

    private readonly ConcurrentBag<IDalamudHook> trackedHooks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GameInteropProviderPluginScoped"/> class.
    /// </summary>
    /// <param name="plugin">Plugin this instance belongs to.</param>
    /// <param name="scanner">SigScanner instance for target module.</param>
    public GameInteropProviderPluginScoped(LocalPlugin plugin, SigScanner scanner)
    {
        this.plugin = plugin;
        this.scanner = scanner;
    }

    /// <inheritdoc/>
    public void InitializeFromAttributes(object self)
    {
        foreach (var hook in SignatureHelper.Initialize(self))
            this.trackedHooks.Add(hook);
    }

    /// <inheritdoc/>
    public Hook<T> FromFunctionPointerVariable<T>(IntPtr address, T detour) where T : Delegate
    {
        var hook = Hook<T>.FromFunctionPointerVariable(address, detour);
        this.trackedHooks.Add(hook);
        return hook;
    }

    /// <inheritdoc/>
    public Hook<T> FromImport<T>(ProcessModule? module, string moduleName, string functionName, uint hintOrOrdinal, T detour) where T : Delegate
    {
        var hook = Hook<T>.FromImport(module, moduleName, functionName, hintOrOrdinal, detour);
        this.trackedHooks.Add(hook);
        return hook;
    }

    /// <inheritdoc/>
    public Hook<T> FromSymbol<T>(string moduleName, string exportName, T detour, IGameInteropProvider.HookBackend backend = IGameInteropProvider.HookBackend.Automatic) where T : Delegate
    {
        var hook = Hook<T>.FromSymbol(moduleName, exportName, detour, backend == IGameInteropProvider.HookBackend.MinHook);
        this.trackedHooks.Add(hook);
        return hook;
    }

    /// <inheritdoc/>
    public Hook<T> FromAddress<T>(IntPtr procAddress, T detour, IGameInteropProvider.HookBackend backend = IGameInteropProvider.HookBackend.Automatic) where T : Delegate
    {
        var hook = Hook<T>.FromAddress(procAddress, detour, backend == IGameInteropProvider.HookBackend.MinHook);
        this.trackedHooks.Add(hook);
        return hook;
    }

    /// <inheritdoc/>
    public Hook<T> FromSignature<T>(string signature, T detour, IGameInteropProvider.HookBackend backend = IGameInteropProvider.HookBackend.Automatic) where T : Delegate
        => this.FromAddress(this.scanner.ScanText(signature), detour, backend);

    /// <inheritdoc/>
    public void Dispose()
    {
        var notDisposed = this.trackedHooks.Where(x => !x.IsDisposed).ToArray();
        if (notDisposed.Length != 0)
            Log.Warning("{PluginName} is leaking {Num} hooks! Make sure that all of them are disposed properly.", this.plugin.InternalName, notDisposed.Length);

        foreach (var hook in notDisposed)
        {
            hook.Dispose();
        }
        
        this.trackedHooks.Clear();
    }
}
