using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Interface.ImGuiBackend;
using Dalamud.Interface.Internal;
using Dalamud.Utility;

using Xunit;

namespace Dalamud.Test.Utility;

public class NativePresentationScopeTests
{
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReleasesAllOwnershipDuringPresentationAndRestoresExactDepth(bool throws)
    {
        var syncRoot = ThreadSafety.NativeFrameworkRenderSyncRoot;
        syncRoot.Enter();
        syncRoot.Enter();
        syncRoot.Enter();

        try
        {
            Assert.False(CanWorkerAcquireLock());

            var exceptionObserved = false;
            try
            {
                using var presentation = ThreadSafety.EnterNativePresentation();
                Assert.False(syncRoot.IsHeldByCurrentThread);
                Assert.True(CanWorkerAcquireLock());

                if (throws)
                    throw new PresentationException();
            }
            catch (PresentationException)
            {
                exceptionObserved = true;
            }

            Assert.Equal(throws, exceptionObserved);
            for (var remaining = 3; remaining > 0; remaining--)
            {
                Assert.True(syncRoot.IsHeldByCurrentThread);
                Assert.False(CanWorkerAcquireLock());
                syncRoot.Exit();
            }

            Assert.False(syncRoot.IsHeldByCurrentThread);
            Assert.True(CanWorkerAcquireLock());
        }
        finally
        {
            ReleaseCurrentThreadOwnership();
        }
    }

    [Fact]
    public void UnownedPresentationLeavesAnotherThreadsOwnershipUntouched()
    {
        var syncRoot = ThreadSafety.NativeFrameworkRenderSyncRoot;
        using var ownerAcquired = new ManualResetEventSlim();
        using var releaseOwner = new ManualResetEventSlim();
        Exception? workerFailure = null;
        var owner = new Thread(() =>
        {
            var acquired = false;
            try
            {
                acquired = syncRoot.TryEnter(WorkerTimeout);
                if (!acquired)
                    throw new TimeoutException("The owner could not acquire the synchronization lock.");

                ownerAcquired.Set();
                if (!releaseOwner.Wait(WorkerTimeout))
                    throw new TimeoutException("The test did not release the owner thread.");
            }
            catch (Exception exception)
            {
                workerFailure = exception;
            }
            finally
            {
                if (acquired)
                    syncRoot.Exit();
            }
        }) { IsBackground = true };

        owner.Start();
        try
        {
            Assert.True(ownerAcquired.Wait(WorkerTimeout));
            Assert.False(syncRoot.IsHeldByCurrentThread);
            Assert.False(CanWorkerAcquireLock());

            using (ThreadSafety.EnterNativePresentation())
            {
                Assert.False(syncRoot.IsHeldByCurrentThread);
                Assert.False(CanWorkerAcquireLock());
            }

            Assert.False(syncRoot.IsHeldByCurrentThread);
            Assert.False(CanWorkerAcquireLock());
        }
        finally
        {
            releaseOwner.Set();
            ReleaseCurrentThreadOwnership();
            Assert.True(owner.Join(WorkerTimeout), "The owner thread did not finish.");
        }

        Assert.Null(workerFailure);
        Assert.True(CanWorkerAcquireLock());
    }

    [Fact]
    public void NestedPresentationsRestoreOnlyTheirOwnOwnership()
    {
        var syncRoot = ThreadSafety.NativeFrameworkRenderSyncRoot;
        syncRoot.Enter();
        syncRoot.Enter();

        try
        {
            using (ThreadSafety.EnterNativePresentation())
            {
                using (ThreadSafety.EnterNativePresentation())
                {
                    Assert.False(syncRoot.IsHeldByCurrentThread);
                    Assert.True(CanWorkerAcquireLock());
                }

                Assert.False(syncRoot.IsHeldByCurrentThread);
                // Reacquire inside the outer scope to give the next nested scope its own saved depth.
                syncRoot.Enter();
                try
                {
                    using (ThreadSafety.EnterNativePresentation())
                    {
                        Assert.False(syncRoot.IsHeldByCurrentThread);
                        Assert.True(CanWorkerAcquireLock());
                    }

                    Assert.True(syncRoot.IsHeldByCurrentThread);
                    Assert.False(CanWorkerAcquireLock());
                }
                finally
                {
                    ReleaseCurrentThreadOwnership();
                }

                Assert.False(syncRoot.IsHeldByCurrentThread);
            }

            Assert.True(syncRoot.IsHeldByCurrentThread);
            syncRoot.Exit();
            Assert.True(syncRoot.IsHeldByCurrentThread);
            syncRoot.Exit();
            Assert.False(syncRoot.IsHeldByCurrentThread);
            Assert.True(CanWorkerAcquireLock());
        }
        finally
        {
            ReleaseCurrentThreadOwnership();
        }
    }

    [Fact]
    public void ContendedPresentationReturnsWithoutEnteringRenderTransaction()
    {
        var syncRoot = ThreadSafety.NativeFrameworkRenderSyncRoot;
        var (manager, draw, _) = CreateRenderFixture();
        Exception? workerFailure = null;
        var worker = new Thread(() =>
        {
            try
            {
                // Uninitialized rendering state detects any work performed by a rejected callback.
                draw(null!);
            }
            catch (Exception exception)
            {
                workerFailure = exception;
            }
        }) { IsBackground = true };

        syncRoot.Enter();
        try
        {
            worker.Start();
            Assert.True(worker.Join(WorkerTimeout), "A contended presentation callback must return while native update still owns the framework lock.");
            Assert.Null(workerFailure);
            Assert.True(syncRoot.IsHeldByCurrentThread);
            Assert.Equal(0, manager.CumulativePresentCalls);
            Assert.False(manager.IsAnyThreadInPresent);
        }
        finally
        {
            ReleaseCurrentThreadOwnership();
            // Release a blocked worker even when the nonblocking assertion fails.
            Assert.True(worker.Join(WorkerTimeout), "The presentation worker did not finish after cleanup.");
        }

        Assert.True(CanWorkerAcquireLock());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void PresentationFailurePreservesCallingThreadsFrameworkOwnership(int recursionDepth)
    {
        var syncRoot = ThreadSafety.NativeFrameworkRenderSyncRoot;
        var (manager, draw, renderLock) = CreateRenderFixture();
        for (var depth = 0; depth < recursionDepth; depth++)
            syncRoot.Enter();

        try
        {
            // A missing queue fails after both guards are entered, before native ImGui access.
            Assert.Throws<NullReferenceException>(() => draw(null!));
            Assert.Equal(1, manager.CumulativePresentCalls);
            Assert.False(manager.IsAnyThreadInPresent);
            Assert.False(renderLock.IsHeldByCurrentThread);

            for (var remaining = recursionDepth; remaining > 0; remaining--)
            {
                Assert.True(syncRoot.IsHeldByCurrentThread);
                Assert.False(CanWorkerAcquireLock());
                syncRoot.Exit();
            }

            Assert.False(syncRoot.IsHeldByCurrentThread);
            Assert.True(CanWorkerAcquireLock());
        }
        finally
        {
            ReleaseCurrentThreadOwnership();
        }
    }

    private static (InterfaceManager Manager, Action<IImGuiBackend> Draw, Lock RenderLock) CreateRenderFixture()
    {
        // Initialize only the render guard; other state stays unset to detect access after admission.
        var manager = (InterfaceManager)RuntimeHelpers.GetUninitializedObject(typeof(InterfaceManager));
        GC.SuppressFinalize(manager);
        var renderLock = new Lock();
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        SetRenderField(manager, "renderDalamudLock", renderLock);
        var draw = typeof(InterfaceManager).GetMethod("RenderDalamudDraw", flags)!
            .CreateDelegate<Action<IImGuiBackend>>(manager);
        return (manager, draw, renderLock);
    }

    private static void SetRenderField<T>(InterfaceManager manager, string name, T value) =>
        typeof(InterfaceManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(manager, value);

    private static bool CanWorkerAcquireLock()
    {
        var acquired = false;
        Exception? workerFailure = null;
        // A separate thread prevents recursive ownership from making the lock appear available.
        var worker = new Thread(() =>
        {
            try
            {
                acquired = ThreadSafety.NativeFrameworkRenderSyncRoot.TryEnter();
                if (acquired)
                    ThreadSafety.NativeFrameworkRenderSyncRoot.Exit();
            }
            catch (Exception exception)
            {
                workerFailure = exception;
            }
        }) { IsBackground = true };

        worker.Start();
        Assert.True(worker.Join(WorkerTimeout), "The lock probe thread did not finish.");
        Assert.Null(workerFailure);
        return acquired;
    }

    private static void ReleaseCurrentThreadOwnership()
    {
        while (ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread)
            ThreadSafety.NativeFrameworkRenderSyncRoot.Exit();
    }

    private sealed class PresentationException : Exception
    {
    }
}
