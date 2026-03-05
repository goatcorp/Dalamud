using System.Collections.Generic;

using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.UI;

using InteropGenerator.Runtime;

using Lumina.Text.ReadOnly;

using Serilog;

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

    private readonly Hook<UIModule.Delegates.ShowWideText> showNormalToastHook;
    private readonly Hook<UIModule.Delegates.ShowText> showQuestToastHook;
    private readonly Hook<UIModule.Delegates.ShowErrorText> showErrorToastHook;

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastGui"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    private unsafe ToastGui()
    {
        this.showNormalToastHook = Hook<UIModule.Delegates.ShowWideText>.FromAddress((nint)UIModule.StaticVirtualTablePointer->ShowWideText, this.HandleNormalToastDetour);
        this.showQuestToastHook = Hook<UIModule.Delegates.ShowText>.FromAddress((nint)UIModule.StaticVirtualTablePointer->ShowText, this.HandleQuestToastDetour);
        this.showErrorToastHook = Hook<UIModule.Delegates.ShowErrorText>.FromAddress((nint)UIModule.StaticVirtualTablePointer->ShowErrorText, this.HandleErrorToastDetour);

        this.showNormalToastHook.Enable();
        this.showQuestToastHook.Enable();
        this.showErrorToastHook.Enable();

        this.framework.BeforeUpdate += this.UpdateQueue;
    }

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
        this.framework.BeforeUpdate -= this.UpdateQueue;

        this.showNormalToastHook.Dispose();
        this.showQuestToastHook.Dispose();
        this.showErrorToastHook.Dispose();
    }

    /// <summary>
    /// Process the toast queue.
    /// </summary>
    private void UpdateQueue(IFramework framework)
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
    /// <inheritdoc />
    public unsafe void ShowNormal(ReadOnlySeString message, ToastOptions? options = null)
    {
        options ??= new ToastOptions();

        using var rssb = new RentedSeStringBuilder();

        fixed (byte* ptr = rssb.Builder.Append(message).GetViewAsSpan())
        {
            this.HandleNormalToastDetour(
                UIModule.Instance(),
                ptr,
                5,
                options.Position == ToastPosition.Top,
                options.Speed == ToastSpeed.Fast,
                0);
        }
    }

    private unsafe void HandleNormalToastDetour(UIModule* thisPtr, CStringPointer text, int layer, bool isTop, bool isFast, uint logMessageId)
    {
        if (!text.HasValue)
            return;

        // call events
        var isHandled = false;
        var str = text.AsReadOnlySeString();
        var options = new ToastOptions
        {
            Position = isTop ? ToastPosition.Top : ToastPosition.Bottom,
            Speed = isFast ? ToastSpeed.Fast : ToastSpeed.Slow,
        };

        foreach (var d in Delegate.EnumerateInvocationList(this.Toast))
        {
            try
            {
                d.Invoke(ref str, ref options, ref isHandled);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", d.Method);
            }
        }

        // do nothing if handled
        if (isHandled)
            return;

        using var rssb = new RentedSeStringBuilder();

        fixed (byte* ptr = rssb.Builder.Append(str).GetViewAsSpan())
        {
            this.showNormalToastHook.Original(
                thisPtr,
                ptr,
                layer,
                options.Position == ToastPosition.Top,
                options.Speed == ToastSpeed.Fast,
                logMessageId);
        }
    }
}

/// <summary>
/// Handles quest toasts.
/// </summary>
internal sealed partial class ToastGui
{
    /// <inheritdoc />
    public unsafe void ShowQuest(ReadOnlySeString message, QuestToastOptions? options = null)
    {
        options ??= new QuestToastOptions();

        var (ioc1, ioc2) = this.DetermineParameterOrder(options);

        using var rssb = new RentedSeStringBuilder();

        fixed (byte* ptr = rssb.Builder.Append(message).GetViewAsSpan())
        {
            this.HandleQuestToastDetour(
                UIModule.Instance(),
                (int)options.Position,
                ptr,
                ioc1,
                options.PlaySound,
                ioc2,
                false);
        }
    }

    private unsafe void HandleQuestToastDetour(UIModule* thisPtr, int position, CStringPointer text, uint iconOrCheck1, bool playSound, uint iconOrCheck2, bool alsoPlaySound)
    {
        if (!text.HasValue)
            return;

        // call events
        var isHandled = false;
        var str = text.AsReadOnlySeString();
        var options = new QuestToastOptions
        {
            Position = (QuestToastPosition)position,
            DisplayCheckmark = iconOrCheck1 == QuestToastCheckmarkMagic,
            IconId = iconOrCheck1 == QuestToastCheckmarkMagic ? iconOrCheck2 : iconOrCheck1,
            PlaySound = playSound,
        };

        foreach (var d in Delegate.EnumerateInvocationList(this.QuestToast))
        {
            try
            {
                d.Invoke(ref str, ref options, ref isHandled);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", d.Method);
            }
        }

        // do nothing if handled
        if (isHandled)
            return;

        var (ioc1, ioc2) = this.DetermineParameterOrder(options);

        using var rssb = new RentedSeStringBuilder();

        fixed (byte* ptr = rssb.Builder.Append(str).GetViewAsSpan())
        {
            this.showQuestToastHook.Original(
                UIModule.Instance(),
                (int)options.Position,
                ptr,
                ioc1,
                options.PlaySound,
                ioc2,
                false);
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
    /// <inheritdoc />
    public unsafe void ShowError(ReadOnlySeString message)
    {
        using var rssb = new RentedSeStringBuilder();

        fixed (byte* ptr = rssb.Builder.Append(message).GetViewAsSpan())
        {
            this.HandleErrorToastDetour(UIModule.Instance(), ptr, false);
        }
    }

    private unsafe void HandleErrorToastDetour(UIModule* thisPtr, CStringPointer text, bool forceVisible)
    {
        if (!text.HasValue)
            return;

        // call events
        var isHandled = false;
        var str = text.AsReadOnlySeString();

        foreach (var d in Delegate.EnumerateInvocationList(this.ErrorToast))
        {
            try
            {
                d.Invoke(ref str, ref isHandled);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", d.Method);
            }
        }

        // do nothing if handled
        if (isHandled)
            return;

        using var rssb = new RentedSeStringBuilder();

        fixed (byte* ptr = rssb.Builder.Append(str).GetViewAsSpan())
        {
            this.showErrorToastHook.Original(thisPtr, ptr, forceVisible);
        }
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
    public void ShowNormal(ReadOnlySeString message, ToastOptions? options = null) => this.toastGuiService.ShowNormal(message, options);

    /// <inheritdoc/>
    public void ShowQuest(ReadOnlySeString message, QuestToastOptions? options = null) => this.toastGuiService.ShowQuest(message, options);

    /// <inheritdoc/>
    public void ShowError(ReadOnlySeString message) => this.toastGuiService.ShowError(message);

    private void ToastForward(ref ReadOnlySeString message, ref ToastOptions options, ref bool isHandled)
        => this.Toast?.Invoke(ref message, ref options, ref isHandled);

    private void QuestToastForward(ref ReadOnlySeString message, ref QuestToastOptions options, ref bool isHandled)
        => this.QuestToast?.Invoke(ref message, ref options, ref isHandled);

    private void ErrorToastForward(ref ReadOnlySeString message, ref bool isHandled)
        => this.ErrorToast?.Invoke(ref message, ref isHandled);
}
