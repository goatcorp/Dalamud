using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.Nameplates.Model;
using Dalamud.Hooking;
using Dalamud.IoC.Internal;
using Dalamud.Memory;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Game.Gui.Nameplates;

/// <summary>
/// This class handles interacting with native Nameplate update events and management.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal class NameplateGui : IInternalDisposableService, INameplatesGui
{
    private readonly GameGui gameGui;
    private readonly ObjectTable objectTable;

    private readonly NameplateGuiAddressResolver addresses;
    private readonly IntPtr namePlatePtr;

    private Hook<SetPlayerNameplateDetourDelegate>? setPlayerNameplateDetourHook = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="NameplateGui"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    private NameplateGui(TargetSigScanner sigScanner)
    {
        // Services
        this.gameGui = Service<GameGui>.Get();
        this.objectTable = Service<ObjectTable>.Get();

        // Pointers
        this.namePlatePtr = this.gameGui.GetAddonByName("NamePlate", 1);

        // Address resolver
        this.addresses = new();
        this.addresses.Setup(sigScanner);

        // Hooks
        this.setPlayerNameplateDetourHook = Hook<SetPlayerNameplateDetourDelegate>.FromAddress(this.addresses.SetPlayerNameplateDetour, this.HandleSetPlayerNameplateDetour);
        this.setPlayerNameplateDetourHook.Enable();
    }

    private unsafe delegate IntPtr SetPlayerNameplateDetourDelegate(IntPtr playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, IntPtr titlePtr, IntPtr namePtr, IntPtr freeCompanyPtr, IntPtr prefix, int iconId);

    /// <inheritdoc/>
    public event INameplatesGui.OnNameplateUpdateDelegate OnNameplateUpdate;

    /// <inheritdoc/>
    public void DisposeService()
    {
        this.setPlayerNameplateDetourHook.Dispose();
    }

    /// <inheritdoc/>
    public T? GetNameplateGameObject<T>(INameplateObject nameplateObject) where T : GameObject
    {
        return this.GetNameplateGameObject<T>(nameplateObject.Pointer);
    }

    /// <inheritdoc/>
    public T? GetNameplateGameObject<T>(IntPtr nameplateObjectPtr) where T : GameObject
    {
        // Get the nameplate object array
        var nameplateAddonPtr = this.gameGui.GetAddonByName("NamePlate", 1);
        var nameplateObjectArrayPtrPtr = nameplateAddonPtr + Marshal.OffsetOf(typeof(AddonNamePlate), nameof(AddonNamePlate.NamePlateObjectArray)).ToInt32();
        var nameplateObjectArrayPtr = Marshal.ReadIntPtr(nameplateObjectArrayPtrPtr);

        if (nameplateObjectArrayPtr == IntPtr.Zero)
            return null;

        // Determine the index of the nameplate object within the nameplate object array
        var namePlateObjectSize = Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject));
        var namePlateObjectPtr0 = nameplateObjectArrayPtr + namePlateObjectSize * 0;
        var namePlateIndex = (nameplateObjectPtr.ToInt64() - namePlateObjectPtr0.ToInt64()) / namePlateObjectSize;
        
        if (namePlateIndex < 0 || namePlateIndex >= AddonNamePlate.NumNamePlateObjects)
            return null;

        // Get the nameplate info array
        var nameplateInfoArrayPtr = IntPtr.Zero;
        unsafe
        {
            var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
            nameplateInfoArrayPtr = new IntPtr(&framework->GetUIModule()->GetRaptureAtkModule()->NamePlateInfoArray);
        }

        // Get the nameplate info for the nameplate object
        var namePlateInfoPtr = new IntPtr(nameplateInfoArrayPtr.ToInt64() + Marshal.SizeOf(typeof(RaptureAtkModule.NamePlateInfo)) * namePlateIndex);
        var namePlateInfo = Marshal.PtrToStructure<RaptureAtkModule.NamePlateInfo>(namePlateInfoPtr);

        // Return the object for its object id
        var objectId = namePlateInfo.ObjectId.ObjectId;
        return this.objectTable.SearchById(objectId) as T;
    }

    private IntPtr HandleSetPlayerNameplateDetour(IntPtr playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, IntPtr titlePtr, IntPtr namePtr, IntPtr freeCompanyPtr, IntPtr prefixPtr, int iconId)
    {
        var ptrToFree = new List<nint>();

        if (this.OnNameplateUpdate != null)
        {
            // Create new nameplate info
            var nameplateInfo = new NameplateInfo
            {
                IsTitleAboveName = isTitleAboveName,
                IsTitleVisible = isTitleVisible,
                IconID = (NameplateStatusIcons)iconId,
            };

            // Add known nameplate nodes
            nameplateInfo.Nodes.Add(new(titlePtr, NameplateNodeName.Title));
            nameplateInfo.Nodes.Add(new(namePtr, NameplateNodeName.Name));
            nameplateInfo.Nodes.Add(new(freeCompanyPtr, NameplateNodeName.FreeCompany));
            nameplateInfo.Nodes.Add(new(prefixPtr, NameplateNodeName.Prefix));

            // Create new nameplate object
            var namePlateObj = new NameplateObject(playerNameplateObjectPtr, nameplateInfo);

            // Invoke event
            this.OnNameplateUpdate.Invoke(namePlateObj);

            // Get new states
            isTitleAboveName = nameplateInfo.IsTitleAboveName;
            isTitleVisible = nameplateInfo.IsTitleVisible;
            iconId = (int)nameplateInfo.IconID;

            // Handle changed nodes
            foreach (var node in nameplateInfo.Nodes)
            {
                if (node.HasChanged)
                {
                    var textRaw = node.Text.Encode();
                    node.Pointer = MemoryHelper.Allocate(textRaw.Length);
                    ptrToFree.Add(node.Pointer);
                }
            }
        }

        // Call original
        var result = this.setPlayerNameplateDetourHook.Original(
            playerNameplateObjectPtr,
            isTitleAboveName,
            isTitleVisible,
            titlePtr,
            namePtr,
            freeCompanyPtr,
            prefixPtr,
            iconId);

        // Free pointers
        ptrToFree.ForEach(n => MemoryHelper.Free(n));

        // Return result
        return result;
    }
}
