using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Hooking.WndProcHook;
using Dalamud.Interface.ImGuiBackend;
using Dalamud.Interface.ImGuiBackend.Delegates;
using Dalamud.Interface.ImGuiBackend.InputHandler;
using Dalamud.Interface.ImGuiBackend.Renderers;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.ReShadeHandling;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using TerraFX.Interop.Windows;

using Xunit;

namespace Dalamud.Test.Interface;

[Collection("Native framework render synchronization")]
public class InterfaceShutdownTests
{
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData("DxgiSwapChainPresentDetour", "dxgiSwapChainPresentHook", "dxgiSwapChainPresentFallback")]
    [InlineData("ReShadeDxgiSwapChainOnPresentDetour", "reShadeDxgiSwapChainPresentHook", "reShadeDxgiSwapChainPresentFallback")]
    [InlineData("AsHookDxgiSwapChainResizeBuffersDetour", "dxgiSwapChainResizeBuffersHook", "dxgiSwapChainResizeBuffersFallback")]
    [InlineData("AsReShadeAddonDxgiSwapChainResizeBuffersDetour", "dxgiSwapChainResizeBuffersHook", "dxgiSwapChainResizeBuffersFallback")]
    [InlineData("KernelDeviceSwapChainPresentDetour", "kernelDeviceSwapchainPresentHook", "kernelDeviceSwapchainPresentFallback")]
    [InlineData("SetCursorDetour", "setCursorHook", "setCursorFallback")]
    public void ShutdownDisablesHooksWithoutBlockingAndDrainsOriginalContinuation(
        string detourName,
        string hookField,
        string fallbackField)
    {
        using var fixture = new ShutdownFixture();
        using var originalEntered = new ManualResetEventSlim();
        using var releaseOriginal = new ManualResetEventSlim();
        var fallbackCalls = 0;
        var originalCalls = 0;
        var hook = fixture.InstallHook(hookField, fallbackField, () =>
        {
            Interlocked.Increment(ref originalCalls);
            originalEntered.Set();
            Assert.True(releaseOriginal.Wait(WorkerTimeout), "The original continuation was not released.");
        }, () => Interlocked.Increment(ref fallbackCalls));
        var invoke = fixture.CreateDetourInvoker(detourName);
        Exception? workerFailure = null;
        Exception? dispatcherFailure = null;
        Task? cleanup = null;
        var dispatcherStarted = false;
        using var dispatcherReturned = new ManualResetEventSlim();
        var worker = new Thread(() =>
        {
            try
            {
                invoke(false);
            }
            catch (Exception exception)
            {
                workerFailure = exception;
            }
        }) { IsBackground = true };
        var dispatcher = new Thread(() =>
        {
            try
            {
                cleanup = fixture.ClearHooks();
            }
            catch (Exception exception)
            {
                dispatcherFailure = exception;
            }
            finally
            {
                dispatcherReturned.Set();
            }
        }) { IsBackground = true };

        worker.Start();
        try
        {
            Assert.True(originalEntered.Wait(WorkerTimeout), "The detour did not reach its original continuation.");
            dispatcher.Start();
            dispatcherStarted = true;
            Assert.True(dispatcherReturned.Wait(WorkerTimeout), "Hook detachment blocked the framework dispatcher.");
            Assert.Null(dispatcherFailure);
            Assert.NotNull(cleanup);
            Assert.True(hook.DisableCalls > 0);
            Assert.False(cleanup.Wait(TimeSpan.FromMilliseconds(100)), "Cleanup completed before the original continuation returned.");
            Assert.False(hook.IsDisposed);
            Assert.Same(hook, fixture.GetField(hookField));
        }
        finally
        {
            releaseOriginal.Set();
            Assert.True(worker.Join(WorkerTimeout), "The callback worker did not finish.");
            if (dispatcherStarted)
                Assert.True(dispatcher.Join(WorkerTimeout), "The cleanup dispatcher did not finish.");
        }

        Assert.Null(workerFailure);
        Assert.True(cleanup!.Wait(WorkerTimeout), "Cleanup did not finish after the callback returned.");
        Assert.True(hook.IsDisposed);
        Assert.Null(fixture.GetField(hookField));
        Assert.Equal(1, originalCalls);
        Assert.Equal(0, fallbackCalls);

        // A caller holding a stale detour must use the native fallback after hooks are released.
        invoke(true);
        Assert.Equal(1, originalCalls);
        Assert.Equal(1, fallbackCalls);
        Assert.Same(cleanup, fixture.ClearHooks());
    }

    [Theory]
    [InlineData("DxgiSwapChainPresentDetour", "dxgiSwapChainPresentHook", "dxgiSwapChainPresentFallback")]
    [InlineData("ReShadeDxgiSwapChainOnPresentDetour", "reShadeDxgiSwapChainPresentHook", "reShadeDxgiSwapChainPresentFallback")]
    [InlineData("KernelDeviceSwapChainPresentDetour", "kernelDeviceSwapchainPresentHook", "kernelDeviceSwapchainPresentFallback")]
    public void OriginalFailureReleasesCallbackSoCleanupCanFinish(
        string detourName,
        string hookField,
        string fallbackField)
    {
        using var fixture = new ShutdownFixture();
        var failure = new ContinuationException();
        var hook = fixture.InstallHook(hookField, fallbackField, () => throw failure, () => { });
        var invoke = fixture.CreateDetourInvoker(detourName);

        Assert.Same(failure, Assert.Throws<ContinuationException>(() => invoke(false)));
        Assert.True(fixture.ClearHooks().Wait(WorkerTimeout), "An exceptional callback kept cleanup blocked.");
        Assert.True(hook.IsDisposed);
    }

    [Fact]
    public void ClearHooksBeforeHookInstallationIsSafeAndIdempotent()
    {
        using var fixture = new ShutdownFixture();
        var cleanup = fixture.ClearHooks();
        Assert.True(cleanup.Wait(WorkerTimeout));
        Assert.Same(cleanup, fixture.ClearHooks());
    }

    [Fact]
    public void StoppedEventCallbacksSkipNativeAndBackendState()
    {
        using var fixture = new ShutdownFixture();
        fixture.InvokeStoppedEventCallbacks();
        Assert.True(fixture.ClearHooks().Wait(WorkerTimeout));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ServiceUnloadYieldsFrameworkUntilOriginalContinuationAndPreservesDisposalOrder(bool startOnFrameworkThread)
    {
        using var fixture = new ShutdownFixture();
        var framework = (Framework)fixture.GetField("framework")!;
        using var dispatcher = new FrameworkDispatcher(framework);
        using var originalEntered = new ManualResetEventSlim();
        using var hooksDetached = new ManualResetEventSlim();
        using var frameworkWorkRan = new ManualResetEventSlim();
        using var releaseOriginal = new ManualResetEventSlim();
        using var unloadReturned = new ManualResetEventSlim();
        using var resourcesDisposed = new ManualResetEventSlim();
        using var followingServiceDisposed = new ManualResetEventSlim();
        using var windowCallbackEntered = new ManualResetEventSlim();
        using var releaseWindowCallback = new ManualResetEventSlim();
        var order = new ConcurrentQueue<string>();
        var hook = fixture.InstallHook("dxgiSwapChainPresentHook", "dxgiSwapChainPresentFallback", () =>
        {
            originalEntered.Set();
            Assert.True(hooksDetached.Wait(WorkerTimeout), "Service unload did not detach interception.");

            // Native Present may need the framework/window thread before it can return.
            var frameworkWork = framework.RunOnFrameworkThread(() => frameworkWorkRan.Set());
            Assert.True(frameworkWork.Wait(WorkerTimeout), "The drain prevented native-dependent framework work.");
            Assert.True(releaseOriginal.Wait(WorkerTimeout), "The original continuation was not released.");
        }, () => throw new InvalidOperationException("Unexpected native fallback before drain."));
        var invoke = fixture.CreateDetourInvoker("DxgiSwapChainPresentDetour");
        fixture.PrepareServiceDisposal(() =>
        {
            Assert.True(framework.IsInFrameworkUpdateThread, "The backend was released away from the window owner thread.");
            Assert.True(hook.IsDisposed);
            order.Enqueue("resources");
            resourcesDisposed.Set();
        });
        var worker = Task.Factory.StartNew(() => invoke(false), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        Task? unload = null;
        Task? windowCallback = null;

        try
        {
            Assert.True(originalEntered.Wait(WorkerTimeout), "The presentation worker did not enter its native continuation.");

            Task BeginUnload()
            {
                // Exercise service ordering with the real interface disposer and isolated neighboring services.
                var result = ServiceManager.UnloadServices(framework,
                [
                    () =>
                    {
                        Assert.True(framework.IsInFrameworkUpdateThread);
                        Assert.Equal(0, hook.DisableCalls);
                        order.Enqueue("preceding service");
                        return Task.CompletedTask;
                    },
                    () =>
                    {
                        Assert.True(framework.IsInFrameworkUpdateThread);
                        var cleanup = fixture.DisposeServiceAsync();
                        order.Enqueue("detach");
                        hooksDetached.Set();
                        return cleanup;
                    },
                    () =>
                    {
                        Assert.True(framework.IsInFrameworkUpdateThread);
                        Assert.True(resourcesDisposed.IsSet);
                        order.Enqueue("following service");
                        followingServiceDisposed.Set();
                        return Task.CompletedTask;
                    },
                ]);
                unloadReturned.Set();
                return result;
            }

            unload = startOnFrameworkThread ? framework.RunOnFrameworkThread(BeginUnload) : Task.Run(BeginUnload);
            Assert.True(unloadReturned.Wait(WorkerTimeout), "Starting service unload synchronously blocked its caller.");
            Assert.True(frameworkWorkRan.Wait(WorkerTimeout), "The framework could not service the native continuation while draining.");
            Assert.False(worker.IsCompleted);
            Assert.False(unload.IsCompleted);
            Assert.False(hook.IsDisposed);
            Assert.False(resourcesDisposed.IsSet);
            Assert.False(followingServiceDisposed.IsSet);

            // Secondary WndProc callbacks are outside the hook drain; owner-thread dispatch must serialize teardown.
            windowCallback = framework.RunOnFrameworkThread(() =>
            {
                windowCallbackEntered.Set();
                Assert.True(releaseWindowCallback.Wait(WorkerTimeout), "The owner-thread operation was not released.");
                Assert.False(resourcesDisposed.IsSet);
            });
            Assert.True(windowCallbackEntered.Wait(WorkerTimeout));
            releaseOriginal.Set();
            await worker.WaitAsync(WorkerTimeout);
            await ((Task)fixture.GetField("hookCleanupTask")!).WaitAsync(WorkerTimeout);
            Assert.False(resourcesDisposed.IsSet);
            Assert.False(unload.IsCompleted);
            releaseWindowCallback.Set();
            await windowCallback.WaitAsync(WorkerTimeout);
            await unload.WaitAsync(WorkerTimeout);
            Assert.True(hook.IsDisposed);
            Assert.True(resourcesDisposed.IsSet);
            Assert.True(followingServiceDisposed.IsSet);
            Assert.Equal(new[] { "preceding service", "detach", "resources", "following service" }, order.ToArray());
        }
        finally
        {
            // Release blocked work before fixture disposal, including when an assertion fails.
            hooksDetached.Set();
            releaseOriginal.Set();
            releaseWindowCallback.Set();
            try
            {
                await Task.WhenAll(worker, unload ?? Task.CompletedTask, windowCallback ?? Task.CompletedTask).WaitAsync(WorkerTimeout);
            }
            finally
            {
                // Unload can resume this continuation on the dispatcher; return to the pool before joining it.
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task InterfaceDisposalWithoutActiveCallbacksPreservesOwnerThread(bool startOnFrameworkThread, bool unloading)
    {
        using var fixture = new ShutdownFixture();
        var framework = (Framework)fixture.GetField("framework")!;
        using var dispatcher = new FrameworkDispatcher(framework, unloading);
        var disposeCalls = 0;
        fixture.PrepareServiceDisposal(() =>
        {
            Assert.True(framework.IsInFrameworkUpdateThread);
            Interlocked.Increment(ref disposeCalls);
        });

        try
        {
            var disposal = startOnFrameworkThread
                ? framework.RunOnFrameworkThread(fixture.DisposeServiceAsync)
                : Task.Run(fixture.DisposeServiceAsync);
            await disposal.WaitAsync(WorkerTimeout);
            Assert.Equal(1, disposeCalls);
            Assert.Null(fixture.GetField("backend"));
        }
        finally
        {
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        }
    }

    [Fact]
    public async Task InterfaceDisposalAfterFrameworkDestructionDoesNotNeedAnotherTick()
    {
        using var fixture = new ShutdownFixture();
        var framework = (Framework)fixture.GetField("framework")!;
        using var dispatcher = new FrameworkDispatcher(framework, unloading: true, destroyed: true);
        var disposeCalls = 0;
        fixture.PrepareServiceDisposal(() =>
        {
            Assert.False(framework.IsInFrameworkUpdateThread);
            Interlocked.Increment(ref disposeCalls);
        });

        await fixture.DisposeServiceAsync().WaitAsync(WorkerTimeout);
        Assert.Equal(1, disposeCalls);
        Assert.Null(fixture.GetField("backend"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ServiceUnloadWithoutLiveFrameworkStillAwaitsEachService(bool unloadingFramework)
    {
        using var fixture = new ShutdownFixture();
        var framework = (Framework)fixture.GetField("framework")!;
        using var dispatcher = new FrameworkDispatcher(framework, unloadingFramework);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStarted = false;
        var followingStarted = false;
        var callerThread = Environment.CurrentManagedThreadId;
        var unload = ServiceManager.UnloadServices(unloadingFramework ? framework : null,
        [
            async () =>
            {
                Assert.Equal(callerThread, Environment.CurrentManagedThreadId);
                firstStarted = true;
                await release.Task.ConfigureAwait(false);
            },
            () =>
            {
                Assert.False(framework.IsInFrameworkUpdateThread);
                followingStarted = true;
                return Task.CompletedTask;
            },
        ]);

        Assert.True(firstStarted);
        Assert.False(followingStarted);
        Assert.False(unload.IsCompleted);
        release.SetResult();
        await unload.WaitAsync(WorkerTimeout);
        Assert.True(followingStarted);
    }

    private delegate void ReShadeObjectCallback(ref ReShadeAddonInterface.ApiObject value);

    private delegate void ReShadePresentCallback(
        ref ReShadeAddonInterface.ApiObject runtime,
        ref ReShadeAddonInterface.ApiObject swapChain,
        ReadOnlySpan<RECT> sourceRect,
        ReadOnlySpan<RECT> destRect,
        ReadOnlySpan<RECT> dirtyRects);

    private interface IFakeHook : IDalamudHook
    {
        int DisableCalls { get; }
    }

    private sealed class FakeHook<T>(T original) : Hook<T>((nint)1), IFakeHook
        where T : Delegate
    {
        private int disableCalls;

        public int DisableCalls => Volatile.Read(ref this.disableCalls);

        public override bool IsEnabled => !this.IsDisposed && this.DisableCalls == 0;

        public override string BackendName => "Shutdown test";

        public override T Original
        {
            get
            {
                this.CheckDisposed();
                return original;
            }
        }

        public override void Disable() => Interlocked.Increment(ref this.disableCalls);

        public override void Enable() => throw new NotSupportedException();
    }

    private sealed class ShutdownFixture : IDisposable
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Lazy<nint[]> AssertionLibraries = new(() => LoadAssertionLibraries());

        private readonly InterfaceManager manager;
        private readonly Func<Task> clearHooks;
        private readonly nint swapChain;
        private readonly nint swapChainVTable;
        private readonly GetDescCallback getDesc = (_, _) => unchecked((int)0x80004005);

        public ShutdownFixture()
        {
            this.manager = Uninitialized<InterfaceManager>();
            foreach (var field in typeof(InterfaceManager).GetFields(PrivateInstance))
            {
                if (field.FieldType == typeof(Lock))
                    field.SetValue(this.manager, new Lock());
                else if (field.FieldType == typeof(object))
                    field.SetValue(this.manager, new object());
            }

            this.SetField("callbackLifetime", new InterfaceCallbackLifetime());
            this.SetField("framework", Uninitialized<Framework>());
            this.SetField("wndProcHookManager", Uninitialized<WndProcHookManager>());
            var resizeStates = typeof(InterfaceManager).GetField("reShadeResizes", PrivateInstance)!;
            resizeStates.SetValue(this.manager, Activator.CreateInstance(resizeStates.FieldType));
            this.clearHooks = typeof(InterfaceManager).GetMethod("ClearHooks", PrivateInstance)!
                .CreateDelegate<Func<Task>>(this.manager);

            // A failing GetDesc rejects the resize target before any game-service lookup.
            this.swapChainVTable = Marshal.AllocHGlobal(18 * nint.Size);
            this.swapChain = Marshal.AllocHGlobal(nint.Size);
            Marshal.WriteIntPtr(this.swapChain, this.swapChainVTable);
            Marshal.WriteIntPtr(this.swapChainVTable, 12 * nint.Size, Marshal.GetFunctionPointerForDelegate(this.getDesc));
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetDescCallback(nint swapChain, nint desc);

        public Task ClearHooks() => this.clearHooks();

        public Task DisposeServiceAsync() => ((IInternalAsyncDisposableService)this.manager).DisposeServiceAsync();

        public void PrepareServiceDisposal(Action onBackendDispose)
        {
            // Assertion-handler disposal uses these native exports; retain the modules across fixtures.
            _ = AssertionLibraries.Value;
            foreach (var name in new[] { "clientState", "assertHandler" })
            {
                var fieldType = typeof(InterfaceManager).GetField(name, PrivateInstance)!.FieldType;
                var value = RuntimeHelpers.GetUninitializedObject(fieldType);
                GC.SuppressFinalize(value);
                this.SetField(name, value);
            }

            this.SetField("frameRetireActions", new List<Action>());
            this.SetField("pendingRetireQueue", new ConcurrentQueue<Action>());
            this.SetField("backend", new RecordingBackend(onBackendDispose));
        }

        private static nint[] LoadAssertionLibraries([CallerFilePath] string sourceFile = "")
        {
            var configuration = typeof(InterfaceShutdownTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
            var nativeDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", "..", "bin", configuration));
            return new[] { "cimgui.dll", "cimplot.dll", "cimguizmo.dll" }
                .Select(name => NativeLibrary.Load(Path.Combine(nativeDirectory, name)))
                .ToArray();
        }

        public object? GetField(string field) => typeof(InterfaceManager).GetField(field, PrivateInstance)!.GetValue(this.manager);

        public void InvokeStoppedEventCallbacks()
        {
            ((InterfaceCallbackLifetime)this.GetField("callbackLifetime")!).Stop();

            // Invalid arguments expose any native or WndProc access after the lifetime has stopped.
            var value = default(ReShadeAddonInterface.ApiObject);
            foreach (var method in new[] { "ReShadeAddonInterfaceOnDestroySwapChain", "ReShadeAddonInterfaceOnInitSwapChain", "ReShadeAddonInterfaceOnReShadeOverlay" })
            {
                typeof(InterfaceManager).GetMethod(method, PrivateInstance)!
                    .CreateDelegate<ReShadeObjectCallback>(this.manager)(ref value);
            }

            typeof(InterfaceManager).GetMethod("ReShadeAddonInterfaceOnPresent", PrivateInstance)!
                .CreateDelegate<ReShadePresentCallback>(this.manager)(ref value, ref value, [], [], []);
            typeof(InterfaceManager).GetMethod("WndProcHookManagerOnPreWndProc", PrivateInstance)!
                .CreateDelegate<Action<WndProcEventArgs>>(this.manager)(null!);
            typeof(InterfaceManager).GetMethod("FrameworkOnUpdate", PrivateInstance)!
                .CreateDelegate<Action<IFramework>>(this.manager)(null!);
        }

        public IFakeHook InstallHook(string hookField, string fallbackField, Action original, Action fallback)
        {
            var hookType = typeof(InterfaceManager).GetField(hookField, PrivateInstance)!.FieldType;
            var delegateType = hookType.GetGenericArguments()[0];
            var originalDelegate = MakeContinuation(delegateType, original);
            var hook = (IFakeHook)typeof(FakeHook<>).MakeGenericType(delegateType)
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0]
                .Invoke([originalDelegate]);
            this.SetField(hookField, hook);
            this.SetField(fallbackField, MakeContinuation(delegateType, fallback));
            return hook;
        }

        public Action<bool> CreateDetourInvoker(string name)
        {
            var method = typeof(InterfaceManager).GetMethod(name, PrivateInstance)!;
            var argumentTypes = method.GetParameters().Select(parameter => parameter.ParameterType.IsPointer
                ? typeof(nint)
                : parameter.ParameterType.IsEnum ? Enum.GetUnderlyingType(parameter.ParameterType) : parameter.ParameterType).ToArray();
            var adapter = new DynamicMethod(name + "TestAdapter", method.ReturnType, [typeof(InterfaceManager), .. argumentTypes], typeof(InterfaceShutdownTests), true);
            var il = adapter.GetILGenerator();
            for (var index = 0; index <= argumentTypes.Length; index++)
                il.Emit(OpCodes.Ldarg, (short)index);
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Ret);

            switch (name)
            {
                case "DxgiSwapChainPresentDetour":
                    var present = adapter.CreateDelegate<Func<nint, uint, uint, int>>(this.manager);
                    return late => Assert.Equal(7, present(0, 0, late ? 0u : 1u));
                case "ReShadeDxgiSwapChainOnPresentDetour":
                    var reShadePresent = adapter.CreateDelegate<Action<nint, uint, nint>>(this.manager);
                    return late => reShadePresent(0, late ? 0u : 1u, 0);
                case "AsHookDxgiSwapChainResizeBuffersDetour":
                case "AsReShadeAddonDxgiSwapChainResizeBuffersDetour":
                    var resize = adapter.CreateDelegate<Func<nint, uint, uint, uint, int, uint, int>>(this.manager);
                    return late => Assert.Equal(7, resize(late ? 0 : this.swapChain, 0, 0, 0, 0, 0));
                case "KernelDeviceSwapChainPresentDetour":
                    var kernel = adapter.CreateDelegate<Action<nint>>(this.manager);
                    return _ => kernel(0);
                case "SetCursorDetour":
                    var cursor = adapter.CreateDelegate<Func<nint, nint>>(this.manager);
                    return _ => Assert.Equal((nint)7, cursor(0));
                default:
                    throw new ArgumentOutOfRangeException(nameof(name));
            }
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(this.swapChain);
            Marshal.FreeHGlobal(this.swapChainVTable);
            GC.KeepAlive(this.getDesc);
        }

        private static T Uninitialized<T>() where T : class
        {
            var instance = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
            GC.SuppressFinalize(instance);
            return instance;
        }

        private static Delegate MakeContinuation(Type delegateType, Action action)
        {
            var signature = delegateType.GetMethod("Invoke")!;
            var parameters = signature.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            var method = new DynamicMethod("ShutdownTestContinuation", signature.ReturnType, [typeof(Action), .. parameters], typeof(InterfaceShutdownTests), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, typeof(Action).GetMethod(nameof(Action.Invoke))!);
            if (signature.ReturnType != typeof(void))
            {
                il.Emit(OpCodes.Ldc_I4_7);
                if (signature.ReturnType == typeof(nint))
                    il.Emit(OpCodes.Conv_I);
            }

            il.Emit(OpCodes.Ret);
            return method.CreateDelegate(delegateType, action);
        }

        private void SetField(string field, object value) => typeof(InterfaceManager).GetField(field, PrivateInstance)!.SetValue(this.manager, value);
    }

    private sealed class FrameworkDispatcher : IDisposable
    {
        private readonly CancellationTokenSource frameworkDestroy = new();
        private readonly CancellationTokenSource frameworkDestroyed = new();
        private readonly ManualResetEventSlim stop = new();
        private readonly Thread thread;
        private readonly bool runDispatcher;

        public FrameworkDispatcher(Framework framework, bool unloading = false, bool destroyed = false)
        {
            var scheduler = new ThreadBoundTaskScheduler();
            this.thread = new Thread(() =>
            {
                while (!this.stop.IsSet)
                {
                    scheduler.Run();
                    this.stop.Wait(TimeSpan.FromMilliseconds(10));
                }
            }) { IsBackground = true, Name = "Service unload framework regression" };
            scheduler.BoundThread = this.thread;
            if (unloading)
                this.frameworkDestroy.Cancel();
            if (destroyed)
                this.frameworkDestroyed.Cancel();

            const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(Framework).GetField("frameworkDestroy", privateInstance)!.SetValue(framework, this.frameworkDestroy);
            typeof(Framework).GetField("frameworkDestroyed", privateInstance)!.SetValue(framework, this.frameworkDestroyed);
            typeof(Framework).GetField("frameworkThreadTaskScheduler", privateInstance)!.SetValue(framework, scheduler);
            typeof(Framework).GetField("<FrameworkThreadTaskFactory>k__BackingField", privateInstance)!.SetValue(framework,
                new TaskFactory(this.frameworkDestroyed.Token, TaskCreationOptions.None, TaskContinuationOptions.None, scheduler));
            this.runDispatcher = !destroyed;
            if (this.runDispatcher)
                this.thread.Start();
        }

        public void Dispose()
        {
            this.stop.Set();
            if (this.runDispatcher)
                Assert.True(this.thread.Join(WorkerTimeout), "The framework dispatcher did not finish.");
            this.stop.Dispose();
            this.frameworkDestroy.Dispose();
            this.frameworkDestroyed.Dispose();
        }
    }

    private sealed class RecordingBackend(Action onDispose) : IWin32Backend
    {
        public event ImGuiBuildUiDelegate? BuildUi { add { } remove { } }

        public event ImGuiNewInputFrameDelegate? NewInputFrame { add { } remove { } }

        public event ImGuiNewRenderFrameDelegate? NewRenderFrame { add { } remove { } }

        public event Action? PostCopy { add { } remove { } }

        public bool UpdateCursor { get; set; }

        public string? IniPath { get; set; }

        public nint DeviceHandle => throw new NotSupportedException();

        public IImGuiInputHandler InputHandler => throw new NotSupportedException();

        public IImGuiRenderer Renderer => throw new NotSupportedException();

        public bool IsResizeInProgress => throw new NotSupportedException();

        public void Dispose() => onDispose();

        public void Step() => throw new NotSupportedException();

        public void Render() => throw new NotSupportedException();

        public bool TryReadViewportBackBuffer(uint viewportId, Action<nint> reader) => throw new NotSupportedException();

        public IDisposable BeginResize() => throw new NotSupportedException();

        public void OnPreResize() => throw new NotSupportedException();

        public void OnPostResize(int newWidth, int newHeight) => throw new NotSupportedException();

        public void InvalidateFonts() => throw new NotSupportedException();

        public bool IsImGuiCursor(nint cursorHandle) => throw new NotSupportedException();

        public bool IsAttachedToPresentationTarget(nint targetHandle) => throw new NotSupportedException();

        public bool IsMainViewportFullScreen() => throw new NotSupportedException();

        public nint? ProcessWndProcW(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam) => throw new NotSupportedException();
    }

    private sealed class ContinuationException : Exception
    {
    }
}
