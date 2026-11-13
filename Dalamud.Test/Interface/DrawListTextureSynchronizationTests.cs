using System;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps.Internal;
using Dalamud.Utility;

using Xunit;

namespace Dalamud.Test.Interface;

/// <summary>
/// Verifies that draw-list rendering and presentation work share framework admission.
/// </summary>
/// <remarks>
/// Admitted presentations use the fixture's configuration failure to stop after queue dispatch, before native ImGui access.
/// </remarks>
[Collection("Native framework render synchronization")]
public class DrawListTextureSynchronizationTests
{
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void MainThreadClearDefersWorkerPresentationAndPreservesQueuedWork()
    {
        using var presentation = new PresentationTestFixture(initialized: true);
        using var draw = new DrawFixture();
        var queuedCalls = 0;
        presentation.BeforeRender.Enqueue(() => queuedCalls++);
        draw.DuringClear = () =>
        {
            Assert.True(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
            RunOnThread(() => presentation.Present());
            Assert.Equal(0, queuedCalls);
            Assert.Single(presentation.BeforeRender);
            Assert.Equal(0, presentation.Manager.CumulativePresentCalls);
        };

        RunOnThread(() =>
        {
            draw.Draw();
            draw.CallbackFailure?.Throw();
            Assert.False(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
        }, mainThread: true);

        Assert.Equal(1, draw.ClearCalls);
        Assert.Equal(new Vector4(0.1f, 0.2f, 0.3f, 0.4f), draw.LastClearColor);
        RunOnThread(() => Assert.Throws<NullReferenceException>(() => presentation.Present()));
        Assert.Equal(1, queuedCalls);
        Assert.Empty(presentation.BeforeRender);
    }

    [Fact]
    public void MainThreadDrawWaitsUntilAdmittedWorkerQueueFinishes()
    {
        using var presentation = new PresentationTestFixture(initialized: true);
        using var draw = new DrawFixture();
        using var drawAttempted = new ManualResetEventSlim();
        using var clearReached = new ManualResetEventSlim();
        var queueFinished = false;
        ExceptionDispatchInfo? drawFailure = null;
        var main = new Thread(() =>
        {
            try
            {
                ThreadSafety.MarkMainThread();
                drawAttempted.Set();
                draw.Draw();
                draw.CallbackFailure?.Throw();
                Assert.False(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
            }
            catch (Exception exception)
            {
                drawFailure = ExceptionDispatchInfo.Capture(exception);
            }
        }) { IsBackground = true };
        draw.DuringClear = () =>
        {
            clearReached.Set();
            Assert.True(queueFinished, "Draw changed the context before queued worker work finished.");
            Assert.True(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
        };

        ExceptionDispatchInfo? queueFailure = null;
        presentation.BeforeRender.Enqueue(() =>
        {
            try
            {
                Assert.True(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
                main.Start();
                Assert.True(drawAttempted.Wait(WorkerTimeout));
                Assert.False(clearReached.Wait(TimeSpan.FromMilliseconds(100)));
            }
            catch (Exception exception)
            {
                queueFailure = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                queueFinished = true;
            }
        });

        RunOnThread(() => Assert.Throws<NullReferenceException>(() => presentation.Present()));
        Assert.True(main.Join(WorkerTimeout), "The main thread draw did not finish after presentation released admission.");
        queueFailure?.Throw();
        drawFailure?.Throw();
        Assert.True(clearReached.IsSet);
        Assert.Equal(1, draw.ClearCalls);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    public void EmptyTargetAndEmptyDrawDataPreserveFrameworkRecursion(int recursionDepth, bool hasTarget)
    {
        using var draw = new DrawFixture(hasTarget);
        draw.DuringClear = () => Assert.True(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
        RunOnThread(() =>
        {
            var guard = ThreadSafety.NativeFrameworkRenderSyncRoot;
            for (var i = 0; i < recursionDepth; i++)
                guard.Enter();

            try
            {
                draw.Draw();
                draw.CallbackFailure?.Throw();
                for (var i = 0; i < recursionDepth; i++)
                {
                    Assert.True(guard.IsHeldByCurrentThread);
                    guard.Exit();
                }

                Assert.False(guard.IsHeldByCurrentThread);
                RunOnThread(() =>
                {
                    Assert.True(guard.TryEnter());
                    guard.Exit();
                });
            }
            finally
            {
                while (guard.IsHeldByCurrentThread)
                    guard.Exit();
            }
        }, mainThread: true);
        Assert.Equal(hasTarget ? 1 : 0, draw.ClearCalls);
    }

    [Fact]
    public void DrawStillRejectsNonMainThreadBeforeChangingGraphicsState()
    {
        using var draw = new DrawFixture();
        RunOnThread(() =>
        {
            Assert.False(ThreadSafety.IsMainThread);
            Assert.Throws<InvalidOperationException>(draw.Draw);
            Assert.False(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
        });
        Assert.Equal(0, draw.ClearCalls);
    }

    private static void RunOnThread(Action action, bool mainThread = false)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (mainThread)
                    ThreadSafety.MarkMainThread();
                action();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        }) { IsBackground = true };
        thread.Start();
        Assert.True(thread.Join(WorkerTimeout), "The graphics test thread did not finish.");
        failure?.Throw();
    }

    /// <summary>
    /// Exercises clearing and early returns through a controlled immediate context.
    /// </summary>
    /// <remarks>Render-target handles are sentinels; the context supports only ClearRenderTargetView.</remarks>
    private sealed class DrawFixture : IDisposable
    {
        private static readonly Action<DrawListTextureWrap, nint> WriteContext = CreateFieldWriter("deviceContext");
        private static readonly Action<DrawListTextureWrap, nint> WriteTarget = CreateFieldWriter("rtv");
        private static readonly Action<DrawListTextureWrap, nint> WritePremultipliedTarget = CreateFieldWriter("rtvPremultiplied");
        private readonly DrawListTextureWrap texture;
        private readonly nint vtable;
        private readonly nint context;
        private readonly ClearCallback clear;

        public DrawFixture(bool hasTarget = true)
        {
            // Bypass device creation and COM ownership; the fixture releases its own unmanaged storage.
            this.texture = (DrawListTextureWrap)RuntimeHelpers.GetUninitializedObject(typeof(DrawListTextureWrap));
            GC.SuppressFinalize(this.texture);
            this.texture.ClearColor = new Vector4(0.1f, 0.2f, 0.3f, 0.4f);
            this.vtable = Marshal.AllocHGlobal(51 * nint.Size);
            for (var i = 0; i < 51; i++)
                Marshal.WriteIntPtr(this.vtable, i * nint.Size, 0);
            this.context = Marshal.AllocHGlobal(nint.Size);
            Marshal.WriteIntPtr(this.context, this.vtable);
            this.clear = (_, target, color) =>
            {
                try
                {
                    this.ClearCalls++;
                    Assert.Equal((nint)42, target);
                    this.LastClearColor = Marshal.PtrToStructure<Vector4>(color);
                    this.DuringClear?.Invoke();
                }
                catch (Exception exception)
                {
                    // Rethrow assertions after returning across the COM boundary.
                    this.CallbackFailure = ExceptionDispatchInfo.Capture(exception);
                }
            };
            // ID3D11DeviceContext::ClearRenderTargetView occupies vtable slot 50.
            Marshal.WriteIntPtr(this.vtable, 50 * nint.Size, Marshal.GetFunctionPointerForDelegate(this.clear));
            WriteContext(this.texture, this.context);
            WriteTarget(this.texture, hasTarget ? 42 : 0);
            WritePremultipliedTarget(this.texture, hasTarget ? 42 : 0);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void ClearCallback(nint self, nint target, nint color);

        public int ClearCalls { get; private set; }

        public Vector4 LastClearColor { get; private set; }

        public Action? DuringClear { get; set; }

        public ExceptionDispatchInfo? CallbackFailure { get; private set; }

        public void Draw() => this.texture.Draw(default(ImDrawData));

        public void Dispose()
        {
            // These fields borrow the fixture's storage; they never own COM references.
            WriteContext(this.texture, 0);
            WriteTarget(this.texture, 0);
            WritePremultipliedTarget(this.texture, 0);
            Marshal.FreeHGlobal(this.context);
            Marshal.FreeHGlobal(this.vtable);
            GC.KeepAlive(this.clear);
        }

        private static Action<DrawListTextureWrap, nint> CreateFieldWriter(string name)
        {
            // Write the pointer stored by ComPtr without invoking COM reference counting.
            var method = new DynamicMethod("WriteDrawTest" + name, typeof(void), [typeof(DrawListTextureWrap), typeof(nint)], typeof(DrawFixture), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, typeof(DrawListTextureWrap).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stind_I);
            il.Emit(OpCodes.Ret);
            return method.CreateDelegate<Action<DrawListTextureWrap, nint>>();
        }
    }
}
