using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal.ReShadeHandling;

using TerraFX.Interop.Windows;

using Xunit;

namespace Dalamud.Test.Interface;

public class ReShadeAddonLifetimeTests
{
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(5);
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    [Theory]
    [InlineData("Present", "presentDelegate", false)]
    [InlineData("Present", "presentDelegate", true)]
    [InlineData("ReShadeOverlay", "reShadeOverlayDelegate", false)]
    [InlineData("ReShadeOverlay", "reShadeOverlayDelegate", true)]
    [InlineData("InitSwapChain", "initSwapChainDelegate", false)]
    [InlineData("InitSwapChain", "initSwapChainDelegate", true)]
    [InlineData("DestroySwapChain", "destroySwapChainDelegate", false)]
    [InlineData("DestroySwapChain", "destroySwapChainDelegate", true)]
    public void DisposeDuringNativeDispatchRetainsCallbacksAndDetachesSubscriber(
        string eventName,
        string callbackField,
        bool pauseAfterDalamud)
    {
        var registration = CreateRegistration();
        using var addon = Attach(registration);
        var calls = new ConcurrentQueue<int>();
        Subscribe(addon, eventName, new Subscriber(() => calls.Enqueue(2)));
        var paused = new ManualResetEventSlim();
        var resume = new ManualResetEventSlim();
        var timedOut = false;

        void ForeignCallback(int position, bool pause)
        {
            calls.Enqueue(position);
            if (pause)
            {
                paused.Set();
                if (!resume.Wait(WorkerTimeout))
                    timedOut = true;
            }
        }

        ObjectCallback before = (ref ReShadeAddonInterface.ApiObject _) => ForeignCallback(1, !pauseAfterDalamud);
        ObjectCallback after = (ref ReShadeAddonInterface.ApiObject _) => ForeignCallback(3, pauseAfterDalamud);
        using var dispatcher = new NativeDispatcher(
            Marshal.GetFunctionPointerForDelegate(before),
            GetCallbackAddress(registration, callbackField),
            Marshal.GetFunctionPointerForDelegate(after));
        var originalCallbacks = dispatcher.ReadCallbacks();
        var worker = Task.Run(() =>
        {
            try
            {
                dispatcher.Invoke();
            }
            finally
            {
                // A failed join must not drop delegates while the native worker still owns their pointers.
                GC.KeepAlive(registration);
                GC.KeepAlive(before);
                GC.KeepAlive(after);
            }
        });
        var joined = false;
        try
        {
            try
            {
                Assert.True(paused.Wait(WorkerTimeout), "Native dispatch did not reach the foreign callback.");

                // With native traversal paused outside Dalamud, disposal must preserve the cached callback vector.
                addon.Dispose();
                CollectGarbage();
                Assert.Equal(originalCallbacks, dispatcher.ReadCallbacks());
            }
            finally
            {
                resume.Set();
                joined = worker.Wait(WorkerTimeout);
                Assert.True(joined, "Native dispatch did not return after release.");
            }

            Assert.False(timedOut, "The paused foreign callback timed out before it was released.");
            Assert.Equal(pauseAfterDalamud ? new[] { 1, 2, 3 } : new[] { 1, 3 }, calls.ToArray());

            // Native ReShade can start additional dispatches after managed cleanup has finished.
            calls.Clear();
            CollectGarbage();
            dispatcher.Invoke();
            Assert.Equal(new[] { 1, 3 }, calls.ToArray());
            Assert.Equal(originalCallbacks, dispatcher.ReadCallbacks());
        }
        finally
        {
            // Keep events usable after a failed join so a late callback cannot throw across the native boundary.
            if (joined)
            {
                paused.Dispose();
                resume.Dispose();
            }

            GC.KeepAlive(registration);
            GC.KeepAlive(before);
            GC.KeepAlive(after);
        }
    }

    [Theory]
    [InlineData("Present", "presentDelegate")]
    [InlineData("ReShadeOverlay", "reShadeOverlayDelegate")]
    [InlineData("InitSwapChain", "initSwapChainDelegate")]
    [InlineData("DestroySwapChain", "destroySwapChainDelegate")]
    public void RepeatedDisposalCannotDetachReplacementAttachment(string eventName, string callbackField)
    {
        var registration = CreateRegistration();
        using var first = Attach(registration);
        Assert.False(TryAttach(registration, out _));
        first.Dispose();

        using var replacement = Attach(registration);
        var calls = 0;
        Subscribe(replacement, eventName, new Subscriber(() => calls++));
        first.Dispose();

        using var dispatcher = new NativeDispatcher(GetCallbackAddress(registration, callbackField));
        dispatcher.Invoke();
        Assert.Equal(1, calls);

        replacement.Dispose();
        dispatcher.Invoke();
        Assert.Equal(1, calls);
        GC.KeepAlive(registration);
    }

    [Fact]
    public void DisposedAttachmentReleasesEverySubscriberWhileAttachmentRemainsAlive()
    {
        var registration = CreateRegistration();
        var (addon, subscriber) = CreateDisposedAttachment(registration);
        CollectGarbage();

        Assert.False(subscriber.IsAlive, "A disposed attachment still owns its event subscribers.");
        GC.KeepAlive(addon);
        GC.KeepAlive(registration);
    }

    private static object CreateRegistration()
    {
        // Isolate each fixture from the process-wide registration and from a real ReShade DLL.
        var type = typeof(ReShadeAddonInterface).GetNestedType("NativeRegistration", BindingFlags.NonPublic)!;
        return Activator.CreateInstance(type, nonPublic: true)!;
    }

    private static bool TryAttach(object registration, out ReShadeAddonInterface? addon)
    {
        object?[] arguments = [null];
        var result = (bool)registration.GetType()
            .GetMethod("TryAttach", PrivateInstance | BindingFlags.Public)!.Invoke(registration, arguments)!;
        addon = (ReShadeAddonInterface?)arguments[0];
        return result;
    }

    private static ReShadeAddonInterface Attach(object registration)
    {
        Assert.True(TryAttach(registration, out var addon));
        return Assert.IsType<ReShadeAddonInterface>(addon);
    }

    private static nint GetCallbackAddress(object registration, string fieldName)
    {
        var storage = registration.GetType().GetField(fieldName, PrivateInstance)!.GetValue(registration)!;
        var callback = (Delegate)storage.GetType().GetField("Delegate")!.GetValue(storage)!;
        return Marshal.GetFunctionPointerForDelegate(callback);
    }

    private static void Subscribe(ReShadeAddonInterface addon, string eventName, Subscriber subscriber)
    {
        var eventInfo = typeof(ReShadeAddonInterface).GetEvent(eventName)!;
        var method = typeof(Subscriber).GetMethod(eventName == "Present" ? "OnPresent" : "OnObject")!;
        eventInfo.AddEventHandler(addon, method.CreateDelegate(eventInfo.EventHandlerType!, subscriber));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (ReShadeAddonInterface Addon, WeakReference Subscriber) CreateDisposedAttachment(object registration)
    {
        var addon = Attach(registration);
        var subscriber = new Subscriber(() => { });
        foreach (var eventName in new[] { "Present", "ReShadeOverlay", "InitSwapChain", "DestroySwapChain" })
            Subscribe(addon, eventName, subscriber);

        var reference = new WeakReference(subscriber);
        addon.Dispose();
        return (addon, reference);
    }

    private static void CollectGarbage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private delegate void ObjectCallback(ref ReShadeAddonInterface.ApiObject value);

    private delegate void DispatchCallback(ref DispatchState state);

    [StructLayout(LayoutKind.Sequential)]
    private struct DispatchState
    {
        public nint Callbacks;
        public nuint Count;
        public ReShadeAddonInterface.ApiObject FirstArgument;
        public ReShadeAddonInterface.ApiObject SecondArgument;
    }

    private sealed class Subscriber(Action callback)
    {
        public void OnObject(ref ReShadeAddonInterface.ApiObject value) => callback();

        public void OnPresent(
            ref ReShadeAddonInterface.ApiObject queue,
            ref ReShadeAddonInterface.ApiObject swapChain,
            ReadOnlySpan<RECT> source,
            ReadOnlySpan<RECT> destination,
            ReadOnlySpan<RECT> dirtyRects) => callback();
    }

    /// <summary>Replays native callback-vector traversal on Windows x64.</summary>
    private sealed class NativeDispatcher : IDisposable
    {
        private readonly nint code;
        private readonly nint callbacks;
        private readonly int callbackCount;
        private readonly DispatchCallback dispatch;
        private readonly object lifetimeLock = new();
        private int activeInvocations;
        private bool disposeRequested;

        public NativeDispatcher(params nint[] callbacks)
        {
            this.callbackCount = callbacks.Length;
            this.callbacks = Marshal.AllocHGlobal(callbacks.Length * nint.Size);
            Marshal.Copy(callbacks, 0, this.callbacks, callbacks.Length);
            this.code = VirtualAlloc(nint.Zero, 4096, 0x3000, 0x40);
            if (this.code == nint.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // Cache vector storage/count before traversal, as ReShade does. Present's extra arguments
            // are ignored by the one-argument object callbacks.
            byte[] instructions =
            [
                0x53,                               // push rbx
                0x56,                               // push rsi
                0x57,                               // push rdi
                0x41, 0x54,                         // push r12
                0x48, 0x83, 0xEC, 0x38,             // sub rsp, 56 (shadow space, args 5/6, alignment)
                0x48, 0x89, 0xCB,                   // mov rbx, rcx
                0x48, 0x8B, 0x33,                   // mov rsi, [rbx]
                0x48, 0x8B, 0x7B, 0x08,             // mov rdi, [rbx+8]
                0x45, 0x31, 0xE4,                   // xor r12d, r12d
                0x49, 0x39, 0xFC,                   // loop: cmp r12, rdi
                0x73, 0x29,                         // jae end
                0x48, 0x8D, 0x4B, 0x10,             // lea rcx, [rbx+16]
                0x48, 0x8D, 0x53, 0x18,             // lea rdx, [rbx+24]
                0x45, 0x31, 0xC0,                   // xor r8d, r8d
                0x45, 0x31, 0xC9,                   // xor r9d, r9d
                0x48, 0xC7, 0x44, 0x24, 0x20, 0, 0, 0, 0, // mov qword [rsp+32], 0
                0x48, 0xC7, 0x44, 0x24, 0x28, 0, 0, 0, 0, // mov qword [rsp+40], 0
                0x42, 0xFF, 0x14, 0xE6,             // call qword [rsi+r12*8]
                0x49, 0xFF, 0xC4,                   // inc r12
                0xEB, 0xD2,                         // jmp loop
                0x48, 0x83, 0xC4, 0x38,             // end: add rsp, 56
                0x41, 0x5C,                         // pop r12
                0x5F,                               // pop rdi
                0x5E,                               // pop rsi
                0x5B,                               // pop rbx
                0xC3,                               // ret
            ];
            Marshal.Copy(instructions, 0, this.code, instructions.Length);
            if (!FlushInstructionCache(new nint(-1), this.code, (nuint)instructions.Length))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            this.dispatch = Marshal.GetDelegateForFunctionPointer<DispatchCallback>(this.code);
        }

        public void Invoke()
        {
            lock (this.lifetimeLock)
            {
                ObjectDisposedException.ThrowIf(this.disposeRequested, this);
                this.activeInvocations++;
            }

            try
            {
                var state = new DispatchState { Callbacks = this.callbacks, Count = (nuint)this.callbackCount };
                this.dispatch(ref state);
            }
            finally
            {
                lock (this.lifetimeLock)
                {
                    if (--this.activeInvocations == 0 && this.disposeRequested)
                        this.ReleaseNativeMemory();
                }
            }
        }

        public nint[] ReadCallbacks()
        {
            lock (this.lifetimeLock)
            {
                ObjectDisposedException.ThrowIf(this.disposeRequested, this);
                var result = new nint[this.callbackCount];
                Marshal.Copy(this.callbacks, result, 0, result.Length);
                return result;
            }
        }

        public void Dispose()
        {
            lock (this.lifetimeLock)
            {
                if (this.disposeRequested)
                    return;

                this.disposeRequested = true;
                if (this.activeInvocations == 0)
                    this.ReleaseNativeMemory();
            }
        }

        private void ReleaseNativeMemory()
        {
            Marshal.FreeHGlobal(this.callbacks);
            if (!VirtualFree(this.code, 0, 0x8000))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint VirtualAlloc(nint address, nuint size, uint allocationType, uint protection);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VirtualFree(nint address, nuint size, uint freeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlushInstructionCache(nint process, nint address, nuint size);
    }
}
