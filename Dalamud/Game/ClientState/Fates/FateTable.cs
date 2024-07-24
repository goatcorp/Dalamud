using System.Collections;
using System.Collections.Generic;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Serilog;

namespace Dalamud.Game.ClientState.Fates;

/// <summary>
/// This collection represents the currently available Fate events.
/// </summary>
[PluginInterface]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IFateTable>]
#pragma warning restore SA1015
internal sealed partial class FateTable : IServiceType, IFateTable
{
    private readonly ClientStateAddressResolver address;

    [ServiceManager.ServiceConstructor]
    private FateTable(ClientState clientState)
    {
        this.address = clientState.AddressResolver;

        Log.Verbose($"Fate table address {Util.DescribeAddress(this.address.FateTablePtr)}");
    }

    /// <inheritdoc/>
    public IntPtr Address => this.address.FateTablePtr;

    /// <inheritdoc/>
    public unsafe int Length
    {
        get
        {
            var fateTable = this.FateTableAddress;
            if (fateTable == IntPtr.Zero)
                return 0;

            // Sonar used this to check if the table was safe to read
            if (Struct->FateDirector == null)
                return 0;

            if (Struct->Fates.First == null || Struct->Fates.Last == null)
                return 0;

            return Struct->Fates.Count;
        }
    }

    /// <summary>
    /// Gets the address of the Fate table.
    /// </summary>
    internal unsafe IntPtr FateTableAddress
    {
        get
        {
            if (this.address.FateTablePtr == IntPtr.Zero)
                return IntPtr.Zero;

            return *(IntPtr*)this.address.FateTablePtr;
        }
    }

    private unsafe FFXIVClientStructs.FFXIV.Client.Game.Fate.FateManager* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Fate.FateManager*)this.FateTableAddress;

    /// <inheritdoc/>
    public IFate? this[int index]
    {
        get
        {
            var address = this.GetFateAddress(index);
            return this.CreateFateReference(address);
        }
    }

    /// <inheritdoc/>
    public bool IsValid(IFate fate)
    {
        var clientState = Service<ClientState>.GetNullable();

        if (fate == null || clientState == null)
            return false;

        if (clientState.LocalContentId == 0)
            return false;

        return true;
    }

    /// <inheritdoc/>
    public unsafe IntPtr GetFateAddress(int index)
    {
        if (index >= this.Length)
            return IntPtr.Zero;

        var fateTable = this.FateTableAddress;
        if (fateTable == IntPtr.Zero)
            return IntPtr.Zero;

        return (IntPtr)this.Struct->Fates[index].Value;
    }

    /// <inheritdoc/>
    public IFate? CreateFateReference(IntPtr offset)
    {
        var clientState = Service<ClientState>.Get();

        if (clientState.LocalContentId == 0)
            return null;

        if (offset == IntPtr.Zero)
            return null;

        return new Fate(offset);
    }
}

/// <summary>
/// This collection represents the currently available Fate events.
/// </summary>
internal sealed partial class FateTable
{
    /// <inheritdoc/>
    int IReadOnlyCollection<IFate>.Count => this.Length;

    /// <inheritdoc/>
    public IEnumerator<IFate> GetEnumerator()
    {
        for (var i = 0; i < this.Length; i++)
        {
            yield return this[i];
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
