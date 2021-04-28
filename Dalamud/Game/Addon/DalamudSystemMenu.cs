using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CheapLoc;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;

using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Dalamud.Game.Addon
{
    internal unsafe class DalamudSystemMenu
    {
        private readonly Dalamud dalamud;

        private delegate void AgentHudOpenSystemMenuProtoype(void* thisPtr, AtkValue* atkValueArgs, uint menuSize);

        private Hook<AgentHudOpenSystemMenuProtoype> hookAgentHudOpenSystemMenu;

        private delegate void AtkValueChangeType(AtkValue* thisPtr, ValueType type);

        private AtkValueChangeType atkValueChangeType;

        private delegate void AtkValueSetString(AtkValue* thisPtr, byte* bytes);

        private AtkValueSetString atkValueSetString;

        private delegate void UiModuleRequestMainCommand(void* thisPtr, int commandId);

        // TODO: Make this into events in Framework.Gui
        private Hook<UiModuleRequestMainCommand> hookUiModuleRequestMainCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="DalamudSystemMenu"/> class.
        /// </summary>
        /// <param name="dalamud">The dalamud instance to act on.</param>
        public DalamudSystemMenu(Dalamud dalamud)
        {
            this.dalamud = dalamud;

            var openSystemMenuAddress = this.dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 32 C0 4C 8B AC 24 ?? ?? ?? ?? 48 8B 8D ?? ?? ?? ??");

            this.hookAgentHudOpenSystemMenu = new Hook<AgentHudOpenSystemMenuProtoype>(
                openSystemMenuAddress,
                new AgentHudOpenSystemMenuProtoype(this.AgentHudOpenSystemMenuDetour),
                this);

            var atkValueChangeTypeAddress =
                this.dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 45 84 F6 48 8D 4C 24 ??");
            this.atkValueChangeType =
                Marshal.GetDelegateForFunctionPointer<AtkValueChangeType>(atkValueChangeTypeAddress);

            var atkValueSetStringAddress =
                this.dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 41 03 ED");
            this.atkValueSetString = Marshal.GetDelegateForFunctionPointer<AtkValueSetString>(atkValueSetStringAddress);

            var uiModuleRequestMainCommmandAddress = this.dalamud.SigScanner.ScanText(
                "40 53 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B 01 8B DA 48 8B F1 FF 90 ?? ?? ?? ??");
            this.hookUiModuleRequestMainCommand = new Hook<UiModuleRequestMainCommand>(
                uiModuleRequestMainCommmandAddress,
                new UiModuleRequestMainCommand(this.UiModuleRequestMainCommandDetour),
                this);
        }

        public void Enable()
        {
            this.hookAgentHudOpenSystemMenu.Enable();
            this.hookUiModuleRequestMainCommand.Enable();
        }

        private void AgentHudOpenSystemMenuDetour(void* thisPtr, AtkValue* atkValueArgs, uint menuSize)
        {
            // the max size (hardcoded) is 0xE/15, but the system menu currently uses 0xC/12
            // this is a just in case that doesnt really matter
            // see if we can add 2 entries
            if (menuSize >= 0xD)
            {
                hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize);
                return;
            }

            // atkValueArgs is actually an array of AtkValues used as args. all their UI code works like this.
            // in this case, menu size is stored in atkValueArgs[4], and the next 15 slots are the MainCommand
            // the 15 slots after that, if they exist, are the entry names, but they are otherwise pulled from MainCommand EXD
            // reference the original function for more details :)

            // step 1) move all the current menu items down so we can put Dalamud at the top like it deserves
            atkValueChangeType(&atkValueArgs[menuSize + 5], ValueType.Int); // currently this value has no type, set it to int
            atkValueChangeType(&atkValueArgs[menuSize + 5 + 1], ValueType.Int);

            for (uint i = menuSize+2; i > 1; i--)
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
            var firstStringEntry = &atkValueArgs[5 + 15];
            atkValueChangeType(firstStringEntry, ValueType.String);
            var secondStringEntry = &atkValueArgs[6 + 15];
            atkValueChangeType(secondStringEntry, ValueType.String);

            var strPlugins = Encoding.UTF8.GetBytes(Loc.Localize("SystemMenuPlugins", "Dalamud Plugins"));
            var strSettings = Encoding.UTF8.GetBytes(Loc.Localize("SystemMenuSettings", "Dalamud Settings"));

            // do this the most terrible way possible since im lazy
            var bytes = stackalloc byte[strPlugins.Length + 1];
            Marshal.Copy(strPlugins, 0, new IntPtr(bytes), strPlugins.Length);
            bytes[strPlugins.Length] = 0x0;

            atkValueSetString(firstStringEntry, bytes); // this allocs the string properly using the game's allocators and copies it, so we dont have to worry about memory fuckups

            var bytes2 = stackalloc byte[strSettings.Length + 1];
            Marshal.Copy(strSettings, 0, new IntPtr(bytes2), strSettings.Length);
            bytes2[strSettings.Length] = 0x0;

            atkValueSetString(secondStringEntry, bytes2);

            // open menu with new size
            var sizeEntry = &atkValueArgs[4];
            sizeEntry->UInt = menuSize + 2;

            this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize + 2);
        }

        private void UiModuleRequestMainCommandDetour(void* thisPtr, int commandId)
        {
            if (commandId == 69420)
            {
                this.dalamud.DalamudUi.OpenPluginInstaller();
            }
            else if (commandId == 69421)
            {
                this.dalamud.DalamudUi.OpenSettings();
            }
            else
            {
                this.hookUiModuleRequestMainCommand.Original(thisPtr, commandId);
            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.hookAgentHudOpenSystemMenu.Dispose();
            this.hookUiModuleRequestMainCommand.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
