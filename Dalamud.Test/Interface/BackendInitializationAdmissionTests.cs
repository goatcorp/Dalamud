using System;
using System.Threading;

using Dalamud.Utility;

using TerraFX.Interop.DirectX;

using Xunit;

namespace Dalamud.Test.Interface;

[Collection("Native framework render synchronization")]
public class BackendInitializationAdmissionTests
{
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void OverlappingFirstPresentationsEnterOnlyOneConstructor()
    {
        using var fixture = new PresentationTestFixture();
        using var constructorEntered = new ManualResetEventSlim();
        using var releaseConstructor = new ManualResetEventSlim();
        fixture.DuringConstruction = () =>
        {
            Assert.True(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
            Assert.True(fixture.RenderLock.IsHeldByCurrentThread);
            constructorEntered.Set();
            Assert.True(releaseConstructor.Wait(WorkerTimeout), "The first constructor was not released.");
        };

        Exception? firstFailure = null;
        var first = new Thread(() =>
        {
            try
            {
                fixture.Present();
            }
            catch (Exception exception)
            {
                firstFailure = exception;
            }
        }) { IsBackground = true };
        first.Start();
        try
        {
            Assert.True(constructorEntered.Wait(WorkerTimeout), "The first presentation did not enter construction.");
            var descriptorCalls = fixture.DescriptorCalls;
            fixture.Present();
            Assert.Equal(descriptorCalls, fixture.DescriptorCalls);
            Assert.Equal(1, fixture.ConstructorCalls);
            Assert.Null(fixture.Manager.Backend);
            Assert.Equal(0, fixture.Manager.CumulativePresentCalls);
        }
        finally
        {
            releaseConstructor.Set();
            Assert.True(first.Join(WorkerTimeout), "The initializer did not finish.");
        }

        fixture.CallbackFailure?.Throw();
        Assert.NotNull(firstFailure);
        Assert.Equal(PresentationTestFixture.InitializationError, firstFailure.HResult);
        Assert.Equal(1, fixture.ReferenceCount);
        Assert.Null(fixture.Manager.Backend);

        // A failed constructor must release ownership and allow retry.
        fixture.DuringConstruction = null;
        AssertInitializationFails(fixture);
        Assert.Equal(2, fixture.ConstructorCalls);
        Assert.Equal(1, fixture.ReferenceCount);
    }

    [Fact]
    public void DeviceOwningCallbackSkipsInitializationWhileFrameworkAdmissionIsUnavailable()
    {
        using var fixture = new PresentationTestFixture();
        var deviceGate = new Lock();
        var queuedWorkRan = false;
        fixture.BeforeRender.Enqueue(() => queuedWorkRan = true);
        using var callbackReturned = new ManualResetEventSlim();
        Exception? workerFailure = null;
        var worker = new Thread(() =>
        {
            try
            {
                // Model an incoming middleware callback that already owns the device critical section.
                using (deviceGate.EnterScope())
                    fixture.Present();
            }
            catch (Exception exception)
            {
                workerFailure = exception;
            }
            finally
            {
                callbackReturned.Set();
            }
        }) { IsBackground = true };

        try
        {
            using (ThreadSafety.NativeFrameworkRenderSyncRoot.EnterScope())
            {
                worker.Start();
                Assert.True(callbackReturned.Wait(WorkerTimeout), "Presentation waited on framework admission while owning the device gate.");
                Assert.Equal(0, fixture.DescriptorCalls);
                Assert.Equal(0, fixture.ConstructorCalls);
                Assert.Equal(0, fixture.Manager.CumulativePresentCalls);
                Assert.False(queuedWorkRan);
                Assert.Single(fixture.BeforeRender);
            }
        }
        finally
        {
            // Join after framework ownership is released, including on assertion failure.
            Assert.True(worker.Join(WorkerTimeout), "The presentation worker did not finish after framework release.");
        }

        Assert.Null(workerFailure);
        Assert.Null(fixture.Manager.Backend);
    }

    [Fact]
    public void ReentrantPresentationDoesNotConstructAnotherBackend()
    {
        using var fixture = new PresentationTestFixture();
        fixture.DuringConstruction = () =>
        {
            if (fixture.ConstructorCalls == 1)
                fixture.Present();
            Assert.Equal(1, fixture.ConstructorCalls);
            Assert.Null(fixture.Manager.Backend);
            Assert.True(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
            Assert.True(fixture.RenderLock.IsHeldByCurrentThread);
        };

        AssertInitializationFails(fixture);
        fixture.CallbackFailure?.Throw();
        Assert.Equal(1, fixture.ConstructorCalls);
        Assert.Equal(1, fixture.ReferenceCount);
        Assert.False(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
        Assert.False(fixture.RenderLock.IsHeldByCurrentThread);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void InitializationFailureRestoresCallingThreadsFrameworkRecursion(int recursionDepth)
    {
        using var fixture = new PresentationTestFixture();
        var frameworkGate = ThreadSafety.NativeFrameworkRenderSyncRoot;
        for (var i = 0; i < recursionDepth; i++)
            frameworkGate.Enter();

        try
        {
            AssertInitializationFails(fixture);
            Assert.Null(fixture.Manager.Backend);
            Assert.False(fixture.RenderLock.IsHeldByCurrentThread);
            Assert.Equal(0, fixture.Manager.CumulativePresentCalls);
            Assert.Equal(1, fixture.ReferenceCount);
            for (var i = 0; i < recursionDepth; i++)
            {
                Assert.True(frameworkGate.IsHeldByCurrentThread);
                frameworkGate.Exit();
            }

            Assert.False(frameworkGate.IsHeldByCurrentThread);
            AssertInitializationFails(fixture);
            Assert.Equal(2, fixture.ConstructorCalls);
        }
        finally
        {
            while (frameworkGate.IsHeldByCurrentThread)
                frameworkGate.Exit();
        }
    }

    [Fact]
    public void TestPresentationAndForeignTargetDoNotInitializeOrDispatchWork()
    {
        using var fixture = new PresentationTestFixture();
        fixture.BeforeRender.Enqueue(() => throw new InvalidOperationException("Ineligible presentation dispatched queued work."));

        fixture.Present(DXGI.DXGI_PRESENT_TEST);
        Assert.Equal(0, fixture.DescriptorCalls);
        fixture.PresentForeignTarget();

        Assert.Equal(0, fixture.ConstructorCalls);
        Assert.Null(fixture.Manager.Backend);
        Assert.Equal(0, fixture.Manager.CumulativePresentCalls);
        Assert.Single(fixture.BeforeRender);
        Assert.False(ThreadSafety.NativeFrameworkRenderSyncRoot.IsHeldByCurrentThread);
        Assert.False(fixture.RenderLock.IsHeldByCurrentThread);
    }

    private static void AssertInitializationFails(PresentationTestFixture fixture)
    {
        var failure = Assert.ThrowsAny<Exception>(() => fixture.Present());
        Assert.Equal(PresentationTestFixture.InitializationError, failure.HResult);
    }
}
