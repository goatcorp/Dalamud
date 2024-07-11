using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;

using FFXIVClientStructs.FFXIV.Component.GUI;
using Serilog;

using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Dalamud.Game.Internal;

/// <summary>
/// This class implements in-game Dalamud options in the in-game System menu.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class DalamudAtkTweaks : IInternalDisposableService
{
    private readonly Hook<AgentHudOpenSystemMenuPrototype> hookAgentHudOpenSystemMenu;

    // TODO: Make this into events in Framework.Gui
    private readonly Hook<UiModuleRequestMainCommand> hookUiModuleRequestMainCommand;

    private readonly Hook<AtkUnitBaseReceiveGlobalEvent> hookAtkUnitBaseReceiveGlobalEvent;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    // [ServiceManager.ServiceDependency]
    // private readonly ContextMenu contextMenu = Service<ContextMenu>.Get();

    private readonly string locDalamudPlugins;
    private readonly string locDalamudSettings;

    private bool disposed = false;

    [ServiceManager.ServiceConstructor]
    private DalamudAtkTweaks(TargetSigScanner sigScanner)
    {
        var openSystemMenuAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B CF 4C 89 B4 24 B8 08 00 00");

        this.hookAgentHudOpenSystemMenu = Hook<AgentHudOpenSystemMenuPrototype>.FromAddress(openSystemMenuAddress, this.AgentHudOpenSystemMenuDetour);

        var uiModuleRequestMainCommandAddress = sigScanner.ScanText("40 53 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B 01 8B DA 48 8B F1 FF 90 ?? ?? ?? ??");
        this.hookUiModuleRequestMainCommand = Hook<UiModuleRequestMainCommand>.FromAddress(uiModuleRequestMainCommandAddress, this.UiModuleRequestMainCommandDetour);

        var atkUnitBaseReceiveGlobalEventAddress = sigScanner.ScanText("48 89 5C 24 ?? 48 89 7C 24 ?? 55 41 54 41 57");
        this.hookAtkUnitBaseReceiveGlobalEvent = Hook<AtkUnitBaseReceiveGlobalEvent>.FromAddress(atkUnitBaseReceiveGlobalEventAddress, this.AtkUnitBaseReceiveGlobalEventDetour);

        this.locDalamudPlugins = Loc.Localize("SystemMenuPlugins", "Dalamud Plugins");
        this.locDalamudSettings = Loc.Localize("SystemMenuSettings", "Dalamud Settings");

        // this.contextMenu.ContextMenuOpened += this.ContextMenuOnContextMenuOpened;
        
        this.hookAgentHudOpenSystemMenu.Enable();
        this.hookUiModuleRequestMainCommand.Enable();
        this.hookAtkUnitBaseReceiveGlobalEvent.Enable();
    }

    /// <summary>Finalizes an instance of the <see cref="DalamudAtkTweaks"/> class.</summary>
    ~DalamudAtkTweaks() => this.Dispose(false);

    private delegate void AgentHudOpenSystemMenuPrototype(void* thisPtr, AtkValue* atkValueArgs, uint menuSize);

    private delegate void UiModuleRequestMainCommand(void* thisPtr, int commandId);

    private delegate IntPtr AtkUnitBaseReceiveGlobalEvent(AtkUnitBase* thisPtr, ushort cmd, uint a3, IntPtr a4, uint* a5);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.Dispose(true);

    private void Dispose(bool disposing)
    {
        if (this.disposed)
            return;

        if (disposing)
        {
            this.hookAgentHudOpenSystemMenu.Dispose();
            this.hookUiModuleRequestMainCommand.Dispose();
            this.hookAtkUnitBaseReceiveGlobalEvent.Dispose();

            // this.contextMenu.ContextMenuOpened -= this.ContextMenuOnContextMenuOpened;
        }

        this.disposed = true;
    }

    /*
    private void ContextMenuOnContextMenuOpened(ContextMenuOpenedArgs args)
    {
        var systemText = Service<DataManager>.GetNullable()?.GetExcelSheet<Addon>()?.GetRow(1059)?.Text?.RawString; // "System"
        var interfaceManager = Service<InterfaceManager>.GetNullable();

        if (systemText == null || interfaceManager == null)
            return;

        if (args.Title == systemText && this.configuration.DoButtonsSystemMenu && interfaceManager.IsDispatchingEvents)
        {
            var dalamudInterface = Service<DalamudInterface>.Get();

            args.Items.Insert(0, new CustomContextMenuItem(this.locDalamudSettings, selectedArgs =>
            {
                dalamudInterface.ToggleSettingsWindow();
            }));

            args.Items.Insert(0, new CustomContextMenuItem(this.locDalamudPlugins, selectedArgs =>
            {
                dalamudInterface.TogglePluginInstallerWindow();
            }));
        }
    }
    */

    private IntPtr AtkUnitBaseReceiveGlobalEventDetour(AtkUnitBase* thisPtr, ushort cmd, uint a3, IntPtr a4, uint* arg)
    {
        // Log.Information("{0}: cmd#{1} a3#{2} - HasAnyFocus:{3}", MemoryHelper.ReadSeStringAsString(out _, new IntPtr(thisPtr->Name)), cmd, a3, WindowSystem.HasAnyWindowSystemFocus);

        // "SendHotkey"
        // 3 == Close
        if (cmd == 12 && WindowSystem.HasAnyWindowSystemFocus && *arg == 3 && this.configuration.IsFocusManagementEnabled)
        {
            Log.Verbose($"Cancelling global event SendHotkey command due to WindowSystem {WindowSystem.FocusedWindowSystemNamespace}");
            return IntPtr.Zero;
        }

        return this.hookAtkUnitBaseReceiveGlobalEvent.Original(thisPtr, cmd, a3, a4, arg);
    }

    private void AgentHudOpenSystemMenuDetour(void* thisPtr, AtkValue* atkValueArgs, uint menuSize)
    {
        if (WindowSystem.HasAnyWindowSystemFocus && this.configuration.IsFocusManagementEnabled)
        {
            Log.Verbose($"Cancelling OpenSystemMenu due to WindowSystem {WindowSystem.FocusedWindowSystemNamespace}");
            return;
        }

        var interfaceManager = Service<InterfaceManager>.GetNullable();
        if (interfaceManager == null)
        {
            this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize);
            return;
        }

        if (!this.configuration.DoButtonsSystemMenu || !interfaceManager.IsDispatchingEvents)
        {
            this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize);
            return;
        }

        // the max size (hardcoded) is 0x12/18, but the system menu currently uses 0xC/12
        // this is a just in case that doesnt really matter
        // see if we can add 2 entries
        if (menuSize >= 0x12)
        {
            this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize);
            return;
        }

        // atkValueArgs is actually an array of AtkValues used as args. all their UI code works like this.
        // in this case, menu size is stored in atkValueArgs[4], and the next 17 slots are the MainCommand
        // the 17 slots after that, if they exist, are the entry names, but they are otherwise pulled from MainCommand EXD
        // reference the original function for more details :)

        // step 1) move all the current menu items down so we can put Dalamud at the top like it deserves
        (&atkValueArgs[menuSize + 5])->ChangeType(ValueType.Int); // currently this value has no type, set it to int
        (&atkValueArgs[menuSize + 5 + 1])->ChangeType(ValueType.Int);

        for (var i = menuSize + 2; i > 1; i--)
        {
            var curEntry = &atkValueArgs[i + 5 - 2];
            var nextEntry = &atkValueArgs[i + 5];

            nextEntry->Int = curEntry->Int;
        }

        // step 2) set our new entries to dummy commands
        var firstEntry = &atkValueArgs[5];
        firstEntry->Int = 69420;
        var secondEntry = &atkValueArgs[6];
        secondEntry->Int = 69421;

        // step 3) create strings for them
        // since the game first checks for strings in the AtkValue argument before pulling them from the exd, if we create strings we dont have to worry
        // about hooking the exd reader, thank god
        var firstStringEntry = &atkValueArgs[5 + 18];
        firstStringEntry->ChangeType(ValueType.String);
        
        var secondStringEntry = &atkValueArgs[6 + 18];
        secondStringEntry->ChangeType(ValueType.String);

        const int color = 539;
        var strPlugins = new SeString().Append(new UIForegroundPayload(color))
                                       .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
                                       .Append(new UIForegroundPayload(0))
                                       .Append(this.locDalamudPlugins).Encode();
        var strSettings = new SeString().Append(new UIForegroundPayload(color))
                                        .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
                                        .Append(new UIForegroundPayload(0))
                                        .Append(this.locDalamudSettings).Encode();
        
        firstStringEntry->SetManagedString(strPlugins);
        secondStringEntry->SetManagedString(strSettings);

        // open menu with new size
        var sizeEntry = &atkValueArgs[4];
        sizeEntry->UInt = menuSize + 2;

        this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize + 2);
    }

    private void UiModuleRequestMainCommandDetour(void* thisPtr, int commandId)
    {
        var dalamudInterface = Service<DalamudInterface>.GetNullable();

        switch (commandId)
        {
            case 69420:
                dalamudInterface?.OpenPluginInstaller();
                break;
            case 69421:
                dalamudInterface?.OpenSettings();
                break;
            default:
                this.hookUiModuleRequestMainCommand.Original(thisPtr, commandId);
                break;
        }
    }
}
