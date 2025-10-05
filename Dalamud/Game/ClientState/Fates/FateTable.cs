using System.Collections;
using System.Collections.Generic;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using CSFateManager = FFXIVClientStructs.FFXIV.Client.Game.Fate.FateManager;

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
    [ServiceManager.ServiceConstructor]
    private FateTable()
    {
    }

    /// <inheritdoc/>
    public unsafe IntPtr Address => (nint)CSFateManager.Instance();

    /// <inheritdoc/>
    public unsafe int Length
    {
        get
        {
            var fateManager = CSFateManager.Instance();
            if (fateManager == null)
                return 0;

            // Sonar used this to check if the table was safe to read
            if (fateManager->FateDirector == null)
                return 0;

            if (fateManager->Fates.First == null || fateManager->Fates.Last == null)
                return 0;

            return fateManager->Fates.Count;
        }
    }

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
        if (fate == null)
            return false;

        var playerState = Service<PlayerState.PlayerState>.Get();
        return playerState.IsLoaded == true;
    }

    /// <inheritdoc/>
    public unsafe IntPtr GetFateAddress(int index)
    {
        if (index >= this.Length)
            return IntPtr.Zero;

        var fateManager = CSFateManager.Instance();
        if (fateManager == null)
            return IntPtr.Zero;

        return (IntPtr)fateManager->Fates[index].Value;
    }

    /// <inheritdoc/>
    public IFate? CreateFateReference(IntPtr offset)
    {
        if (offset == IntPtr.Zero)
            return null;

        var playerState = Service<PlayerState.PlayerState>.Get();
        if (!playerState.IsLoaded)
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
