using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

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
        get => this.objectTable.CreateObjectReference((nint)this.Struct->GetHardTarget());
        set => this.Struct->SetHardTarget((CSGameObject*)(value?.Address ?? 0));
    }

    /// <inheritdoc/>
    public IGameObject? MouseOverTarget
    {
        get => this.objectTable.CreateObjectReference((nint)this.Struct->MouseOverTarget);
        set => this.Struct->MouseOverTarget = (CSGameObject*)(value?.Address ?? 0);
    }

    /// <inheritdoc/>
    public IGameObject? FocusTarget
    {
        get => this.objectTable.CreateObjectReference((nint)this.Struct->FocusTarget);
        set => this.Struct->FocusTarget = (CSGameObject*)(value?.Address ?? 0);
    }

    /// <inheritdoc/>
    public IGameObject? PreviousTarget
    {
        get => this.objectTable.CreateObjectReference((nint)this.Struct->PreviousTarget);
        set => this.Struct->PreviousTarget = (CSGameObject*)(value?.Address ?? 0);
    }

    /// <inheritdoc/>
    public IGameObject? SoftTarget
    {
        get => this.objectTable.CreateObjectReference((nint)this.Struct->GetSoftTarget());
        set => this.Struct->SetSoftTarget((CSGameObject*)(value?.Address ?? 0));
    }

    /// <inheritdoc/>
    public IGameObject? GPoseTarget
    {
        get => this.objectTable.CreateObjectReference((nint)this.Struct->GPoseTarget);
        set => this.Struct->GPoseTarget = (CSGameObject*)(value?.Address ?? 0);
    }

    /// <inheritdoc/>
    public IGameObject? MouseOverNameplateTarget
    {
        get => this.objectTable.CreateObjectReference((nint)this.Struct->MouseOverNameplateTarget);
        set => this.Struct->MouseOverNameplateTarget = (CSGameObject*)(value?.Address ?? 0);
    }

    private TargetSystem* Struct => TargetSystem.Instance();
}
