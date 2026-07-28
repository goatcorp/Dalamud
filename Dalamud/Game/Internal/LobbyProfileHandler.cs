using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CheapLoc;

using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Internal;

/// <summary>
/// This class implements character-specific profile loading on log in.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class LobbyProfileHandler : IInternalDisposableService
{
    private static readonly ModuleLog Log = ModuleLog.Create<LobbyProfileHandler>();

    [ServiceManager.ServiceDependency]
    private readonly AgentLifecycle agentLifecycle = Service<AgentLifecycle>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ProfileManager profileManager = Service<ProfileManager>.Get();

    private readonly AgentLifecycleEventListener agentLobbyPreEventListener;
    private Task lobbyProfileApplyTask = Task.CompletedTask;

    private bool disposed = false;

    [ServiceManager.ServiceConstructor]
    private LobbyProfileHandler()
    {
        this.agentLobbyPreEventListener = new AgentLifecycleEventListener(AgentEvent.PreReceiveEvent, Agent.AgentId.Lobby, this.AgentLobbyPreReceiveEvent);
        this.agentLifecycle.RegisterListener(this.agentLobbyPreEventListener);
    }

    /// <summary>Finalizes an instance of the <see cref="LobbyProfileHandler"/> class.</summary>
    ~LobbyProfileHandler() => this.Dispose(false);

    private string LocDalamudLoadingPluginsForCharacter => Loc.Localize("LoadingPluginsForCharacter", "Loading plugins for this character...");

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.Dispose(true);

    private void Dispose(bool disposing)
    {
        if (this.disposed)
            return;

        if (disposing)
        {
            this.agentLifecycle.UnregisterListener(this.agentLobbyPreEventListener);
        }

        this.disposed = true;
    }

    private void AgentLobbyPreReceiveEvent(AgentEvent type, AgentArgs args)
    {
        // Don't do anything if no profiles have character-specific plugin enabling, since in that case we won't be
        // doing any loading on login and thus don't need to delay the login prompt
        if (this.profileManager.Profiles.All(x => x.Model is ProfileModelV1 { EnableForCharacters: false }))
            return;

        const int loginEventKind = 0x03;
        const int recursionSentinel = 69420;

        if (args is not AgentReceiveEventArgs receiveEventArgs)
            return;

        if (receiveEventArgs.EventKind is not loginEventKind)
            return;

        if (!receiveEventArgs.AtkValueEnumerable.Any())
            return;

        if (!receiveEventArgs.AtkValueEnumerable.ElementAt(0).TryGet(out int eventValue))
            return;

        // Prevent recursion from our own injected event
        if (receiveEventArgs.AtkValueEnumerable.Count() == 2 &&
            receiveEventArgs.AtkValueEnumerable.ElementAt(1).TryGet(out int eventValue2) && eventValue2 == recursionSentinel)
        {
            Log.Verbose("Prevent recursion (eventValue {EventValue})", eventValue);
            return;
        }

        var waitingForProfileLoad = !this.lobbyProfileApplyTask.IsCompleted;

        if (!waitingForProfileLoad && eventValue != 0)
        {
            Log.Verbose("Dismissing login prompt (eventValue {EventValue})", eventValue);
            return;
        }

        args.PreventOriginal();

        if (!waitingForProfileLoad && eventValue == 0)
        {
            var addonSelectYesno = Service<GameGui>.Get().GetAddonByName<AddonSelectYesno>("SelectYesno");

            using var rssb = new RentedSeStringBuilder();

            addonSelectYesno->PromptText->SetText(rssb.Builder
                .PushColorType(539)
                .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
                .PopColorType()
                .Append(this.LocDalamudLoadingPluginsForCharacter)
                .GetViewAsSpan());

            addonSelectYesno->YesButton->SetEnabledState(false);
            addonSelectYesno->NoButton->SetEnabledState(false);
            addonSelectYesno->ShouldFireCallbackAndHideOrClose = true;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(60000);
            var delayTask = Task.Delay(30000, cts.Token); // Just in case something goes wrong, we don't want to leave the player stuck on this screen forever
            var applyTask = Task.Run(() => this.profileManager.ApplyAllWantStatesAsync("Login start"), cts.Token);

            this.lobbyProfileApplyTask = Task.WhenAny(delayTask, applyTask).ContinueWith(_ =>
            {
                Service<Framework>.Get().Run(() =>
                {
                    addonSelectYesno->ShouldFireCallbackAndHideOrClose = false;
                    addonSelectYesno->YesButton->SetEnabledState(true);
                    addonSelectYesno->NoButton->SetEnabledState(true);
                    addonSelectYesno->Close(false);

                    var dummyRet = stackalloc AtkValue[1];
                    dummyRet->Type = AtkValueType.Undefined;
                    dummyRet->Int = recursionSentinel;

                    var okAtkValue = stackalloc AtkValue[2];
                    okAtkValue[0].Type = AtkValueType.Int;
                    okAtkValue[0].Int = 0;
                    okAtkValue[1].Type = AtkValueType.Int;
                    okAtkValue[1].Int = recursionSentinel;

                    AgentLobby.Instance()->ReceiveEvent(
                        dummyRet,
                        okAtkValue,
                        2,
                        loginEventKind);
                });
            }, cts.Token);
        }
    }
}
