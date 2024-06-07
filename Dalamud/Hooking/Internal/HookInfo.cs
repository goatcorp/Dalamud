using System.Diagnostics;
using System.Reflection;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Class containing information about registered hooks.
/// </summary>
internal class HookInfo
{
    private ulong? inProcessMemory = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="HookInfo"/> class.
    /// </summary>
    /// <param name="hook">The tracked hook.</param>
    /// <param name="hookDelegate">The hook delegate.</param>
    /// <param name="assembly">The assembly implementing the hook.</param>
    public HookInfo(IDalamudHook hook, Delegate hookDelegate, Assembly assembly)
    {
        this.Hook = hook;
        this.Delegate = hookDelegate;
        this.Assembly = assembly;
    }

    /// <summary>
    /// Gets the RVA of the hook.
    /// </summary>
    internal ulong? InProcessMemory
    {
        get
        {
            if (this.Hook.IsDisposed)
                return 0;

            if (this.inProcessMemory == null)
                return null;

            if (this.inProcessMemory.Value > 0)
                return this.inProcessMemory.Value;

            var p = Process.GetCurrentProcess().MainModule;
            var begin = (ulong)p.BaseAddress.ToInt64();
            var end = begin + (ulong)p.ModuleMemorySize;
            var hookAddr = (ulong)this.Hook.Address.ToInt64();

            if (hookAddr >= begin && hookAddr <= end)
            {
                return this.inProcessMemory = hookAddr - begin;
            }
            else
            {
                return this.inProcessMemory = null;
            }
        }
    }

    /// <summary>
    /// Gets the tracked hook.
    /// </summary>
    internal IDalamudHook Hook { get; }

    /// <summary>
    /// Gets the tracked delegate.
    /// </summary>
    internal Delegate Delegate { get; }

    /// <summary>
    /// Gets the assembly implementing the hook.
    /// </summary>
    internal Assembly Assembly { get; }
}
