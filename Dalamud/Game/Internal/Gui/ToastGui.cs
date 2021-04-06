using System;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Internal.Gui
{
    public class ToastGui
    {
        private Dalamud Dalamud { get; }

        private ToastGuiAddressResolver Address { get; }

        private delegate IntPtr ShowToastDelegate(IntPtr manager, IntPtr text, int layer, byte bool1, byte bool2, int logMessageId);

        private readonly ShowToastDelegate showToast;

        public ToastGui(SigScanner scanner, Dalamud dalamud)
        {
            Dalamud = dalamud;

            Address = new ToastGuiAddressResolver();
            Address.Setup(scanner);

            this.showToast = Marshal.GetDelegateForFunctionPointer<ShowToastDelegate>(Address.ShowToast);
        }

        /// <summary>
        /// Show a toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown</param>
        public void Show(string message)
        {
            this.Show(Encoding.UTF8.GetBytes(message));
        }

        /// <summary>
        /// Show a toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown</param>
        public void Show(SeString message)
        {
            this.Show(message.Encode());
        }

        private void Show(byte[] bytes)
        {
            var manager = Dalamud.Framework.Gui.GetUIModule();

            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    this.showToast(manager, (IntPtr) ptr, 5, 0, 1, 0);
                }
            }
        }
    }
}
