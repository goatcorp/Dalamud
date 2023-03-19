using System;
using System.Collections.Generic;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Dalamud.Fools.Plugins;

public class OopsMaybeLalafells : IFoolsPlugin
{
        // Plugin
        
        public OopsMaybeLalafells()
        {
            var scanner = Service<SigScanner>.Get();
            var addr = scanner.ScanText(SetupCharacterSig);
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
                    if (obj.ObjectIndex > 241) break;

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

        private const string SetupCharacterSig = "E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 48 8B D7 E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 8B C7";
        
        private delegate char SetupCharacterDelegate(nint a1, nint a2);
        private Hook<SetupCharacterDelegate> SetupCharacterHook = null!;

        private unsafe char SetupCharacterDetour(nint a1, nint a2)
        {
            // Roll the dice
            if (Rng.Next(0, 4) == 0)
            {
                var customize = (byte*)a2;
                customize[(int)CustomizeIndex.Race] = 3;

                var face = customize + (int)CustomizeIndex.FaceType;
                *face = (byte)(1 + ((*face - 1) % 4));
                
                var equipTar = (ushort)(customize[(int)CustomizeIndex.Gender] == 0 ? 92 : 93);
                for (var i = 1; i < 5; i++)
                {
                    var equip = (ushort*)(a2 + 28 + (i * 4));
                    if (ReplaceIDs.Contains(*equip))
                        *equip = equipTar;
                }
            }

            return SetupCharacterHook.Original(a1, a2);
        }
}
