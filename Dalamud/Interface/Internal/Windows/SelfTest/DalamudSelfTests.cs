using Dalamud.Excel.Sheets;
using Dalamud.Interface.Internal.Windows.SelfTest.Steps;
using Dalamud.Plugin.SelfTest.Internal;

namespace Dalamud.Interface.Internal.Windows.SelfTest;

/// <summary>
/// Class handling Dalamud self-test registration.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class DalamudSelfTests : IServiceType
{
    [ServiceManager.ServiceConstructor]
    private DalamudSelfTests(SelfTestRegistry registry)
    {
        registry.RegisterDalamudSelfTestSteps([
            new LoginEventSelfTestStep(),
            new WaitFramesSelfTestStep(1000),
            new FrameworkTaskSchedulerSelfTestStep(),
            new HookVerifierSelfTestStep(),
            new EnterTerritorySelfTestStep(148, "Central Shroud"),
            new ItemPayloadSelfTestStep(),
            new ContextMenuSelfTestStep(),
            new NamePlateSelfTestStep(),
            new ActorTableSelfTestStep(),
            new FateTableSelfTestStep(),
            new AetheryteListSelfTestStep(),
            new ConditionSelfTestStep(),
            new ToastSelfTestStep(),
            new TargetSelfTestStep(),
            new KeyStateSelfTestStep(),
            new GamepadStateSelfTestStep(),
            new ChatSelfTestStep(),
            new HoverSelfTestStep(),
            new LuminaSheetsHashTestStep(),
            new LuminaSelfTestStep<Item>(true),
            new LuminaSelfTestStep<Level>(true),
            new LuminaSelfTestStep<global::Dalamud.Excel.Sheets.Action>(true),
            new LuminaSelfTestStep<Quest>(true),
            new LuminaSelfTestStep<TerritoryType>(false),
            new AgentLifecycleSelfTestStep(),
            new AddonLifecycleSelfTestStep(),
            new PartyFinderSelfTestStep(),
            new HandledExceptionSelfTestStep(),
            new DutyStateSelfTestStep(),
            new GameConfigSelfTestStep(),
            new MarketBoardSelfTestStep(),
            new SheetRedirectResolverSelfTestStep(),
            new NounProcessorSelfTestStep(),
            new SeStringEvaluatorSelfTestStep(),
            new CompletionSelfTestStep(),
            new LogoutEventSelfTestStep()
        ]);
    }
}
