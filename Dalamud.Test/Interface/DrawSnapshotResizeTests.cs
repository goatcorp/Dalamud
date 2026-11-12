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
    public void ResizePreventsReplayUntilAnotherCaptureSucceeds()
    {
        using var fixture = new BackendFixture();

        // The original implementation had no validity flag; seed it when present so both versions reach
        // the renderer before resize, and the regression is detected by replay behavior afterward.
        typeof(Dx11Win32Backend).GetField("hasSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(fixture.Backend, true);
        Assert.Throws<NullReferenceException>(fixture.Backend.Render);
        fixture.AssertLocksReleased();

        fixture.Backend.EnterResize();
        try
        {
            Assert.True(fixture.Backend.IsResizeInProgress);
            fixture.Backend.Render();
        }
        finally
        {
            fixture.Backend.ExitResize();
        }

        Assert.False(fixture.Backend.IsResizeInProgress);
        fixture.Backend.Render();
        fixture.Backend.Render();

        fixture.AssertLocksReleased();
    }

    private sealed class BackendFixture : IDisposable
    {
        private readonly Lock renderLock = new();
        private readonly ReaderWriterLockSlim drawDataLock = new(LockRecursionPolicy.NoRecursion);
        private readonly DrawDataSnapshot snapshot = new();

        public BackendFixture()
        {
            // Exercise actual resize/render coordination without constructing a D3D device or ImGui context.
            // The absent renderer makes any attempt to submit an invalid snapshot fail the test.
            this.Backend = (Dx11Win32Backend)RuntimeHelpers.GetUninitializedObject(typeof(Dx11Win32Backend));
            GC.SuppressFinalize(this.Backend);
            this.SetField("renderLock", this.renderLock);
            this.SetField("drawDataLock", this.drawDataLock);
            this.SetField("snapshot", this.snapshot);
            this.SetField("viewportSnapshots", this.Viewports);
        }

        public Dx11Win32Backend Backend { get; }

        public ViewportSnapshot Viewports { get; } = new();

        public void Dispose()
        {
            this.snapshot.Dispose();
            this.Viewports.Dispose();
            this.drawDataLock.Dispose();
        }

        public void AssertLocksReleased()
        {
            Assert.False(this.renderLock.IsHeldByCurrentThread);
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
