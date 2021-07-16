using System;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Internal
{
    /// <summary>
    /// The address resolver for the <see cref="Framework"/> class.
    /// </summary>
    public sealed class FrameworkAddressResolver : BaseAddressResolver
    {
        /// <summary>
        /// Gets the base address native Framework class.
        /// </summary>
        public IntPtr BaseAddress { get; private set; }

        /// <summary>
        /// Gets the address for the native GuiManager class.
        /// </summary>
        public IntPtr GuiManager { get; private set; }

        /// <summary>
        /// Gets the address for the native ScriptManager class.
        /// </summary>
        public IntPtr ScriptManager { get; private set; }

        /// <inheritdoc/>
        protected override void Setup64Bit(SigScanner sig)
        {
            this.SetupFramework(sig);

            // Xiv__Framework__GetGuiManager+8 000   mov     rax, [rcx+2C00h]
            // Xiv__Framework__GetGuiManager+F 000   retn
            this.GuiManager = Marshal.ReadIntPtr(this.BaseAddress, 0x2C08);

            // Called from Framework::Init
            this.ScriptManager = this.BaseAddress + 0x2C68; // note that no deref here
        }

        private void SetupFramework(SigScanner scanner)
        {
            // Dissasembly of part of the .dtor
            // 00007FF701AD665A | 48 C7 05 ?? ?? ?? ?? 00 00 00 00      | MOV     QWORD PTR DS:[g_mainFramework],0
            // 00007FF701AD6665 | E8 ?? ?? ?? ??                        | CALL    ffxiv_dx11.7FF701E27130
            // 00007FF701AD666A | 48 8D ?? ?? ?? 00 00                  | LEA     RCX,QWORD PTR DS:[RBX + 2C38]
            // 00007FF701AD6671 | E8 ?? ?? ?? ??                        | CALL    ffxiv_dx11.7FF701E2A7D0
            // 00007FF701AD6676 | 48 8D ?? ?? ?? ?? ??                  | LEA     RAX,QWORD PTR DS:[7FF702C31F80
            var fwDtor = scanner.ScanText("48 C7 05 ?? ?? ?? ?? 00 00 00 00 E8 ?? ?? ?? ?? 48 8D ?? ?? ?? 00 00 E8 ?? ?? ?? ?? 48 8D");
            var fwOffset = Marshal.ReadInt32(fwDtor + 3);
            var pFramework = scanner.ResolveRelativeAddress(fwDtor + 11, fwOffset);

            // Framework does not change once initialized in startup so don't bother to deref again and again.
            this.BaseAddress = Marshal.ReadIntPtr(pFramework);
        }
    }
}
