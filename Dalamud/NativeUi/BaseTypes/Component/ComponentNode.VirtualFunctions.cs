using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Component;

/// <summary>
/// .
/// </summary>
internal abstract unsafe partial class ComponentNode
{
    /// <summary>
    /// Global event callback for events that the game wired up to this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="eventParam">Event param.</param>
    /// <param name="atkEvent">Event struct.</param>
    /// <param name="atkEventData">Event data.</param>
    protected virtual void OnReceiveGlobalEvent(AtkComponentBase* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        this.originalVirtualTable->ReceiveGlobalEvent(thisPtr, eventType, eventParam, atkEvent, atkEventData);
    }

    /// <summary>
    /// Event callback for events that the game wired up to this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="eventParam">Event param.</param>
    /// <param name="atkEvent">Event struct.</param>
    /// <param name="atkEventData">Event data.</param>
    protected virtual void OnReceiveEvent(AtkComponentBase* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        this.originalVirtualTable->ReceiveEvent(thisPtr, eventType, eventParam, atkEvent, atkEventData);
    }

    /// <summary>
    /// Initialize callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    protected virtual void OnInitialize(AtkComponentBase* thisPtr)
    {
        this.originalVirtualTable->Initialize(thisPtr);
    }

    /// <summary>
    /// Unloading callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    protected virtual void OnDeinitialize(AtkComponentBase* thisPtr)
    {
        this.originalVirtualTable->Deinitialize(thisPtr);
    }

    /// <summary>
    /// Per-frame update callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <param name="delta">Time since last update.</param>
    protected virtual void OnUpdate(AtkComponentBase* thisPtr, float delta)
    {
        this.originalVirtualTable->Update(thisPtr, delta);
    }

    /// <summary>
    /// Draw callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    protected virtual void OnDraw(AtkComponentBase* thisPtr)
    {
        this.originalVirtualTable->Draw(thisPtr);
    }

    /// <summary>
    /// Setup callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    protected virtual void OnSetup(AtkComponentBase* thisPtr)
    {
        this.originalVirtualTable->Setup(thisPtr);
    }

    /// <summary>
    /// Enable state changed callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <param name="enabled">If this component should be set as enabled.</param>
    protected virtual void OnSetEnabledState(AtkComponentBase* thisPtr, bool enabled)
    {
        this.originalVirtualTable->SetEnabledState(thisPtr, enabled);
    }

    /// <summary>
    /// Play sound effect callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    protected virtual void OnPlaySoundEffect(AtkComponentBase* thisPtr)
    {
        this.originalVirtualTable->PlaySoundEffect(thisPtr);
    }

    /// <summary>
    /// GetAtkResNode callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <returns>Pointer to AtkResNode.</returns>
    protected virtual AtkResNode* OnGetAtkResNode(AtkComponentBase* thisPtr)
    {
        return this.originalVirtualTable->GetAtkResNode(thisPtr);
    }

    /// <summary>
    /// GetFocusNode callback for this component.
    /// </summary>
    /// <remarks>
    /// Overriden to return <see cref="FocusNode"/>.
    /// </remarks>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <returns>Pointer to the node that should be focused.</returns>
    protected virtual AtkResNode* OnGetFocusNode(AtkComponentBase* thisPtr)
    {
        return this.FocusNode;
    }

    /// <summary>
    /// Initialization from data callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <param name="data">Pointer to AtkUldComponentDataBase or derived type.</param>
    protected virtual void OnInitializeFromComponentData(AtkComponentBase* thisPtr, void* data)
    {
        this.originalVirtualTable->InitializeFromComponentData(thisPtr, data);
    }

    private AtkEventListener* Destructor(AtkComponentBase* thisPtr, byte freeFlags)
    {
        var result = this.originalVirtualTable->Dtor(thisPtr, freeFlags);

        if ((freeFlags & 1) == 1)
        {
            // Free our custom virtual table, the game doesn't know this exists and won't clear it on its own.
            IMemorySpace.Free(this.modifiedVirtualTable, 0x8 * VirtualTableEntryCount);
            this.modifiedVirtualTable = null;
        }

        return result;
    }
}
