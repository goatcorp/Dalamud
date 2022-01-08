using System;
using System.Collections.Generic;
using System.Text;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game.Gui.Toast
{
    /// <summary>
    /// This class facilitates interacting with and creating native toast windows.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public sealed partial class ToastGui : IDisposable
    {
        private const uint QuestToastCheckmarkMagic = 60081;

        private readonly ToastGuiAddressResolver address;

        private readonly Queue<(byte[] Message, ToastOptions Options)> normalQueue = new();
        private readonly Queue<(byte[] Message, QuestToastOptions Options)> questQueue = new();
        private readonly Queue<byte[]> errorQueue = new();

        private readonly Hook<ShowNormalToastDelegate> showNormalToastHook;
        private readonly Hook<ShowQuestToastDelegate> showQuestToastHook;
        private readonly Hook<ShowErrorToastDelegate> showErrorToastHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToastGui"/> class.
        /// </summary>
        internal ToastGui()
        {
            this.address = new ToastGuiAddressResolver();
            this.address.Setup();

            this.showNormalToastHook = new Hook<ShowNormalToastDelegate>(this.address.ShowNormalToast, new ShowNormalToastDelegate(this.HandleNormalToastDetour));
            this.showQuestToastHook = new Hook<ShowQuestToastDelegate>(this.address.ShowQuestToast, new ShowQuestToastDelegate(this.HandleQuestToastDetour));
            this.showErrorToastHook = new Hook<ShowErrorToastDelegate>(this.address.ShowErrorToast, new ShowErrorToastDelegate(this.HandleErrorToastDetour));
        }

        #region Event delegates

        /// <summary>
        /// A delegate type used when a normal toast window appears.
        /// </summary>
        /// <param name="message">The message displayed.</param>
        /// <param name="options">Assorted toast options.</param>
        /// <param name="isHandled">Whether the toast has been handled or should be propagated.</param>
        public delegate void OnNormalToastDelegate(ref SeString message, ref ToastOptions options, ref bool isHandled);

        /// <summary>
        /// A delegate type used when a quest toast window appears.
        /// </summary>
        /// <param name="message">The message displayed.</param>
        /// <param name="options">Assorted toast options.</param>
        /// <param name="isHandled">Whether the toast has been handled or should be propagated.</param>
        public delegate void OnQuestToastDelegate(ref SeString message, ref QuestToastOptions options, ref bool isHandled);

        /// <summary>
        /// A delegate type used when an error toast window appears.
        /// </summary>
        /// <param name="message">The message displayed.</param>
        /// <param name="isHandled">Whether the toast has been handled or should be propagated.</param>
        public delegate void OnErrorToastDelegate(ref SeString message, ref bool isHandled);

        #endregion

        #region Marshal delegates

        private delegate IntPtr ShowNormalToastDelegate(IntPtr manager, IntPtr text, int layer, byte isTop, byte isFast, int logMessageId);

        private delegate byte ShowQuestToastDelegate(IntPtr manager, int position, IntPtr text, uint iconOrCheck1, byte playSound, uint iconOrCheck2, byte alsoPlaySound);

        private delegate byte ShowErrorToastDelegate(IntPtr manager, IntPtr text, byte respectsHidingMaybe);

        #endregion

        #region Events

        /// <summary>
        /// Event that will be fired when a toast is sent by the game or a plugin.
        /// </summary>
        public event OnNormalToastDelegate Toast;

        /// <summary>
        /// Event that will be fired when a quest toast is sent by the game or a plugin.
        /// </summary>
        public event OnQuestToastDelegate QuestToast;

        /// <summary>
        /// Event that will be fired when an error toast is sent by the game or a plugin.
        /// </summary>
        public event OnErrorToastDelegate ErrorToast;

        #endregion

        /// <summary>
        /// Enables this module.
        /// </summary>
        public void Enable()
        {
            this.showNormalToastHook.Enable();
            this.showQuestToastHook.Enable();
            this.showErrorToastHook.Enable();
        }

        /// <summary>
        /// Disposes of managed and unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.showNormalToastHook.Dispose();
            this.showQuestToastHook.Dispose();
            this.showErrorToastHook.Dispose();
        }

        /// <summary>
        /// Process the toast queue.
        /// </summary>
        internal void UpdateQueue()
        {
            while (this.normalQueue.Count > 0)
            {
                var (message, options) = this.normalQueue.Dequeue();
                this.ShowNormal(message, options);
            }

            while (this.questQueue.Count > 0)
            {
                var (message, options) = this.questQueue.Dequeue();
                this.ShowQuest(message, options);
            }

            while (this.errorQueue.Count > 0)
            {
                var message = this.errorQueue.Dequeue();
                this.ShowError(message);
            }
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
                var ptr = (byte*)text;
                while (*ptr != 0)
                {
                    bytes.Add(*ptr);
                    ptr += 1;
                }
            }

            // call events
            return SeString.Parse(bytes.ToArray());
        }
    }

    /// <summary>
    /// Handles normal toasts.
    /// </summary>
    public sealed partial class ToastGui
    {
        /// <summary>
        /// Show a toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown.</param>
        /// <param name="options">Options for the toast.</param>
        public void ShowNormal(string message, ToastOptions options = null)
        {
            options ??= new ToastOptions();
            this.normalQueue.Enqueue((Encoding.UTF8.GetBytes(message), options));
        }

        /// <summary>
        /// Show a toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown.</param>
        /// <param name="options">Options for the toast.</param>
        public void ShowNormal(SeString message, ToastOptions options = null)
        {
            options ??= new ToastOptions();
            this.normalQueue.Enqueue((message.Encode(), options));
        }

        private void ShowNormal(byte[] bytes, ToastOptions options = null)
        {
            options ??= new ToastOptions();

            var manager = Service<GameGui>.Get().GetUIModule();

            // terminate the string
            var terminated = Terminate(bytes);

            unsafe
            {
                fixed (byte* ptr = terminated)
                {
                    this.HandleNormalToastDetour(manager, (IntPtr)ptr, 5, (byte)options.Position, (byte)options.Speed, 0);
                }
            }
        }

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
                Position = (ToastPosition)isTop,
                Speed = (ToastSpeed)isFast,
            };

            this.Toast?.Invoke(ref str, ref options, ref isHandled);

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
                    return this.showNormalToastHook.Original(manager, (IntPtr)message, layer, (byte)options.Position, (byte)options.Speed, logMessageId);
                }
            }
        }
    }

    /// <summary>
    /// Handles quest toasts.
    /// </summary>
    public sealed partial class ToastGui
    {
        /// <summary>
        /// Show a quest toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown.</param>
        /// <param name="options">Options for the toast.</param>
        public void ShowQuest(string message, QuestToastOptions options = null)
        {
            options ??= new QuestToastOptions();
            this.questQueue.Enqueue((Encoding.UTF8.GetBytes(message), options));
        }

        /// <summary>
        /// Show a quest toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown.</param>
        /// <param name="options">Options for the toast.</param>
        public void ShowQuest(SeString message, QuestToastOptions options = null)
        {
            options ??= new QuestToastOptions();
            this.questQueue.Enqueue((message.Encode(), options));
        }

        private void ShowQuest(byte[] bytes, QuestToastOptions options = null)
        {
            options ??= new QuestToastOptions();

            var manager = Service<GameGui>.Get().GetUIModule();

            // terminate the string
            var terminated = Terminate(bytes);

            var (ioc1, ioc2) = this.DetermineParameterOrder(options);

            unsafe
            {
                fixed (byte* ptr = terminated)
                {
                    this.HandleQuestToastDetour(
                        manager,
                        (int)options.Position,
                        (IntPtr)ptr,
                        ioc1,
                        options.PlaySound ? (byte)1 : (byte)0,
                        ioc2,
                        0);
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
                Position = (QuestToastPosition)position,
                DisplayCheckmark = iconOrCheck1 == QuestToastCheckmarkMagic,
                IconId = iconOrCheck1 == QuestToastCheckmarkMagic ? iconOrCheck2 : iconOrCheck1,
                PlaySound = playSound == 1,
            };

            this.QuestToast?.Invoke(ref str, ref options, ref isHandled);

            // do nothing if handled
            if (isHandled)
            {
                return 0;
            }

            var terminated = Terminate(str.Encode());

            var (ioc1, ioc2) = this.DetermineParameterOrder(options);

            unsafe
            {
                fixed (byte* message = terminated)
                {
                    return this.showQuestToastHook.Original(
                        manager,
                        (int)options.Position,
                        (IntPtr)message,
                        ioc1,
                        options.PlaySound ? (byte)1 : (byte)0,
                        ioc2,
                        0);
                }
            }
        }

        private (uint IconOrCheck1, uint IconOrCheck2) DetermineParameterOrder(QuestToastOptions options)
        {
            return options.DisplayCheckmark
                ? (QuestToastCheckmarkMagic, options.IconId)
                : (options.IconId, 0);
        }
    }

    /// <summary>
    /// Handles error toasts.
    /// </summary>
    public sealed partial class ToastGui
    {
        /// <summary>
        /// Show an error toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown.</param>
        public void ShowError(string message)
        {
            this.errorQueue.Enqueue(Encoding.UTF8.GetBytes(message));
        }

        /// <summary>
        /// Show an error toast message with the given content.
        /// </summary>
        /// <param name="message">The message to be shown.</param>
        public void ShowError(SeString message)
        {
            this.errorQueue.Enqueue(message.Encode());
        }

        private void ShowError(byte[] bytes)
        {
            var manager = Service<GameGui>.Get().GetUIModule();

            // terminate the string
            var terminated = Terminate(bytes);

            unsafe
            {
                fixed (byte* ptr = terminated)
                {
                    this.HandleErrorToastDetour(manager, (IntPtr)ptr, 0);
                }
            }
        }

        private byte HandleErrorToastDetour(IntPtr manager, IntPtr text, byte respectsHidingMaybe)
        {
            if (text == IntPtr.Zero)
            {
                return 0;
            }

            // call events
            var isHandled = false;
            var str = this.ParseString(text);

            this.ErrorToast?.Invoke(ref str, ref isHandled);

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
                    return this.showErrorToastHook.Original(manager, (IntPtr)message, respectsHidingMaybe);
                }
            }
        }
    }
}
