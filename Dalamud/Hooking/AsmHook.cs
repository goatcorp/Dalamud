using System.Reflection;
using System.Reflection.Emit;

using Dalamud.Hooking.Internal;

using Reloaded.Hooks;

namespace Dalamud.Hooking;

/// <summary>
/// Manages a hook which can be used to intercept a call to native function.
/// This class is basically a thin wrapper around the LocalHook type to provide helper functions.
/// </summary>
public sealed class AsmHook : IDisposable, IDalamudHook
{
    private readonly IntPtr address;
    private readonly Reloaded.Hooks.Definitions.IAsmHook hookImpl;

    private bool isActivated = false;
    private bool isEnabled = false;

    private DynamicMethod statsMethod;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsmHook"/> class.
    /// This is an assembly hook and should not be used for except under unique circumstances.
    /// Hook is not activated until Enable() method is called.
    /// </summary>
    /// <param name="address">A memory address to install a hook.</param>
    /// <param name="assembly">Assembly code representing your hook.</param>
    /// <param name="name">The name of what you are hooking, since a delegate is not required.</param>
    /// <param name="asmHookBehaviour">How the hook is inserted into the execution flow.</param>
    public AsmHook(IntPtr address, byte[] assembly, string name, AsmHookBehaviour asmHookBehaviour = AsmHookBehaviour.ExecuteFirst)
    {
        address = HookManager.FollowJmp(address);

        // We cannot call TrimAfterHook here because the hook is activated by the caller.
        HookManager.RegisterUnhooker(address);

        this.address = address;
        this.hookImpl = ReloadedHooks.Instance.CreateAsmHook(assembly, address.ToInt64(), (Reloaded.Hooks.Definitions.Enums.AsmHookBehaviour)asmHookBehaviour);

        this.statsMethod = new DynamicMethod(name, null, null);
        this.statsMethod.GetILGenerator().Emit(OpCodes.Ret);
        var dele = this.statsMethod.CreateDelegate(typeof(Action));

        HookManager.TrackedHooks.TryAdd(Guid.NewGuid(), new HookInfo(this, dele, Assembly.GetCallingAssembly()));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsmHook"/> class.
    /// This is an assembly hook and should not be used for except under unique circumstances.
    /// Hook is not activated until Enable() method is called.
    /// </summary>
    /// <param name="address">A memory address to install a hook.</param>
    /// <param name="assembly">FASM syntax assembly code representing your hook. The first line should be use64.</param>
    /// <param name="name">The name of what you are hooking, since a delegate is not required.</param>
    /// <param name="asmHookBehaviour">How the hook is inserted into the execution flow.</param>
    public AsmHook(IntPtr address, string[] assembly, string name, AsmHookBehaviour asmHookBehaviour = AsmHookBehaviour.ExecuteFirst)
    {
        address = HookManager.FollowJmp(address);

        // We cannot call TrimAfterHook here because the hook is activated by the caller.
        HookManager.RegisterUnhooker(address);

        this.address = address;
        this.hookImpl = ReloadedHooks.Instance.CreateAsmHook(assembly, address.ToInt64(), (Reloaded.Hooks.Definitions.Enums.AsmHookBehaviour)asmHookBehaviour);

        this.statsMethod = new DynamicMethod(name, null, null);
        this.statsMethod.GetILGenerator().Emit(OpCodes.Ret);
        var dele = this.statsMethod.CreateDelegate(typeof(Action));

        HookManager.TrackedHooks.TryAdd(Guid.NewGuid(), new HookInfo(this, dele, Assembly.GetCallingAssembly()));
    }

    /// <summary>
    /// Gets a memory address of the target function.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
    public IntPtr Address
    {
        get
        {
            this.CheckDisposed();
            return this.address;
        }
    }

    /// <summary>
    /// Gets a value indicating whether or not the hook is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get
        {
            this.CheckDisposed();
            return this.isEnabled;
        }
    }

    /// <summary>
    /// Gets a value indicating whether or not the hook has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public string BackendName => "Reloaded/Asm";

    /// <summary>
    /// Remove a hook from the current process.
    /// </summary>
    public void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.IsDisposed = true;

        if (this.isEnabled)
        {
            this.isEnabled = false;
            this.hookImpl.Disable();
        }
    }

    /// <summary>
    /// Starts intercepting a call to the function.
    /// </summary>
    public void Enable()
    {
        this.CheckDisposed();

        if (!this.isActivated)
        {
            this.isActivated = true;
            this.hookImpl.Activate();
        }

        if (!this.isEnabled)
        {
            this.isEnabled = true;
            this.hookImpl.Enable();
        }
    }

    /// <summary>
    /// Stops intercepting a call to the function.
    /// </summary>
    public void Disable()
    {
        this.CheckDisposed();

        if (!this.isEnabled)
            return;

        if (this.isEnabled)
        {
            this.isEnabled = false;
            this.hookImpl.Disable();
        }
    }

    /// <summary>
    /// Check if this object has been disposed already.
    /// </summary>
    private void CheckDisposed()
    {
        if (this.IsDisposed)
        {
            throw new ObjectDisposedException(message: "Hook is already disposed", null);
        }
    }
}
