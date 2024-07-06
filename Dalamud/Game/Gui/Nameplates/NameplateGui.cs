using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.Nameplates.Model;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Game.Gui.Nameplates;

/// <summary>
/// This class handles interacting with native Nameplate update events and management.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class NameplateGui : IInternalDisposableService, INameplateGui
{
    [ServiceManager.ServiceDependency]
    private readonly GameGui gameGui = Service<GameGui>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ObjectTable objectTable = Service<ObjectTable>.Get();

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycle = Service<AddonLifecycle>.Get();

    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 5C 24 ?? 45 38 BE", DetourName = nameof(HandleSetPlayerNameplateDetour))]
    private readonly Hook<SetPlayerNameplateDetourDelegate> setPlayerNameplateDetourHook;
    private readonly AddonLifecycleEventListener listenerPostSetup;
    private readonly AddonLifecycleEventListener listenerPreFinalize;
    private nint namePlatePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NameplateGui"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    private NameplateGui(TargetSigScanner sigScanner)
    {
        // Pointers
        this.listenerPostSetup = new AddonLifecycleEventListener(AddonEvent.PostSetup, "NamePlate", this.AddonLifecycle_PostSetup);
        this.listenerPreFinalize = new AddonLifecycleEventListener(AddonEvent.PreFinalize, "NamePlate", this.AddonLifecycle_PreFinalize);
        this.addonLifecycle.RegisterListener(this.listenerPostSetup);
        this.addonLifecycle.RegisterListener(this.listenerPreFinalize);

        // Hooks
        SignatureHelper.Initialize(this);
        this.setPlayerNameplateDetourHook.Enable();
    }

    private unsafe delegate nint SetPlayerNameplateDetourDelegate(nint playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, nint titlePtr, nint namePtr, nint freeCompanyPtr, nint prefix, int iconId);

    /// <inheritdoc/>
    public event INameplateGui.OnNameplateUpdateDelegate OnNameplateUpdate;

    /// <inheritdoc/>
    public void DisposeService()
    {
        this.setPlayerNameplateDetourHook.Dispose();
        this.addonLifecycle.UnregisterListener(this.listenerPostSetup);
        this.addonLifecycle.UnregisterListener(this.listenerPreFinalize);
    }

    /// <summary>
    /// Gets the index of a given nameplate.
    /// </summary>
    /// <param name="nameplateObjectPtr">The nameplate object pointer where you want the index from.</param>
    /// <returns>Returns the index for the given nameplate.</returns>
    public long GetNameplateIndex(nint nameplateObjectPtr)
    {
        // Get the nameplate object array
        var nameplateObjectArrayPtrPtr = this.namePlatePtr + Marshal.OffsetOf(typeof(AddonNamePlate), nameof(AddonNamePlate.NamePlateObjectArray)).ToInt32();
        var nameplateObjectArrayPtr = Marshal.ReadIntPtr(nameplateObjectArrayPtrPtr);

        if (nameplateObjectArrayPtr == nint.Zero)
            return -1;

        // Determine the index of the nameplate object within the nameplate object array
        var namePlateObjectSize = Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject));
        var namePlateObjectPtr0 = nameplateObjectArrayPtr + namePlateObjectSize * 0;
        var namePlateIndex = (nameplateObjectPtr.ToInt64() - namePlateObjectPtr0.ToInt64()) / namePlateObjectSize;

        if (namePlateIndex < 0 || namePlateIndex >= AddonNamePlate.NumNamePlateObjects)
            return -1;

        return namePlateIndex;
    }

    /// <summary>
    /// Gets the corresponding <see cref="GameObject"/> for the nameplate by its index.
    /// </summary>
    /// <param name="namePlateIndex">The index of the namplate where you want to get the <see cref="GameObject"/> for.</param>
    /// <returns>Returns the corresponding <see cref="GameObject"/> for the nameplate.</returns>
    public IGameObject? GetNameplateGameObject(long namePlateIndex)
    {
        if (namePlateIndex == -1)
            return null;

        // Get the nameplate info
        RaptureAtkModule.NamePlateInfo namePlateInfo;
        unsafe
        {
            var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
            namePlateInfo = framework->GetUIModule()->GetRaptureAtkModule()->NamePlateInfoEntries[(int)namePlateIndex];
        }

        // Return the object for its object id
        var objectId = namePlateInfo.ObjectId.ObjectId;
        return this.objectTable.SearchById(objectId);
    }

    private void AddonLifecycle_PostSetup(AddonEvent type, AddonArgs args)
    {
        if (type == AddonEvent.PostSetup)
            this.namePlatePtr = args.Addon;
    }

    private void AddonLifecycle_PreFinalize(AddonEvent type, AddonArgs args)
    {
        if (type == AddonEvent.PreFinalize)
            this.namePlatePtr = nint.Zero;
    }

    private unsafe nint HandleSetPlayerNameplateDetour(nint playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, nint titlePtr, nint namePtr, nint freeCompanyPtr, nint prefixPtr, int iconId)
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

            // Add known nameplate elements
            nameplateInfo.Elements.AddRange([
                new(titlePtr, NameplateElementType.Title),
                new(namePtr, NameplateElementType.Name),
                new(freeCompanyPtr, NameplateElementType.FreeCompany),
                new(prefixPtr, NameplateElementType.Prefix)
            ]);

            // Create new nameplate object
            var namePlateObj = new NameplateObject(playerNameplateObjectPtr, nameplateInfo);

            // Invoke event
            this.OnNameplateUpdate.Invoke(namePlateObj);

            // Get new states
            isTitleAboveName = nameplateInfo.IsTitleAboveName;
            isTitleVisible = nameplateInfo.IsTitleVisible;
            iconId = (int)nameplateInfo.IconID;

            // Handle changed elements
            foreach (var element in nameplateInfo.Elements)
            {
                if (element.HasChanged)
                {
                    // Copy back known element properties
                    switch (element.Type)
                    {
                        case NameplateElementType.Title:
                            titlePtr = this.PluginAllocate(element.Text);
                            break;
                        case NameplateElementType.Name:
                            namePtr = this.PluginAllocate(element.Text);
                            break;
                        case NameplateElementType.FreeCompany:
                            freeCompanyPtr = this.PluginAllocate(element.Text);
                            break;
                        case NameplateElementType.Prefix:
                            prefixPtr = this.PluginAllocate(element.Text);
                            break;
                    }
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
        if (ptrToFree.Count > 0)
            ptrToFree.ForEach(this.PluginFree);

        // Return result
        return result;
    }

    private nint PluginAllocate(SeString seString)
    {
        return this.PluginAllocate(seString.Encode());
    }

    private nint PluginAllocate(byte[] bytes)
    {
        var pointer = Marshal.AllocHGlobal(bytes.Length + 1);
        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        Marshal.WriteByte(pointer, bytes.Length, 0);
        return pointer;
    }

    private void PluginFree(nint ptr)
    {
        Marshal.FreeHGlobal(ptr);
    }
}
