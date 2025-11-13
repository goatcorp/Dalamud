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

        var fateManager = CSFateManager.Instance();
        if (fateManager == null)
            return IntPtr.Zero;

        return (IntPtr)fateManager->Fates[index].Value;
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
        return new Enumerator(this);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private struct Enumerator(FateTable fateTable) : IEnumerator<IFate>
    {
        private int index = 0;

        public IFate Current { get; private set; }

        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            if (this.index == fateTable.Length) return false;
            this.Current = fateTable[this.index];
            this.index++;
            return true;
        }

        public void Reset()
        {
            this.index = 0;
        }

        public void Dispose()
        {
        }
    }
}
