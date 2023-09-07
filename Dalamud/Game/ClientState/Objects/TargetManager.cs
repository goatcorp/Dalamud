using System;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
#pragma warning disable CS0618

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

    /// <inheritdoc/>
    public GameObject? GPoseTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->GPoseTarget);
        set => Struct->GPoseTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }
    
    /// <inheritdoc/>
    public GameObject? MouseOverNameplateTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)Struct->MouseOverNameplateTarget);
        set => Struct->MouseOverNameplateTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    private FFXIVClientStructs.FFXIV.Client.Game.Control.TargetSystem* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Control.TargetSystem*)this.Address;

    /// <summary>
    /// Sets the current target.
    /// </summary>
    /// <param name="actor">Actor to target.</param>
    [Obsolete("Use Target Property", false)]
    public void SetTarget(GameObject? actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero);

    /// <summary>
    /// Sets the mouseover target.
    /// </summary>
    /// <param name="actor">Actor to target.</param>
    [Obsolete("Use MouseOverTarget Property", false)]
    public void SetMouseOverTarget(GameObject? actor) => this.SetMouseOverTarget(actor?.Address ?? IntPtr.Zero);

    /// <summary>
    /// Sets the focus target.
    /// </summary>
    /// <param name="actor">Actor to target.</param>
    [Obsolete("Use FocusTarget Property", false)]
    public void SetFocusTarget(GameObject? actor) => this.SetFocusTarget(actor?.Address ?? IntPtr.Zero);

    /// <summary>
    /// Sets the previous target.
    /// </summary>
    /// <param name="actor">Actor to target.</param>
    [Obsolete("Use PreviousTarget Property", false)]
    public void SetPreviousTarget(GameObject? actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero);

    /// <summary>
    /// Sets the soft target.
    /// </summary>
    /// <param name="actor">Actor to target.</param>
    [Obsolete("Use SoftTarget Property", false)]
    public void SetSoftTarget(GameObject? actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero);

    /// <summary>
    /// Sets the current target.
    /// </summary>
    /// <param name="actorAddress">Actor (address) to target.</param>
    [Obsolete("Use Target Property", false)]
    public void SetTarget(IntPtr actorAddress) => Struct->Target = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

    /// <summary>
    /// Sets the mouseover target.
    /// </summary>
    /// <param name="actorAddress">Actor (address) to target.</param>
    [Obsolete("Use MouseOverTarget Property", false)]
    public void SetMouseOverTarget(IntPtr actorAddress) => Struct->MouseOverTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

    /// <summary>
    /// Sets the focus target.
    /// </summary>
    /// <param name="actorAddress">Actor (address) to target.</param>
    [Obsolete("Use FocusTarget Property", false)]
    public void SetFocusTarget(IntPtr actorAddress) => Struct->FocusTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

    /// <summary>
    /// Sets the previous target.
    /// </summary>
    /// <param name="actorAddress">Actor (address) to target.</param>
    [Obsolete("Use PreviousTarget Property", false)]
    public void SetPreviousTarget(IntPtr actorAddress) => Struct->PreviousTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

    /// <summary>
    /// Sets the soft target.
    /// </summary>
    /// <param name="actorAddress">Actor (address) to target.</param>
    [Obsolete("Use SoftTarget Property", false)]
    public void SetSoftTarget(IntPtr actorAddress) => Struct->SoftTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

    /// <summary>
    /// Clears the current target.
    /// </summary>
    [Obsolete("Use Target Property", false)]
    public void ClearTarget() => this.SetTarget(IntPtr.Zero);

    /// <summary>
    /// Clears the mouseover target.
    /// </summary>
    [Obsolete("Use MouseOverTarget Property", false)]
    public void ClearMouseOverTarget() => this.SetMouseOverTarget(IntPtr.Zero);

    /// <summary>
    /// Clears the focus target.
    /// </summary>
    [Obsolete("Use FocusTarget Property", false)]
    public void ClearFocusTarget() => this.SetFocusTarget(IntPtr.Zero);

    /// <summary>
    /// Clears the previous target.
    /// </summary>
    [Obsolete("Use PreviousTarget Property", false)]
    public void ClearPreviousTarget() => this.SetPreviousTarget(IntPtr.Zero);

    /// <summary>
    /// Clears the soft target.
    /// </summary>
    [Obsolete("Use SoftTarget Property", false)]
    public void ClearSoftTarget() => this.SetSoftTarget(IntPtr.Zero);
}
