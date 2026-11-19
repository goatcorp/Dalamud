using System;
using System.Threading;

using Dalamud.Interface.Internal;

using Xunit;

namespace Dalamud.Test.Interface;

public class InterfaceCallbackLifetimeTests
{
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PendingObservation = TimeSpan.FromMilliseconds(100);

    [Fact]
    public void DrainWaitsThroughRenderingAndNativeContinuation()
    {
        var lifetime = new InterfaceCallbackLifetime();
        using var renderEntered = new ManualResetEventSlim();
        using var releaseRender = new ManualResetEventSlim();
        using var originalEntered = new ManualResetEventSlim();
        using var releaseOriginal = new ManualResetEventSlim();
        using var drainStarted = new ManualResetEventSlim();
        using var drained = new ManualResetEventSlim();
        var callback = new Worker(() =>
        {
            using var scope = lifetime.Enter();
            Assert.True(scope.IsTracked);
            Assert.True(scope.CanUseBackend);
            renderEntered.Set();
            Assert.True(releaseRender.Wait(WorkerTimeout));

            // Simulate Original() continuing after render locks have been released.
            originalEntered.Set();
            Assert.True(releaseOriginal.Wait(WorkerTimeout));
        });
        Worker? cleanup = null;
        try
        {
            Assert.True(renderEntered.Wait(WorkerTimeout));
            lifetime.Stop();
            cleanup = new Worker(() =>
            {
                drainStarted.Set();
                lifetime.WaitForDrain();
                drained.Set();
            });
            Assert.True(drainStarted.Wait(WorkerTimeout));
            Assert.False(drained.Wait(PendingObservation));

            releaseRender.Set();
            Assert.True(originalEntered.Wait(WorkerTimeout));
            Assert.False(drained.Wait(PendingObservation));

            releaseOriginal.Set();
            Assert.True(drained.Wait(WorkerTimeout));
        }
        finally
        {
            releaseRender.Set();
            releaseOriginal.Set();
            callback.Join();
            cleanup?.Join();
        }
    }

    [Fact]
    public void CallbackEnteringAfterStopStillProtectsNativeContinuation()
    {
        var lifetime = new InterfaceCallbackLifetime();
        lifetime.Stop();
        using var callbackEntered = new ManualResetEventSlim();
        using var releaseCallback = new ManualResetEventSlim();
        using var drainStarted = new ManualResetEventSlim();
        using var drained = new ManualResetEventSlim();
        var callback = new Worker(() =>
        {
            using var scope = lifetime.Enter();
            Assert.True(scope.IsTracked);
            Assert.False(scope.CanUseBackend);
            callbackEntered.Set();
            Assert.True(releaseCallback.Wait(WorkerTimeout));
        });
        Worker? cleanup = null;
        try
        {
            Assert.True(callbackEntered.Wait(WorkerTimeout));
            cleanup = new Worker(() =>
            {
                drainStarted.Set();
                lifetime.WaitForDrain();
                drained.Set();
            });
            Assert.True(drainStarted.Wait(WorkerTimeout));
            Assert.False(drained.Wait(PendingObservation));
            releaseCallback.Set();
            Assert.True(drained.Wait(WorkerTimeout));
        }
        finally
        {
            releaseCallback.Set();
            callback.Join();
            cleanup?.Join();
        }
    }

    [Fact]
    public void LateCallbacksCannotAccessDisposedBackendOrHookResources()
    {
        var lifetime = new InterfaceCallbackLifetime();
        lifetime.Stop();
        lifetime.WaitForDrain();

        using var lateCallback = lifetime.Enter();
        Assert.False(lateCallback.IsTracked);
        Assert.False(lateCallback.CanUseBackend);

        // Untracked entries must not hold up subsequent drain requests.
        lifetime.Stop();
        lifetime.WaitForDrain();
    }

    [Fact]
    public void NestedCallbacksRemainTrackedUntilTheOuterContinuationReturns()
    {
        var lifetime = new InterfaceCallbackLifetime();
        using var nestedReturned = new ManualResetEventSlim();
        using var releaseOuter = new ManualResetEventSlim();
        using var drainStarted = new ManualResetEventSlim();
        using var drained = new ManualResetEventSlim();
        var callback = new Worker(() =>
        {
            using var outer = lifetime.Enter();
            using (var inner = lifetime.Enter())
            {
                Assert.True(outer.IsTracked && inner.IsTracked);
                Assert.True(outer.CanUseBackend && inner.CanUseBackend);
            }

            nestedReturned.Set();
            Assert.True(releaseOuter.Wait(WorkerTimeout));
        });
        Worker? cleanup = null;
        try
        {
            Assert.True(nestedReturned.Wait(WorkerTimeout));
            lifetime.Stop();
            cleanup = new Worker(() =>
            {
                drainStarted.Set();
                lifetime.WaitForDrain();
                drained.Set();
            });
            Assert.True(drainStarted.Wait(WorkerTimeout));
            Assert.False(drained.Wait(PendingObservation));
            releaseOuter.Set();
            Assert.True(drained.Wait(WorkerTimeout));
        }
        finally
        {
            releaseOuter.Set();
            callback.Join();
            cleanup?.Join();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExceptionReleasesCallbacksBeforeOrAfterStop(bool stopBeforeException)
    {
        var lifetime = new InterfaceCallbackLifetime();
        var failure = new CallbackException();
        Assert.Same(failure, Assert.Throws<CallbackException>((Action)(() =>
        {
            using var outer = lifetime.Enter();
            if (stopBeforeException)
                lifetime.Stop();
            using var inner = lifetime.Enter();
            Assert.True(inner.IsTracked);
            Assert.Equal(!stopBeforeException, inner.CanUseBackend);
            throw failure;
        })));

        lifetime.Stop();
        var cleanup = new Worker(lifetime.WaitForDrain);
        cleanup.Join();
        using var lateCallback = lifetime.Enter();
        Assert.False(lateCallback.IsTracked);
        Assert.False(lateCallback.CanUseBackend);
    }

    [Fact]
    public void RepeatedStopAndDrainKeepAdmissionClosed()
    {
        var lifetime = new InterfaceCallbackLifetime();
        lifetime.Stop();
        lifetime.Stop();
        lifetime.WaitForDrain();
        lifetime.WaitForDrain();
        lifetime.Stop();
        using var scope = lifetime.Enter();
        Assert.False(scope.IsTracked);
        Assert.False(scope.CanUseBackend);
    }

    private sealed class Worker
    {
        private readonly Thread thread;
        private Exception? failure;

        public Worker(Action action)
        {
            this.thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    this.failure = exception;
                }
            }) { IsBackground = true };
            this.thread.Start();
        }

        public void Join()
        {
            Assert.True(this.thread.Join(WorkerTimeout), "The callback or shutdown worker did not finish.");
            Assert.Null(this.failure);
        }
    }

    private sealed class CallbackException : Exception
    {
    }
}
