using System;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

namespace Dalamud.Game.ClientState.Objects;

/// <summary>
/// Get and set various kinds of targets for the player.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<ITargetManager>]
#pragma warning restore SA1015
public sealed unsafe class TargetManager : IServiceType, ITargetManager
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
    public GameObject? Target
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->Target);
        set => this.SetTarget(value);
    }

    /// <inheritdoc/>
    public GameObject? MouseOverTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->MouseOverTarget);
        set => this.SetMouseOverTarget(value);
    }

    /// <inheritdoc/>
    public GameObject? FocusTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->FocusTarget);
        set => this.SetFocusTarget(value);
    }

    /// <inheritdoc/>
    public GameObject? PreviousTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->PreviousTarget);
        set => this.SetPreviousTarget(value);
    }

    /// <inheritdoc/>
    public GameObject? SoftTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->SoftTarget);
        set => this.SetSoftTarget(value);
    }

    private FFXIVClientStructs.FFXIV.Client.Game.Control.TargetSystem* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Control.TargetSystem*)this.Address;

    /// <inheritdoc/>
    public void SetTarget(GameObject? actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero);

    /// <inheritdoc/>
    public void SetMouseOverTarget(GameObject? actor) => this.SetMouseOverTarget(actor?.Address ?? IntPtr.Zero);

    /// <inheritdoc/>
    public void SetFocusTarget(GameObject? actor) => this.SetFocusTarget(actor?.Address ?? IntPtr.Zero);

    /// <inheritdoc/>
    public void SetPreviousTarget(GameObject? actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero);

    /// <inheritdoc/>
    public void SetSoftTarget(GameObject? actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero);

    /// <inheritdoc/>
    public void SetTarget(IntPtr actorAddress) => Struct->Target = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

    /// <inheritdoc/>
    public void SetMouseOverTarget(IntPtr actorAddress) => Struct->MouseOverTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

    /// <inheritdoc/>
    public void SetFocusTarget(IntPtr actorAddress) => Struct->FocusTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

    /// <inheritdoc/>
    public void SetPreviousTarget(IntPtr actorAddress) => Struct->PreviousTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

    /// <inheritdoc/>
    public void SetSoftTarget(IntPtr actorAddress) => Struct->SoftTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

    /// <inheritdoc/>
    public void ClearTarget() => this.SetTarget(IntPtr.Zero);

    /// <inheritdoc/>
    public void ClearMouseOverTarget() => this.SetMouseOverTarget(IntPtr.Zero);

    /// <inheritdoc/>
    public void ClearFocusTarget() => this.SetFocusTarget(IntPtr.Zero);

    /// <inheritdoc/>
    public void ClearPreviousTarget() => this.SetPreviousTarget(IntPtr.Zero);

    /// <inheritdoc/>
    public void ClearSoftTarget() => this.SetSoftTarget(IntPtr.Zero);
}
