using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Hooking;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Dalamud.Fools.Plugins;

public class OopsMaybeLalafells : IFoolsPlugin
{
        // Oops, Maybe Lalafells?
        // This plugin is deliberately nerfed to prevent a fully-formed revival of the original.
        
        public OopsMaybeLalafells()
        {
            var scanner = Service<SigScanner>.Get();
            var addr = scanner.Module.BaseAddress + 0x0484F60; // Deliberate choice in line with the above comment - this is intended to break after the next patch.
            SetupCharacterHook = Hook<SetupCharacterDelegate>.FromAddress(addr, SetupCharacterDetour);
            SetupCharacterHook.Enable();
            RedrawAll();
        }

        public void Dispose()
        {
            SetupCharacterHook.Disable();
            SetupCharacterHook.Dispose();

            RedrawAll();
        }

        private unsafe void RedrawAll()
        {
            Service<Framework>.Get().RunOnFrameworkThread(() => {
                var objects = Service<ObjectTable>.Get();
                foreach (var obj in objects)
                {
                    if (obj.ObjectIndex > 241 && obj.ObjectIndex < 301) continue;

                    var csObject = (GameObject*)obj.Address;
                    if (csObject == null) continue;

                    csObject->DisableDraw();
                    csObject->EnableDraw();
                }
            });
        }
        
        // The Lalafellinator
        
        private readonly Random Rng = new();
        
        private readonly List<ushort> ReplaceIDs = new() { 84, 85, 86, 87, 88, 89, 90, 91, 257, 258, 581, 597, 744 };
        private delegate char SetupCharacterDelegate(nint a1, nint a2);
        private Hook<SetupCharacterDelegate> SetupCharacterHook = null!;

        private char SetupCharacterDetour(nint a1, nint a2)
        {
            try
            {
                var custom = Marshal.PtrToStructure<CustomizeData>(a2);
                
                // Roll the dice
                if (custom.Race != 3 && Rng.Next(0, 4) == 0)
                {
                    custom.Race = 3;
                    custom.Tribe = (byte)(((custom.Race * 2) - 1) + 1 - (custom.Tribe % 2));
                    custom.FaceType = (byte)(1 + ((custom.FaceType - 1) % 4));
                    custom.ModelType %= 2;
                    Marshal.StructureToPtr(custom, a2, true);
                    
                    var equipTar = (ushort)(custom.Gender == 0 ? 92 : 93);
                    for (var i = 1; i < 5; i++)
                    {
                        var ofs = a2 + 28 + (i * 4);
                        var equip = (ushort)Marshal.ReadInt16(ofs);
                        if (ReplaceIDs.Contains(equip))
                            Marshal.WriteInt16(ofs, (short)equipTar);
                    }
                }
            }
            catch (Exception e)
            {
                PluginLog.Error(e.ToString(), e);
            }

            return SetupCharacterHook.Original(a1, a2);
        }
        
        // Customize shit

        [StructLayout(LayoutKind.Explicit)]
        private struct CustomizeData
        {
            [FieldOffset((int)CustomizeIndex.FaceType)] public byte FaceType;
            [FieldOffset((int)CustomizeIndex.ModelType)] public byte ModelType;
            [FieldOffset((int)CustomizeIndex.Race)] public byte Race;
            [FieldOffset((int)CustomizeIndex.Tribe)] public byte Tribe;
            [FieldOffset((int)CustomizeIndex.Gender)] public byte Gender;
        }
}
