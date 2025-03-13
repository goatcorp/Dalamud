using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dalamud.Game;

/// <summary>
/// Base memory address resolver.
/// </summary>
public abstract class BaseAddressResolver
{
    /// <summary>
    /// Gets a list of memory addresses that were found, to list in /xldata.
    /// </summary>
    public static Dictionary<string, List<(string ClassName, IntPtr Address)>> DebugScannedValues { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the resolver has successfully run <see cref="Setup32Bit(ISigScanner)"/> or <see cref="Setup64Bit(ISigScanner)"/>.
    /// </summary>
    protected bool IsResolved { get; set; }

    /// <summary>
    /// Setup the resolver, calling the appropriate method based on the process architecture.
    /// </summary>
    /// <param name="scanner">The SigScanner instance.</param>
    public void Setup(ISigScanner scanner)
    {
        // Because C# don't allow to call virtual function while in ctor
        // we have to do this shit :\

        if (this.IsResolved)
        {
            return;
        }

        if (scanner.Is32BitProcess)
        {
            this.Setup32Bit(scanner);
        }
        else
        {
            this.Setup64Bit(scanner);
        }

        this.SetupInternal(scanner);

        var className = this.GetType().Name;
        var list = new List<(string, IntPtr)>();
        lock (DebugScannedValues)
            DebugScannedValues[className] = list;

        foreach (var property in this.GetType().GetProperties().Where(x => x.PropertyType == typeof(IntPtr)))
        {
            list.Add((property.Name, (IntPtr)property.GetValue(this)));
        }

        this.IsResolved = true;
    }

    /// <summary>
    /// Fetch vfunc N from a pointer to the vtable and return a delegate function pointer.
    /// </summary>
    /// <typeparam name="T">The delegate to marshal the function pointer to.</typeparam>
    /// <param name="address">The address of the virtual table.</param>
    /// <param name="vtableOffset">The offset from address to the vtable pointer.</param>
    /// <param name="count">The vfunc index.</param>
    /// <returns>A delegate function pointer that can be invoked.</returns>
    public T GetVirtualFunction<T>(IntPtr address, int vtableOffset, int count) where T : class
    {
        // Get vtable
        var vtable = Marshal.ReadIntPtr(address, vtableOffset);

        // Get an address to the function
        var functionAddress = Marshal.ReadIntPtr(vtable, IntPtr.Size * count);

        return Marshal.GetDelegateForFunctionPointer<T>(functionAddress);
    }

    /// <summary>
    /// Setup the resolver by finding any necessary memory addresses.
    /// </summary>
    /// <param name="scanner">The SigScanner instance.</param>
    protected virtual void Setup32Bit(ISigScanner scanner)
    {
        throw new NotSupportedException("32 bit version is not supported.");
    }

    /// <summary>
    /// Setup the resolver by finding any necessary memory addresses.
    /// </summary>
    /// <param name="scanner">The SigScanner instance.</param>
    protected virtual void Setup64Bit(ISigScanner scanner)
    {
        throw new NotSupportedException("64 bit version is not supported.");
    }

    /// <summary>
    /// Setup the resolver by finding any necessary memory addresses.
    /// </summary>
    /// <param name="scanner">The SigScanner instance.</param>
    protected virtual void SetupInternal(ISigScanner scanner)
    {
        // Do nothing
    }
}
