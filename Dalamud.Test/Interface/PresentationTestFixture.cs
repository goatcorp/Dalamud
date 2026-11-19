using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.ImGuiBackend;
using Dalamud.Interface.Internal;
using Dalamud.Interface.ManagedFontAtlas;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Test.Interface;

/// <summary>
/// Exercises presentation admission with a minimal swap chain and controlled backend failures.
/// </summary>
/// <remarks>
/// Replaces the cached display swap chain; callers must use the native framework render synchronization collection.
/// </remarks>
internal sealed class PresentationTestFixture : IDisposable
{
    public const int InitializationError = unchecked((int)0x887A0001);

    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly FieldInfo DisplaySwapChain = typeof(SwapChainHelper)
        .GetField("foundGameDisplaySwapChain", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly Func<nint> ReadDisplaySwapChain = CreateDisplayReader();
    private static readonly Action<nint> WriteDisplaySwapChain = CreateDisplayWriter();
    private readonly nint previousDisplaySwapChain;
    private readonly nint vtable;
    private readonly nint swapChain;
    private readonly nint foreignSwapChain;
    private readonly GetDescCallback getDesc;
    private readonly GetDeviceCallback getDevice;
    private readonly ReferenceCallback addRef;
    private readonly ReferenceCallback release;
    private readonly QueryInterfaceCallback queryInterface;
    private readonly Action<InterfaceManager, nint, uint> present;
    private int references = 1;
    private int descriptorCalls;
    private int constructorCalls;

    /// <summary>
    /// Creates a presentation target without starting game services or submitting graphics work.
    /// </summary>
    /// <param name="initialized">
    /// Whether to bypass backend construction. When enabled, unset configuration causes rendering to fail
    /// after queue dispatch and before native ImGui access; otherwise, construction fails at GetDevice.
    /// </param>
    public PresentationTestFixture(bool initialized = false)
    {
        // Supply only the manager state used by admission and queued presentation work.
        this.Manager = (InterfaceManager)RuntimeHelpers.GetUninitializedObject(typeof(InterfaceManager));
        GC.SuppressFinalize(this.Manager);
        this.SetField("renderDalamudLock", this.RenderLock);
        this.SetField("runBeforeImGuiRender", this.BeforeRender);
        this.SetField("dalamudAtlas", DispatchProxy.Create<IFontAtlas, ReadyAtlasProxy>());

        if (initialized)
        {
            var backend = (Dx11Win32Backend)RuntimeHelpers.GetUninitializedObject(typeof(Dx11Win32Backend));
            GC.SuppressFinalize(backend);
            this.SetField("backend", backend);
        }

        // Both chains share a minimal COM vtable; their window handles distinguish eligible targets.
        this.vtable = Marshal.AllocHGlobal(18 * nint.Size);
        for (var i = 0; i < 18; i++)
            Marshal.WriteIntPtr(this.vtable, i * nint.Size, 0);
        this.swapChain = Marshal.AllocHGlobal(nint.Size);
        Marshal.WriteIntPtr(this.swapChain, this.vtable);
        this.foreignSwapChain = Marshal.AllocHGlobal(nint.Size);
        Marshal.WriteIntPtr(this.foreignSwapChain, this.vtable);
        this.getDesc = (self, desc) =>
        {
            Interlocked.Increment(ref this.descriptorCalls);
            Marshal.StructureToPtr(new DXGI_SWAP_CHAIN_DESC { OutputWindow = (HWND)(self == this.foreignSwapChain ? 43 : 42) }, desc, false);
            return 0;
        };
        this.getDevice = (nint _, nint _, out nint device) =>
        {
            device = 0;
            Interlocked.Increment(ref this.constructorCalls);
            try
            {
                this.DuringConstruction?.Invoke();
            }
            catch (Exception exception)
            {
                // Capture assertions for rethrow after returning across the COM boundary.
                this.CallbackFailure = ExceptionDispatchInfo.Capture(exception);
            }

            return InitializationError;
        };
        this.addRef = _ => (uint)Interlocked.Increment(ref this.references);
        this.release = _ => (uint)Interlocked.Decrement(ref this.references);
        this.queryInterface = (nint _, nint _, out nint value) =>
        {
            value = 0;
            return unchecked((int)0x80004002);
        };
        this.SetSlot(0, this.queryInterface);
        this.SetSlot(1, this.addRef);
        this.SetSlot(2, this.release);
        this.SetSlot(7, this.getDevice);
        this.SetSlot(12, this.getDesc);
        this.previousDisplaySwapChain = ReadDisplaySwapChain();
        WriteDisplaySwapChain(this.swapChain);

        // Bridge native-sized handles to the private pointer-based presentation entry point.
        var method = new DynamicMethod("PresentTestSwapChain", typeof(void), [typeof(InterfaceManager), typeof(nint), typeof(uint)], typeof(PresentationTestFixture), true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Conv_U);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, typeof(InterfaceManager).GetMethod("RenderDalamudDraw", PrivateInstance)!);
        il.Emit(OpCodes.Ret);
        this.present = method.CreateDelegate<Action<InterfaceManager, nint, uint>>();
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDescCallback(nint self, nint description);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDeviceCallback(nint self, nint iid, out nint device);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ReferenceCallback(nint self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int QueryInterfaceCallback(nint self, nint iid, out nint value);

    public InterfaceManager Manager { get; }

    public Lock RenderLock { get; } = new();

    public ConcurrentQueue<Action> BeforeRender { get; } = new();

    public Action? DuringConstruction { get; set; }

    public ExceptionDispatchInfo? CallbackFailure { get; private set; }

    public int DescriptorCalls => Volatile.Read(ref this.descriptorCalls);

    public int ConstructorCalls => Volatile.Read(ref this.constructorCalls);

    public int ReferenceCount => Volatile.Read(ref this.references);

    public void Present(uint flags = 0) => this.present(this.Manager, this.swapChain, flags);

    public void PresentForeignTarget() => this.present(this.Manager, this.foreignSwapChain, 0);

    public void Dispose()
    {
        WriteDisplaySwapChain(this.previousDisplaySwapChain);
        Marshal.FreeHGlobal(this.swapChain);
        Marshal.FreeHGlobal(this.foreignSwapChain);
        Marshal.FreeHGlobal(this.vtable);
        // The unmanaged vtable does not root its callback delegates.
        GC.KeepAlive(this.getDesc);
        GC.KeepAlive(this.getDevice);
        GC.KeepAlive(this.addRef);
        GC.KeepAlive(this.release);
        GC.KeepAlive(this.queryInterface);
    }

    private static Func<nint> CreateDisplayReader()
    {
        var method = new DynamicMethod("ReadTestDisplaySwapChain", typeof(nint), Type.EmptyTypes, typeof(PresentationTestFixture), true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldsfld, DisplaySwapChain);
        il.Emit(OpCodes.Conv_I);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Func<nint>>();
    }

    private static Action<nint> CreateDisplayWriter()
    {
        var method = new DynamicMethod("WriteTestDisplaySwapChain", typeof(void), [typeof(nint)], typeof(PresentationTestFixture), true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Conv_U);
        il.Emit(OpCodes.Stsfld, DisplaySwapChain);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Action<nint>>();
    }

    private void SetField<T>(string name, T value) =>
        typeof(InterfaceManager).GetField(name, PrivateInstance)!.SetValue(this.Manager, value);

    private void SetSlot(int index, Delegate callback) =>
        Marshal.WriteIntPtr(this.vtable, index * nint.Size, Marshal.GetFunctionPointerForDelegate(callback));

    /// <summary>
    /// Supplies the atlas readiness queried before rendering.
    /// </summary>
    public class ReadyAtlasProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_HasBuiltAtlas" => true,
            "get_BuildTask" => Task.CompletedTask,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }
}
