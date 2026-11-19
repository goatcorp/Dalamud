using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using Dalamud.Interface.ImGuiBackend;
using Dalamud.Interface.ImGuiBackend.InputHandler;

using TerraFX.Interop.DirectX;

using Xunit;

namespace Dalamud.Test.Interface;

[Collection("Native framework render synchronization")]
public class BackendConstructionTests
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private const int InitializationError = unchecked((int)0x887A0001);
    private static readonly Lazy<bool> NativeLibraries = new(() => PrepareNativeLibraries());
    private static readonly Action<Dx11Win32Backend, nint, bool> InitializeBackend = CreateConstructorAdapter();

    [Theory]
    [InlineData("GetDevice", 2, 0, 0, 0)]
    [InlineData("GetBuffer", 2, 1, 1, 0)]
    [InlineData("GetDesc", 2, 1, 1, 1)]
    public void FailedConstructionPreservesInitializationErrorAndReleasesAcquiredReferences(
        string failure,
        int swapChainReleases,
        int deviceReleases,
        int contextReleases,
        int bufferReleases)
    {
        using var fixture = new ConstructionFixture(failure);
        var backend = NewBackendWithoutFinalization();

        var exception = Assert.ThrowsAny<Exception>(() => InitializeBackend(backend, fixture.SwapChain.Address, false));

        Assert.Equal(InitializationError, exception.HResult);
        fixture.AssertReleased(swapChainReleases, deviceReleases, contextReleases, bufferReleases);

        // Repeated disposal must not release references already returned by constructor rollback.
        backend.Dispose();
        backend.Dispose();
        fixture.AssertReleased(swapChainReleases, deviceReleases, contextReleases, bufferReleases);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void RendererConstructionFailureReleasesBackendAndRestoresPreviousContexts(int failedDescCall)
    {
        _ = NativeLibraries.Value;
        using var fixture = new ConstructionFixture("RendererGetDesc", failedDescCall);
        var backend = NewBackendWithoutFinalization();
        var previousGui = ImGui.CreateContext();
        ImGui.SetCurrentContext(previousGui);
        ImPlot.SetImGuiContext(previousGui);
        var previousPlot = ImPlot.CreateContext();
        ImPlot.SetCurrentContext(previousPlot);
        try
        {
            var exception = Assert.ThrowsAny<Exception>(() => InitializeBackend(backend, fixture.SwapChain.Address, false));

            Assert.Equal(InitializationError, exception.HResult);
            Assert.Equal(previousGui, ImGui.GetCurrentContext());
            Assert.Equal(previousPlot, ImPlot.GetCurrentContext());

            // Main-viewport creation queries the descriptor after the renderer acquires its references.
            var rendererReferences = failedDescCall == 3 ? 2 : 1;
            fixture.AssertReleased(2, rendererReferences, rendererReferences, 1);
            backend.Dispose();
            fixture.AssertReleased(2, rendererReferences, rendererReferences, 1);
        }
        finally
        {
            ImPlot.DestroyContext(previousPlot);
            ImGui.DestroyContext(previousGui);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CleanupReleasesOwnedContextsAndPreservesUnrelatedCurrentContexts(bool createPlot)
    {
        _ = NativeLibraries.Value;
        var backend = NewBackendWithoutFinalization();
        var ownedGui = ImGui.CreateContext();
        ImGui.SetCurrentContext(ownedGui);
        ImPlot.SetImGuiContext(ownedGui);
        var ownedPlot = createPlot ? ImPlot.CreateContext() : default;
        SetField(backend, "imguiContext", ownedGui);
        SetField(backend, "implotContext", ownedPlot);

        var unrelatedGui = ImGui.CreateContext();
        ImGui.SetCurrentContext(unrelatedGui);
        ImPlot.SetImGuiContext(unrelatedGui);
        var unrelatedPlot = ImPlot.CreateContext();
        ImPlot.SetCurrentContext(unrelatedPlot);
        try
        {
            backend.Dispose();
            backend.Dispose();

            Assert.Equal(unrelatedGui, ImGui.GetCurrentContext());
            Assert.Equal(unrelatedPlot, ImPlot.GetCurrentContext());
            Assert.Equal(default(ImGuiContextPtr), GetField<ImGuiContextPtr>(backend, "imguiContext"));
            Assert.Equal(default(ImPlotContextPtr), GetField<ImPlotContextPtr>(backend, "implotContext"));
        }
        finally
        {
            ImPlot.DestroyContext(unrelatedPlot);
            ImGui.DestroyContext(unrelatedGui);
        }
    }

    [Fact]
    public void InputConstructionFailureRemovesCallbacksWithoutUnregisteringAnotherOwner()
    {
        _ = NativeLibraries.Value;
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        // Force registration to fail after the input handler installs its platform callbacks.
        using var existingClass = new RegisteredInputWindowClass();
        var input = (Win32InputHandler)RuntimeHelpers.GetUninitializedObject(typeof(Win32InputHandler));
        GC.SuppressFinalize(input);

        // Invoke the constructor on a retained instance so cleanup can be checked after it throws.
        var initialize = new DynamicMethod(
            "InitializeInputConstructionFixture",
            typeof(void),
            [typeof(Win32InputHandler)],
            typeof(BackendConstructionTests),
            true);
        var il = initialize.GetILGenerator();
        var constructor = Assert.Single(typeof(Win32InputHandler).GetConstructors());
        var window = il.DeclareLocal(constructor.GetParameters()[0].ParameterType);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldloc, window);
        il.Emit(OpCodes.Call, constructor);
        il.Emit(OpCodes.Ret);
        var construct = initialize.CreateDelegate<Action<Win32InputHandler>>();
        try
        {
            var exception = Assert.ThrowsAny<Exception>(() => construct(input));

            Assert.IsAssignableFrom<ExternalException>(exception);
            Assert.Equal(nint.Zero, ReadNativeField<ImGuiIO, ImGuiIOPtr>(ImGui.GetIO(), "BackendPlatformName"));
            foreach (var name in new[]
                     {
                         "PlatformCreateWindow", "PlatformDestroyWindow", "PlatformShowWindow", "PlatformSetWindowPos",
                         "PlatformGetWindowPos", "PlatformSetWindowSize", "PlatformGetWindowSize", "PlatformSetWindowFocus",
                         "PlatformGetWindowFocus", "PlatformGetWindowMinimized", "PlatformSetWindowTitle", "PlatformSetWindowAlpha",
                         "PlatformUpdateWindow",
                     })
                Assert.Equal(nint.Zero, ReadNativeField<ImGuiPlatformIO, ImGuiPlatformIOPtr>(ImGui.GetPlatformIO(), name));
            Assert.True(existingClass.IsRegistered());

            input.Dispose();
            Assert.True(existingClass.IsRegistered());
        }
        finally
        {
            ImGui.DestroyContext(context);
        }
    }

    private static Dx11Win32Backend NewBackendWithoutFinalization()
    {
        var backend = (Dx11Win32Backend)RuntimeHelpers.GetUninitializedObject(typeof(Dx11Win32Backend));
        // Keep native cleanup on the test thread while retaining failed instances for assertions.
        GC.SuppressFinalize(backend);
        return backend;
    }

    private static Action<Dx11Win32Backend, nint, bool> CreateConstructorAdapter()
    {
        var constructor = Assert.Single(typeof(Dx11Win32Backend).GetConstructors());
        var adapter = new DynamicMethod(
            "InitializeBackendConstructionFixture",
            typeof(void),
            [typeof(Dx11Win32Backend), typeof(nint), typeof(bool)],
            typeof(BackendConstructionTests),
            true);
        var il = adapter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        // Call preserves the fixture instance and runs the production constructor's field initializers.
        il.Emit(OpCodes.Call, constructor);
        il.Emit(OpCodes.Ret);
        return adapter.CreateDelegate<Action<Dx11Win32Backend, nint, bool>>();
    }

    private static void SetField<T>(Dx11Win32Backend backend, string name, T value) =>
        typeof(Dx11Win32Backend).GetField(name, PrivateInstance)!.SetValue(backend, value);

    private static T GetField<T>(Dx11Win32Backend backend, string name) =>
        (T)typeof(Dx11Win32Backend).GetField(name, PrivateInstance)!.GetValue(backend)!;

    private static nint ReadNativeField<TNative, TWrapper>(TWrapper wrapper, string name)
        where TNative : struct
        where TWrapper : struct =>
        Marshal.ReadIntPtr(Unsafe.As<TWrapper, nint>(ref wrapper), (int)Marshal.OffsetOf<TNative>(name));

    private static bool PrepareNativeLibraries([CallerFilePath] string sourceFile = "")
    {
        var configuration = typeof(BackendConstructionTests).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var nativeDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", "..", "bin", configuration));
        var bindingDirectory = Path.GetDirectoryName(typeof(ImGui).Assembly.Location)!;
        foreach (var name in new[] { "cimgui.dll", "cimplot.dll", "cimguizmo.dll" })
        {
            var destination = Path.Combine(bindingDirectory, name);
            if (!File.Exists(destination))
                File.Copy(Path.Combine(nativeDirectory, name), destination);
        }

        return true;
    }

    private sealed class ConstructionFixture : IDisposable
    {
        public ConstructionFixture(string failure, int failedDescCall = 2)
        {
            var descCalls = 0;
            this.Device.SetCallback(37, new GetFeatureLevelCallback(_ => (int)D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0));
            this.Device.SetCallback(40, new GetImmediateContextCallback((nint _, out nint context) => context = this.Context.Acquire()));
            this.SwapChain.SetCallback(7, new GetDeviceCallback((nint _, nint _, out nint device) =>
            {
                device = failure == "GetDevice" ? 0 : this.Device.Acquire();
                return failure == "GetDevice" ? InitializationError : 0;
            }));
            this.SwapChain.SetCallback(9, new GetBufferCallback((nint _, uint _, nint _, out nint buffer) =>
            {
                buffer = failure == "GetBuffer" ? 0 : this.Buffer.Acquire();
                return failure == "GetBuffer" ? InitializationError : 0;
            }));
            this.SwapChain.SetCallback(12, new GetDescCallback((_, desc) =>
            {
                descCalls++;
                if (failure == "GetDesc" || (failure == "RendererGetDesc" && descCalls == failedDescCall))
                    return InitializationError;

                Marshal.StructureToPtr(default(DXGI_SWAP_CHAIN_DESC), desc, false);
                return 0;
            }));
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetDeviceCallback(nint self, nint iid, out nint device);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void GetImmediateContextCallback(nint self, out nint context);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetFeatureLevelCallback(nint self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetBufferCallback(nint self, uint index, nint iid, out nint buffer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetDescCallback(nint self, nint desc);

        public NativeComObject SwapChain { get; } = new(18);

        private NativeComObject Device { get; } = new(43);

        private NativeComObject Context { get; } = new(3);

        private NativeComObject Buffer { get; } = new(3);

        public void AssertReleased(int swapChain, int device, int context, int buffer)
        {
            // The fixture retains the initial reference to each controlled COM object.
            foreach (var (obj, releases) in new[] { (this.SwapChain, swapChain), (this.Device, device), (this.Context, context), (this.Buffer, buffer) })
            {
                Assert.Equal(1, obj.ReferenceCount);
                Assert.Equal(releases, obj.ReleaseCalls);
            }
        }

        public void Dispose()
        {
            this.SwapChain.Dispose();
            this.Device.Dispose();
            this.Context.Dispose();
            this.Buffer.Dispose();
        }
    }

    private sealed class NativeComObject : IDisposable
    {
        private readonly nint vtable;

        // Keep managed callbacks alive until their unmanaged vtable entries are freed.
        private readonly List<Delegate> callbacks = [];

        public NativeComObject(int slots)
        {
            this.vtable = Marshal.AllocHGlobal(slots * nint.Size);
            for (var i = 0; i < slots; i++)
                Marshal.WriteIntPtr(this.vtable, i * nint.Size, 0);
            this.Address = Marshal.AllocHGlobal(nint.Size);
            Marshal.WriteIntPtr(this.Address, this.vtable);
            this.SetCallback(0, new QueryInterfaceCallback((nint _, nint _, out nint value) =>
            {
                value = 0;
                return unchecked((int)0x80004002);
            }));
            this.SetCallback(1, new ReferenceCallback(_ => (uint)++this.ReferenceCount));
            this.SetCallback(2, new ReferenceCallback(_ =>
            {
                this.ReleaseCalls++;
                return (uint)--this.ReferenceCount;
            }));
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int QueryInterfaceCallback(nint self, nint iid, out nint value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint ReferenceCallback(nint self);

        public nint Address { get; }

        public int ReferenceCount { get; private set; } = 1;

        public int ReleaseCalls { get; private set; }

        public nint Acquire()
        {
            this.ReferenceCount++;
            return this.Address;
        }

        public void SetCallback(int slot, Delegate callback)
        {
            this.callbacks.Add(callback);
            Marshal.WriteIntPtr(this.vtable, slot * nint.Size, Marshal.GetFunctionPointerForDelegate(callback));
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(this.Address);
            Marshal.FreeHGlobal(this.vtable);
            GC.KeepAlive(this.callbacks);
        }
    }

    private sealed class RegisteredInputWindowClass : IDisposable
    {
        private readonly string name = typeof(Win32InputHandler).GetNestedType("ViewportHandler", BindingFlags.NonPublic)!.FullName!;
        private readonly nint instance = Marshal.GetHINSTANCE(typeof(Win32InputHandler).Module);
        private readonly nint namePointer;

        public RegisteredInputWindowClass()
        {
            this.namePointer = Marshal.StringToHGlobalUni(this.name);
            var value = this.CreateDescription();
            var user32 = NativeLibrary.Load("user32.dll");
            try
            {
                value.WindowProcedure = NativeLibrary.GetExport(user32, "DefWindowProcW");
                Assert.NotEqual((ushort)0, RegisterClassExW(ref value));
            }
            finally
            {
                NativeLibrary.Free(user32);
            }
        }

        public bool IsRegistered()
        {
            var value = this.CreateDescription();
            return GetClassInfoExW(this.instance, this.name, ref value);
        }

        public void Dispose()
        {
            Assert.True(UnregisterClassW(this.name, this.instance));
            Marshal.FreeHGlobal(this.namePointer);
        }

        private WindowClass CreateDescription() => new()
        {
            Size = (uint)Marshal.SizeOf<WindowClass>(),
            Instance = this.instance,
            ClassName = this.namePointer,
        };

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern ushort RegisterClassExW(ref WindowClass value);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClassInfoExW(nint instance, string name, ref WindowClass value);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterClassW(string name, nint instance);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WindowClass
        {
            public uint Size;
            public uint Style;
            public nint WindowProcedure;
            public int ClassExtra;
            public int WindowExtra;
            public nint Instance;
            public nint Icon;
            public nint Cursor;
            public nint Background;
            public nint MenuName;
            public nint ClassName;
            public nint SmallIcon;
        }
    }
}
