using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;

namespace Dalamud.Game.Internal.Gui
{
    public sealed class ToastGui : IDisposable
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

        private delegate IntPtr ShowToastDelegate(IntPtr manager, IntPtr text, int layer, byte isTop, byte isFast, int logMessageId);

        private Dalamud Dalamud { get; }

        private ToastGuiAddressResolver Address { get; }

        private Queue<(byte[] message, ToastOptions options)> ToastQueue { get; } = new Queue<(byte[] message, ToastOptions options)>();


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
        /// <param name="options">Options for the toast</param>
        public void Show(string message, ToastOptions options = null)
        {
            options ??= new ToastOptions();
            this.ToastQueue.Enqueue((Encoding.UTF8.GetBytes(message), options));
        }

        /// <summary>
        /// Show a toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown</param>
        /// <param name="options">Options for the toast</param>
        public void Show(SeString message, ToastOptions options = null)
        {
            options ??= new ToastOptions();
            this.ToastQueue.Enqueue((message.Encode(), options));
        }

        /// <summary>
        /// Process the toast queue.
        /// </summary>
        internal void UpdateQueue()
        {
            while (this.ToastQueue.Count > 0)
            {
                var (message, options) = this.ToastQueue.Dequeue();
                this.Show(message, options);
            }
        }

        private void Show(byte[] bytes, ToastOptions options = null)
        {
            options ??= new ToastOptions();

            var manager = this.Dalamud.Framework.Gui.GetUIModule();

            // terminate the string
            var terminated = new byte[bytes.Length + 1];
            Array.Copy(bytes, 0, terminated, 0, bytes.Length);
            terminated[^1] = 0;

            unsafe
            {
                fixed (byte* ptr = terminated)
                {
                    this.HandleToastDetour(manager, (IntPtr)ptr, 5, (byte)options.Position, (byte)options.Speed, 0);
                }
            }
        }

        private IntPtr HandleToastDetour(IntPtr manager, IntPtr text, int layer, byte isTop, byte isFast, int logMessageId)
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
                    return this.showToastHook.Original(manager, (IntPtr)message, layer, isTop, isFast, logMessageId);
                }
            }
        }
    }

    public sealed class ToastOptions
    {
        public ToastPosition Position { get; set; } = ToastPosition.Bottom;

        public ToastSpeed Speed { get; set; } = ToastSpeed.Slow;
    }

    public enum ToastPosition : byte
    {
        Bottom = 0,
        Top = 1,
    }

    public enum ToastSpeed : byte
    {
        Slow = 0,
        Fast = 1,
    }
}
