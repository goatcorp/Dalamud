using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Serilog;
using SharpDX.Text;

namespace Dalamud.Game.Internal.Gui
{
    public sealed class FlyTextGui : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="kind">The FlyTextKind. See <see cref="FlyTextKind"/>.</param>
        /// <param name="actorIndex">The index of the actor to place flytext on. Indexing unknown. 1 places flytext on local player.</param>
        /// <param name="val1">Value1 passed to the native flytext function.</param>
        /// <param name="val2">Value2 passed to the native flytext function. Seems unused.</param>
        /// <param name="text1">Text1 passed to the native flytext function.</param>
        /// <param name="text2">Text2 passed to the native flytext function.</param>
        /// <param name="color">Color passed to the native flytext function. Changes flytext color.</param>
        /// <param name="icon">Icon ID passed to the native flytext function. Only displays with select FlyTextKind.</param>
        /// <param name="dirty">Must be set to <c>true</c> if any value has been changed and must be reflected in the flytext call.</param>
        /// <param name="handled">Must be set if the subscribing function wants to cancel the flytext from appearing..</param>
        public delegate void OnFlyTextDelegate(
            ref FlyTextKind kind,
            ref uint actorIndex,
            ref uint val1,
            ref uint val2,
            ref SeString text1,
            ref SeString text2,
            ref uint color,
            ref uint icon,
            ref bool dirty,
            ref bool handled);

        public event OnFlyTextDelegate OnFlyText;

        private readonly Hook<AddFlyTextDelegate> addFlyTextHook;

        private delegate void AddFlyTextDelegate(
            IntPtr thisPtr,
            uint actorIndex,
            uint messageMax,
            IntPtr numbers,
            uint offsetNum,
            uint offsetNumMax,
            IntPtr strings,
            uint offsetStr,
            uint offsetStrMax,
            uint unknown);

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

        private static byte[] Terminate(byte[] source)
        {
            var terminated = new byte[source.Length + 1];
            Array.Copy(source, 0, terminated, 0, source.Length);
            terminated[^1] = 0;

            return terminated;
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
        public unsafe void AddFlyText(FlyTextKind kind, uint actorIndex, uint val1, uint val2, SeString text1, SeString text2, uint color, uint icon)
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

            fixed (byte* pText1 = text1.Encode())
            {
                fixed (byte* pText2 = text2.Encode())
                {
                    strArray->StringArray[strOffset + 0] = pText1;
                    strArray->StringArray[strOffset + 1] = pText2;

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
                }
            }
        }

        public unsafe void AddFlyTextDetour(
            IntPtr thisPtr,
            uint actorIndex,
            uint messageMax,
            IntPtr numbers,
            uint offsetNum,
            uint offsetNumMax,
            IntPtr strings,
            uint offsetStr,
            uint offsetStrMax,
            uint unknown)
        {
            try
            {
                var numArray = (NumberArrayData*) numbers;
                var strArray = (StringArrayData*) strings;

                var tmpKind = (FlyTextKind) numArray->IntArray[offsetNum + 1];

                var tmpVal1 = unchecked((uint) numArray->IntArray[offsetNum + 2]);
                var tmpVal2 = unchecked((uint) numArray->IntArray[offsetNum + 3]);

                var tmpColor = unchecked((uint) numArray->IntArray[offsetNum + 5]);
                var tmpIcon = unchecked((uint) numArray->IntArray[offsetNum + 6]);

                var tmpText1 = this.Dalamud.SeStringManager.Parse((IntPtr)strArray->StringArray[offsetStr + 0]);
                var tmpText2 = this.Dalamud.SeStringManager.Parse((IntPtr)strArray->StringArray[offsetStr + 1]);

                var dirty = false;
                var handled = false;

                this.OnFlyText?.Invoke(
                    ref tmpKind,
                    ref actorIndex,
                    ref tmpVal1,
                    ref tmpVal2,
                    ref tmpText1,
                    ref tmpText2,
                    ref tmpColor,
                    ref tmpIcon,
                    ref dirty,
                    ref handled);

                // If handled, ignore the original call
                if (handled) return;

                // If not dirty, make the original call
                if (!dirty)
                {
                    this.addFlyTextHook.Original(
                        thisPtr,
                        actorIndex,
                        messageMax,
                        numbers,
                        offsetNum,
                        offsetNumMax,
                        strings,
                        offsetStr,
                        offsetStrMax,
                        unknown);
                    return;
                }

                // Update the flytext values
                numArray->IntArray[offsetNum + 1] = (int)tmpKind;
                numArray->IntArray[offsetNum + 2] = unchecked((int)tmpVal1);
                numArray->IntArray[offsetNum + 3] = unchecked((int)tmpVal2);
                numArray->IntArray[offsetNum + 5] = unchecked((int)tmpColor);
                numArray->IntArray[offsetNum + 6] = unchecked((int)tmpIcon);

                var terminated1 = Terminate(tmpText1.Encode());
                var terminated2 = Terminate(tmpText2.Encode());

                // We can use fixed here as our text is copied into text nodes during the function
                fixed (byte* pText1 = terminated1, pText2 = terminated2)
                {
                    strArray->StringArray[offsetStr + 0] = pText1;
                    strArray->StringArray[offsetStr + 1] = pText2;

                    this.addFlyTextHook.Original(
                        thisPtr,
                        actorIndex,
                        messageMax,
                        numbers,
                        offsetNum,
                        offsetNumMax,
                        strings,
                        offsetStr,
                        offsetStrMax,
                        unknown);
                }
            }
            catch (Exception e)
            {
                Log.Debug(e, "Exception occurred in AddFlyTextDetour!");
            }
        }
    }
}
