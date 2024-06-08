using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.Nameplates.EventArgs;
using Dalamud.Game.Gui.Nameplates.Model;
using Dalamud.Game.Text.SeStringHandling;
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
    public T? GetNameplateGameObject<T>(NameplateObject nameplateObject) where T : GameObject
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
            nameplateInfoArrayPtr = new IntPtr(&framework->GetUiModule()->GetRaptureAtkModule()->NamePlateInfoArray);
        }

        // Get the nameplate info for the nameplate object
        var namePlateInfoPtr = new IntPtr(nameplateInfoArrayPtr.ToInt64() + Marshal.SizeOf(typeof(RaptureAtkModule.NamePlateInfo)) * namePlateIndex);
        var namePlateInfo = Marshal.PtrToStructure<RaptureAtkModule.NamePlateInfo>(namePlateInfoPtr);

        // Return the object for its object id
        var objectId = namePlateInfo.ObjectID.ObjectID;
        return this.objectTable.SearchById(objectId) as T;
    }

    private IntPtr HandleSetPlayerNameplateDetour(IntPtr playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, IntPtr titlePtr, IntPtr namePtr, IntPtr freeCompanyPtr, IntPtr prefixPtr, int iconId)
    {

        if (this.OnNameplateUpdate != null)
        {
            // Create NamePlateObject if possible
            var namePlateObj = new NameplateObject(playerNameplateObjectPtr);

            // Create new event
            var nameplateInfo = new NameplateInfo
            {
                Title = MemoryHelper.ReadSeStringNullTerminated(titlePtr),
                Name = MemoryHelper.ReadSeStringNullTerminated(namePtr),
                FreeCompany = MemoryHelper.ReadSeStringNullTerminated(freeCompanyPtr),
                Prefix = MemoryHelper.ReadSeStringNullTerminated(prefixPtr),
                IsTitleAboveName = isTitleAboveName,
                IsTitleVisible = isTitleVisible,
                IconID = (StatusIcons)iconId,
            };

            // Invoke event
            var eventArgs = new NameplateUpdateEventArgs(nameplateInfo, namePlateObj);
            this.OnNameplateUpdate.Invoke(eventArgs);

            if (eventArgs.HasChanged)
            {
                // Get new states
                isTitleAboveName = nameplateInfo.IsTitleAboveName;
                isTitleVisible = nameplateInfo.IsTitleVisible;
                iconId = (int)nameplateInfo.IconID;

                // Get new Title string content
                var titleRaw = nameplateInfo.Title.Encode();
                var titleNewRaw = nameplateInfo.Title.Encode();
                if (!titleRaw.SequenceEqual(titleNewRaw))
                    MemoryHelper.WriteSeString(titlePtr, nameplateInfo.Title);

                // Get new Name string content
                var nameRaw = nameplateInfo.Name.Encode();
                var nameNewRaw = nameplateInfo.Name.Encode();
                if (!nameRaw.SequenceEqual(nameNewRaw))
                    MemoryHelper.WriteSeString(namePtr, nameplateInfo.Name);

                // Get new Free Company string content
                var freeCompanyRaw = nameplateInfo.FreeCompany.Encode();
                var freeCompanyNewRaw = nameplateInfo.FreeCompany.Encode();
                if (!freeCompanyRaw.SequenceEqual(freeCompanyNewRaw))
                    MemoryHelper.WriteSeString(freeCompanyPtr, nameplateInfo.FreeCompany);

                // Get new Prefix string content
                var prefixRaw = nameplateInfo.Prefix.Encode();
                var prefixNewRaw = nameplateInfo.Prefix.Encode();
                if (!prefixRaw.SequenceEqual(prefixNewRaw))
                    MemoryHelper.WriteSeString(prefixPtr, nameplateInfo.Prefix);
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

        // Return result
        return result;
    }
}
