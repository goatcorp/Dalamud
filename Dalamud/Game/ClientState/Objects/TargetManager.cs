using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.IoC.Internal;

#pragma warning disable CS0618

namespace Dalamud.Game.ClientState.Objects;

/// <summary>
/// Get and set various kinds of targets for the player.
/// </summary>
[PluginInterface]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<ITargetManager>]
#pragma warning restore SA1015
internal sealed unsafe class TargetManager : IServiceType, ITargetManager
{
    [ServiceManager.ServiceDependency]
    private readonly ClientState clientState = Service<ClientState>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ObjectTable objectTable = Service<ObjectTable>.Get();

    private readonly ClientStateAddressResolver address;

    [ServiceManager.ServiceConstructor]
    private TargetManager()
    {
        this.address = this.clientState.AddressResolver;
    }

    /// <inheritdoc/>
    public IntPtr Address => this.address.TargetManager;

    /// <inheritdoc/>
    public IGameObject? Target
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->Target);
        set => Struct->Target = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    /// <inheritdoc/>
    public IGameObject? MouseOverTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->MouseOverTarget);
        set => Struct->MouseOverTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    /// <inheritdoc/>
    public IGameObject? FocusTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->FocusTarget);
        set => Struct->FocusTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    /// <inheritdoc/>
    public IGameObject? PreviousTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->PreviousTarget);
        set => Struct->PreviousTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    /// <inheritdoc/>
    public IGameObject? SoftTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->SoftTarget);
        set => Struct->SoftTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    /// <inheritdoc/>
    public IGameObject? GPoseTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->GPoseTarget);
        set => Struct->GPoseTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }
    
    /// <inheritdoc/>
    public IGameObject? MouseOverNameplateTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->MouseOverNameplateTarget);
        set => Struct->MouseOverNameplateTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    private FFXIVClientStructs.FFXIV.Client.Game.Control.TargetSystem* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Control.TargetSystem*)this.Address;
}
