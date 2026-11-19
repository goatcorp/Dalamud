using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Utility;

using Lumina.Data.Files;
using Lumina.Data.Parsing.Tex.Buffers;

using Xunit;

namespace Dalamud.Test.Interface;

[Collection("Native framework render synchronization")]
public class TexturePresentationDispatchTests
{
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void SynchronousTexCreationDuringDrawDoesNotWaitForPresentationBoundLoaderWork()
    {
        using var fixture = new DispatchFixture();
        using var device = new ResourceCreationFixture(fixture.Textures);
        using var conversionCancellation = new CancellationTokenSource();
        using var loader = new DynamicPriorityQueueLoader(1);
        typeof(TextureManager).GetField("dynamicPriorityTextureLoader", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(fixture.Textures, loader);
        var conversionCalls = 0;
        // Occupy the only loader worker with work that cannot finish until presentation drains the queue.
        var conversion = loader.LoadAsync(
            null,
            async _ =>
            {
                await fixture.Run(() => { Interlocked.Increment(ref conversionCalls); }).WaitAsync(conversionCancellation.Token);
                return 42;
            },
            default);
        var file = new TexFile
        {
            Header = new()
            {
                Type = TexFile.Attribute.TextureType2D,
                Format = TexFile.TextureFormat.B8G8R8A8,
                Width = 1,
                Height = 1,
                Depth = 1,
                MipLevelsCount = 1,
            },
            TextureBuffer = new B8G8R8A8TextureBuffer(TexFile.Attribute.TextureType2D, 1, 1, 1, [4], [0, 0, 255, 255]),
        };
        ExceptionDispatchInfo? drawFailure = null;
        var draw = new Thread(() =>
        {
            try
            {
                ThreadSafety.MarkMainThread();
                // Model Draw during Step, while the game thread owns framework admission.
                using (ThreadSafety.NativeFrameworkRenderSyncRoot.EnterScope())
                {
                    var texture = fixture.Textures.CreateFromTexFile(file);
                    try
                    {
                        Assert.Equal(1, texture.Width);
                        Assert.Equal(1, texture.Height);
                        Assert.False(texture.Handle.IsNull);
                    }
                    finally
                    {
                        ((IDeferredDisposable)texture).RealDispose();
                        GC.SuppressFinalize(texture);
                    }
                }
            }
            catch (Exception exception)
            {
                drawFailure = ExceptionDispatchInfo.Capture(exception);
            }
        }) { IsBackground = true };
        var drawStarted = false;
        try
        {
            Assert.True(SpinWait.SpinUntil(() => fixture.BeforeRender.Count == 1, WorkerTimeout));
            draw.Start();
            drawStarted = true;
            Assert.True(draw.Join(WorkerTimeout), "Synchronous TEX creation waited for a loader worker awaiting presentation.");
            drawFailure?.Throw();
            Assert.False(conversion.IsCompleted);
            Assert.Equal(0, conversionCalls);
            Assert.Single(fixture.BeforeRender);
        }
        finally
        {
            // Drain directly so a blocked Draw caller can finish while holding framework admission.
            using (fixture.RenderLock.EnterScope())
                fixture.DrainBeforeRender();
            conversionCancellation.CancelAfter(WorkerTimeout);
            if (drawStarted)
                Assert.True(draw.Join(WorkerTimeout), "The Draw caller did not finish after recovery.");
            try
            {
                conversion.WaitAsync(WorkerTimeout).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Cancellation releases the loader worker if setup fails before its callback is queued.
            }
        }

        Assert.Equal(42, conversion.GetAwaiter().GetResult());
        Assert.Equal(1, conversionCalls);
        Assert.Empty(fixture.BeforeRender);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonOwnerQueuesBothOverloadsUntilPresentationOwnerDrains(bool presentationActive)
    {
        using var fixture = new DispatchFixture();
        using var ownerReady = new ManualResetEventSlim();
        using var drainRequested = new ManualResetEventSlim();
        Exception? ownerFailure = null;
        var ownerThreadId = 0;
        var owner = new Thread(() =>
        {
            try
            {
                ownerThreadId = Environment.CurrentManagedThreadId;
                if (presentationActive)
                {
                    fixture.Present(() =>
                    {
                        ownerReady.Set();
                        Assert.True(drainRequested.Wait(WorkerTimeout));
                        fixture.DrainBeforeRender();
                    });
                }
                else
                {
                    ownerReady.Set();
                    Assert.True(drainRequested.Wait(WorkerTimeout));
                    fixture.Present(fixture.DrainBeforeRender);
                }
            }
            catch (Exception exception)
            {
                ownerFailure = exception;
            }
        }) { IsBackground = true };

        var actionCalls = 0;
        var functionCalls = 0;
        var actionThreadId = 0;
        var functionThreadId = 0;
        Task? actionTask = null;
        Task<int>? functionTask = null;
        owner.Start();
        try
        {
            Assert.True(ownerReady.Wait(WorkerTimeout));
            Assert.False(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
            Assert.False(fixture.RenderLock.IsHeldByCurrentThread);
            actionTask = fixture.Run(() =>
            {
                actionCalls++;
                actionThreadId = Environment.CurrentManagedThreadId;
            });
            functionTask = fixture.Run(() =>
            {
                functionCalls++;
                functionThreadId = Environment.CurrentManagedThreadId;
                return 42;
            });

            Assert.False(
                actionTask.IsCompleted || functionTask.IsCompleted,
                $"Nonowner dispatch completed before owner drain: Action={actionTask.IsCompleted}, Func={functionTask.IsCompleted}.");
            Assert.Equal(0, actionCalls);
            Assert.Equal(0, functionCalls);
            Assert.Equal(2, fixture.BeforeRender.Count);
        }
        finally
        {
            drainRequested.Set();
            Assert.True(owner.Join(WorkerTimeout), "The presentation owner did not finish.");
        }

        Assert.Null(ownerFailure);
        Assert.True(actionTask!.Wait(WorkerTimeout));
        Assert.True(functionTask!.Wait(WorkerTimeout));
        Assert.Equal(42, functionTask.Result);
        Assert.Equal(1, actionCalls);
        Assert.Equal(1, functionCalls);
        Assert.Equal(ownerThreadId, actionThreadId);
        Assert.Equal(ownerThreadId, functionThreadId);
        Assert.Empty(fixture.BeforeRender);
        fixture.Present(fixture.DrainBeforeRender);
        Assert.Equal(1, actionCalls);
        Assert.Equal(1, functionCalls);
    }

    [Fact]
    public void WorkerOwnerRunsInlineAcrossNestedPresentationAndQueuesAfterRelease()
    {
        using var fixture = new DispatchFixture();
        RunOnWorker(() =>
        {
            Assert.False(ThreadSafety.IsMainThread);
            var calls = 0;
            fixture.Present(() =>
            {
                Assert.True(fixture.Run(() => { calls++; }).IsCompletedSuccessfully);
                fixture.Present(() =>
                {
                    var nested = fixture.Run(() => 17);
                    Assert.True(nested.IsCompletedSuccessfully);
                    Assert.Equal(17, nested.GetAwaiter().GetResult());
                });
                Assert.True(fixture.RenderLock.IsHeldByCurrentThread);
                Assert.True(fixture.Run(() => { calls++; }).IsCompletedSuccessfully);
                Assert.Empty(fixture.BeforeRender);
            });

            Assert.Equal(2, calls);
            Assert.False(fixture.RenderLock.IsHeldByCurrentThread);
            Assert.False(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
            var queued = fixture.Run(() => { calls++; });
            Assert.False(queued.IsCompleted);
            Assert.Equal(2, calls);
            fixture.Present(fixture.DrainBeforeRender);
            queued.WaitAsync(WorkerTimeout).GetAwaiter().GetResult();
            Assert.True(queued.IsCompletedSuccessfully);
            Assert.Equal(3, calls);
        });
    }

    [Fact]
    public void FrameworkOwnershipAloneDoesNotAuthorizeInlineTextureWork()
    {
        using var fixture = new DispatchFixture();
        RunOnWorker(() =>
        {
            Task actionTask;
            Task<int> functionTask;
            using (ThreadSafety.NativeFrameworkRenderSyncRoot.EnterScope())
            {
                actionTask = fixture.Run(() => { });
                functionTask = fixture.Run(() => 23);
                Assert.False(actionTask.IsCompleted);
                Assert.False(functionTask.IsCompleted);
                Assert.Equal(2, fixture.BeforeRender.Count);
            }

            fixture.Present(fixture.DrainBeforeRender);
            actionTask.WaitAsync(WorkerTimeout).GetAwaiter().GetResult();
            Assert.True(actionTask.IsCompletedSuccessfully);
            Assert.Equal(23, functionTask.WaitAsync(WorkerTimeout).GetAwaiter().GetResult());
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothOverloadsPropagateFailuresAndAllowFollowingWork(bool inline)
    {
        using var fixture = new DispatchFixture();
        RunOnWorker(() =>
        {
            var actionFailure = new DispatchException();
            var functionFailure = new DispatchException();
            Task? actionTask = null;
            Task<int>? functionTask = null;
            Task<int>? followingTask = null;
            void Dispatch()
            {
                actionTask = fixture.Run((Action)(() => throw actionFailure));
                functionTask = fixture.Run((Func<int>)(() => throw functionFailure));
                followingTask = fixture.Run(() => 31);
            }

            if (inline)
                fixture.Present(Dispatch);
            else
                Dispatch();

            if (!inline)
            {
                Assert.False(actionTask!.IsCompleted);
                Assert.False(functionTask!.IsCompleted);
                fixture.Present(fixture.DrainBeforeRender);
            }

            Assert.Same(actionFailure, Assert.Throws<DispatchException>(() => actionTask!.WaitAsync(WorkerTimeout).GetAwaiter().GetResult()));
            Assert.Same(functionFailure, Assert.Throws<DispatchException>(() => functionTask!.WaitAsync(WorkerTimeout).GetAwaiter().GetResult()));
            Assert.True(actionTask!.IsFaulted);
            Assert.True(functionTask!.IsFaulted);
            Assert.Equal(31, followingTask!.WaitAsync(WorkerTimeout).GetAwaiter().GetResult());
            Assert.Empty(fixture.BeforeRender);
            Assert.False(fixture.RenderLock.IsHeldByCurrentThread);
            Assert.False(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
        });
    }

    private static void RunOnWorker(Action action)
    {
        Exception? workerFailure = null;
        var worker = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                workerFailure = exception;
            }
        }) { IsBackground = true };
        worker.Start();
        Assert.True(worker.Join(WorkerTimeout), "The texture worker did not finish.");
        Assert.Null(workerFailure);
    }

    private sealed class DispatchFixture : IDisposable
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly PresentationTestFixture presentation = new(initialized: true);
        private readonly Func<Action, Task> runAction;
        private readonly Func<Func<int>, Task<int>> runFunction;

        public DispatchFixture()
        {
            this.DrainBeforeRender = typeof(InterfaceManager).GetMethod("PreImGuiRender", PrivateInstance)!
                .CreateDelegate<Action>(this.presentation.Manager);

            var textures = this.Textures = (TextureManager)RuntimeHelpers.GetUninitializedObject(typeof(TextureManager));
            GC.SuppressFinalize(textures);
            typeof(TextureManager).GetField("interfaceManager", PrivateInstance)!.SetValue(textures, this.presentation.Manager);
            var dispatchers = typeof(TextureManager).GetMethods(PrivateInstance).Where(method => method.Name == "RunDuringPresent").ToArray();
            this.runAction = dispatchers.Single(method => !method.IsGenericMethod)
                .CreateDelegate<Func<Action, Task>>(textures);
            this.runFunction = dispatchers.Single(method => method.IsGenericMethod).MakeGenericMethod(typeof(int))
                .CreateDelegate<Func<Func<int>, Task<int>>>(textures);
        }

        public Lock RenderLock => this.presentation.RenderLock;

        public TextureManager Textures { get; }

        public ConcurrentQueue<Action> BeforeRender => this.presentation.BeforeRender;

        public Action DrainBeforeRender { get; }

        public void Dispose() => this.presentation.Dispose();

        public Task Run(Action action) => this.runAction(action);

        public Task<int> Run(Func<int> function) => this.runFunction(function);

        public void Present(Action action)
        {
            // Retain framework ownership so presentation admission succeeds deterministically.
            Assert.True(ThreadSafety.NativeFrameworkRenderSyncRoot.TryEnter(WorkerTimeout));
            try
            {
                var callbackFinished = false;
                ExceptionDispatchInfo? callbackFailure = null;
                this.BeforeRender.Enqueue(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception exception)
                    {
                        callbackFailure = ExceptionDispatchInfo.Capture(exception);
                    }
                    finally
                    {
                        callbackFinished = true;
                    }
                });

                Assert.Throws<NullReferenceException>(() => this.presentation.Present());
                Assert.True(callbackFinished);
                callbackFailure?.Throw();
            }
            finally
            {
                ThreadSafety.NativeFrameworkRenderSyncRoot.Exit();
            }
        }
    }

    /// <summary>Supplies a WARP device for TEX creation without initializing game services.</summary>
    private sealed class ResourceCreationFixture : IDisposable
    {
        private readonly CancellationTokenSource disposeCts = new();
        private readonly nint device;

        public ResourceCreationFixture(TextureManager textures)
        {
            const BindingFlags privateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
            Marshal.ThrowExceptionForHR(D3D11CreateDevice(0, 5, 0, 0, 0, 0, 7, out this.device, out _, out var context));
            Marshal.Release(context);
            typeof(TextureManager).GetField("disposeCts", privateInstance)!.SetValue(textures, this.disposeCts);
            typeof(TextureManager).GetField("dalamudConfiguration", privateInstance)!
                .SetValue(textures, new DalamudConfiguration { UseTexturePluginTracking = false });

            // Write the native pointer inside ComPtr without changing COM reference ownership.
            var writer = new DynamicMethod("SetTexCreationDevice", typeof(void), [typeof(TextureManager), typeof(nint)], typeof(ResourceCreationFixture), true);
            var il = writer.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, typeof(TextureManager).GetField("device", privateInstance)!);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stind_I);
            il.Emit(OpCodes.Ret);
            writer.CreateDelegate<Action<TextureManager, nint>>()(textures, this.device);
        }

        public void Dispose()
        {
            Marshal.Release(this.device);
            this.disposeCts.Dispose();
        }

        [DllImport("d3d11.dll")]
        private static extern int D3D11CreateDevice(nint adapter, int driverType, nint software, uint flags, nint featureLevels, uint featureLevelCount, uint version, out nint device, out uint featureLevel, out nint context);
    }

    private sealed class DispatchException : Exception
    {
    }
}
