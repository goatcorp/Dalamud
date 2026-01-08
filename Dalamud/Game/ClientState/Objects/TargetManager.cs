using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

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
    private readonly ObjectTable objectTable = Service<ObjectTable>.Get();

    [ServiceManager.ServiceConstructor]
    private TargetManager()
    {
    }

    /// <inheritdoc/>
    public IGameObject? Target
    {
        get => this.objectTable.CreateObjectReference((IntPtr)this.Struct->GetHardTarget());
        set => this.Struct->SetHardTarget((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address);
    }

    /// <inheritdoc/>
    public IGameObject? MouseOverTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)this.Struct->MouseOverTarget);
        set => this.Struct->MouseOverTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    /// <inheritdoc/>
    public IGameObject? FocusTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)this.Struct->FocusTarget);
        set => this.Struct->FocusTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    /// <inheritdoc/>
    public IGameObject? PreviousTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)this.Struct->PreviousTarget);
        set => this.Struct->PreviousTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    /// <inheritdoc/>
    public IGameObject? SoftTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)this.Struct->GetSoftTarget());
        set => this.Struct->SetSoftTarget((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address);
    }

    /// <inheritdoc/>
    public IGameObject? GPoseTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)this.Struct->GPoseTarget);
        set => this.Struct->GPoseTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    /// <inheritdoc/>
    public IGameObject? MouseOverNameplateTarget
    {
        get => this.objectTable.CreateObjectReference((IntPtr)this.Struct->MouseOverNameplateTarget);
        set => this.Struct->MouseOverNameplateTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)value?.Address;
    }

    private TargetSystem* Struct => TargetSystem.Instance();
}
