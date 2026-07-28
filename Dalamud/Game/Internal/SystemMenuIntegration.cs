using CheapLoc;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Text;
using Dalamud.Hooking;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace Dalamud.Game.Internal;

/// <summary>
/// This class implements in-game Dalamud options in the in-game System menu.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class SystemMenuIntegration : IInternalDisposableService
{
    private static readonly ModuleLog Log = ModuleLog.Create<SystemMenuIntegration>();

    private readonly Hook<AgentHUD.Delegates.OpenSystemMenu> hookAgentHudOpenSystemMenu;
    private readonly Hook<UIModule.Delegates.ExecuteMainCommand> hookUiModuleExecuteMainCommand; // TODO: Make this into events in Framework.Gui

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    // [ServiceManager.ServiceDependency]
    // private readonly ContextMenu contextMenu = Service<ContextMenu>.Get();

    private bool disposed = false;

    [ServiceManager.ServiceConstructor]
    private SystemMenuIntegration()
    {
        this.hookAgentHudOpenSystemMenu = Hook<AgentHUD.Delegates.OpenSystemMenu>.FromAddress(AgentHUD.Addresses.OpenSystemMenu.Value, this.AgentHudOpenSystemMenuDetour);
        this.hookUiModuleExecuteMainCommand = Hook<UIModule.Delegates.ExecuteMainCommand>.FromAddress((nint)UIModule.StaticVirtualTablePointer->ExecuteMainCommand, this.UiModuleExecuteMainCommandDetour);

        // this.contextMenu.ContextMenuOpened += this.ContextMenuOnContextMenuOpened;

        this.hookAgentHudOpenSystemMenu.Enable();
        this.hookUiModuleExecuteMainCommand.Enable();
    }

    /// <summary>Finalizes an instance of the <see cref="SystemMenuIntegration"/> class.</summary>
    ~SystemMenuIntegration() => this.Dispose(false);

    private string LocDalamudPlugins => Loc.Localize("SystemMenuPlugins", "Dalamud Plugins");

    private string LocDalamudSettings => Loc.Localize("SystemMenuSettings", "Dalamud Settings");

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.Dispose(true);

    private void Dispose(bool disposing)
    {
        if (this.disposed)
            return;

        if (disposing)
        {
            this.hookAgentHudOpenSystemMenu.Dispose();
            this.hookUiModuleExecuteMainCommand.Dispose();

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

            args.Items.Insert(0, new CustomContextMenuItem(this.LocDalamudSettings, selectedArgs =>
            {
                dalamudInterface.ToggleSettingsWindow();
            }));

            args.Items.Insert(0, new CustomContextMenuItem(this.LocDalamudPlugins, selectedArgs =>
            {
                dalamudInterface.TogglePluginInstallerWindow();
            }));
        }
    }
    */

    private void AgentHudOpenSystemMenuDetour(AgentHUD* thisPtr, AtkValue* atkValueArgs, uint menuSize)
    {
        if (WindowSystem.ShouldInhibitAtkCloseEvents && this.configuration.IsFocusManagementEnabled)
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

        const int maxEntries = 20; // the hardcoded amount of maximum entries
        const int startIndex = 5; // the offset at which entries start
        const int offset = 2; // the amount of entries we want to inject

        var newMenuSize = (int)menuSize + offset;
        if (newMenuSize >= maxEntries)
        {
            this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize);
            return;
        }

        using var values = new RentedAtkValues(startIndex + (maxEntries * 2));

        // copy beginning of AtkValues
        for (var i = 0; i < startIndex; i++)
            values[i].Copy(&atkValueArgs[i]);

        // copy entries, but shifted
        for (var i = startIndex; i < startIndex + menuSize; i++)
        {
            values[i + offset].Copy(&atkValueArgs[i]);
            values[i + offset + maxEntries].Copy(&atkValueArgs[i + maxEntries]);
        }

        // set new menu size
        values[3].SetInt(newMenuSize);

        // set our new entries to dummy commands
        const int color = 539;
        using var rssb = new RentedSeStringBuilder();
        var entryIndex = startIndex;

        values[entryIndex].SetInt(69420);
        values[entryIndex + maxEntries].SetManagedString(rssb.Builder
            .PushColorType(color)
            .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
            .PopColorType()
            .Append(this.LocDalamudPlugins)
            .GetViewAsSpan());

        rssb.Builder.Clear();
        entryIndex++;

        values[entryIndex].SetInt(69421);
        values[entryIndex + maxEntries].SetManagedString(rssb.Builder
            .PushColorType(color)
            .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
            .PopColorType()
            .Append(this.LocDalamudSettings)
            .GetViewAsSpan());

        this.hookAgentHudOpenSystemMenu.Original(thisPtr, values, (uint)newMenuSize);
    }

    private void UiModuleExecuteMainCommandDetour(UIModule* thisPtr, uint commandId)
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
                this.hookUiModuleExecuteMainCommand.Original(thisPtr, commandId);
                break;
        }
    }
}
