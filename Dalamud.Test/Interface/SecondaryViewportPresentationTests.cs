using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Interface.ImGuiBackend;
using Dalamud.Interface.ImGuiBackend.InputHandler;
using Dalamud.Interface.ImGuiBackend.Renderers;
using Dalamud.Interface.Internal;
using Dalamud.IoC.Internal;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using Xunit;

namespace Dalamud.Test.Interface;

/// <summary>
/// Verifies window-thread secondary presentation and main-viewport worker replay.
/// </summary>
[Collection("Native framework render synchronization")]
public class SecondaryViewportPresentationTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);
    private static string stage = string.Empty;

    [Fact]
    public void SecondaryPresentProcessesSynchronousWindowMessageBeforeWorkerReplay()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        RunOnOwner(() =>
        {
            using var fixture = new BackendFixture();
            using (ThreadSafety.NativeFrameworkRenderSyncRoot.EnterScope())
            {
                // Initialize the secondary HWND before replacing its Present callback.
                stage = "first Step";
                fixture.Backend.Step();
                stage = "second Step";
                fixture.Backend.Step();
                stage = "installing secondary Present proxy";
                Assert.Equal(2, ImGui.GetPlatformIO().Viewports.Size);
                using var presentation = new SynchronousPresent(ImGui.GetPlatformIO().Viewports[1]);

                using var pipelineState = fixture.BindPipelineState();
                stage = "controlled Step";
                fixture.Backend.Step();
                presentation.Failure?.Throw();
                Assert.Equal(1, presentation.PresentCalls);
                Assert.Equal(1, presentation.MessageCalls);
                Assert.True(presentation.SendSucceeded);
                Assert.Equal(Environment.CurrentManagedThreadId, presentation.PresentThread);
                pipelineState.AssertRestored();

                // Keep the MTA owner blocked during worker replay; secondary Present would time out
                // waiting for this thread to dispatch its window message.
                using var workerEntered = new ManualResetEventSlim();
                ExceptionDispatchInfo? workerFailure = null;
                var worker = new Thread(() =>
                {
                    try
                    {
                        Assert.True(ThreadSafety.NativeFrameworkRenderSyncRoot.TryEnter(Limit));
                        try
                        {
                            workerEntered.Set();
                            fixture.Backend.Render();
                            fixture.Backend.Render();
                        }
                        finally
                        {
                            ThreadSafety.NativeFrameworkRenderSyncRoot.Exit();
                        }
                    }
                    catch (Exception exception)
                    {
                        workerFailure = ExceptionDispatchInfo.Capture(exception);
                    }
                }) { IsBackground = true };
                Action<nint> original = _ =>
                {
                    worker.Start();
                    Assert.True(workerEntered.Wait(Limit, cancellationToken));
                };
                var handoff = typeof(InterfaceManager).GetMethod("CallOriginalKernelDeviceSwapChainPresent", BindingFlags.Static | BindingFlags.NonPublic)!;
                var nativeOriginal = original.Method.CreateDelegate(handoff.GetParameters()[0].ParameterType, original.Target);
                try
                {
                    handoff.Invoke(null, [nativeOriginal, nint.Zero]);
                    Assert.True(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
                }
                finally
                {
                    Assert.True(worker.Join(Limit));
                }

                workerFailure?.Throw();
                presentation.Failure?.Throw();
                Assert.Equal(1, presentation.PresentCalls);
                Assert.Equal(1, presentation.MessageCalls);
                pipelineState.AssertRestored();
            }
        });
    }

    [Fact]
    public void WorkerCannotBuildFrameOrSubmitSecondaryViewport()
    {
        // The affinity checks must precede any context, draw-data, or renderer-handle access.
        var backend = (Dx11Win32Backend)RuntimeHelpers.GetUninitializedObject(typeof(Dx11Win32Backend));
        var renderer = (Dx11Renderer)RuntimeHelpers.GetUninitializedObject(typeof(Dx11Renderer));
        GC.SuppressFinalize(backend);
        GC.SuppressFinalize(renderer);
        ExceptionDispatchInfo? failure = null;
        var worker = new Thread(() =>
        {
            try
            {
                Assert.Throws<InvalidOperationException>(backend.Step);
                Assert.Throws<InvalidOperationException>(() => renderer.RenderViewport(0, default));
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        }) { IsBackground = true };
        worker.Start();
        Assert.True(worker.Join(Limit));
        failure?.Throw();
    }

    private static void RunOnOwner(Action action)
    {
        ExceptionDispatchInfo? failure = null;
        var owner = new Thread(() =>
        {
            try
            {
                ThreadSafety.MarkMainThread();
                using var services = new ServiceContainerScope();
                action();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        }) { IsBackground = true };
        owner.SetApartmentState(ApartmentState.MTA);
        owner.Start();
        Assert.True(owner.Join(TimeSpan.FromSeconds(30)), "Owner timed out during " + stage);
        failure?.Throw();
    }

    /// <summary>
    /// Supplies the IoC container needed by optional service probes without starting game services.
    /// </summary>
    private sealed class ServiceContainerScope : IDisposable
    {
        private static readonly FieldInfo ContainerSource = typeof(Service<ServiceContainer>).GetField("instanceTcs", BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly object previousSource = ContainerSource.GetValue(null)!;

        public ServiceContainerScope()
        {
            // Optional service probes register during generic static initialization.
            var source = new TaskCompletionSource<ServiceContainer>();
            source.SetResult(new ServiceContainer());
            ContainerSource.SetValue(null, source);
        }

        public void Dispose() => ContainerSource.SetValue(null, this.previousSource);
    }

    private static nint NativeAddress<T>(T value)
        where T : struct => Unsafe.As<T, nint>(ref value);

    private static nint ReadPointer<TNative, TWrapper>(TWrapper wrapper, string name)
        where TNative : struct
        where TWrapper : struct => Marshal.ReadIntPtr(NativeAddress(wrapper), (int)Marshal.OffsetOf<TNative>(name));

    private static void SetField<T>(object target, string name, T value) =>
        target.GetType().GetField(name, PrivateInstance)!.SetValue(target, value);

    private static T ComMethod<T>(nint instance, int slot)
        where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * nint.Size));

    private static Action<object, nint> PointerWriter(Type type, string name)
    {
        var adapter = new DynamicMethod("WritePresentationTest" + name, typeof(void), [typeof(object), typeof(nint)], typeof(SecondaryViewportPresentationTests), true);
        var il = adapter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, type);
        il.Emit(OpCodes.Ldflda, type.GetField(name, PrivateInstance)!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stind_I);
        il.Emit(OpCodes.Ret);
        return adapter.CreateDelegate<Action<object, nint>>();
    }

    private static nint ReadComPointer(object target, string name)
    {
        var adapter = new DynamicMethod("ReadPresentationTest" + name, typeof(nint), [typeof(object)], typeof(SecondaryViewportPresentationTests), true);
        var il = adapter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, target.GetType());
        il.Emit(OpCodes.Ldflda, target.GetType().GetField(name, PrivateInstance)!);
        il.Emit(OpCodes.Ldind_I);
        il.Emit(OpCodes.Ret);
        return adapter.CreateDelegate<Func<object, nint>>()(target);
    }

    /// <summary>
    /// Creates a hidden Win32 backend with a software D3D11 device and real platform callbacks.
    /// </summary>
    private sealed class BackendFixture : IDisposable
    {
        private readonly ReaderWriterLockSlim snapshotLock = new(LockRecursionPolicy.NoRecursion);
        private readonly DrawDataSnapshot snapshot = new();
        private readonly nint window;
        private readonly nint device;
        private readonly nint context;
        private readonly nint swapChain;
        private readonly bool comInitialized;

        public BackendFixture()
        {
            stage = "loading native bindings";
            PrepareNativeLibraries();
            this.comInitialized = CoInitializeEx(0, 0) >= 0;
            this.window = CreateWindowExW(0, "STATIC", "Dalamud presentation regression", 0, 0, 0, 160, 100, 0, 0, 0, 0);
            Assert.NotEqual(nint.Zero, this.window);
            var desc = new DXGI_SWAP_CHAIN_DESC
            {
                BufferDesc = { Width = 160, Height = 100, Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM },
                SampleDesc = new(1, 0),
                BufferUsage = 0x20,
                BufferCount = 1,
                OutputWindow = (HWND)this.window,
                Windowed = true,
            };
            Marshal.ThrowExceptionForHR(D3D11CreateDeviceAndSwapChain(0, 5, 0, 0, 0, 0, 7, ref desc, out this.swapChain, out this.device, out _, out this.context));

            stage = "creating ImGui context";
            var guiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(guiContext);
            ImGuizmo.SetImGuiContext(guiContext);
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
            ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.PlatformHasViewports | ImGuiBackendFlags.RendererHasViewports;
            ImGui.GetIO().Fonts.Build();
            var rendererConstructor = typeof(Dx11Renderer).GetConstructors()[0];
            var adapter = new DynamicMethod("CreatePresentationTestRenderer", typeof(Dx11Renderer), [typeof(nint), typeof(nint), typeof(nint)], typeof(SecondaryViewportPresentationTests), true);
            var il = adapter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Newobj, rendererConstructor);
            il.Emit(OpCodes.Ret);
            stage = "creating renderer";
            var renderer = adapter.CreateDelegate<Func<nint, nint, nint, Dx11Renderer>>()(this.swapChain, this.device, this.context);

            // Force the HWND swap-chain path even when DirectComposition is available.
            var dcompAddress = ReadComPointer(renderer, "dcompDevice");
            if (dcompAddress != 0)
                Marshal.Release(dcompAddress);
            PointerWriter(typeof(Dx11Renderer), "dcompDevice")(renderer, 0);

            stage = "initializing platform input";
            // Register platform callbacks without the input constructor's game-service dependencies.
            var input = (Win32InputHandler)RuntimeHelpers.GetUninitializedObject(typeof(Win32InputHandler));
            GC.SuppressFinalize(input);
            SetField(input, "hWnd", (HWND)this.window);
            SetField(input, "cursors", new HCURSOR[9]);
            var platformName = Marshal.StringToHGlobalAnsi("presentation-regression");
            SetField(input, "platformNamePtr", platformName);
            Marshal.WriteIntPtr(NativeAddress(ImGui.GetIO()), (int)Marshal.OffsetOf<ImGuiIO>("BackendPlatformName"), platformName);
            Marshal.WriteIntPtr(NativeAddress(ImGui.GetMainViewport()), (int)Marshal.OffsetOf<ImGuiViewport>("PlatformHandleRaw"), this.window);
            var wndProc = typeof(Win32InputHandler).GetField("wndProcDelegate", PrivateInstance)!;
            wndProc.SetValue(input, typeof(Win32InputHandler).GetMethod("WndProcDetour", PrivateInstance)!.CreateDelegate(wndProc.FieldType, input));
            var handlerType = typeof(Win32InputHandler).GetNestedType("ViewportHandler", BindingFlags.NonPublic)!;
            var handler = Activator.CreateInstance(handlerType, input)!;
            handlerType.GetMethod("Initialize")!.Invoke(handler, null);
            SetField(input, "viewportHandler", handler);
            input.IniPath = null;
            // Hide test windows while preserving native window creation and destruction.
            this.HideWindow = _ => { };
            Marshal.WriteIntPtr(NativeAddress(ImGui.GetPlatformIO()), (int)Marshal.OffsetOf<ImGuiPlatformIO>("PlatformShowWindow"), Marshal.GetFunctionPointerForDelegate(this.HideWindow));

            this.Backend = (Dx11Win32Backend)RuntimeHelpers.GetUninitializedObject(typeof(Dx11Win32Backend));
            GC.SuppressFinalize(this.Backend);
            SetField(this.Backend, "imguiRenderer", renderer);
            SetField(this.Backend, "imguiInput", input);
            SetField(this.Backend, "imguiContext", guiContext);
            SetField(this.Backend, "imguizmoContextSet", true);
            SetField(this.Backend, "renderLock", new Lock());
            SetField(this.Backend, "drawDataLock", this.snapshotLock);
            SetField(this.Backend, "snapshot", this.snapshot);
            SetField(this.Backend, "targetWidth", 160);
            SetField(this.Backend, "targetHeight", 100);
            this.Backend.NewRenderFrame += () => stage = "input NewFrame";
            this.Backend.NewInputFrame += () => stage = "ImGui NewFrame";
            this.Backend.BuildUi += () =>
            {
                stage = "building UI";
                ImGui.SetNextWindowPos(new Vector2(350, 100), ImGuiCond.Always);
                ImGui.SetNextWindowSize(new Vector2(120, 80), ImGuiCond.Always);
                ImGui.Begin("Secondary presentation regression", ImGuiWindowFlags.NoSavedSettings);
                ImGui.TextUnformatted("Secondary");
                ImGui.End();
                stage = "platform completion";
            };
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void HideWindowCallback(nint viewport);

        public Dx11Win32Backend Backend { get; }

        private HideWindowCallback HideWindow { get; }

        public PipelineState BindPipelineState() => new(this.device, this.context);

        public void Dispose()
        {
            this.Backend.Dispose();
            this.snapshotLock.Dispose();
            Marshal.Release(this.swapChain);
            Marshal.Release(this.context);
            Marshal.Release(this.device);
            DestroyWindow(this.window);
            if (this.comInitialized)
                CoUninitialize();
            GC.KeepAlive(this.HideWindow);
        }

        private static void PrepareNativeLibraries([CallerFilePath] string sourceFile = "")
        {
            var configuration = typeof(SecondaryViewportPresentationTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
            var nativeDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", "..", "bin", configuration));
            var bindingDirectory = Path.GetDirectoryName(typeof(ImGui).Assembly.Location)!;
            foreach (var name in new[] { "cimgui.dll", "cimguizmo.dll" })
            {
                var destination = Path.Combine(bindingDirectory, name);
                if (!File.Exists(destination))
                    File.Copy(Path.Combine(nativeDirectory, name), destination);
            }
        }

        [DllImport("d3d11.dll")]
        private static extern int D3D11CreateDeviceAndSwapChain(nint adapter, int driverType, nint software, uint flags, nint featureLevels, uint featureLevelCount, uint version, ref DXGI_SWAP_CHAIN_DESC desc, out nint swapChain, out nint device, out D3D_FEATURE_LEVEL featureLevel, out nint context);

        [DllImport("ole32.dll")]
        private static extern int CoInitializeEx(nint reserved, uint flags);

        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern nint CreateWindowExW(uint exStyle, string className, string title, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(nint window);
    }

    /// <summary>
    /// Binds render-target and UAV state to verify that backend rendering restores the caller's pipeline.
    /// </summary>
    private sealed class PipelineState : IDisposable
    {
        private readonly nint context;
        private readonly nint texture;
        private readonly nint renderTarget;
        private readonly nint buffer;
        private readonly nint unorderedView;

        public PipelineState(nint device, nint context)
        {
            this.context = context;
            var textureDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = 4,
                Height = 4,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
                SampleDesc = new(1, 0),
                BindFlags = 0x20,
            };
            Marshal.ThrowExceptionForHR(ComMethod<CreateTextureCallback>(device, 5)(device, ref textureDesc, 0, out this.texture));
            Marshal.ThrowExceptionForHR(ComMethod<CreateViewCallback>(device, 9)(device, this.texture, 0, out this.renderTarget));
            var bufferDesc = new D3D11_BUFFER_DESC { ByteWidth = 64, BindFlags = 0x80, MiscFlags = 0x20 };
            Marshal.ThrowExceptionForHR(ComMethod<CreateBufferCallback>(device, 3)(device, ref bufferDesc, 0, out this.buffer));
            var viewDesc = Marshal.AllocHGlobal(20);
            try
            {
                // D3D11_UNORDERED_ACCESS_VIEW_DESC for a raw buffer, including its union fields.
                Marshal.WriteInt32(viewDesc, 0, (int)DXGI_FORMAT.DXGI_FORMAT_R32_TYPELESS);
                Marshal.WriteInt32(viewDesc, 4, 1);
                Marshal.WriteInt32(viewDesc, 8, 0);
                Marshal.WriteInt32(viewDesc, 12, 16);
                Marshal.WriteInt32(viewDesc, 16, 1);
                Marshal.ThrowExceptionForHR(ComMethod<CreateViewCallback>(device, 8)(device, this.buffer, viewDesc, out this.unorderedView));
            }
            finally
            {
                Marshal.FreeHGlobal(viewDesc);
            }

            // A nonzero UAV start and trailing null slots exercise both bounds and pointer offsets on restore.
            var target = this.renderTarget;
            var unordered = this.unorderedView;
            ComMethod<SetTargetsCallback>(context, 34)(context, 1, ref target, 0, 3, 1, ref unordered, 0);
            this.AssertRestored();
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateTextureCallback(nint self, ref D3D11_TEXTURE2D_DESC desc, nint initial, out nint texture);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateBufferCallback(nint self, ref D3D11_BUFFER_DESC desc, nint initial, out nint buffer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateViewCallback(nint self, nint resource, nint desc, out nint view);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SetTargetsCallback(nint self, uint count, ref nint target, nint depth, uint start, uint unorderedCount, ref nint unordered, nint initialCounts);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void GetTargetsCallback(nint self, uint count, out nint target, nint depth, uint start, uint unorderedCount, out nint unordered);

        public void AssertRestored()
        {
            ComMethod<GetTargetsCallback>(this.context, 90)(this.context, 1, out var target, 0, 3, 1, out var unordered);
            try
            {
                Assert.Equal(this.renderTarget, target);
                Assert.Equal(this.unorderedView, unordered);
            }
            finally
            {
                if (target != 0)
                    Marshal.Release(target);
                if (unordered != 0)
                    Marshal.Release(unordered);
            }
        }

        public void Dispose()
        {
            var emptyTarget = nint.Zero;
            var emptyUnordered = nint.Zero;
            ComMethod<SetTargetsCallback>(this.context, 34)(this.context, 1, ref emptyTarget, 0, 3, 1, ref emptyUnordered, 0);
            Marshal.Release(this.unorderedView);
            Marshal.Release(this.buffer);
            Marshal.Release(this.renderTarget);
            Marshal.Release(this.texture);
        }
    }

    /// <summary>
    /// Replaces secondary Present with a synchronous message to the viewport's owning thread.
    /// </summary>
    private sealed class SynchronousPresent : IDisposable
    {
        private const uint TestMessage = 0x8001;
        private readonly object viewportData;
        private readonly Action<object, nint> writeSwapChain;
        private readonly object originalSwapChain;
        private readonly nint window;
        private readonly nint previousWndProc;
        private readonly nint vtable;
        private readonly nint swapChain;
        private readonly PresentCallback present;
        private readonly WindowProcedure wndProc;

        public SynchronousPresent(ImGuiViewportPtr viewport)
        {
            this.viewportData = GCHandle.FromIntPtr(ReadPointer<ImGuiViewport, ImGuiViewportPtr>(viewport, "RendererUserData")).Target!;
            var swapChainField = this.viewportData.GetType().GetField("swapChain", PrivateInstance)!;
            this.originalSwapChain = swapChainField.GetValue(this.viewportData)!;
            this.writeSwapChain = PointerWriter(this.viewportData.GetType(), "swapChain");
            this.window = ReadPointer<ImGuiViewport, ImGuiViewportPtr>(viewport, "PlatformHandleRaw");
            this.wndProc = (window, message, wparam, lparam) =>
            {
                if (message == TestMessage)
                {
                    this.MessageCalls++;
                    return 1;
                }

                return CallWindowProcW(this.previousWndProc, window, message, wparam, lparam);
            };
            this.previousWndProc = SetWindowLongPtrW(this.window, -4, Marshal.GetFunctionPointerForDelegate(this.wndProc));
            Assert.NotEqual(nint.Zero, this.previousWndProc);
            this.vtable = Marshal.AllocHGlobal(18 * nint.Size);
            for (var i = 0; i < 18; i++)
                Marshal.WriteIntPtr(this.vtable, i * nint.Size, 0);
            this.swapChain = Marshal.AllocHGlobal(nint.Size);
            Marshal.WriteIntPtr(this.swapChain, this.vtable);
            this.present = (_, _, _) =>
            {
                try
                {
                    this.PresentCalls++;
                    this.PresentThread = Environment.CurrentManagedThreadId;
                    this.SendSucceeded = SendMessageTimeoutW(this.window, TestMessage, 0, 0, 3, 1000, out _) != 0;
                }
                catch (Exception exception)
                {
                    this.Failure = ExceptionDispatchInfo.Capture(exception);
                }

                return 0;
            };
            Marshal.WriteIntPtr(this.vtable, 8 * nint.Size, Marshal.GetFunctionPointerForDelegate(this.present));
            this.writeSwapChain(this.viewportData, this.swapChain);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PresentCallback(nint self, uint syncInterval, uint flags);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate nint WindowProcedure(nint window, uint message, nuint wparam, nint lparam);

        public int PresentCalls { get; private set; }

        public int MessageCalls { get; private set; }

        public int PresentThread { get; private set; }

        public bool SendSucceeded { get; private set; }

        public ExceptionDispatchInfo? Failure { get; private set; }

        public void Dispose()
        {
            // Restore the borrowed swap-chain pointer before normal viewport disposal.
            this.viewportData.GetType().GetField("swapChain", PrivateInstance)!.SetValue(this.viewportData, this.originalSwapChain);
            SetWindowLongPtrW(this.window, -4, this.previousWndProc);
            Marshal.FreeHGlobal(this.swapChain);
            Marshal.FreeHGlobal(this.vtable);
            GC.KeepAlive(this.present);
            GC.KeepAlive(this.wndProc);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern nint SetWindowLongPtrW(nint window, int index, nint value);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern nint CallWindowProcW(nint previous, nint window, uint message, nuint wparam, nint lparam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern nint SendMessageTimeoutW(nint window, uint message, nuint wparam, nint lparam, uint flags, uint timeout, out nuint result);
    }
}
