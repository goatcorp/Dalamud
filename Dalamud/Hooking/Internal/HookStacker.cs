using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// This class manages the stacking of hooks.
/// </summary>
/// <typeparam name="T">Delegate type to represent a function prototype. This must be the same prototype as original function.</typeparam>
internal class HookStacker<T> : IDalamudHook where T : Delegate
{
    private readonly object syncRoot = new();
    private Hook<T>? hookImpl;

    /// <summary>
    /// Initializes a new instance of the <see cref="HookStacker{T}"/> class.
    /// </summary>
    /// <param name="address">Address of this hook stacker.</param>
    public HookStacker(nint address)
    {
        this.Address = address;
        HookManager.HookStackTracker[address] = this;
        this.BackendDelegate = this.CreateBackendDelegate();
    }

    /// <inheritdoc/>
    public nint Address { get; }

    /// <inheritdoc/>
    public bool IsEnabled
    {
        get
        {
            lock (this.syncRoot)
            {
                return this.HookStack.Any(h => h.Hook.IsEnabled) ||
                    this.BeforeNotifyHookStack.Any(h => h.Hook.IsEnabled) ||
                    this.AfterNotifyHookStack.Any(h => h.Hook.IsEnabled);
            }
        }
    }

    /// <inheritdoc/>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public string BackendName => this.hookImpl.BackendName;

    /// <summary>
    /// Gets the first stacked delegate to be called by the underlying hook.
    /// </summary>
    internal T FirstStackedDelegate { get; private set; }

    /// <summary>
    /// Gets the backend delgate, which wraps the first stacked delegate so
    /// it can be dynamically updated without recreating the underlying hook.
    /// </summary>
    internal T BackendDelegate { get; }

    /// <summary>
    /// Gets or sets a linked list of stacked hooks in the priority range [1,254].
    /// The caller must hold the syncRoot hook stack lock when accessing this list.
    /// This list is always sorted in a descending order based on hook priority.
    /// </summary>
    private LinkedList<HookInfo> HookStack { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of before-notify hooks with priority 255.
    /// The caller must hold the syncRoot hook stack lock when accessing this list.
    /// </summary>
    private List<HookInfo> BeforeNotifyHookStack { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of after-notify hooks with priority 0.
    /// The caller must hold the syncRoot hook stack lock when accessing this list.
    /// </summary>
    private List<HookInfo> AfterNotifyHookStack { get; set; } = [];

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        if (this.IsDisposed)
            return;

        lock (this.syncRoot)
        {
            foreach (var hook in this.HookStack)
                hook.Hook.Dispose();

            foreach (var hook in this.BeforeNotifyHookStack)
                hook.Hook.Dispose();

            foreach (var hook in this.AfterNotifyHookStack)
                hook.Hook.Dispose();

            this.HookStack = [];
            this.BeforeNotifyHookStack = [];
            this.AfterNotifyHookStack = [];
            this.hookImpl?.Dispose();
            HookManager.HookStackTracker.TryRemove(this.Address, out _);
        }

        this.IsDisposed = true;
    }

    /// <summary>
    /// Sets the backend hook implementation.
    /// </summary>
    /// <param name="hookImpl">Backend hook all other hooks are stacked on.</param>
    internal void SetBackend(Hook<T> hookImpl)
    {
        this.hookImpl = hookImpl;
        this.FirstStackedDelegate = this.hookImpl.Original;
        this.hookImpl.Enable();
        this.hookImpl.Disable();
    }

    /// <summary>
    /// Rebuilds all delegates after a hook state change.
    /// </summary>
    internal void UpdateDelegates()
    {
        this.FirstStackedDelegate = this.CreateFirstStackedDelegate();

        foreach (var hook in this.HookStack)
        {
            ((HollowHook<T>)hook.Hook).SetOriginal(this.CreateOriginalDelegate(hook));
        }

        if (this.IsEnabled) this.hookImpl.Enable();
        else this.hookImpl.Disable();
    }

    /// <summary>
    /// Adds a hook to the hook stack.
    /// </summary>
    /// <param name="hook">The hook to be added.</param>
    public void Add(HookInfo hook)
    {
        lock (this.syncRoot)
        {
            switch (hook.Priority)
            {
                case byte.MaxValue:
                    this.BeforeNotifyHookStack.Add(hook);
                    break;
                case byte.MinValue:
                    this.AfterNotifyHookStack.Add(hook);
                    break;
                default:
                    this.HookStack.AddFirst(hook);
                    var sortedHooks = this.HookStack.OrderByDescending(h => h.Priority);
                    this.HookStack = new LinkedList<HookInfo>(sortedHooks);
                    break;
            }

            this.UpdateDelegates();
        }
    }

    /// <summary>
    /// Removes a hook from the hook stack.
    /// </summary>
    /// <param name="hook">The hook to be removed.</param>
    internal void Remove(IDalamudHook hook)
    {
        lock (this.syncRoot)
        {
            var node = this.HookStack.FirstOrDefault(h => ReferenceEquals(h.Hook, hook));
            this.HookStack.Remove(node);

            node = this.BeforeNotifyHookStack.FirstOrDefault(h => ReferenceEquals(h.Hook, hook));
            this.BeforeNotifyHookStack.Remove(node);

            node = this.AfterNotifyHookStack.FirstOrDefault(h => ReferenceEquals(h.Hook, hook));
            this.AfterNotifyHookStack.Remove(node);

            this.UpdateDelegates();

            if (this.HookStack.Count == 0 && this.BeforeNotifyHookStack.Count == 0 && this.AfterNotifyHookStack.Count == 0)
                this.Dispose();
        }
    }

    private T CreateBackendDelegate()
    {
        var method = typeof(T).GetMethod("Invoke");
        var parameters = method.GetParameters();

        var dynamicMethod = new DynamicMethod(
            "DalamudHook",
            method.ReturnType,
            Array.ConvertAll(parameters, p => p.ParameterType),
            typeof(HookStacker<T>));

        var ilGenerator = dynamicMethod.GetILGenerator();

        var hookStackTrackerGetter = typeof(HookManager).GetProperty("HookStackTracker", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).GetGetMethod(true);
        ilGenerator.Emit(OpCodes.Call, hookStackTrackerGetter);
        ilGenerator.Emit(OpCodes.Ldc_I8, this.Address);
        ilGenerator.Emit(OpCodes.Conv_I);

        var tryGetValueMethod = typeof(ConcurrentDictionary<nint, IDalamudHook>).GetMethod("TryGetValue");
        var localHookStacker = ilGenerator.DeclareLocal(typeof(IDalamudHook));

        ilGenerator.Emit(OpCodes.Ldloca_S, localHookStacker);
        ilGenerator.Emit(OpCodes.Callvirt, tryGetValueMethod);

        var getItemLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Brtrue_S, getItemLabel);

        // Case where element does not exist in the dictionary (which should never happen)
        ilGenerator.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes));
        ilGenerator.Emit(OpCodes.Throw);

        ilGenerator.MarkLabel(getItemLabel);
        ilGenerator.Emit(OpCodes.Ldloc_S, localHookStacker);
        ilGenerator.Emit(OpCodes.Castclass, typeof(HookStacker<T>));

        var firstStackedDelegateGetter = typeof(HookStacker<T>).GetProperty("FirstStackedDelegate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetGetMethod(true);
        ilGenerator.Emit(OpCodes.Callvirt, firstStackedDelegateGetter);

        for (var i = 0; i < parameters.Length; i++)
        {
            ilGenerator.Emit(OpCodes.Ldarg, i);
        }

        ilGenerator.Emit(OpCodes.Call, method);
        ilGenerator.Emit(OpCodes.Ret);

        return (T)dynamicMethod.CreateDelegate(typeof(T));
    }

    private T CreateFirstStackedDelegate()
    {
        lock (this.syncRoot)
        {
            var beforeNotify = (T)Delegate.Combine(this.BeforeNotifyHookStack.Where(h => h.Hook.IsEnabled).Select(h => h.Delegate).ToArray());
            var node = this.HookStack.First;

            while (node is not null && !node.Value.Hook.IsEnabled)
                node = node.Next;

            if (node is not null)
                return (T)Delegate.Combine(beforeNotify, node.Value.Delegate);

            var afterNotify = (T)Delegate.Combine(this.AfterNotifyHookStack.Where(h => h.Hook.IsEnabled).Select(h => h.Delegate).ToArray());

            return (T)Delegate.Combine(beforeNotify, afterNotify, this.hookImpl.Original);
        }
    }

    private T CreateOriginalDelegate(HookInfo callingHook)
    {
        lock (this.syncRoot)
        {
            var node = this.HookStack.Find(callingHook);
            node = node.Next;

            while (node is not null && !node.Value.Hook.IsEnabled)
                node = node.Next;

            if (node is not null)
                return (T)node.Value.Delegate;

            var afterNotify = (T)Delegate.Combine(this.AfterNotifyHookStack.Where(h => h.Hook.IsEnabled).Select(h => h.Delegate).ToArray());

            return (T)Delegate.Combine(afterNotify, this.hookImpl.Original);
        }
    }
}
