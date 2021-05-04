using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Game.Internal.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Game.Internal.Gui
{
    public sealed class FlyTextGui : IDisposable
    {
        #region Hooks

        private readonly Hook<AddFlyTextDelegate> addFlyTextHook;

        #endregion

        #region Delegates

        private delegate void AddFlyTextDelegate(
            IntPtr thisPtr,
            uint section,
            uint messageMax,
            IntPtr numbers,
            uint offsetNum,
            uint offsetNumMax,
            IntPtr strings,
            uint offsetStr,
            uint offsetStrMax,
            uint unknown);

        #endregion

        private Dalamud Dalamud { get; }

        private FlyTextGuiAddressResolver Address { get; }

        public FlyTextGui(SigScanner scanner, Dalamud dalamud)
        {
            this.Dalamud = dalamud;

            this.Address = new FlyTextGuiAddressResolver();
            this.Address.Setup(scanner);

            this.addFlyTextHook = new Hook<AddFlyTextDelegate>(this.Address.AddFlyText, new AddFlyTextDelegate(this.AddFlyTextDetour));
        }

        public void Enable()
        {
            this.addFlyTextHook.Enable();
        }

        public void Dispose()
        {
            this.addFlyTextHook.Dispose();
        }

        /// <summary>
        /// Displays a Flytext in-game on the local player.
        /// </summary>
        /// <param name="kind">The FlyTextKind. See <see cref="FlyTextKind"/>.</param>
        /// <param name="actorIndex">The index of the actor to place flytext on. Indexing unknown. 1 places flytext on local player.</param>
        /// <param name="val1">Value1 passed to the native flytext function.</param>
        /// <param name="val2">Value2 passed to the native flytext function. Seems unused.</param>
        /// <param name="text1">Text1 passed to the native flytext function.</param>
        /// <param name="text2">Text2 passed to the native flytext function.</param>
        /// <param name="color">Color passed to the native flytext function. Changes flytext color.</param>
        /// <param name="icon">Icon ID passed to the native flytext function. Only displays with select FlyTextKind.</param>
        public unsafe void AddFlyText(FlyTextKind kind, uint actorIndex, uint val1, uint val2, string text1, string text2, uint color, uint icon)
        {
            // The hook for this function must be past the actorIndex check
            // to avoid an unhookable conditional jump, so we manually check
            // here as to not introduce unintended behavior
            if (actorIndex >= 10)
                return;

            int numIndex = 28;
            int strIndex = 25;

            uint numOffset = 147;
            uint strOffset = 28;

            // Get the UI module and flytext addon pointers
            var ui = (UIModule*)this.Dalamud.Framework.Gui.GetUIModule();
            var flytext = this.Dalamud.Framework.Gui.GetUiObjectByName("_FlyText", 1);

            if (ui == null || flytext == IntPtr.Zero)
                return;

            // Get the number and string arrays we need
            var atkArrayDataHolder = ui->RaptureAtkModule.AtkModule.AtkArrayDataHolder;
            var numArray = atkArrayDataHolder._NumberArrays[numIndex];
            var strArray = atkArrayDataHolder._StringArrays[strIndex];

            // Write the values to the arrays using a known valid flytext region
            numArray->IntArray[numOffset + 0] = 1;
            numArray->IntArray[numOffset + 1] = (int)kind;
            numArray->IntArray[numOffset + 2] = unchecked((int)val1);
            numArray->IntArray[numOffset + 3] = unchecked((int)val2);
            numArray->IntArray[numOffset + 4] = 5;
            numArray->IntArray[numOffset + 5] = unchecked((int)color);
            numArray->IntArray[numOffset + 6] = unchecked((int)icon);
            numArray->IntArray[numOffset + 7] = 0;
            numArray->IntArray[numOffset + 8] = 0;
            numArray->IntArray[numOffset + 9] = 0;

            var pText1 = Marshal.StringToHGlobalAnsi(text1);
            var pText2 = Marshal.StringToHGlobalAnsi(text2);

            strArray->StringArray[strOffset + 0] = (byte*)pText1.ToPointer();
            strArray->StringArray[strOffset + 1] = (byte*)pText2.ToPointer();

            this.addFlyTextHook.Original(
                flytext,
                actorIndex,
                14,
                (IntPtr)numArray,
                numOffset,
                9,
                (IntPtr)strArray,
                strOffset,
                2,
                0);

            Marshal.FreeHGlobal(pText1);
            Marshal.FreeHGlobal(pText2);
        }

        public void AddFlyTextDetour(
            IntPtr thisPtr,
            uint section,
            uint messageMax,
            IntPtr numbers,
            uint offsetNum,
            uint offsetNumMax,
            IntPtr strings,
            uint offsetStr,
            uint offsetStrMax,
            uint unknown)
        {
            // TODO: In the future, fire an event to subscribers that a FlyText has been added.
            this.addFlyTextHook.Original(thisPtr, section, messageMax, numbers, offsetNum, offsetNumMax, strings, offsetStr, offsetStrMax, unknown);
        }
    }
}
