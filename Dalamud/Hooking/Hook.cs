using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

using Dalamud.Configuration.Internal;
using Dalamud.Hooking.Internal;

namespace Dalamud.Hooking;

/// <summary>
/// Manages a hook which can be used to intercept a call to native function.
/// This class is basically a thin wrapper around the LocalHook type to provide helper functions.
/// </summary>
/// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
public abstract class Hook<T> : IDalamudHook where T : Delegate
{
#pragma warning disable SA1310
    // ReSharper disable once InconsistentNaming
    private const ulong IMAGE_ORDINAL_FLAG64 = 0x8000000000000000;
    // ReSharper disable once InconsistentNaming
    private const uint IMAGE_ORDINAL_FLAG32 = 0x80000000;
#pragma warning restore SA1310

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
    /// Gets a value indicating whether or not the hook is enabled.
    /// </summary>
    public virtual bool IsEnabled => throw new NotImplementedException();

    /// <summary>
    /// Gets a value indicating whether or not the hook has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public virtual string BackendName => throw new NotImplementedException();
    
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
    /// <returns>The hook with the supplied parameters.</returns>
    internal static Hook<T> FromFunctionPointerVariable(IntPtr address, T detour)
    {
        return new FunctionPointerVariableHook<T>(address, detour, Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Creates a hook by rewriting import table address.
    /// </summary>
    /// <param name="module">Module to check for. Current process' main module if null.</param>
    /// <param name="moduleName">Name of the DLL, including the extension.</param>
    /// <param name="functionName">Decorated name of the function.</param>
    /// <param name="hintOrOrdinal">Hint or ordinal. 0 to unspecify.</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <returns>The hook with the supplied parameters.</returns>
    internal static unsafe Hook<T> FromImport(ProcessModule? module, string moduleName, string functionName, uint hintOrOrdinal, T detour)
    {
        module ??= Process.GetCurrentProcess().MainModule;
        if (module == null)
            throw new InvalidOperationException("Current module is null?");
        var pDos = (PeHeader.IMAGE_DOS_HEADER*)module.BaseAddress;
        var pNt = (PeHeader.IMAGE_FILE_HEADER*)(module.BaseAddress + (int)pDos->e_lfanew + 4);
        var isPe64 = pNt->SizeOfOptionalHeader == Marshal.SizeOf<PeHeader.IMAGE_OPTIONAL_HEADER64>();
        PeHeader.IMAGE_DATA_DIRECTORY* pDataDirectory;
        if (isPe64)
        {
            var pOpt = (PeHeader.IMAGE_OPTIONAL_HEADER64*)(module.BaseAddress + (int)pDos->e_lfanew + 4 + Marshal.SizeOf<PeHeader.IMAGE_FILE_HEADER>());
            pDataDirectory = &pOpt->ImportTable;
        }
        else
        {
            var pOpt = (PeHeader.IMAGE_OPTIONAL_HEADER32*)(module.BaseAddress + (int)pDos->e_lfanew + 4 + Marshal.SizeOf<PeHeader.IMAGE_FILE_HEADER>());
            pDataDirectory = &pOpt->ImportTable;
        }

        var moduleNameLowerWithNullTerminator = (moduleName + "\0").ToLowerInvariant();
        foreach (ref var importDescriptor in new Span<PeHeader.IMAGE_IMPORT_DESCRIPTOR>(
                     (PeHeader.IMAGE_IMPORT_DESCRIPTOR*)(module.BaseAddress + (int)pDataDirectory->VirtualAddress),
                     (int)(pDataDirectory->Size / Marshal.SizeOf<PeHeader.IMAGE_IMPORT_DESCRIPTOR>())))
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
            if (currentDllNameWithNullTerminator.ToLowerInvariant() != moduleNameLowerWithNullTerminator)
                continue;

            if (isPe64)
            {
                return new FunctionPointerVariableHook<T>(FromImportHelper64(module.BaseAddress, ref importDescriptor, ref *pDataDirectory, functionName, hintOrOrdinal), detour, Assembly.GetCallingAssembly());
            }
            else
            {
                return new FunctionPointerVariableHook<T>(FromImportHelper32(module.BaseAddress, ref importDescriptor, ref *pDataDirectory, functionName, hintOrOrdinal), detour, Assembly.GetCallingAssembly());
            }
        }

        throw new MissingMethodException("Specified dll not found");
    }

    /// <summary>
    /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
    /// The hook is not activated until Enable() method is called.
    /// </summary>
    /// <param name="moduleName">A name of the module currently loaded in the memory. (e.g. ws2_32.dll).</param>
    /// <param name="exportName">A name of the exported function name (e.g. send).</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <returns>The hook with the supplied parameters.</returns>
    internal static Hook<T> FromSymbol(string moduleName, string exportName, T detour)
        => FromSymbol(moduleName, exportName, detour, false);

    /// <summary>
    /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
    /// The hook is not activated until Enable() method is called.
    /// Please do not use MinHook unless you have thoroughly troubleshot why Reloaded does not work.
    /// </summary>
    /// <param name="moduleName">A name of the module currently loaded in the memory. (e.g. ws2_32.dll).</param>
    /// <param name="exportName">A name of the exported function name (e.g. send).</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <param name="useMinHook">Use the MinHook hooking library instead of Reloaded.</param>
    /// <returns>The hook with the supplied parameters.</returns>
    internal static Hook<T> FromSymbol(string moduleName, string exportName, T detour, bool useMinHook)
    {
        if (EnvironmentConfiguration.DalamudForceMinHook)
            useMinHook = true;

        var moduleHandle = NativeFunctions.GetModuleHandleW(moduleName);
        if (moduleHandle == IntPtr.Zero)
            throw new Exception($"Could not get a handle to module {moduleName}");

        var procAddress = NativeFunctions.GetProcAddress(moduleHandle, exportName);
        if (procAddress == IntPtr.Zero)
            throw new Exception($"Could not get the address of {moduleName}::{exportName}");

        procAddress = HookManager.FollowJmp(procAddress);
        if (useMinHook)
            return new MinHookHook<T>(procAddress, detour, Assembly.GetCallingAssembly());
        else
            return new ReloadedHook<T>(procAddress, detour, Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
    /// The hook is not activated until Enable() method is called.
    /// Please do not use MinHook unless you have thoroughly troubleshot why Reloaded does not work.
    /// </summary>
    /// <param name="procAddress">A memory address to install a hook.</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <param name="useMinHook">Use the MinHook hooking library instead of Reloaded.</param>
    /// <returns>The hook with the supplied parameters.</returns>
    internal static Hook<T> FromAddress(IntPtr procAddress, T detour, bool useMinHook = false)
    {
        if (EnvironmentConfiguration.DalamudForceMinHook)
            useMinHook = true;

        procAddress = HookManager.FollowJmp(procAddress);
        if (useMinHook)
            return new MinHookHook<T>(procAddress, detour, Assembly.GetCallingAssembly());
        else
            return new ReloadedHook<T>(procAddress, detour, Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Check if this object has been disposed already.
    /// </summary>
    protected void CheckDisposed()
    {
        if (this.IsDisposed)
        {
            throw new ObjectDisposedException(message: "Hook is already disposed", null);
        }
    }

    private static unsafe IntPtr FromImportHelper32(IntPtr baseAddress, ref PeHeader.IMAGE_IMPORT_DESCRIPTOR desc, ref PeHeader.IMAGE_DATA_DIRECTORY dir, string functionName, uint hintOrOrdinal)
    {
        var importLookupsOversizedSpan = new Span<uint>((uint*)(baseAddress + (int)desc.OriginalFirstThunk), (int)((dir.Size - desc.OriginalFirstThunk) / Marshal.SizeOf<int>()));
        var importAddressesOversizedSpan = new Span<uint>((uint*)(baseAddress + (int)desc.FirstThunk), (int)((dir.Size - desc.FirstThunk) / Marshal.SizeOf<int>()));

        var functionNameWithNullTerminator = functionName + "\0";
        for (int i = 0, i_ = Math.Min(importLookupsOversizedSpan.Length, importAddressesOversizedSpan.Length); i < i_ && importLookupsOversizedSpan[i] != 0 && importAddressesOversizedSpan[i] != 0; i++)
        {
            var importLookup = importLookupsOversizedSpan[i];

            // Is this entry importing by ordinals? A lot of socket functions are the case.
            if ((importLookup & IMAGE_ORDINAL_FLAG32) != 0)
            {
                var ordinal = importLookup & ~IMAGE_ORDINAL_FLAG32;

                // Is this the entry?
                if (hintOrOrdinal == 0 || ordinal != hintOrOrdinal)
                    continue;

                // Is this entry not importing by ordinals, and are we using hint exclusively to find the entry?
            }
            else
            {
                var hint = Marshal.ReadInt16(baseAddress + (int)importLookup);

                if (functionName.Length > 0)
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
                        (int)Math.Min(dir.VirtualAddress + dir.Size - (uint)baseAddress - importLookup - 2, (uint)functionNameWithNullTerminator.Length));

                    // Is this entry about the function that we're looking for?
                    if (currentFunctionNameWithNullTerminator != functionNameWithNullTerminator)
                        continue;
                }
            }

            return baseAddress + (int)desc.FirstThunk + (i * Marshal.SizeOf<int>());
        }

        throw new MissingMethodException("Specified method not found");
    }

    private static unsafe IntPtr FromImportHelper64(IntPtr baseAddress, ref PeHeader.IMAGE_IMPORT_DESCRIPTOR desc, ref PeHeader.IMAGE_DATA_DIRECTORY dir, string functionName, uint hintOrOrdinal)
    {
        var importLookupsOversizedSpan = new Span<ulong>((ulong*)(baseAddress + (int)desc.OriginalFirstThunk), (int)((dir.Size - desc.OriginalFirstThunk) / Marshal.SizeOf<ulong>()));
        var importAddressesOversizedSpan = new Span<ulong>((ulong*)(baseAddress + (int)desc.FirstThunk), (int)((dir.Size - desc.FirstThunk) / Marshal.SizeOf<ulong>()));

        var functionNameWithNullTerminator = functionName + "\0";
        for (int i = 0, i_ = Math.Min(importLookupsOversizedSpan.Length, importAddressesOversizedSpan.Length); i < i_ && importLookupsOversizedSpan[i] != 0 && importAddressesOversizedSpan[i] != 0; i++)
        {
            var importLookup = importLookupsOversizedSpan[i];

            // Is this entry importing by ordinals? A lot of socket functions are the case.
            if ((importLookup & IMAGE_ORDINAL_FLAG64) != 0)
            {
                var ordinal = importLookup & ~IMAGE_ORDINAL_FLAG64;

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

            return baseAddress + (int)desc.FirstThunk + (i * Marshal.SizeOf<ulong>());
        }

        throw new MissingMethodException("Specified method not found");
    }
}
