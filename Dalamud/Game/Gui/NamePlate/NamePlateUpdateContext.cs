using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.NamePlate;

/// <summary>
/// Contains information related to the pending nameplate data update. This is only valid for a single frame and should
/// not be kept across frames.
/// </summary>
public interface INamePlateUpdateContext
{
    /// <summary>
    /// Gets the number of active nameplates. The actual number visible may be lower than this in cases where some
    /// nameplates are hidden by default (based on in-game "Display Name Settings" and so on).
    /// </summary>
    int ActiveNamePlateCount { get; }

    /// <summary>
    /// Gets a value indicating whether the game is currently performing a full update of all active nameplates.
    /// </summary>
    bool IsFullUpdate { get; }

    /// <summary>
    /// Gets the address of the NamePlate addon.
    /// </summary>
    nint AddonAddress { get; }

    /// <summary>
    /// Gets the address of the NamePlate addon's number array data container.
    /// </summary>
    nint NumberArrayDataAddress { get; }

    /// <summary>
    /// Gets the address of the NamePlate addon's string array data container.
    /// </summary>
    nint StringArrayDataAddress { get; }

    /// <summary>
    /// Gets the address of the first entry in the NamePlate addon's int array.
    /// </summary>
    nint NumberArrayDataEntryAddress { get; }
}

/// <summary>
/// Contains information related to the pending nameplate data update. This is only valid for a single frame and should
/// not be kept across frames.
/// </summary>
internal unsafe class NamePlateUpdateContext : INamePlateUpdateContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamePlateUpdateContext"/> class.
    /// </summary>
    /// <param name="objectTable">An object table.</param>
    /// <param name="args">The addon lifecycle arguments for the update request.</param>
    internal NamePlateUpdateContext(ObjectTable objectTable, AddonRequestedUpdateArgs args)
    {
        this.ObjectTable = objectTable;
        this.RaptureAtkModule = FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkModule.Instance();
        this.Ui3DModule = UIModule.Instance()->GetUI3DModule();
        this.ResetState(args);
    }

    /// <summary>
    /// Gets the number of active nameplates. The actual number visible may be lower than this in cases where some
    /// nameplates are hidden by default (based on in-game "Display Name Settings" and so on).
    /// </summary>
    public int ActiveNamePlateCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the game is currently performing a full update of all active nameplates.
    /// </summary>
    public bool IsFullUpdate { get; private set; }

    /// <summary>
    /// Gets the address of the NamePlate addon.
    /// </summary>
    public nint AddonAddress => (nint)this.Addon;

    /// <summary>
    /// Gets the address of the NamePlate addon's number array data container.
    /// </summary>
    public nint NumberArrayDataAddress => (nint)this.NumberData;

    /// <summary>
    /// Gets the address of the NamePlate addon's string array data container.
    /// </summary>
    public nint StringArrayDataAddress => (nint)this.StringData;

    /// <summary>
    /// Gets the address of the first entry in the NamePlate addon's int array.
    /// </summary>
    public nint NumberArrayDataEntryAddress => (nint)this.NumberStruct;

    /// <summary>
    /// Gets the RaptureAtkModule.
    /// </summary>
    internal RaptureAtkModule* RaptureAtkModule { get; }

    /// <summary>
    /// Gets the Ui3DModule.
    /// </summary>
    internal UI3DModule* Ui3DModule { get; }

    /// <summary>
    /// Gets the ObjectTable.
    /// </summary>
    internal ObjectTable ObjectTable { get; }

    /// <summary>
    /// Gets a pointer to the NamePlate addon.
    /// </summary>
    internal AddonNamePlate* Addon { get; private set; }

    /// <summary>
    /// Gets a pointer to the NamePlate addon's number array data container.
    /// </summary>
    internal NumberArrayData* NumberData { get; private set; }

    /// <summary>
    /// Gets a pointer to the NamePlate addon's string array data container.
    /// </summary>
    internal StringArrayData* StringData { get; private set; }

    /// <summary>
    /// Gets a pointer to the NamePlate addon's number array entries as a struct.
    /// </summary>
    internal AddonNamePlate.NamePlateIntArrayData* NumberStruct { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether any handler in the current context has instantiated a part builder.
    /// </summary>
    internal bool HasParts { get; set; }

    /// <summary>
    /// Resets the state of the context based on the provided addon lifecycle arguments.
    /// </summary>
    /// <param name="args">The addon lifecycle arguments for the update request.</param>
    internal void ResetState(AddonRequestedUpdateArgs args)
    {
        this.Addon = (AddonNamePlate*)args.Addon;
        this.NumberData = ((NumberArrayData**)args.NumberArrayData)![NamePlateGui.NumberArrayIndex];
        this.NumberStruct = (AddonNamePlate.NamePlateIntArrayData*)this.NumberData->IntArray;
        this.StringData = ((StringArrayData**)args.StringArrayData)![NamePlateGui.StringArrayIndex];
        this.HasParts = false;

        this.ActiveNamePlateCount = this.NumberStruct->ActiveNamePlateCount;
        this.IsFullUpdate = this.Addon->DoFullUpdate != 0;
    }
}
