using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Networking.Rpc.Model;

namespace Dalamud.Networking.Rpc.Service.Links;

#if DEBUG

/// <summary>
/// A debug controller for link handling.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class DebugLinkHandler : IInternalDisposableService
{
    private readonly LinkHandlerService linkHandlerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLinkHandler"/> class.
    /// </summary>
    /// <param name="linkHandler">Injected LinkHandler.</param>
    [ServiceManager.ServiceConstructor]
    public DebugLinkHandler(LinkHandlerService linkHandler)
    {
        this.linkHandlerService = linkHandler;

        this.linkHandlerService.Register("debug", this.HandleLink);
    }

    /// <inheritdoc/>
    public void DisposeService()
    {
        this.linkHandlerService.Unregister("debug", this.HandleLink);
    }

    private void HandleLink(DalamudUri uri)
    {
        var action = uri.Path.Split("/").GetValue(1)?.ToString();
        switch (action)
        {
            case "toast":
                this.ShowToast(uri);
                break;
            case "notification":
                this.ShowNotification(uri);
                break;
        }
    }

    private void ShowToast(DalamudUri uri)
    {
        var message = uri.QueryParams.Get("message") ?? "Hello, world!";
        Service<ToastGui>.Get().ShowNormal(message);
    }

    private void ShowNotification(DalamudUri uri)
    {
        Service<NotificationManager>.Get().AddNotification(
            new Notification
            {
                Title = uri.QueryParams.Get("title"),
                Content = uri.QueryParams.Get("content") ?? "Hello, world!",
            });
    }
}

#endif
