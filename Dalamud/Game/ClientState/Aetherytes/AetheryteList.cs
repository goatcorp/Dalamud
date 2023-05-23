using System.Collections;
using System.Collections.Generic;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Serilog;

namespace Dalamud.Game.ClientState.Aetherytes;

/// <summary>
/// This collection represents the list of available Aetherytes in the Teleport window.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
public sealed unsafe partial class AetheryteList : IServiceType
{
    [ServiceManager.ServiceDependency]
    private readonly ClientState clientState = Service<ClientState>.Get();

    private readonly Telepo* telepoInstance = Telepo.Instance();

    [ServiceManager.ServiceConstructor]
    private AetheryteList()
    {
        Log.Verbose($"Teleport address 0x{((nint)this.telepoInstance).ToInt64():X}");
    }

    /// <summary>
    /// Gets the amount of Aetherytes the local player has unlocked.
    /// </summary>
    public int Length
    {
        get
        {
            if (this.clientState.LocalPlayer == null)
                return 0;

            this.Update();

            if (this.telepoInstance->TeleportList.First == this.telepoInstance->TeleportList.Last)
                return 0;

            return (int)this.telepoInstance->TeleportList.Size();
        }
    }

    /// <summary>
    /// Gets a Aetheryte Entry at the specified index.
    /// </summary>
    /// <param name="index">Index.</param>
    /// <returns>A <see cref="AetheryteEntry"/> at the specified index.</returns>
    public AetheryteEntry? this[int index]
    {
        get
        {
            if (index < 0 || index >= this.Length)
            {
                return null;
            }

            if (this.clientState.LocalPlayer == null)
                return null;

            return new AetheryteEntry(this.telepoInstance->TeleportList.Get((ulong)index));
        }
    }

    private void Update()
    {
        // this is very very important as otherwise it crashes
        if (this.clientState.LocalPlayer == null)
            return;

        this.telepoInstance->UpdateAetheryteList();
    }
}

/// <summary>
/// This collection represents the list of available Aetherytes in the Teleport window.
/// </summary>
public sealed partial class AetheryteList : IReadOnlyCollection<AetheryteEntry>
{
    /// <inheritdoc/>
    public int Count => this.Length;

    /// <inheritdoc/>
    public IEnumerator<AetheryteEntry> GetEnumerator()
    {
        for (var i = 0; i < this.Length; i++)
        {
            yield return this[i];
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
