using System;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Internal {
    public sealed class FrameworkAddressResolver : BaseAddressResolver {
        public IntPtr BaseAddress { get; private set; }
        
        public IntPtr GuiManager { get; private set; }
        
        public IntPtr ScriptManager { get; private set; }
        
        
        protected override void Setup64Bit(SigScanner sig) {
            SetupFramework(sig);
            
            // Xiv__Framework__GetGuiManager+8 000   mov     rax, [rcx+2C00h]
            // Xiv__Framework__GetGuiManager+F 000   retn
            GuiManager = Marshal.ReadIntPtr(BaseAddress, 0x2C08);

            // Called from Framework::Init
            ScriptManager = BaseAddress + 0x2C68; // note that no deref here
        }

        private void SetupFramework(SigScanner scanner) {
            // Dissasembly of part of the .dtor
            // 00007FF701AD665A | 48 C7 05 ?? ?? ?? ?? 00 00 00 00      | MOV     QWORD PTR DS:[g_mainFramework],0
            // 00007FF701AD6665 | E8 ?? ?? ?? ??                        | CALL    ffxiv_dx11.7FF701E27130
            // 00007FF701AD666A | 48 8D ?? ?? ?? 00 00                  | LEA     RCX,QWORD PTR DS:[RBX + 2C38]
            // 00007FF701AD6671 | E8 ?? ?? ?? ??                        | CALL    ffxiv_dx11.7FF701E2A7D0
            // 00007FF701AD6676 | 48 8D ?? ?? ?? ?? ??                  | LEA     RAX,QWORD PTR DS:[7FF702C31F80
            var fwDtor = scanner.ScanText("48C705????????00000000 E8???????? 488D??????0000 E8???????? 488D");
            var fwOffset = Marshal.ReadInt32(fwDtor + 3);
            var pFramework = scanner.ResolveRelativeAddress(fwDtor + 11, fwOffset);
            
            // Framework does not change once initialized in startup so don't bother to deref again and again.
            BaseAddress = Marshal.ReadIntPtr(pFramework);
        }
    }
}
