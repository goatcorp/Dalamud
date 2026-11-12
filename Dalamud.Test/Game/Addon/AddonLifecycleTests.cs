using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.NativeWrapper;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Xunit;

namespace Dalamud.Test.Game.Addon;

public class AddonLifecycleTests
{
    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    [InlineData(false, 2)]
    [InlineData(true, 3)]
    [InlineData(false, 3)]
    [InlineData(true, 4)]
    [InlineData(false, 4)]
    public void NestedDispatchKeepsOuterRegistrationDeferred(bool globalListener, int nestedCase)
    {
        using var fixture = new LifecycleFixture();
        var addonName = globalListener ? string.Empty : LifecycleFixture.AddonName;
        var originalCalls = 0;
        var addedCalls = 0;
        var requestedRegistration = false;
        var addedVisibleDuringDispatch = false;
        var originalOperationReached = false;
        var added = fixture.Listener(AddonEvent.PreDraw, addonName, (_, _) => addedCalls++);

        // Cover missing/mismatched listeners, normal dispatch, recursion, and listener failure.
        switch (nestedCase)
        {
            case 1:
                fixture.Register(fixture.Listener(AddonEvent.PreHide, "OtherAddon", (_, _) => { }));
                break;
            case 2:
                fixture.Register(fixture.Listener(AddonEvent.PreHide, addonName, (_, _) => { }));
                break;
            case 3:
                fixture.Register(fixture.Listener(AddonEvent.PreHide, addonName, (_, _) => fixture.Dispatch(AddonEvent.PostHide)));
                break;
            case 4:
                fixture.Register(fixture.Listener(AddonEvent.PreHide, addonName, (_, _) => throw new ListenerException()));
                break;
        }

        void OnOriginalListener(AddonEvent _, AddonArgs args)
        {
            originalCalls++;
            if (requestedRegistration)
                return;

            requestedRegistration = true;
            fixture.Dispatch(AddonEvent.PreHide);
            fixture.Register(added);
            addedVisibleDuringDispatch = fixture.Contains(added);
        }

        fixture.Register(fixture.Listener(AddonEvent.PreDraw, addonName, OnOriginalListener));
        fixture.Register(fixture.Listener(AddonEvent.PreDraw, addonName, OnOriginalListener));

        var failure = Record.Exception(() =>
        {
            fixture.Dispatch(AddonEvent.PreDraw);
            originalOperationReached = true;
        });

        Assert.Null(failure);
        Assert.True(originalOperationReached);
        Assert.Equal(2, originalCalls);
        Assert.False(addedVisibleDuringDispatch);
        Assert.False(fixture.Contains(added));
        Assert.Equal(0, addedCalls);

        fixture.RunQueuedTasks();
        Assert.True(fixture.Contains(added));
        Assert.Equal(0, addedCalls);

        fixture.Dispatch(AddonEvent.PreDraw);
        Assert.Equal(4, originalCalls);
        Assert.Equal(1, addedCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyTopLevelDispatchAllowsImmediateRegistration(bool eventHasOtherAddonListeners)
    {
        using var fixture = new LifecycleFixture();
        if (eventHasOtherAddonListeners)
            fixture.Register(fixture.Listener(AddonEvent.PreHide, "OtherAddon", (_, _) => { }));

        fixture.Dispatch(AddonEvent.PreHide);

        var calls = 0;
        var listener = fixture.Listener(AddonEvent.PreDraw, LifecycleFixture.AddonName, (_, _) => calls++);
        fixture.Register(listener);
        Assert.True(fixture.Contains(listener));
        fixture.Dispatch(AddonEvent.PreDraw);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NestedDispatchDefersRemovalAndImmediatelySuppressesRequestedListener(bool globalListener)
    {
        using var fixture = new LifecycleFixture();
        var addonName = globalListener ? string.Empty : LifecycleFixture.AddonName;
        var removedCalls = 0;
        var stillRegisteredDuringDispatch = false;
        var markedDuringDispatch = false;
        var removed = fixture.Listener(AddonEvent.PreDraw, addonName, (_, _) => removedCalls++);

        fixture.Register(fixture.Listener(AddonEvent.PreDraw, addonName, (_, _) =>
        {
            fixture.Dispatch(AddonEvent.PreHide);
            fixture.Unregister(removed);
            stillRegisteredDuringDispatch = fixture.Contains(removed);
            markedDuringDispatch = removed.IsRequestedToClear;
        }));
        fixture.Register(removed);

        fixture.Dispatch(AddonEvent.PreDraw);

        Assert.True(stillRegisteredDuringDispatch);
        Assert.True(markedDuringDispatch);
        Assert.True(fixture.Contains(removed));
        Assert.Equal(0, removedCalls);
        fixture.RunQueuedTasks();
        Assert.False(fixture.Contains(removed));
        Assert.Equal(0, removedCalls);
    }

    [Fact]
    public void ThrowingListenerDoesNotStopDispatchOrLeaveLaterRegistrationDeferred()
    {
        using var fixture = new LifecycleFixture();
        var followingCalls = 0;
        fixture.Register(fixture.Listener(AddonEvent.PreDraw, string.Empty, (_, _) => throw new ListenerException()));
        fixture.Register(fixture.Listener(AddonEvent.PreDraw, string.Empty, (_, _) => followingCalls++));

        Assert.Null(Record.Exception(() => fixture.Dispatch(AddonEvent.PreDraw)));
        Assert.Equal(1, followingCalls);

        var listener = fixture.Listener(AddonEvent.PreHide, LifecycleFixture.AddonName, (_, _) => { });
        fixture.Register(listener);
        Assert.True(fixture.Contains(listener));
    }

    private sealed class LifecycleFixture : IDisposable
    {
        public const string AddonName = "TestAddon";

        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly CancellationTokenSource frameworkDestroy = new();
        private readonly CancellationTokenSource frameworkDestroyed = new();
        private readonly ThreadBoundTaskScheduler scheduler = new(Thread.CurrentThread);
        private readonly AddonLifecycle lifecycle;
        private readonly AddonArgs args;
        private readonly nint addon;

        public LifecycleFixture()
        {
            // Initialize dispatch and framework scheduling without installing game hooks.
            var framework = (Framework)RuntimeHelpers.GetUninitializedObject(typeof(Framework));
            SetField(framework, "frameworkDestroy", this.frameworkDestroy);
            SetField(framework, "frameworkDestroyed", this.frameworkDestroyed);
            SetField(framework, "frameworkThreadTaskScheduler", this.scheduler);
            SetField(framework, "<FrameworkThreadTaskFactory>k__BackingField", new TaskFactory(
                this.frameworkDestroyed.Token,
                TaskCreationOptions.None,
                TaskContinuationOptions.None,
                this.scheduler));

            this.lifecycle = (AddonLifecycle)RuntimeHelpers.GetUninitializedObject(typeof(AddonLifecycle));
            SetField(this.lifecycle, "framework", framework);
            SetField(this.lifecycle, "<EventListeners>k__BackingField", new Dictionary<AddonEvent, Dictionary<string, HashSet<AddonLifecycleEventListener>>>());

            // Dispatch reads the name buffer from this zero-initialized native addon.
            var addonSize = Marshal.SizeOf<AtkUnitBase>();
            this.addon = Marshal.AllocHGlobal(addonSize);
            Marshal.Copy(new byte[addonSize], 0, this.addon, addonSize);
            var name = Encoding.UTF8.GetBytes(AddonName);
            Marshal.Copy(name, 0, this.addon + (int)Marshal.OffsetOf<AtkUnitBase>("_name"), name.Length);
            this.args = new AddonArgs { Addon = new AtkUnitBasePtr(this.addon), AddonName = AddonName };
        }

        public AddonLifecycleEventListener Listener(AddonEvent eventType, string addonName, IAddonLifecycle.AddonEventDelegate callback)
            => new(eventType, addonName, callback);

        public void Register(AddonLifecycleEventListener listener) => this.lifecycle.RegisterListener(listener);

        public void Unregister(AddonLifecycleEventListener listener) => this.lifecycle.UnregisterListener(listener);

        public void Dispatch(AddonEvent eventType) => this.lifecycle.InvokeListenersSafely(eventType, this.args);

        public bool Contains(AddonLifecycleEventListener listener)
            => this.lifecycle.EventListeners.TryGetValue(listener.EventType, out var addons)
               && addons.TryGetValue(listener.AddonName, out var listeners)
               && listeners.Contains(listener);

        public void RunQueuedTasks() => this.scheduler.Run();

        public void Dispose()
        {
            Marshal.FreeHGlobal(this.addon);
            this.frameworkDestroy.Dispose();
            this.frameworkDestroyed.Dispose();
        }

        private static void SetField(object instance, string name, object value)
            => instance.GetType().GetField(name, PrivateInstance)!.SetValue(instance, value);
    }

    private sealed class ListenerException : Exception
    {
    }
}
