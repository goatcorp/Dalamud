using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Class facilitating the casting of the delegates of an underlying hook.
/// </summary>
/// <typeparam name="T">Delegate of the desired hook.</typeparam>
/// <typeparam name="TBase">Delegate of the underlying hook.</typeparam>
internal class CastingHook<T, TBase> : Hook<T> where T : Delegate where TBase : Delegate
{
    private static readonly object SyncRoot = new();
    private static int instanceCount;
    private T originalFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="CastingHook{T, TBase}"/> class.
    /// </summary>
    /// <param name="address">A memory address to install a hook.</param>
    /// <param name="baseHook">The base hook this hook is casting its delegates to.</param>
    /// <param name="detour">The original detour.</param>
    public CastingHook(nint address, HollowHook<TBase> baseHook, T detour)
        : base(address)
    {
        this.BaseHook = baseHook;
        this.Detour = detour;

        lock (SyncRoot)
        {
            this.InstanceId = instanceCount++;
            SelfLookup[this.InstanceId] = this;
        }

        this.BaseHook.OriginalChanged += this.UpdateOriginal;
    }

    /// <inheritdoc/>
    public override T Original => this.originalFunction;

    /// <inheritdoc/>
    public override bool IsEnabled => this.BaseHook.IsEnabled;

    /// <inheritdoc/>
    public override string BackendName => this.BaseHook.BackendName;

    /// <summary>
    /// Gets a static dictionary that allows to retrieve the correct CastingHook in a static context.
    /// </summary>
    internal static ConcurrentDictionary<int, IDalamudHook> SelfLookup { get; } = [];

    /// <summary>
    /// Gets the base hook this hook is casting to.
    /// </summary>
    internal HollowHook<TBase> BaseHook { get; }

    /// <summary>
    /// Gets the original detour.
    /// </summary>
    internal T Detour { get; }

    private int InstanceId { get; }

    private bool Thunked { get; set; }

    /// <inheritdoc/>
    public override void Dispose() 
    {
        this.BaseHook.OriginalChanged -= this.UpdateOriginal;
        SelfLookup.TryRemove(this.InstanceId, out _);
        this.BaseHook.Dispose();
    }

    /// <inheritdoc/>
    public override void Enable()
    {
        this.BaseHook.Enable();
    }

    /// <inheritdoc/>
    public override void Disable()
    {
        this.BaseHook.Disable();
    }

    /// <summary>
    /// Attempts to cast a delegate as required by the base Hook type.
    /// </summary>
    /// <returns>Delegate of type TBase.</returns>
    public TBase GetBaseHookDetour()
    {
        try
        {
            return DirectCastDelegateTo<TBase>((MulticastDelegate)(Delegate)this.Detour);
        }
        catch (ArgumentException ex)
        {
            HookManager.Log.Warning(ex, $"Creating IL thunk to cast incomaptible hook signatures from {typeof(T).Name} to {typeof(TBase).Name}");
        }

        this.Thunked = true;
        this.originalFunction = this.CreateOriginalThunk();
        return this.CreateDetourThunk();
    }

    private static TTarget DirectCastDelegateTo<TTarget>(MulticastDelegate source)
    {
        return (TTarget)(object)source.GetInvocationList()
            .Select(sourceItem => Delegate.CreateDelegate(typeof(TTarget), sourceItem.Target, sourceItem.Method))
            .Aggregate<Delegate, Delegate>(null, Delegate.Combine);
    }

    private void UpdateOriginal()
    {
        // Thunked base hooks do not need updating of the original delegate.
        if (this.Thunked)
            return;

        this.originalFunction = DirectCastDelegateTo<T>((MulticastDelegate)(Delegate)this.BaseHook.Original);
    }

    private TBase CreateDetourThunk()
    {
        var method = typeof(TBase).GetMethod("Invoke");
        var parameters = method.GetParameters();

        var dynamicMethod = new DynamicMethod(
            "CastingHookDetourThunk",
            method.ReturnType,
            Array.ConvertAll(parameters, p => p.ParameterType),
            typeof(CastingHook<T, TBase>));

        var ilGenerator = dynamicMethod.GetILGenerator();

        var selfLookupGetter = typeof(CastingHook<T, TBase>).GetProperty("SelfLookup", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).GetGetMethod(true);
        ilGenerator.Emit(OpCodes.Call, selfLookupGetter);
        ilGenerator.Emit(OpCodes.Ldc_I4, this.InstanceId);

        var tryGetValueMethod = typeof(ConcurrentDictionary<int, IDalamudHook>).GetMethod("TryGetValue");
        var localHook = ilGenerator.DeclareLocal(typeof(IDalamudHook));

        ilGenerator.Emit(OpCodes.Ldloca_S, localHook);
        ilGenerator.Emit(OpCodes.Callvirt, tryGetValueMethod);

        var getItemLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Brtrue_S, getItemLabel);

        // Case where element does not exist in the dictionary (which should never happen)
        ilGenerator.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes));
        ilGenerator.Emit(OpCodes.Throw);

        ilGenerator.MarkLabel(getItemLabel);
        ilGenerator.Emit(OpCodes.Ldloc_S, localHook);
        ilGenerator.Emit(OpCodes.Castclass, typeof(CastingHook<T, TBase>));

        var detourGetter = typeof(CastingHook<T, TBase>).GetProperty("Detour", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetGetMethod(true);
        ilGenerator.Emit(OpCodes.Callvirt, detourGetter);

        for (var i = 0; i < parameters.Length; i++)
        {
            ilGenerator.Emit(OpCodes.Ldarg, i);
        }

        ilGenerator.Emit(OpCodes.Call, method);

        // Handle special cases for nint to pointer types and vice versa
        if (method.ReturnType.IsPointer)
            ilGenerator.Emit(OpCodes.Conv_U);

        if (method.ReturnType == typeof(nint))
            ilGenerator.Emit(OpCodes.Conv_I);

        ilGenerator.Emit(OpCodes.Ret);

        return (TBase)dynamicMethod.CreateDelegate(typeof(TBase));
    }

    private T CreateOriginalThunk()
    {
        var method = typeof(T).GetMethod("Invoke");
        var parameters = method.GetParameters();

        var dynamicMethod = new DynamicMethod(
            "CastingHookOriginalThunk",
            method.ReturnType,
            Array.ConvertAll(parameters, p => p.ParameterType),
            typeof(CastingHook<T, TBase>));

        var ilGenerator = dynamicMethod.GetILGenerator();

        var selfLookupGetter = typeof(CastingHook<T, TBase>).GetProperty("SelfLookup", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).GetGetMethod(true);
        ilGenerator.Emit(OpCodes.Call, selfLookupGetter);
        ilGenerator.Emit(OpCodes.Ldc_I4, this.InstanceId);

        var tryGetValueMethod = typeof(ConcurrentDictionary<int, IDalamudHook>).GetMethod("TryGetValue");
        var localHook = ilGenerator.DeclareLocal(typeof(IDalamudHook));

        ilGenerator.Emit(OpCodes.Ldloca_S, localHook);
        ilGenerator.Emit(OpCodes.Callvirt, tryGetValueMethod);

        var getItemLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Brtrue_S, getItemLabel);

        // Case where element does not exist in the dictionary (which should never happen)
        ilGenerator.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes));
        ilGenerator.Emit(OpCodes.Throw);

        ilGenerator.MarkLabel(getItemLabel);
        ilGenerator.Emit(OpCodes.Ldloc_S, localHook);
        ilGenerator.Emit(OpCodes.Castclass, typeof(CastingHook<T, TBase>));

        var baseHookGetter = typeof(CastingHook<T, TBase>).GetProperty("BaseHook", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetGetMethod(true);
        ilGenerator.Emit(OpCodes.Callvirt, baseHookGetter);

        var originalGetter = typeof(HollowHook<TBase>).GetProperty("Original", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(true);
        ilGenerator.Emit(OpCodes.Callvirt, originalGetter);

        for (var i = 0; i < parameters.Length; i++)
        {
            ilGenerator.Emit(OpCodes.Ldarg, i);
        }

        ilGenerator.Emit(OpCodes.Call, method);

        // Handle special cases for nint to pointer types and vice versa
        if (method.ReturnType.IsPointer)
            ilGenerator.Emit(OpCodes.Conv_U);

        if (method.ReturnType == typeof(nint))
            ilGenerator.Emit(OpCodes.Conv_I);

        ilGenerator.Emit(OpCodes.Ret);

        return (T)dynamicMethod.CreateDelegate(typeof(T));
    }
}
