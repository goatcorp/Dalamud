using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Statuses;

/// <summary>
/// This collection represents the status effects an actor is afflicted by.
/// </summary>
public sealed unsafe partial class StatusList
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatusList"/> class.
    /// </summary>
    /// <param name="address">Address of the status list.</param>
    internal StatusList(IntPtr address)
    {
        this.Address = address;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusList"/> class.
    /// </summary>
    /// <param name="pointer">Pointer to the status list.</param>
    internal unsafe StatusList(void* pointer)
        : this((IntPtr)pointer)
    {
    }

    /// <summary>
    /// Gets the address of the status list in memory.
    /// </summary>
    public IntPtr Address { get; }

    /// <summary>
    /// Gets the amount of status effect slots the actor has.
    /// </summary>
    public int Length => Struct->NumValidStatuses;

    private static int StatusSize { get; } = Marshal.SizeOf<FFXIVClientStructs.FFXIV.Client.Game.Status>();

    private FFXIVClientStructs.FFXIV.Client.Game.StatusManager* Struct => (FFXIVClientStructs.FFXIV.Client.Game.StatusManager*)this.Address;

    /// <summary>
    /// Get a status effect at the specified index.
    /// </summary>
    /// <param name="index">Status Index.</param>
    /// <returns>The status at the specified index.</returns>
    public Status? this[int index]
    {
        get
        {
            if (index < 0 || index > this.Length)
                return null;

            var addr = this.GetStatusAddress(index);
            return CreateStatusReference(addr);
        }
    }

    /// <summary>
    /// Create a reference to an FFXIV actor status list.
    /// </summary>
    /// <param name="address">The address of the status list in memory.</param>
    /// <returns>The status object containing the requested data.</returns>
    public static StatusList? CreateStatusListReference(IntPtr address)
    {
        // The use case for CreateStatusListReference and CreateStatusReference to be static is so
        // fake status lists can be generated. Since they aren't exposed as services, it's either
        // here or somewhere else.
        var clientState = Service<ClientState>.Get();

        if (clientState.LocalContentId == 0)
            return null;

        if (address == IntPtr.Zero)
            return null;

        return new StatusList(address);
    }

    /// <summary>
    /// Create a reference to an FFXIV actor status.
    /// </summary>
    /// <param name="address">The address of the status effect in memory.</param>
    /// <returns>The status object containing the requested data.</returns>
    public static Status? CreateStatusReference(IntPtr address)
    {
        var clientState = Service<ClientState>.Get();

        if (clientState.LocalContentId == 0)
            return null;

        if (address == IntPtr.Zero)
            return null;

        return new Status(address);
    }

    /// <summary>
    /// Gets the address of the party member at the specified index of the party list.
    /// </summary>
    /// <param name="index">The index of the party member.</param>
    /// <returns>The memory address of the party member.</returns>
    public IntPtr GetStatusAddress(int index)
    {
        if (index < 0 || index >= this.Length)
            return IntPtr.Zero;

        return (IntPtr)(this.Struct->Status + (index * StatusSize));
    }
}

/// <summary>
/// This collection represents the status effects an actor is afflicted by.
/// </summary>
public sealed partial class StatusList : IReadOnlyCollection<Status>, ICollection
{
    /// <inheritdoc/>
    int IReadOnlyCollection<Status>.Count => this.Length;

    /// <inheritdoc/>
    int ICollection.Count => this.Length;

    /// <inheritdoc/>
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc/>
    object ICollection.SyncRoot => this;

    /// <inheritdoc/>
    public IEnumerator<Status> GetEnumerator()
    {
        for (var i = 0; i < this.Length; i++)
        {
            var status = this[i];

            if (status == null || status.StatusId == 0)
                continue;

            yield return status;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <inheritdoc/>
    void ICollection.CopyTo(Array array, int index)
    {
        for (var i = 0; i < this.Length; i++)
        {
            array.SetValue(this[i], index);
            index++;
        }
    }
}
