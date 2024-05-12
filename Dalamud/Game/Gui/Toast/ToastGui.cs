using System.Collections.Generic;
using System.Text;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

namespace Dalamud.Game.Gui.Toast;

/// <summary>
/// This class facilitates interacting with and creating native toast windows.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal sealed partial class ToastGui : IInternalDisposableService, IToastGui
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
    /// <param name="sigScanner">Sig scanner to use.</param>
    [ServiceManager.ServiceConstructor]
    private ToastGui(TargetSigScanner sigScanner)
    {
        this.address = new ToastGuiAddressResolver();
        this.address.Setup(sigScanner);

        this.showNormalToastHook = Hook<ShowNormalToastDelegate>.FromAddress(this.address.ShowNormalToast, this.HandleNormalToastDetour);
        this.showQuestToastHook = Hook<ShowQuestToastDelegate>.FromAddress(this.address.ShowQuestToast, this.HandleQuestToastDetour);
        this.showErrorToastHook = Hook<ShowErrorToastDelegate>.FromAddress(this.address.ShowErrorToast, this.HandleErrorToastDetour);

        this.showNormalToastHook.Enable();
        this.showQuestToastHook.Enable();
        this.showErrorToastHook.Enable();
    }

    #region Marshal delegates

    private delegate IntPtr ShowNormalToastDelegate(IntPtr manager, IntPtr text, int layer, byte isTop, byte isFast, int logMessageId);

    private delegate byte ShowQuestToastDelegate(IntPtr manager, int position, IntPtr text, uint iconOrCheck1, byte playSound, uint iconOrCheck2, byte alsoPlaySound);

    private delegate byte ShowErrorToastDelegate(IntPtr manager, IntPtr text, byte respectsHidingMaybe);

    #endregion

    #region Events
    
    /// <inheritdoc/>
    public event IToastGui.OnNormalToastDelegate? Toast;

    /// <inheritdoc/>
    public event IToastGui.OnQuestToastDelegate? QuestToast;

    /// <inheritdoc/>
    public event IToastGui.OnErrorToastDelegate? ErrorToast;

    #endregion

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
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
internal sealed partial class ToastGui
{
    /// <inheritdoc/>
    public void ShowNormal(string message, ToastOptions? options = null)
    {
        options ??= new ToastOptions();
        this.normalQueue.Enqueue((Encoding.UTF8.GetBytes(message), options));
    }
    
    /// <inheritdoc/>
    public void ShowNormal(SeString message, ToastOptions? options = null)
    {
        options ??= new ToastOptions();
        this.normalQueue.Enqueue((message.Encode(), options));
    }

    private void ShowNormal(byte[] bytes, ToastOptions? options = null)
    {
        options ??= new ToastOptions();

        var manager = Service<GameGui>.GetNullable()?.GetUIModule();
        if (manager == null)
            return;

        // terminate the string
        var terminated = Terminate(bytes);

        unsafe
        {
            fixed (byte* ptr = terminated)
            {
                this.HandleNormalToastDetour(manager!.Value, (IntPtr)ptr, 5, (byte)options.Position, (byte)options.Speed, 0);
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
internal sealed partial class ToastGui
{
    /// <inheritdoc/>
    public void ShowQuest(string message, QuestToastOptions? options = null)
    {
        options ??= new QuestToastOptions();
        this.questQueue.Enqueue((Encoding.UTF8.GetBytes(message), options));
    }
    
    /// <inheritdoc/>
    public void ShowQuest(SeString message, QuestToastOptions? options = null)
    {
        options ??= new QuestToastOptions();
        this.questQueue.Enqueue((message.Encode(), options));
    }

    private void ShowQuest(byte[] bytes, QuestToastOptions? options = null)
    {
        options ??= new QuestToastOptions();

        var manager = Service<GameGui>.GetNullable()?.GetUIModule();
        if (manager == null)
            return;

        // terminate the string
        var terminated = Terminate(bytes);

        var (ioc1, ioc2) = this.DetermineParameterOrder(options);

        unsafe
        {
            fixed (byte* ptr = terminated)
            {
                this.HandleQuestToastDetour(
                    manager!.Value,
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
internal sealed partial class ToastGui
{
    /// <inheritdoc/>
    public void ShowError(string message)
    {
        this.errorQueue.Enqueue(Encoding.UTF8.GetBytes(message));
    }

    /// <inheritdoc/>
    public void ShowError(SeString message)
    {
        this.errorQueue.Enqueue(message.Encode());
    }

    private void ShowError(byte[] bytes)
    {
        var manager = Service<GameGui>.GetNullable()?.GetUIModule();
        if (manager == null)
            return;

        // terminate the string
        var terminated = Terminate(bytes);

        unsafe
        {
            fixed (byte* ptr = terminated)
            {
                this.HandleErrorToastDetour(manager!.Value, (IntPtr)ptr, 0);
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

/// <summary>
/// Plugin scoped version of ToastGui.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IToastGui>]
#pragma warning restore SA1015
internal class ToastGuiPluginScoped : IInternalDisposableService, IToastGui
{
    [ServiceManager.ServiceDependency]
    private readonly ToastGui toastGuiService = Service<ToastGui>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastGuiPluginScoped"/> class.
    /// </summary>
    internal ToastGuiPluginScoped()
    {
        this.toastGuiService.Toast += this.ToastForward;
        this.toastGuiService.QuestToast += this.QuestToastForward;
        this.toastGuiService.ErrorToast += this.ErrorToastForward;
    }
    
    /// <inheritdoc/>
    public event IToastGui.OnNormalToastDelegate? Toast;
    
    /// <inheritdoc/>
    public event IToastGui.OnQuestToastDelegate? QuestToast;
    
    /// <inheritdoc/>
    public event IToastGui.OnErrorToastDelegate? ErrorToast;
    
    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.toastGuiService.Toast -= this.ToastForward;
        this.toastGuiService.QuestToast -= this.QuestToastForward;
        this.toastGuiService.ErrorToast -= this.ErrorToastForward;

        this.Toast = null;
        this.QuestToast = null;
        this.ErrorToast = null;
    }
    
    /// <inheritdoc/>
    public void ShowNormal(string message, ToastOptions? options = null) => this.toastGuiService.ShowNormal(message, options);

    /// <inheritdoc/>
    public void ShowNormal(SeString message, ToastOptions? options = null) => this.toastGuiService.ShowNormal(message, options);

    /// <inheritdoc/>
    public void ShowQuest(string message, QuestToastOptions? options = null) => this.toastGuiService.ShowQuest(message, options);

    /// <inheritdoc/>
    public void ShowQuest(SeString message, QuestToastOptions? options = null) => this.toastGuiService.ShowQuest(message, options);

    /// <inheritdoc/>
    public void ShowError(string message) => this.toastGuiService.ShowError(message);

    /// <inheritdoc/>
    public void ShowError(SeString message) => this.toastGuiService.ShowError(message);

    private void ToastForward(ref SeString message, ref ToastOptions options, ref bool isHandled)
        => this.Toast?.Invoke(ref message, ref options, ref isHandled);

    private void QuestToastForward(ref SeString message, ref QuestToastOptions options, ref bool isHandled)
        => this.QuestToast?.Invoke(ref message, ref options, ref isHandled);

    private void ErrorToastForward(ref SeString message, ref bool isHandled)
        => this.ErrorToast?.Invoke(ref message, ref isHandled);
}
