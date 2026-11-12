using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Interface.ImGuiBackend;

using Xunit;

namespace Dalamud.Test.Interface;

public class DrawSnapshotResizeTests
{
    [Fact]
    public void RenderBeforeFirstCaptureSkipsRendererAndReleasesLocks()
    {
        using var fixture = new BackendFixture();

        fixture.Backend.Render();
        fixture.Backend.Render();

        fixture.AssertLocksReleased();
    }

    [Fact]
    public void ViewportReadDuringResizeSkipsCallbackAndReleasesLocks()
    {
        using var fixture = new BackendFixture();
        var callbackInvoked = false;

        using (fixture.Backend.BeginResize())
        {
            Assert.False(fixture.Backend.TryReadViewportBackBuffer(1, _ => callbackInvoked = true));
            Assert.False(callbackInvoked);
            fixture.AssertLocksReleased();
        }

        Assert.False(fixture.Backend.IsResizeInProgress);
        fixture.AssertLocksReleased();
    }

    [Fact]
    public void ViewportReadRejectsResizeAnnouncedWhileWaitingForSnapshotExclusion()
    {
        using var fixture = new BackendFixture();
        using var readCompleted = new ManualResetEventSlim();
        using var resizeAcquired = new ManualResetEventSlim();
        using var finishResize = new ManualResetEventSlim();
        var cancellationToken = TestContext.Current.CancellationToken;
        Exception? readException = null;
        Exception? resizeException = null;
        bool? readResult = null;
        var callbackInvoked = false;
        var reader = new Thread(() =>
        {
            try
            {
                readResult = fixture.Backend.TryReadViewportBackBuffer(1, _ => callbackInvoked = true);
            }
            catch (Exception exception)
            {
                readException = exception;
            }
            finally
            {
                readCompleted.Set();
            }
        }) { IsBackground = true };
        var resizer = new Thread(() =>
        {
            try
            {
                using var scope = fixture.Backend.BeginResize();
                resizeAcquired.Set();
                if (!finishResize.Wait(TimeSpan.FromSeconds(5), cancellationToken))
                    throw new TimeoutException("The test did not release the resize scope.");
            }
            catch (Exception exception)
            {
                resizeException = exception;
            }
        }) { IsBackground = true };

        var readerStarted = false;
        var resizerStarted = false;
        try
        {
            fixture.DrawDataLock.EnterWriteLock();
            try
            {
                reader.Start();
                readerStarted = true;
                Assert.True(SpinWait.SpinUntil(() => fixture.DrawDataLock.WaitingReadCount == 1, TimeSpan.FromSeconds(5)));
                resizer.Start();
                resizerStarted = true;
                Assert.True(SpinWait.SpinUntil(() => fixture.DrawDataLock.WaitingWriteCount == 1, TimeSpan.FromSeconds(5)));
                Assert.True(fixture.Backend.IsResizeInProgress);
            }
            finally
            {
                fixture.DrawDataLock.ExitWriteLock();
            }

            Assert.True(resizeAcquired.Wait(TimeSpan.FromSeconds(5), cancellationToken));
            Assert.True(readCompleted.Wait(TimeSpan.FromSeconds(5), cancellationToken));
            Assert.Null(readException);
            Assert.Equal(false, readResult);
            Assert.False(callbackInvoked);
            fixture.AssertLocksReleased();
        }
        finally
        {
            finishResize.Set();
            if (readerStarted)
                Assert.True(reader.Join(TimeSpan.FromSeconds(5)));
            if (resizerStarted)
                Assert.True(resizer.Join(TimeSpan.FromSeconds(5)));
        }

        Assert.Null(resizeException);
        Assert.False(fixture.Backend.IsResizeInProgress);
        using (fixture.Backend.BeginResize())
            Assert.True(fixture.Backend.IsResizeInProgress);
        fixture.AssertLocksReleased();
    }

    [Fact]
    public void RecursiveViewportReadFailsWithoutLeakingRenderExclusion()
    {
        using var fixture = new BackendFixture();
        var callbackInvoked = false;

        fixture.DrawDataLock.EnterReadLock();
        try
        {
            Assert.Throws<LockRecursionException>(
                () => fixture.Backend.TryReadViewportBackBuffer(1, _ => callbackInvoked = true));
        }
        finally
        {
            fixture.DrawDataLock.ExitReadLock();
        }

        Assert.False(callbackInvoked);
        fixture.AssertLocksReleased();
        using (fixture.Backend.BeginResize())
            Assert.True(fixture.Backend.IsResizeInProgress);
        fixture.AssertLocksReleased();
    }

    [Fact]
    public void ResizePreventsReplayUntilAnotherCaptureSucceeds()
    {
        using var fixture = new BackendFixture();

        // Enable replay so the rendererless fixture detects a draw attempt before resize.
        typeof(Dx11Win32Backend).GetField("hasSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(fixture.Backend, true);
        Assert.Throws<NullReferenceException>(fixture.Backend.Render);
        fixture.AssertLocksReleased();

        using (fixture.Backend.BeginResize())
        {
            Assert.True(fixture.Backend.IsResizeInProgress);
            fixture.Backend.Render();
        }

        Assert.False(fixture.Backend.IsResizeInProgress);
        fixture.Backend.Render();
        fixture.Backend.Render();

        fixture.AssertLocksReleased();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedResizeScopesKeepExclusionUntilBothAreDisposed(bool disposeOuterFirst)
    {
        using var fixture = new BackendFixture();
        using var outer = fixture.Backend.BeginResize();
        using var inner = fixture.Backend.BeginResize();

        (disposeOuterFirst ? outer : inner).Dispose();

        Assert.True(fixture.Backend.IsResizeInProgress);
        fixture.Backend.Render();
        fixture.AssertLocksReleased();

        (disposeOuterFirst ? inner : outer).Dispose();

        Assert.False(fixture.Backend.IsResizeInProgress);
        fixture.AssertLocksReleased();
    }

    [Fact]
    public void ResizeScopeCanBeDisposedOnAnotherThread()
    {
        using var fixture = new BackendFixture();
        using var scope = fixture.Backend.BeginResize();

        RunOnWorker(scope.Dispose);

        Assert.False(fixture.Backend.IsResizeInProgress);
        fixture.AssertLocksReleased();

        using (fixture.Backend.BeginResize())
            Assert.True(fixture.Backend.IsResizeInProgress);

        Assert.False(fixture.Backend.IsResizeInProgress);
    }

    [Fact]
    public void DisposingOldScopeAgainCannotReleaseANewerResize()
    {
        using var fixture = new BackendFixture();
        using var previous = fixture.Backend.BeginResize();
        previous.Dispose();
        using var current = fixture.Backend.BeginResize();

        RunOnWorker(previous.Dispose);

        Assert.True(fixture.Backend.IsResizeInProgress);
        fixture.Backend.Render();

        current.Dispose();
        Assert.False(fixture.Backend.IsResizeInProgress);
        fixture.AssertLocksReleased();
    }

    [Fact]
    public void ConcurrentDisposalsCannotReleaseAnotherActiveScope()
    {
        using var fixture = new BackendFixture();
        using var shared = fixture.Backend.BeginResize();
        using var other = fixture.Backend.BeginResize();
        using var ready = new Barrier(2);
        var cancellationToken = TestContext.Current.CancellationToken;
        Exception? workerException = null;
        var worker = new Thread(() =>
        {
            try
            {
                if (!ready.SignalAndWait(TimeSpan.FromSeconds(5), cancellationToken))
                    throw new TimeoutException("The disposing threads did not rendezvous.");
                shared.Dispose();
            }
            catch (Exception exception)
            {
                workerException = exception;
            }
        }) { IsBackground = true };
        worker.Start();

        try
        {
            Assert.True(ready.SignalAndWait(TimeSpan.FromSeconds(5), cancellationToken));
            shared.Dispose();
        }
        finally
        {
            Assert.True(worker.Join(TimeSpan.FromSeconds(5)));
        }

        Assert.Null(workerException);
        Assert.True(fixture.Backend.IsResizeInProgress);
        other.Dispose();
        Assert.False(fixture.Backend.IsResizeInProgress);
        fixture.AssertLocksReleased();
    }

    [Fact]
    public void FailedResizeAcquisitionDoesNotLeavePendingState()
    {
        using var fixture = new BackendFixture();
        fixture.DrawDataLock.EnterReadLock();
        try
        {
            Assert.Throws<LockRecursionException>(() => fixture.Backend.BeginResize());
            Assert.False(fixture.Backend.IsResizeInProgress);
        }
        finally
        {
            fixture.DrawDataLock.ExitReadLock();
        }

        fixture.AssertLocksReleased();
    }

    [Fact]
    public void FailedResizeDisposalPreservesOwnershipUntilItCanBeRetried()
    {
        using var fixture = new BackendFixture();
        using var scope = fixture.Backend.BeginResize();
        fixture.DrawDataLock.EnterReadLock();
        try
        {
            Assert.Throws<LockRecursionException>(scope.Dispose);
            Assert.True(fixture.Backend.IsResizeInProgress);
        }
        finally
        {
            fixture.DrawDataLock.ExitReadLock();
        }

        scope.Dispose();
        Assert.False(fixture.Backend.IsResizeInProgress);
        fixture.AssertLocksReleased();
    }

    [Fact]
    public void AnotherThreadCanBeginResizeWhileAnOuterScopeIsActive()
    {
        using var fixture = new BackendFixture();
        using var outer = fixture.Backend.BeginResize();
        using var acquired = new ManualResetEventSlim();
        using var finish = new ManualResetEventSlim();
        var cancellationToken = TestContext.Current.CancellationToken;
        Exception? workerException = null;
        var worker = new Thread(() =>
        {
            try
            {
                using var inner = fixture.Backend.BeginResize();
                acquired.Set();
                if (!finish.Wait(TimeSpan.FromSeconds(5), cancellationToken))
                    throw new TimeoutException("The test did not release the inner resize scope.");
            }
            catch (Exception exception)
            {
                workerException = exception;
            }
        }) { IsBackground = true };
        worker.Start();

        try
        {
            Assert.True(acquired.Wait(TimeSpan.FromSeconds(5), cancellationToken));
            outer.Dispose();
            Assert.True(fixture.Backend.IsResizeInProgress);
            fixture.Backend.Render();
            fixture.AssertLocksReleased();
        }
        finally
        {
            // Release the outer scope on timeout so a blocked worker can finish.
            outer.Dispose();
            finish.Set();
            Assert.True(worker.Join(TimeSpan.FromSeconds(5)));
        }

        Assert.Null(workerException);
        Assert.False(fixture.Backend.IsResizeInProgress);
    }

    [Fact]
    public void PendingResizeAnnouncesAndWaitsForAnActiveReader()
    {
        using var fixture = new BackendFixture();
        using var acquired = new ManualResetEventSlim();
        using var finish = new ManualResetEventSlim();
        var cancellationToken = TestContext.Current.CancellationToken;
        Exception? workerException = null;
        var worker = new Thread(() =>
        {
            try
            {
                using var scope = fixture.Backend.BeginResize();
                acquired.Set();
                if (!finish.Wait(TimeSpan.FromSeconds(5), cancellationToken))
                    throw new TimeoutException("The test did not release the resize scope.");
            }
            catch (Exception exception)
            {
                workerException = exception;
            }
        }) { IsBackground = true };

        fixture.DrawDataLock.EnterReadLock();
        try
        {
            try
            {
                worker.Start();
                Assert.True(SpinWait.SpinUntil(() => fixture.DrawDataLock.WaitingWriteCount == 1, TimeSpan.FromSeconds(5)));
                Assert.True(fixture.Backend.IsResizeInProgress);
                Assert.False(acquired.IsSet);

                // Pending resize suppresses new work before existing readers drain.
                fixture.Backend.Render();
                var callbackInvoked = false;
                Assert.False(fixture.Backend.TryReadViewportBackBuffer(1, _ => callbackInvoked = true));
                Assert.False(callbackInvoked);
            }
            finally
            {
                fixture.DrawDataLock.ExitReadLock();
            }

            Assert.True(acquired.Wait(TimeSpan.FromSeconds(5), cancellationToken));
            Assert.True(fixture.Backend.IsResizeInProgress);
            fixture.AssertLocksReleased();
        }
        finally
        {
            finish.Set();
            Assert.True(worker.Join(TimeSpan.FromSeconds(5)));
        }

        Assert.Null(workerException);
        Assert.False(fixture.Backend.IsResizeInProgress);
        fixture.AssertLocksReleased();
    }

    private static void RunOnWorker(Action action)
    {
        Exception? workerException = null;
        var worker = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                workerException = exception;
            }
        }) { IsBackground = true };
        worker.Start();
        Assert.True(worker.Join(TimeSpan.FromSeconds(5)));
        Assert.Null(workerException);
    }

    private sealed class BackendFixture : IDisposable
    {
        private readonly Lock renderLock = new();
        private readonly ReaderWriterLockSlim drawDataLock = new(LockRecursionPolicy.NoRecursion);
        private readonly DrawDataSnapshot snapshot = new();

        public BackendFixture()
        {
            // Omit graphics initialization so any draw submission fails while lock coordination remains testable.
            this.Backend = (Dx11Win32Backend)RuntimeHelpers.GetUninitializedObject(typeof(Dx11Win32Backend));
            GC.SuppressFinalize(this.Backend);
            this.SetField("renderLock", this.renderLock);
            this.SetField("drawDataLock", this.drawDataLock);
            this.SetField("snapshot", this.snapshot);
            this.SetField("viewportSnapshots", this.Viewports);
        }

        public Dx11Win32Backend Backend { get; }

        public ViewportSnapshot Viewports { get; } = new();

        public ReaderWriterLockSlim DrawDataLock => this.drawDataLock;

        public void Dispose()
        {
            this.snapshot.Dispose();
            this.Viewports.Dispose();
            this.drawDataLock.Dispose();
        }

        public void AssertLocksReleased()
        {
            Assert.False(this.renderLock.IsHeldByCurrentThread);
            Assert.True(this.renderLock.TryEnter());
            this.renderLock.Exit();
            Assert.False(this.drawDataLock.IsReadLockHeld);
            Assert.False(this.drawDataLock.IsWriteLockHeld);
            Assert.True(this.drawDataLock.TryEnterWriteLock(0));
            this.drawDataLock.ExitWriteLock();
        }

        public void SetField<T>(string name, T value) =>
            typeof(Dx11Win32Backend).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(this.Backend, value);
    }
}
