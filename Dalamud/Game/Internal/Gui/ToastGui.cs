using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Game.Internal.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;

namespace Dalamud.Game.Internal.Gui
{
    public sealed class ToastGui : IDisposable
    {
        internal const uint QuestToastCheckmarkMagic = 60081;

        #region Events

        public delegate void OnNormalToastDelegate(ref SeString message, ref ToastOptions options, ref bool isHandled);

        public delegate void OnQuestToastDelegate(ref SeString message, ref QuestToastOptions options, ref bool isHandled);

        public delegate void OnErrorToastDelegate(ref SeString message, ref bool isHandled);

        /// <summary>
        /// Event that will be fired when a toast is sent by the game or a plugin.
        /// </summary>
        public event OnNormalToastDelegate OnToast;

        /// <summary>
        /// Event that will be fired when a quest toast is sent by the game or a plugin.
        /// </summary>
        public event OnQuestToastDelegate OnQuestToast;

        /// <summary>
        /// Event that will be fired when an error toast is sent by the game or a plugin.
        /// </summary>
        public event OnErrorToastDelegate OnErrorToast;

        #endregion

        #region Hooks

        private readonly Hook<ShowNormalToastDelegate> showNormalToastHook;

        private readonly Hook<ShowQuestToastDelegate> showQuestToastHook;

        private readonly Hook<ShowErrorToastDelegate> showErrorToastHook;

        #endregion

        #region Delegates

        private delegate IntPtr ShowNormalToastDelegate(IntPtr manager, IntPtr text, int layer, byte isTop, byte isFast, int logMessageId);

        private delegate byte ShowQuestToastDelegate(IntPtr manager, int position, IntPtr text, uint iconOrCheck1, byte playSound, uint iconOrCheck2, byte alsoPlaySound);

        private delegate byte ShowErrorToastDelegate(IntPtr manager, IntPtr text, int layer, byte respectsHidingMaybe);

        private delegate IntPtr GetAtkModuleDelegate(IntPtr uiModule);

        #endregion

        private Dalamud Dalamud { get; }

        private ToastGuiAddressResolver Address { get; }

        private Queue<(byte[], ToastOptions)> NormalQueue { get; } = new Queue<(byte[], ToastOptions)>();

        private Queue<(byte[], QuestToastOptions)> QuestQueue { get; } = new Queue<(byte[], QuestToastOptions)>();

        private Queue<byte[]> ErrorQueue { get; } = new Queue<byte[]>();

        public ToastGui(SigScanner scanner, Dalamud dalamud)
        {
            this.Dalamud = dalamud;

            this.Address = new ToastGuiAddressResolver();
            this.Address.Setup(scanner);

            this.showNormalToastHook = new Hook<ShowNormalToastDelegate>(this.Address.ShowNormalToast, new ShowNormalToastDelegate(this.HandleNormalToastDetour));
            this.showQuestToastHook = new Hook<ShowQuestToastDelegate>(this.Address.ShowQuestToast, new ShowQuestToastDelegate(this.HandleQuestToastDetour));
            this.showErrorToastHook = new Hook<ShowErrorToastDelegate>(this.Address.ShowErrorToast, new ShowErrorToastDelegate(this.HandleErrorToastDetour));
        }

        public void Enable()
        {
            this.showNormalToastHook.Enable();
            this.showQuestToastHook.Enable();
            this.showErrorToastHook.Enable();
        }

        public void Dispose()
        {
            this.showNormalToastHook.Dispose();
            this.showQuestToastHook.Dispose();
            this.showErrorToastHook.Dispose();
        }

        private static byte[] Terminate(byte[] source)
        {
            var terminated = new byte[source.Length + 1];
            Array.Copy(source, 0, terminated, 0, source.Length);
            terminated[^1] = 0;

            return terminated;
        }

        private SeString ParseString(IntPtr text)
        {
            var bytes = new List<byte>();
            unsafe
            {
                var ptr = (byte*) text;
                while (*ptr != 0)
                {
                    bytes.Add(*ptr);
                    ptr += 1;
                }
            }

            // call events
            return this.Dalamud.SeStringManager.Parse(bytes.ToArray());
        }

        /// <summary>
        /// Process the toast queue.
        /// </summary>
        internal void UpdateQueue()
        {
            while (this.NormalQueue.Count > 0)
            {
                var (message, options) = this.NormalQueue.Dequeue();
                this.ShowNormal(message, options);
            }

            while (this.QuestQueue.Count > 0)
            {
                var (message, options) = this.QuestQueue.Dequeue();
                this.ShowQuest(message, options);
            }

            while (this.ErrorQueue.Count > 0)
            {
                var message = this.ErrorQueue.Dequeue();
                this.ShowError(message);
            }
        }

        #region Normal API

        /// <summary>
        /// Show a toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown</param>
        /// <param name="options">Options for the toast</param>
        public void ShowNormal(string message, ToastOptions options = null)
        {
            options ??= new ToastOptions();
            this.NormalQueue.Enqueue((Encoding.UTF8.GetBytes(message), options));
        }

        /// <summary>
        /// Show a toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown</param>
        /// <param name="options">Options for the toast</param>
        public void ShowNormal(SeString message, ToastOptions options = null)
        {
            options ??= new ToastOptions();
            this.NormalQueue.Enqueue((message.Encode(), options));
        }

        private void ShowNormal(byte[] bytes, ToastOptions options = null)
        {
            options ??= new ToastOptions();

            var manager = this.Dalamud.Framework.Gui.GetUIModule();

            // terminate the string
            var terminated = Terminate(bytes);

            unsafe
            {
                fixed (byte* ptr = terminated)
                {
                    this.HandleNormalToastDetour(manager, (IntPtr) ptr, 5, (byte) options.Position, (byte) options.Speed, 0);
                }
            }
        }

        #endregion

        #region Quest API

        /// <summary>
        /// Show a quest toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown</param>
        /// <param name="options">Options for the toast</param>
        public void ShowQuest(string message, QuestToastOptions options = null)
        {
            options ??= new QuestToastOptions();
            this.QuestQueue.Enqueue((Encoding.UTF8.GetBytes(message), options));
        }

        /// <summary>
        /// Show a quest toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown</param>
        /// <param name="options">Options for the toast</param>
        public void ShowQuest(SeString message, QuestToastOptions options = null)
        {
            options ??= new QuestToastOptions();
            this.QuestQueue.Enqueue((message.Encode(), options));
        }

        private void ShowQuest(byte[] bytes, QuestToastOptions options = null)
        {
            options ??= new QuestToastOptions();

            var manager = this.Dalamud.Framework.Gui.GetUIModule();

            // terminate the string
            var terminated = Terminate(bytes);

            var (ioc1, ioc2) = options.DetermineParameterOrder();

            unsafe
            {
                fixed (byte* ptr = terminated)
                {
                    this.HandleQuestToastDetour(
                        manager,
                        (int) options.Position,
                        (IntPtr) ptr,
                        ioc1,
                        options.PlaySound ? (byte) 1 : (byte) 0,
                        ioc2,
                        0);
                }
            }
        }

        #endregion

        #region Error API

        /// <summary>
        /// Show an error toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown</param>
        public void ShowError(string message)
        {
            this.ErrorQueue.Enqueue(Encoding.UTF8.GetBytes(message));
        }

        /// <summary>
        /// Show an error toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown</param>
        public void ShowError(SeString message)
        {
            this.ErrorQueue.Enqueue(message.Encode());
        }

        private void ShowError(byte[] bytes)
        {
            var uiModule = this.Dalamud.Framework.Gui.GetUIModule();
            var vtbl = Marshal.ReadIntPtr(uiModule);
            var atkModulePtr = Marshal.ReadIntPtr(vtbl + (7 * 8));
            var getAtkModule = Marshal.GetDelegateForFunctionPointer<GetAtkModuleDelegate>(atkModulePtr);
            var manager = getAtkModule(uiModule);

            // terminate the string
            var terminated = Terminate(bytes);

            unsafe
            {
                fixed (byte* ptr = terminated)
                {
                    this.HandleErrorToastDetour(manager, (IntPtr) ptr, 10, 0);
                }
            }
        }

        #endregion

        private IntPtr HandleNormalToastDetour(IntPtr manager, IntPtr text, int layer, byte isTop, byte isFast, int logMessageId)
        {
            if (text == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            // call events
            var isHandled = false;
            var str = this.ParseString(text);
            var options = new ToastOptions
            {
                Position = (ToastPosition) isTop,
                Speed = (ToastSpeed) isFast,
            };

            this.OnToast?.Invoke(ref str, ref options, ref isHandled);

            // do nothing if handled
            if (isHandled)
            {
                return IntPtr.Zero;
            }

            var terminated = Terminate(str.Encode());

            unsafe
            {
                fixed (byte* message = terminated)
                {
                    return this.showNormalToastHook.Original(manager, (IntPtr) message, layer, (byte) options.Position, (byte) options.Speed, logMessageId);
                }
            }
        }

        private byte HandleQuestToastDetour(IntPtr manager, int position, IntPtr text, uint iconOrCheck1, byte playSound, uint iconOrCheck2, byte alsoPlaySound)
        {
            if (text == IntPtr.Zero)
            {
                return 0;
            }

            // call events
            var isHandled = false;
            var str = this.ParseString(text);
            var options = new QuestToastOptions
            {
                Position = (QuestToastPosition) position,
                DisplayCheckmark = iconOrCheck1 == QuestToastCheckmarkMagic,
                IconId = iconOrCheck1 == QuestToastCheckmarkMagic ? iconOrCheck2 : iconOrCheck1,
                PlaySound = playSound == 1,
            };

            this.OnQuestToast?.Invoke(ref str, ref options, ref isHandled);

            // do nothing if handled
            if (isHandled)
            {
                return 0;
            }

            var terminated = Terminate(str.Encode());

            var (ioc1, ioc2) = options.DetermineParameterOrder();

            unsafe
            {
                fixed (byte* message = terminated)
                {
                    return this.showQuestToastHook.Original(
                        manager,
                        (int) options.Position,
                        (IntPtr) message,
                        ioc1,
                        options.PlaySound ? (byte) 1 : (byte) 0,
                        ioc2,
                        0);
                }
            }
        }

        private byte HandleErrorToastDetour(IntPtr manager, IntPtr text, int layer, byte respectsHidingMaybe)
        {
            if (text == IntPtr.Zero)
            {
                return 0;
            }

            // call events
            var isHandled = false;
            var str = this.ParseString(text);

            this.OnErrorToast?.Invoke(ref str, ref isHandled);

            // do nothing if handled
            if (isHandled)
            {
                return 0;
            }

            var terminated = Terminate(str.Encode());

            unsafe
            {
                fixed (byte* message = terminated)
                {
                    return this.showErrorToastHook.Original(manager, (IntPtr) message, layer, respectsHidingMaybe);
                }
            }
        }
    }
}
