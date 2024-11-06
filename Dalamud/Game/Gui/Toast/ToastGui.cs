using System.Collections.Generic;
using System.Text;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Memory;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Game.Gui.Toast;

/// <summary>
/// This class facilitates interacting with and creating native toast windows.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed partial class ToastGui : IInternalDisposableService, IToastGui
{
    private const uint QuestToastCheckmarkMagic = 60081;

    private readonly Queue<(byte[] Message, ToastOptions Options)> normalQueue = new();
    private readonly Queue<(byte[] Message, QuestToastOptions Options)> questQueue = new();
    private readonly Queue<byte[]> errorQueue = new();

    private readonly Hook<ShowNormalToastDelegate> showNormalToastHook;
    private readonly Hook<ShowQuestToastDelegate> showQuestToastHook;
    private readonly Hook<ShowErrorToastDelegate> showErrorToastHook;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastGui"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    private unsafe ToastGui()
    {
        this.showNormalToastHook = Hook<ShowNormalToastDelegate>.FromAddress((nint)UIModule.StaticVirtualTablePointer->ShowWideText, this.HandleNormalToastDetour);
        this.showQuestToastHook = Hook<ShowQuestToastDelegate>.FromAddress((nint)UIModule.StaticVirtualTablePointer->ShowText, this.HandleQuestToastDetour);
        this.showErrorToastHook = Hook<ShowErrorToastDelegate>.FromAddress((nint)UIModule.StaticVirtualTablePointer->ShowErrorText, this.HandleErrorToastDetour);

        this.showNormalToastHook.Enable();
        this.showQuestToastHook.Enable();
        this.showErrorToastHook.Enable();
    }

    #region Marshal delegates

    private unsafe delegate void ShowNormalToastDelegate(UIModule* thisPtr, byte* text, int layer, byte isTop, byte isFast, uint logMessageId);

    private unsafe delegate void ShowQuestToastDelegate(UIModule* thisPtr, int position, byte* text, uint iconOrCheck1, byte playSound, uint iconOrCheck2, byte alsoPlaySound);

    private unsafe delegate void ShowErrorToastDelegate(UIModule* thisPtr, byte* text, byte respectsHidingMaybe);

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

    private unsafe void ShowNormal(byte[] bytes, ToastOptions? options = null)
    {
        options ??= new ToastOptions();

        this.HandleNormalToastDetour(
            UIModule.Instance(),
            bytes.NullTerminate().AsPointer(),
            5,
            (byte)options.Position,
            (byte)options.Speed,
            0);
    }

    private unsafe void HandleNormalToastDetour(UIModule* thisPtr, byte* text, int layer, byte isTop, byte isFast, uint logMessageId)
    {
        if (text == null)
            return;

        // call events
        var isHandled = false;
        var str = MemoryHelper.ReadSeStringNullTerminated((nint)text);
        var options = new ToastOptions
        {
            Position = (ToastPosition)isTop,
            Speed = (ToastSpeed)isFast,
        };

        this.Toast?.Invoke(ref str, ref options, ref isHandled);

        // do nothing if handled
        if (isHandled)
            return;

        this.showNormalToastHook.Original(
            thisPtr,
            str.EncodeWithNullTerminator().AsPointer(),
            layer,
            (byte)(options.Position == ToastPosition.Top ? 1 : 0),
            (byte)(options.Speed == ToastSpeed.Fast ? 1 : 0),
            logMessageId);
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

    private unsafe void ShowQuest(byte[] bytes, QuestToastOptions? options = null)
    {
        options ??= new QuestToastOptions();

        var (ioc1, ioc2) = this.DetermineParameterOrder(options);

        this.HandleQuestToastDetour(
            UIModule.Instance(),
            (int)options.Position,
            bytes.NullTerminate().AsPointer(),
            ioc1,
            (byte)(options.PlaySound ? 1 : 0),
            ioc2,
            0);
    }

    private unsafe void HandleQuestToastDetour(UIModule* thisPtr, int position, byte* text, uint iconOrCheck1, byte playSound, uint iconOrCheck2, byte alsoPlaySound)
    {
        if (text == null)
            return;

        // call events
        var isHandled = false;
        var str = SeString.Parse(text);
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
            return;

        var (ioc1, ioc2) = this.DetermineParameterOrder(options);

        this.showQuestToastHook.Original(
            UIModule.Instance(),
            (int)options.Position,
            str.EncodeWithNullTerminator().AsPointer(),
            ioc1,
            (byte)(options.PlaySound ? 1 : 0),
            ioc2,
            0);
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

    private unsafe void ShowError(byte[] bytes)
    {
        this.HandleErrorToastDetour(UIModule.Instance(), bytes.NullTerminate().AsPointer(), 0);
    }

    private unsafe void HandleErrorToastDetour(UIModule* thisPtr, byte* text, byte respectsHidingMaybe)
    {
        if (text == null)
            return;

        // call events
        var isHandled = false;
        var str = SeString.Parse(text);

        this.ErrorToast?.Invoke(ref str, ref isHandled);

        // do nothing if handled
        if (isHandled)
            return;

        this.showErrorToastHook.Original(thisPtr, str.EncodeWithNullTerminator().AsPointer(), respectsHidingMaybe);
    }
}

/// <summary>
/// Plugin scoped version of ToastGui.
/// </summary>
[PluginInterface]
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
