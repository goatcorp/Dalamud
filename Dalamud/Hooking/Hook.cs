using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

using Dalamud.Configuration.Internal;
using Dalamud.Hooking.Internal;
using Dalamud.Hooking.Internal.Verification;

using Serilog;

using TerraFX.Interop.Windows;

namespace Dalamud.Hooking;

/// <summary>
/// Manages a hook which can be used to intercept a call to native function.
/// This class is basically a thin wrapper around the LocalHook type to provide helper functions.
/// </summary>
/// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
public abstract class Hook<T> : IDalamudHook where T : Delegate
{
    private readonly IntPtr address;

    /// <summary>
    /// Initializes a new instance of the <see cref="Hook{T}"/> class.
    /// </summary>
    /// <param name="address">A memory address to install a hook.</param>
    internal Hook(IntPtr address)
    {
        this.address = address;
    }

    /// <summary>
    /// Gets a memory address of the target function.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
    public IntPtr Address
    {
        get
        {
            this.CheckDisposed();
            return this.address;
        }
    }

    /// <summary>
    /// Gets a delegate function that can be used to call the actual function as if function is not hooked yet.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
    public virtual T Original => throw new NotImplementedException();

    /// <summary>
    /// Gets a delegate function that can be used to call the actual function as if function is not hooked yet.
    /// This can be called even after Dispose.
    /// </summary>
    public T OriginalDisposeSafe
        => this.IsDisposed ? Marshal.GetDelegateForFunctionPointer<T>(this.address) : this.Original;

    /// <summary>
    /// Gets a value indicating whether the hook is enabled.
    /// </summary>
    public virtual bool IsEnabled => throw new NotImplementedException();

    /// <summary>
    /// Gets a value indicating whether the hook has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public virtual string BackendName => throw new NotImplementedException();

    /// <summary>
    /// Gets the unique GUID for this hook.
    /// </summary>
    protected Guid HookId { get; } = Guid.NewGuid();

    /// <summary>
    /// Remove a hook from the current process.
    /// </summary>
    public virtual void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.IsDisposed = true;
    }

    /// <summary>
    /// Starts intercepting a call to the function.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
    public virtual void Enable() => throw new NotImplementedException();

    /// <summary>
    /// Stops intercepting a call to the function.
    /// </summary>
    public virtual void Disable() => throw new NotImplementedException();

    /// <summary>
    /// Creates a hook by rewriting import table address.
    /// </summary>
    /// <param name="address">A memory address to install a hook.</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <param name="callingAssembly">Calling assembly.</param>
    /// <returns>The hook with the supplied parameters.</returns>
    internal static Hook<T> FromFunctionPointerVariable(IntPtr address, T detour, Assembly? callingAssembly = null)
    {
        return new FunctionPointerVariableHook<T>(address, detour, callingAssembly ?? Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Creates a hook by rewriting import table address.
    /// </summary>
    /// <param name="module">Module to check for. Current process' main module if null.</param>
    /// <param name="moduleName">Name of the DLL, including the extension.</param>
    /// <param name="functionName">Decorated name of the function.</param>
    /// <param name="hintOrOrdinal">Hint or ordinal. 0 to unspecify.</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <param name="callingAssembly">Calling assembly.</param>
    /// <returns>The hook with the supplied parameters.</returns>
    internal static unsafe Hook<T> FromImport(ProcessModule? module, string moduleName, string functionName, uint hintOrOrdinal, T detour, Assembly? callingAssembly = null)
    {
        module ??= Process.GetCurrentProcess().MainModule;
        if (module == null)
            throw new InvalidOperationException("Current module is null?");
        var pDos = (IMAGE_DOS_HEADER*)module.BaseAddress;
        var pNt = (IMAGE_NT_HEADERS64*)(module.BaseAddress + pDos->e_lfanew);
        var pDataDirectory = &pNt->OptionalHeader.DataDirectory[IMAGE.IMAGE_DIRECTORY_ENTRY_IMPORT];
        var moduleNameLowerWithNullTerminator = (moduleName + "\0").ToLowerInvariant();
        foreach (ref var importDescriptor in new Span<IMAGE_IMPORT_DESCRIPTOR>(
                     (IMAGE_IMPORT_DESCRIPTOR*)(module.BaseAddress + (int)pDataDirectory->VirtualAddress),
                     (int)(pDataDirectory->Size / sizeof(IMAGE_IMPORT_DESCRIPTOR))))
        {
            // Having all zero values signals the end of the table. We didn't find anything.
            if (importDescriptor.Characteristics == 0)
                throw new MissingMethodException("Specified dll not found");

            // Skip invalid entries, just in case.
            if (importDescriptor.Name == 0)
                continue;

            // Name must be contained in this directory.
            if (importDescriptor.Name < pDataDirectory->VirtualAddress)
                continue;
            var currentDllNameWithNullTerminator = Marshal.PtrToStringUTF8(
                module.BaseAddress + (int)importDescriptor.Name,
                (int)Math.Min(pDataDirectory->Size + pDataDirectory->VirtualAddress - importDescriptor.Name, moduleNameLowerWithNullTerminator.Length));

            // Is this entry about the DLL that we're looking for? (Case insensitive)
            if (!currentDllNameWithNullTerminator.Equals(moduleNameLowerWithNullTerminator, StringComparison.InvariantCultureIgnoreCase))
                continue;

            return new FunctionPointerVariableHook<T>(FromImportHelper(module.BaseAddress, ref importDescriptor, ref *pDataDirectory, functionName, hintOrOrdinal), detour, callingAssembly ?? Assembly.GetCallingAssembly());
        }

        throw new MissingMethodException("Specified dll not found");
    }

    /// <summary>
    /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
    /// The hook is not activated until Enable() method is called.
    /// Please do not use MinHook unless you have thoroughly troubleshot why Reloaded does not work.
    /// </summary>
    /// <param name="moduleName">A name of the module currently loaded in the memory. (e.g. ws2_32.dll).</param>
    /// <param name="exportName">A name of the exported function name (e.g. send).</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <param name="useMinHook">Use the MinHook hooking library instead of Reloaded.</param>
    /// <param name="callingAssembly">Calling assembly.</param>
    /// <returns>The hook with the supplied parameters.</returns>
    internal static Hook<T> FromSymbol(string moduleName, string exportName, T detour, bool useMinHook = false, Assembly? callingAssembly = null)
    {
        if (EnvironmentConfiguration.DalamudForceMinHook)
            useMinHook = true;

        var moduleHandle = Windows.Win32.PInvoke.GetModuleHandle(moduleName);
        if (moduleHandle.IsNull)
            throw new Exception($"Could not get a handle to module {moduleName}");

        var procAddress = Windows.Win32.PInvoke.GetProcAddress(moduleHandle, exportName);
        if (procAddress.IsNull)
            throw new Exception($"Could not get the address of {moduleName}::{exportName}");

        var address = HookManager.FollowJmp(procAddress.Value);
        if (useMinHook)
            return new MinHookHook<T>(address, detour, callingAssembly ?? Assembly.GetCallingAssembly());
        else
            return new ReloadedHook<T>(address, detour, callingAssembly ?? Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
    /// The hook is not activated until Enable() method is called.
    /// Please do not use MinHook unless you have thoroughly troubleshot why Reloaded does not work.
    /// </summary>
    /// <param name="procAddress">A memory address to install a hook.</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <param name="useMinHook">Use the MinHook hooking library instead of Reloaded.</param>
    /// <param name="callingAssembly">Calling assembly.</param>
    /// <returns>The hook with the supplied parameters.</returns>
    internal static Hook<T> FromAddress(IntPtr procAddress, T detour, bool useMinHook = false, Assembly? callingAssembly = null)
    {
        if (EnvironmentConfiguration.DalamudForceMinHook)
            useMinHook = true;

        var assembly = callingAssembly ?? Assembly.GetCallingAssembly();

        // TODO: Only log verification exceptions for now, figure out how to handle this
        if (!HookVerifier.TryVerify<T>(procAddress, assembly, out var exceptions))
        {
            foreach (var exception in exceptions)
                Log.Error(exception, $"Hook verification failed - this may cause crashes and subtle bugs");
        }

        procAddress = HookManager.FollowJmp(procAddress);
        if (useMinHook)
            return new MinHookHook<T>(procAddress, detour, assembly);
        else
            return new ReloadedHook<T>(procAddress, detour, assembly);
    }

    /// <summary>
    /// Check if this object has been disposed already.
    /// </summary>
    protected void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
    }

    private static unsafe IntPtr FromImportHelper(IntPtr baseAddress, ref IMAGE_IMPORT_DESCRIPTOR desc, ref IMAGE_DATA_DIRECTORY dir, string functionName, uint hintOrOrdinal)
    {
        var importLookupsOversizedSpan = new Span<ulong>((ulong*)(baseAddress + (int)desc.OriginalFirstThunk), (int)((dir.Size - desc.OriginalFirstThunk) / sizeof(ulong)));
        var importAddressesOversizedSpan = new Span<ulong>((ulong*)(baseAddress + (int)desc.FirstThunk), (int)((dir.Size - desc.FirstThunk) / sizeof(ulong)));

        var functionNameWithNullTerminator = functionName + "\0";
        for (int i = 0, i_ = Math.Min(importLookupsOversizedSpan.Length, importAddressesOversizedSpan.Length); i < i_ && importLookupsOversizedSpan[i] != 0 && importAddressesOversizedSpan[i] != 0; i++)
        {
            var importLookup = importLookupsOversizedSpan[i];

            // Is this entry importing by ordinals? A lot of socket functions are the case.
            if ((importLookup & IMAGE.IMAGE_ORDINAL_FLAG64) != 0)
            {
                var ordinal = importLookup & ~IMAGE.IMAGE_ORDINAL_FLAG64;

                // Is this the entry?
                if (hintOrOrdinal == 0 || ordinal != hintOrOrdinal)
                    continue;

                // Is this entry not importing by ordinals, and are we using hint exclusively to find the entry?
            }
            else
            {
                var hint = Marshal.ReadInt16(baseAddress + (int)importLookup);

                if (functionName.Length == 0)
                {
                    // Is this the entry?
                    if (hint != hintOrOrdinal)
                        continue;
                }
                else
                {
                    // Name must be contained in this directory.
                    var currentFunctionNameWithNullTerminator = Marshal.PtrToStringUTF8(
                        baseAddress + (int)importLookup + 2,
                        (int)Math.Min((ulong)dir.VirtualAddress + dir.Size - (ulong)baseAddress - importLookup - 2, (ulong)functionNameWithNullTerminator.Length));

                    // Is this entry about the function that we're looking for?
                    if (currentFunctionNameWithNullTerminator != functionNameWithNullTerminator)
                        continue;
                }
            }

            return baseAddress + (int)desc.FirstThunk + (i * sizeof(ulong));
        }

        throw new MissingMethodException("Specified method not found");
    }
}
