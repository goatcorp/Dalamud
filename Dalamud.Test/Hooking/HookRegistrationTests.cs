using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Hooking;
using Dalamud.Hooking.Internal;

using Xunit;

namespace Dalamud.Test.Hooking;

/// <summary>
/// Verifies callback identity, registration lifetime, and hook chain integrity.
/// </summary>
public class HookRegistrationTests
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeFunctionDelegate(int value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int AlternateNativeFunctionDelegate(int value);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DuplicateCallbackIsRejectedWithoutChangingExistingHook(bool enabled)
    {
        using var function = new NativeFunction();
        var owner = new HookOwner(100);
        var firstDetour = new NativeFunctionDelegate(owner.Detour);
        var secondDetour = new NativeFunctionDelegate(owner.Detour);
        Assert.Equal(firstDetour, secondDetour);

        // Equal delegates can have distinct native entry points; compare managed callback identity.
        Assert.NotEqual(
            Marshal.GetFunctionPointerForDelegate(firstDetour),
            Marshal.GetFunctionPointerForDelegate(secondDetour));

        using var first = new ReloadedHook<NativeFunctionDelegate>(function.Address, firstDetour, typeof(HookRegistrationTests).Assembly);
        owner.Hook = first;
        if (enabled)
            first.Enable();

        var originalPatch = function.ReadCode();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ReloadedHook<NativeFunctionDelegate>(function.Address, secondDetour, typeof(HookRegistrationTests).Assembly));

        Assert.Contains("Dispose the previous hook before creating a replacement.", exception.Message);
        Assert.Equal(originalPatch, function.ReadCode());
        Assert.Equal(enabled, first.IsEnabled);
        Assert.False(first.IsDisposed);
        Assert.Equal(enabled ? 102 : 2, function.Call(1));
        Assert.Equal(enabled ? 1 : 0, owner.Calls);
        GC.KeepAlive(secondDetour);
    }

    [Fact]
    public void EquivalentCallbacksWithDifferentDelegateTypesAreRejected()
    {
        using var function = new NativeFunction();
        var owner = new HookOwner(100);
        var firstDetour = new NativeFunctionDelegate(owner.Detour);
        using var first = new ReloadedHook<NativeFunctionDelegate>(function.Address, firstDetour, typeof(HookRegistrationTests).Assembly);
        owner.Hook = first;
        first.Enable();

        var alternateDetour = new AlternateNativeFunctionDelegate(owner.Detour);
        Assert.Equal(firstDetour.Method, alternateDetour.Method);
        Assert.Same(firstDetour.Target, alternateDetour.Target);
        Assert.Throws<InvalidOperationException>(() =>
            new ReloadedHook<AlternateNativeFunctionDelegate>(function.Address, alternateDetour, typeof(HookRegistrationTests).Assembly));
        Assert.Equal(102, function.Call(1));
        Assert.Equal(1, owner.Calls);
    }

    [Fact]
    public void SafetyHookBackendCannotBypassExistingCallbackRegistration()
    {
        using var function = new NativeFunction();
        var owner = new HookOwner(100);
        using var first = new ReloadedHook<NativeFunctionDelegate>(function.Address, owner.Detour, typeof(HookRegistrationTests).Assembly);
        owner.Hook = first;
        first.Enable();
        var originalPatch = function.ReadCode();

        // Duplicate validation must precede native backend initialization.
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new SafetyHookHook<NativeFunctionDelegate>(function.Address, owner.Detour, typeof(HookRegistrationTests).Assembly));

        Assert.Contains("Dispose the previous hook before creating a replacement.", exception.Message);
        Assert.Equal(originalPatch, function.ReadCode());
        Assert.True(first.IsEnabled);
        Assert.False(first.IsDisposed);
        Assert.Equal(102, function.Call(1));
        Assert.Equal(1, owner.Calls);
    }

    [Fact]
    public void DifferentReceiversCanChainEvenWhenTheyCompareEqual()
    {
        using var function = new NativeFunction();
        var firstOwner = new HookOwner(100);
        var secondOwner = new HookOwner(1000);
        Assert.True(firstOwner.Equals(secondOwner));

        using var first = new ReloadedHook<NativeFunctionDelegate>(function.Address, firstOwner.Detour, typeof(HookRegistrationTests).Assembly);
        firstOwner.Hook = first;
        first.Enable();
        using var second = new ReloadedHook<NativeFunctionDelegate>(function.Address, secondOwner.Detour, typeof(HookRegistrationTests).Assembly);
        secondOwner.Hook = second;
        second.Enable();

        Assert.Equal(1102, function.Call(1));
        Assert.Equal(1, firstOwner.Calls);
        Assert.Equal(1, secondOwner.Calls);
    }

    [Fact]
    public void DifferentMethodsOnOneReceiverCanChain()
    {
        using var function = new NativeFunction();
        var owner = new MultipleCallbackOwner();
        using var first = new ReloadedHook<NativeFunctionDelegate>(function.Address, owner.FirstDetour, typeof(HookRegistrationTests).Assembly);
        owner.FirstHook = first;
        first.Enable();
        using var second = new ReloadedHook<NativeFunctionDelegate>(function.Address, owner.SecondDetour, typeof(HookRegistrationTests).Assembly);
        owner.SecondHook = second;
        second.Enable();

        Assert.Equal(1102, function.Call(1));
    }

    [Fact]
    public void SameCallbackCanHookDifferentAddresses()
    {
        using var firstFunction = new NativeFunction();
        using var secondFunction = new NativeFunction();
        using var first = new ReloadedHook<NativeFunctionDelegate>(firstFunction.Address, AddHundred, typeof(HookRegistrationTests).Assembly);
        using var second = new ReloadedHook<NativeFunctionDelegate>(secondFunction.Address, AddHundred, typeof(HookRegistrationTests).Assembly);
        first.Enable();
        second.Enable();
        Assert.Equal(101, firstFunction.Call(1));
        Assert.Equal(101, secondFunction.Call(1));
    }

    [Fact]
    public void DifferentNativeCallbackAddressesAreNotDuplicateManagedCallbacks()
    {
        using var function = new NativeFunction();
        using var firstNativeCallback = new NativeFunction();
        using var secondNativeCallback = new NativeFunction();
        Assert.Equal(firstNativeCallback.Call.Method, secondNativeCallback.Call.Method);
        Assert.Null(firstNativeCallback.Call.Target);
        Assert.Null(secondNativeCallback.Call.Target);
        Assert.NotEqual(firstNativeCallback.Call, secondNativeCallback.Call);

        using var first = new ReloadedHook<NativeFunctionDelegate>(function.Address, firstNativeCallback.Call, typeof(HookRegistrationTests).Assembly);
        first.Enable();
        using var second = new ReloadedHook<NativeFunctionDelegate>(function.Address, secondNativeCallback.Call, typeof(HookRegistrationTests).Assembly);
        second.Enable();
        Assert.Equal(2, function.Call(1));
    }

    [Fact]
    public void SameNativeCallbackAddressIsRejected()
    {
        using var function = new NativeFunction();
        using var nativeCallback = new NativeFunction();
        var duplicateCallback = Marshal.GetDelegateForFunctionPointer<NativeFunctionDelegate>(nativeCallback.Address);
        Assert.Equal(nativeCallback.Call, duplicateCallback);
        using var first = new ReloadedHook<NativeFunctionDelegate>(function.Address, nativeCallback.Call, typeof(HookRegistrationTests).Assembly);
        first.Enable();

        Assert.Throws<InvalidOperationException>(() =>
            new ReloadedHook<NativeFunctionDelegate>(function.Address, duplicateCallback, typeof(HookRegistrationTests).Assembly));
        Assert.True(first.IsEnabled);
        Assert.Equal(2, function.Call(1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisposedCallbackCanBeReplacedWithoutBreakingAnotherHook(bool replaceNewest)
    {
        using var function = new NativeFunction();
        var firstOwner = new HookOwner(100);
        var secondOwner = new HookOwner(1000);
        using var first = new ReloadedHook<NativeFunctionDelegate>(function.Address, firstOwner.Detour, typeof(HookRegistrationTests).Assembly);
        firstOwner.Hook = first;
        first.Enable();
        using var second = new ReloadedHook<NativeFunctionDelegate>(function.Address, secondOwner.Detour, typeof(HookRegistrationTests).Assembly);
        secondOwner.Hook = second;
        second.Enable();
        var replacingOwner = replaceNewest ? secondOwner : firstOwner;

        try
        {
            for (var i = 0; i < 20; i++)
            {
                Assert.Equal(1102, function.Call(1));
                replacingOwner.Hook.Dispose();
                Assert.Equal(replaceNewest ? 102 : 1002, function.Call(1));
                replacingOwner.Hook = new ReloadedHook<NativeFunctionDelegate>(function.Address, replacingOwner.Detour, typeof(HookRegistrationTests).Assembly);
                replacingOwner.Hook.Enable();
            }

            firstOwner.Calls = 0;
            secondOwner.Calls = 0;
            Assert.Equal(1102, function.Call(1));
            Assert.Equal(1, firstOwner.Calls);
            Assert.Equal(1, secondOwner.Calls);
        }
        finally
        {
            replacingOwner.Hook.Dispose();
        }
    }

    [Fact]
    public void FunctionPointerDuplicateIsRejectedBeforeChangingPointer()
    {
        using var function = new NativeFunction();
        using var storage = new FunctionPointerStorage(function.Address);
        var owner = new HookOwner(100);
        using var first = new FunctionPointerVariableHook<NativeFunctionDelegate>(storage.Address, owner.Detour, typeof(HookRegistrationTests).Assembly);
        owner.Hook = first;
        first.Enable();
        var originalPointer = Marshal.ReadIntPtr(storage.Address);

        Assert.Throws<InvalidOperationException>(() =>
            new FunctionPointerVariableHook<NativeFunctionDelegate>(storage.Address, owner.Detour, typeof(HookRegistrationTests).Assembly));

        Assert.Equal(originalPointer, Marshal.ReadIntPtr(storage.Address));
        var call = Marshal.GetDelegateForFunctionPointer<NativeFunctionDelegate>(Marshal.ReadIntPtr(storage.Address));
        Assert.Equal(102, call(1));
        Assert.Equal(1, owner.Calls);
    }

    [Fact]
    public async Task ConcurrentDuplicateRegistrationKeepsOnlyOneHook()
    {
        using var function = new NativeFunction();
        using var start = new ManualResetEventSlim();
        var owner = new HookOwner(100);
        var hooks = new ReloadedHook<NativeFunctionDelegate>?[2];
        var failures = new Exception?[2];
        var tasks = new Task[2];
        var cancellationToken = TestContext.Current.CancellationToken;
        for (var i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[index] = Task.Run(() =>
            {
                if (!start.Wait(TimeSpan.FromSeconds(5), cancellationToken))
                    throw new TimeoutException("The registration test was not started.");

                try
                {
                    hooks[index] = new ReloadedHook<NativeFunctionDelegate>(function.Address, owner.Detour, typeof(HookRegistrationTests).Assembly);
                }
                catch (Exception exception)
                {
                    failures[index] = exception;
                }
            }, cancellationToken);
        }

        start.Set();
        try
        {
            await Task.WhenAll(tasks);
            Assert.Single(Array.FindAll(hooks, hook => hook != null));
            Assert.Single(Array.FindAll(failures, exception => exception is InvalidOperationException));
            owner.Hook = hooks[0] ?? hooks[1]!;
            owner.Hook.Enable();
            Assert.Equal(102, function.Call(1));
            Assert.Equal(1, owner.Calls);
        }
        finally
        {
            foreach (var hook in hooks)
                hook?.Dispose();
        }
    }

    // Test callbacks and native fixtures.
    private static int AddHundred(int value) => value + 100;

    private sealed class HookOwner(int amount)
    {
        private int depth;

        public Hook<NativeFunctionDelegate> Hook { get; set; } = null!;

        public int Calls { get; set; }

        // Equal receivers must remain distinct callback targets.
        public override bool Equals(object? obj) => obj is HookOwner;

        public override int GetHashCode() => 0;

        public int Detour(int value)
        {
            this.Calls++;
            // Bound a broken hook chain so assertions fail without exhausting the stack.
            if (this.depth++ >= 32)
            {
                this.depth--;
                return -100000;
            }

            try
            {
                return amount + this.Hook.Original(value);
            }
            finally
            {
                this.depth--;
            }
        }
    }

    private sealed class MultipleCallbackOwner
    {
        public Hook<NativeFunctionDelegate> FirstHook { get; set; } = null!;

        public Hook<NativeFunctionDelegate> SecondHook { get; set; } = null!;

        public int FirstDetour(int value) => 100 + this.FirstHook.Original(value);

        public int SecondDetour(int value) => 1000 + this.SecondHook.Original(value);
    }

    private sealed class NativeFunction : IDisposable
    {
        public NativeFunction()
        {
            this.Address = VirtualAlloc(IntPtr.Zero, 4096, 0x3000, 0x40);
            if (this.Address == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // Windows x64: a patchable NOP prefix, followed by return arg0 + 1.
            var code = new byte[37];
            Array.Fill(code, (byte)0x90, 0, 32);
            new byte[] { 0x8B, 0xC1, 0xFF, 0xC0, 0xC3 }.CopyTo(code, 32);
            Marshal.Copy(code, 0, this.Address, code.Length);
            if (!FlushInstructionCache(new IntPtr(-1), this.Address, (nuint)code.Length))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            this.Call = Marshal.GetDelegateForFunctionPointer<NativeFunctionDelegate>(this.Address);
        }

        public IntPtr Address { get; }

        public NativeFunctionDelegate Call { get; }

        public byte[] ReadCode()
        {
            var bytes = new byte[37];
            Marshal.Copy(this.Address, bytes, 0, bytes.Length);
            return bytes;
        }

        public void Dispose()
        {
            ForgetTarget(this.Address);
            if (!VirtualFree(this.Address, 0, 0x8000))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAlloc(IntPtr address, nuint size, uint allocationType, uint protection);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VirtualFree(IntPtr address, nuint size, uint freeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlushInstructionCache(IntPtr process, IntPtr address, nuint size);
    }

    private sealed class FunctionPointerStorage : IDisposable
    {
        public FunctionPointerStorage(IntPtr function)
        {
            this.Address = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(this.Address, function);
        }

        public IntPtr Address { get; }

        public void Dispose()
        {
            ForgetTarget(this.Address);
            Marshal.FreeHGlobal(this.Address);
        }
    }

    private static void ForgetTarget(IntPtr address)
    {
        // Freed fixture addresses may be reused; discard their process-wide hook state first.
        using var scope = HookManager.HookEnableSyncRoot.EnterScope();
        HookManager.Unhookers.TryRemove(address, out _);
        HookManager.MultiHookTracker.TryRemove(address, out _);
    }
}
