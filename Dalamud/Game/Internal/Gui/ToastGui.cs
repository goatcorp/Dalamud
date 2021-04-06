using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;

namespace Dalamud.Game.Internal.Gui
{
    public class ToastGui : IDisposable
    {
        #region Events

        public delegate void OnToastDelegate(ref SeString message, ref bool isHandled);

        /// <summary>
        /// Event that will be fired when a toast is sent by the game or a plugin.
        /// </summary>
        public event OnToastDelegate OnToast;

        #endregion

        #region Hooks

        private readonly Hook<ShowToastDelegate> showToastHook;

        #endregion

        private delegate IntPtr ShowToastDelegate(IntPtr manager, IntPtr text, int layer, byte bool1, byte bool2, int logMessageId);

        private Dalamud Dalamud { get; }

        private ToastGuiAddressResolver Address { get; }

        private Queue<byte[]> ToastQueue { get; } = new Queue<byte[]>();


        public ToastGui(SigScanner scanner, Dalamud dalamud)
        {
            this.Dalamud = dalamud;

            this.Address = new ToastGuiAddressResolver();
            this.Address.Setup(scanner);

            Marshal.GetDelegateForFunctionPointer<ShowToastDelegate>(this.Address.ShowToast);
            this.showToastHook = new Hook<ShowToastDelegate>(this.Address.ShowToast, new ShowToastDelegate(this.HandleToastDetour));
        }

        public void Enable()
        {
            this.showToastHook.Enable();
        }

        public void Dispose()
        {
            this.showToastHook.Dispose();
        }

        /// <summary>
        /// Show a toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown</param>
        public void Show(string message)
        {
            this.ToastQueue.Enqueue(Encoding.UTF8.GetBytes(message));
        }

        /// <summary>
        /// Show a toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown</param>
        public void Show(SeString message)
        {
            this.ToastQueue.Enqueue(message.Encode());
        }

        /// <summary>
        /// Process the toast queue.
        /// </summary>
        internal void UpdateQueue()
        {
            while (this.ToastQueue.Count > 0)
            {
                var message = this.ToastQueue.Dequeue();
                this.Show(message);
            }
        }

        private void Show(byte[] bytes)
        {
            var manager = this.Dalamud.Framework.Gui.GetUIModule();

            // terminate the string
            var terminated = new byte[bytes.Length + 1];
            Array.Copy(bytes, 0, terminated, 0, bytes.Length);
            terminated[^1] = 0;

            unsafe
            {
                fixed (byte* ptr = terminated)
                {
                    this.HandleToastDetour(manager, (IntPtr)ptr, 5, 0, 1, 0);
                }
            }
        }

        private IntPtr HandleToastDetour(IntPtr manager, IntPtr text, int layer, byte bool1, byte bool2, int logMessageId)
        {
            if (text == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            // get the message as an sestring
            var bytes = new List<byte>();
            unsafe
            {
                var ptr = (byte*)text;
                while (*ptr != 0)
                {
                    bytes.Add(*ptr);
                    ptr += 1;
                }
            }

            // call events
            var isHandled = false;
            var str = this.Dalamud.SeStringManager.Parse(bytes.ToArray());

            this.OnToast?.Invoke(ref str, ref isHandled);

            // do nothing if handled
            if (isHandled)
            {
                return IntPtr.Zero;
            }

            var encoded = str.Encode();
            var terminated = new byte[encoded.Length + 1];
            Array.Copy(encoded, 0, terminated, 0, encoded.Length);
            terminated[^1] = 0;

            unsafe
            {
                fixed (byte* message = terminated)
                {
                    return this.showToastHook.Original(manager, (IntPtr)message, layer, bool1, bool2, logMessageId);
                }
            }
        }
    }
}
