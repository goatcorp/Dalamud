using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.Nameplates.Model;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

using TerraFX.Interop.Windows;

namespace Dalamud.Game.Gui.Nameplates;

/// <summary>
/// This class handles interacting with native Nameplate update events and management.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class NameplateGui : IInternalDisposableService, INameplatesGui
{
    [ServiceManager.ServiceDependency]
    private readonly GameGui gameGui = Service<GameGui>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ObjectTable objectTable = Service<ObjectTable>.Get();

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycle = Service<AddonLifecycle>.Get();

    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 5C 24 ?? 45 38 BE", DetourName = nameof(HandleSetPlayerNameplateDetour))]
    private readonly Hook<SetPlayerNameplateDetourDelegate> setPlayerNameplateDetourHook;
    private nint namePlatePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NameplateGui"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    private NameplateGui(TargetSigScanner sigScanner)
    {
        // Pointers
        this.addonLifecycle.RegisterListener(new AddonLifecycleEventListener(AddonEvent.PostSetup, "NamePlate", this.AddonLifecycle_Event));
        this.addonLifecycle.RegisterListener(new AddonLifecycleEventListener(AddonEvent.PreFinalize, "NamePlate", this.AddonLifecycle_Event));

        // Hooks
        SignatureHelper.Initialize(this);
        this.setPlayerNameplateDetourHook.Enable();
    }

    private unsafe delegate nint SetPlayerNameplateDetourDelegate(nint playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, nint titlePtr, nint namePtr, nint freeCompanyPtr, nint prefix, int iconId);

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
    public T? GetNameplateGameObject<T>(nint nameplateObjectPtr) where T : GameObject
    {
        // Get the nameplate object array
        var nameplateObjectArrayPtrPtr = this.namePlatePtr + Marshal.OffsetOf(typeof(AddonNamePlate), nameof(AddonNamePlate.NamePlateObjectArray)).ToInt32();
        var nameplateObjectArrayPtr = Marshal.ReadIntPtr(nameplateObjectArrayPtrPtr);

        if (nameplateObjectArrayPtr == nint.Zero)
            return null;

        // Determine the index of the nameplate object within the nameplate object array
        var namePlateObjectSize = Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject));
        var namePlateObjectPtr0 = nameplateObjectArrayPtr + namePlateObjectSize * 0;
        var namePlateIndex = (nameplateObjectPtr.ToInt64() - namePlateObjectPtr0.ToInt64()) / namePlateObjectSize;
        
        if (namePlateIndex < 0 || namePlateIndex >= AddonNamePlate.NumNamePlateObjects)
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
        return this.objectTable.SearchById(objectId) as T;
    }

    private void AddonLifecycle_Event(AddonEvent type, AddonArgs args)
    {
        if (type == AddonEvent.PreFinalize)
            this.namePlatePtr = args.Addon;
        else if (type == AddonEvent.PreFinalize)
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

            // Add known nameplate nodes
            nameplateInfo.Nodes.AddRange([
                new(titlePtr, NameplateNodeName.Title),
                new(namePtr, NameplateNodeName.Name),
                new(freeCompanyPtr, NameplateNodeName.FreeCompany),
                new(prefixPtr, NameplateNodeName.Prefix)
            ]);

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
                    // Copy back known node properties
                    switch (node.Name)
                    {
                        case NameplateNodeName.Title:
                            titlePtr = this.PluginAllocate(node.Text);
                            //MemoryHelper.WriteSeString(titlePtr, node.Text);
                            break;
                        case NameplateNodeName.Name:
                            namePtr = this.PluginAllocate(node.Text);
                            //MemoryHelper.WriteSeString(namePtr, node.Text);
                            break;
                        case NameplateNodeName.FreeCompany:
                            freeCompanyPtr = this.PluginAllocate(node.Text);
                            //MemoryHelper.WriteSeString(freeCompanyPtr, node.Text);
                            break;
                        case NameplateNodeName.Prefix:
                            prefixPtr = this.PluginAllocate(node.Text);
                            //MemoryHelper.WriteSeString(prefixPtr, node.Text);
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
