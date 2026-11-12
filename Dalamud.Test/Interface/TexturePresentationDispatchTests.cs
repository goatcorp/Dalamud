using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.ImGuiBackend;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Utility;

using Xunit;

namespace Dalamud.Test.Interface;

[Collection("Native framework render synchronization")]
public class TexturePresentationDispatchTests
{
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonOwnerQueuesBothOverloadsUntilPresentationOwnerDrains(bool presentationActive)
    {
        var fixture = new DispatchFixture();
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
        var fixture = new DispatchFixture();
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
        var fixture = new DispatchFixture();
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
        var fixture = new DispatchFixture();
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

    private sealed class DispatchFixture
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly InterfaceManager manager;
        private readonly Action<IImGuiBackend> draw;
        private readonly Func<Action, Task> runAction;
        private readonly Func<Func<int>, Task<int>> runFunction;

        public DispatchFixture()
        {
            // Initialize the dispatcher and queue without starting game services or D3D.
            this.manager = (InterfaceManager)RuntimeHelpers.GetUninitializedObject(typeof(InterfaceManager));
            GC.SuppressFinalize(this.manager);
            typeof(InterfaceManager).GetField("renderDalamudLock", PrivateInstance)!.SetValue(this.manager, this.RenderLock);
            typeof(InterfaceManager).GetField("runBeforeImGuiRender", PrivateInstance)!.SetValue(this.manager, this.BeforeRender);
            this.draw = typeof(InterfaceManager).GetMethod("RenderDalamudDraw", PrivateInstance)!
                .CreateDelegate<Action<IImGuiBackend>>(this.manager);
            this.DrainBeforeRender = typeof(InterfaceManager).GetMethod("PreImGuiRender", PrivateInstance)!
                .CreateDelegate<Action>(this.manager);

            var textures = (TextureManager)RuntimeHelpers.GetUninitializedObject(typeof(TextureManager));
            GC.SuppressFinalize(textures);
            typeof(TextureManager).GetField("interfaceManager", PrivateInstance)!.SetValue(textures, this.manager);
            var dispatchers = typeof(TextureManager).GetMethods(PrivateInstance).Where(method => method.Name == "RunDuringPresent").ToArray();
            this.runAction = dispatchers.Single(method => !method.IsGenericMethod)
                .CreateDelegate<Func<Action, Task>>(textures);
            this.runFunction = dispatchers.Single(method => method.IsGenericMethod).MakeGenericMethod(typeof(int))
                .CreateDelegate<Func<Func<int>, Task<int>>>(textures);
        }

        public Lock RenderLock { get; } = new();

        public ConcurrentQueue<Action> BeforeRender { get; } = new();

        public Action DrainBeforeRender { get; }

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

                // Unset configuration fails after queue dispatch, exercising guard release on failure.
                Assert.Throws<NullReferenceException>(() => this.draw(null!));
                Assert.True(callbackFinished);
                callbackFailure?.Throw();
            }
            finally
            {
                ThreadSafety.NativeFrameworkRenderSyncRoot.Exit();
            }
        }
    }

    private sealed class DispatchException : Exception
    {
    }
}
