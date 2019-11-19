using System;

namespace Dalamud.Game.Internal.Gui {
    public sealed class ChatGuiAddressResolver : BaseAddressResolver {
        public IntPtr BaseAddress { get; }
        
        public IntPtr PrintMessage { get; private set; }
        public IntPtr PopulateItemLinkObject { get; private set; }

        public ChatGuiAddressResolver(IntPtr baseAddres) {
            BaseAddress = baseAddres;
        }
        
        
        /*
         --- for reference: 4.57 ---
         
.text:00000001405CD210 ; __int64 __fastcall Xiv::Gui::ChatGui::PrintMessage(__int64 handler, unsigned __int16 chatType, __int64 senderName, __int64 message, int senderActorId, char isLocal)
.text:00000001405CD210 Xiv__Gui__ChatGui__PrintMessage proc near
.text:00000001405CD210                                         ; CODE XREF: sub_1401419F0+201↑p
.text:00000001405CD210                                         ; sub_140141D10+220↑p ...
.text:00000001405CD210
.text:00000001405CD210 var_220         = qword ptr -220h
.text:00000001405CD210 var_218         = byte ptr -218h
.text:00000001405CD210 var_210         = word ptr -210h
.text:00000001405CD210 var_208         = byte ptr -208h
.text:00000001405CD210 var_200         = word ptr -200h
.text:00000001405CD210 var_1FC         = dword ptr -1FCh
.text:00000001405CD210 var_1F8         = qword ptr -1F8h
.text:00000001405CD210 var_1F0         = qword ptr -1F0h
.text:00000001405CD210 var_1E8         = qword ptr -1E8h
.text:00000001405CD210 var_1E0         = dword ptr -1E0h
.text:00000001405CD210 var_1DC         = word ptr -1DCh
.text:00000001405CD210 var_1DA         = word ptr -1DAh
.text:00000001405CD210 var_1D8         = qword ptr -1D8h
.text:00000001405CD210 var_1D0         = byte ptr -1D0h
.text:00000001405CD210 var_1C8         = qword ptr -1C8h
.text:00000001405CD210 var_1B0         = dword ptr -1B0h
.text:00000001405CD210 var_1AC         = dword ptr -1ACh
.text:00000001405CD210 var_1A8         = dword ptr -1A8h
.text:00000001405CD210 var_1A4         = dword ptr -1A4h
.text:00000001405CD210 var_1A0         = dword ptr -1A0h
.text:00000001405CD210 var_160         = dword ptr -160h
.text:00000001405CD210 var_15C         = dword ptr -15Ch
.text:00000001405CD210 var_140         = dword ptr -140h
.text:00000001405CD210 var_138         = dword ptr -138h
.text:00000001405CD210 var_130         = byte ptr -130h
.text:00000001405CD210 var_C0          = byte ptr -0C0h
.text:00000001405CD210 var_50          = qword ptr -50h
.text:00000001405CD210 var_38          = qword ptr -38h
.text:00000001405CD210 var_30          = qword ptr -30h
.text:00000001405CD210 var_28          = qword ptr -28h
.text:00000001405CD210 var_20          = qword ptr -20h
.text:00000001405CD210 senderActorId   = dword ptr  30h
.text:00000001405CD210 isLocal         = byte ptr  38h
.text:00000001405CD210
.text:00000001405CD210 ; __unwind { // __GSHandlerCheck
.text:00000001405CD210                 push    rbp
.text:00000001405CD212                 push    rdi
.text:00000001405CD213                 push    r14
.text:00000001405CD215                 push    r15
.text:00000001405CD217                 lea     rbp, [rsp-128h]
.text:00000001405CD21F                 sub     rsp, 228h
.text:00000001405CD226                 mov     rax, cs:__security_cookie
.text:00000001405CD22D                 xor     rax, rsp
.text:00000001405CD230                 mov     [rbp+140h+var_50], rax
.text:00000001405CD237                 xor     r10b, r10b
.text:00000001405CD23A                 mov     [rsp+240h+var_1F8], rcx
.text:00000001405CD23F                 xor     eax, eax
.text:00000001405CD241                 mov     r11, r9
.text:00000001405CD244                 mov     r14, r8
.text:00000001405CD247                 mov     r9d, eax
.text:00000001405CD24A                 movzx   r15d, dx
.text:00000001405CD24E                 lea     r8, [rcx+0C10h]
.text:00000001405CD255                 mov     rdi, rcx
         */
        
        protected override void Setup64Bit(SigScanner sig) {
            //PrintMessage = sig.ScanText("4055 57 41 ?? 41 ?? 488DAC24D8FEFFFF 4881EC28020000 488B05???????? 4833C4 488985F0000000 4532D2 48894C2448"); LAST PART FOR 5.1???
            PrintMessage =
                sig.ScanText(
                    "4055 57 41 ?? 41 ?? 4157488DAC24E0FE FFFF4881EC2002 0000488B05???? ????48 33C4488985F000 000045 32D248894C2448");
            //PrintMessage = sig.ScanText("4055 57 41 ?? 41 ?? 488DAC24E8FEFFFF 4881EC18020000 488B05???????? 4833C4 488985E0000000 4532D2 48894C2438"); old

            //PrintMessage = sig.ScanText("40 55 57 41 56 41 57 48  8D AC 24 D8 FE FF FF 48 81 EC 28 02 00 00 48 8B  05 63 47 4A 01 48 33 C4 48 89 85 F0 00 00 00 45  32 D2 48 89 4C 24 48 33");

            //PopulateItemLinkObject = sig.ScanText("48 89 5C 24 08 57 48 83  EC 20 80 7A 06 00 48 8B DA 48 8B F9 74 14 48 8B  CA E8 32 03 00 00 48 8B C8 E8 FA F2 B0 FF 8B C8  EB 1D 0F B6 42 14 8B 4A");

            //PopulateItemLinkObject = sig.ScanText(      "48 89 5C 24 08 57 48 83  EC 20 80 7A 06 00 48 8B DA 48 8B F9 74 14 48 8B  CA E8 32 03 00 00 48 8B C8 E8 ?? ?? B0 FF 8B C8  EB 1D 0F B6 42 14 8B 4A"); 5.0
            PopulateItemLinkObject = sig.ScanText("48 89 5C 24 08 57 48 83  EC 20 80 7A 06 00 48 8B DA 48 8B F9 74 14 48 8B  CA E8 32 03 00 00 48 8B C8 E8 ?? ?? ?? FF 8B C8  EB 1D 0F B6 42 14 8B 4A");
        }
    }
}
