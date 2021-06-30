using System;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Internal.Gui
{
    /// <summary>
    /// The address resolver for the <see cref="GameGui"/> class.
    /// </summary>
    internal sealed class GameGuiAddressResolver : BaseAddressResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameGuiAddressResolver"/> class.
        /// </summary>
        /// <param name="baseAddress">The base address of the native GuiManager class.</param>
        public GameGuiAddressResolver(IntPtr baseAddress)
        {
            this.BaseAddress = baseAddress;
        }

        /// <summary>
        /// Gets the base address of the native GuiManager class.
        /// </summary>
        public IntPtr BaseAddress { get; private set; }

        /// <summary>
        /// Gets the address of the native ChatManager class.
        /// </summary>
        public IntPtr ChatManager { get; private set; }

        /// <summary>
        /// Gets the address of the native SetGlobalBgm method.
        /// </summary>
        public IntPtr SetGlobalBgm { get; private set; }

        /// <summary>
        /// Gets the address of the native HandleItemHover method.
        /// </summary>
        public IntPtr HandleItemHover { get; private set; }

        /// <summary>
        /// Gets the address of the native HandleItemOut method.
        /// </summary>
        public IntPtr HandleItemOut { get; private set; }

        /// <summary>
        /// Gets the address of the native HandleActionHover method.
        /// </summary>
        public IntPtr HandleActionHover { get; private set; }

        /// <summary>
        /// Gets the address of the native HandleActionOut method.
        /// </summary>
        public IntPtr HandleActionOut { get; private set; }

        /// <summary>
        /// Gets the address of the native HandleImm method.
        /// </summary>
        public IntPtr HandleImm { get; private set; }

        /// <summary>
        /// Gets the address of the native GetUIObject method.
        /// </summary>
        public IntPtr GetUIObject { get; private set; }

        /// <summary>
        /// Gets the address of the native GetMatrixSingleton method.
        /// </summary>
        public IntPtr GetMatrixSingleton { get; private set; }

        /// <summary>
        /// Gets the address of the native ScreenToWorld method.
        /// </summary>
        public IntPtr ScreenToWorld { get; private set; }

        /// <summary>
        /// Gets the address of the native ToggleUiHide method.
        /// </summary>
        public IntPtr ToggleUiHide { get; private set; }

        /// <summary>
        /// Gets the address of the native Client::UI::UIModule getter method.
        /// </summary>
        public IntPtr GetBaseUIObject { get; private set; }

        /// <summary>
        /// Gets the address of the native GetUIObjectByName method.
        /// </summary>
        public IntPtr GetUIObjectByName { get; private set; }

        /// <summary>
        /// Gets the address of the native GetUIModule method.
        /// </summary>
        public IntPtr GetUIModule { get; private set; }

        /// <summary>
        /// Gets the address of the native GetAgentModule method.
        /// </summary>
        public IntPtr GetAgentModule { get; private set; }

        /// <inheritdoc/>
        protected override void Setup64Bit(SigScanner sig)
        {
            this.SetGlobalBgm = sig.ScanText("4C 8B 15 ?? ?? ?? ?? 4D 85 D2 74 58");
            this.HandleItemHover = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 89 AE ?? ?? ?? ??");
            this.HandleItemOut = sig.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 4D");
            this.HandleActionHover = sig.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 83 F8 0F");
            this.HandleActionOut = sig.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 48 8B F9 4D 85 C0 74 1F");
            this.HandleImm = sig.ScanText("E8 ?? ?? ?? ?? 84 C0 75 10 48 83 FF 09");
            this.GetUIObject = sig.ScanText("E8 ?? ?? ?? ?? 48 8B C8 48 8B 10 FF 52 40 80 88 ?? ?? ?? ?? 01 E9");
            this.GetMatrixSingleton = sig.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
            this.ScreenToWorld = sig.ScanText("48 83 EC 48 48 8B 05 ?? ?? ?? ?? 4D 8B D1");
            this.ToggleUiHide = sig.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B6 B9 ?? ?? ?? ?? B8 ?? ?? ?? ??");
            this.GetBaseUIObject = sig.ScanText("E8 ?? ?? ?? ?? 41 B8 01 00 00 00 48 8D 15 ?? ?? ?? ?? 48 8B 48 20 E8 ?? ?? ?? ?? 48 8B CF");
            this.GetUIObjectByName = sig.ScanText("E8 ?? ?? ?? ?? 48 8B CF 48 89 87 ?? ?? 00 00 E8 ?? ?? ?? ?? 41 B8 01 00 00 00");
            this.GetUIModule = sig.ScanText("E8 ?? ?? ?? ?? 48 8B C8 48 85 C0 75 2D");

            var uiModuleVtableSig = sig.GetStaticAddressFromSig("48 8D 05 ?? ?? ?? ?? 4C 89 61 28");
            this.GetAgentModule = Marshal.ReadIntPtr(uiModuleVtableSig, 34 * IntPtr.Size);
        }

        /// <inheritdoc/>
        protected override void SetupInternal(SigScanner scanner)
        {
            // Xiv__UiManager__GetChatManager   000   lea     rax, [rcx+13E0h]
            // Xiv__UiManager__GetChatManager+7 000   retn
            this.ChatManager = this.BaseAddress + 0x13E0;
        }
    }
}
