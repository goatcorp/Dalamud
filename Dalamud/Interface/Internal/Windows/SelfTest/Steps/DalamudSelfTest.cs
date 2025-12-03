using Dalamud.Plugin.SelfTest.Internal;
using Lumina.Excel.Sheets;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Class handling Dalamud self-test registration.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class DalamudSelfTest : IServiceType
{
    [ServiceManager.ServiceConstructor]
    private DalamudSelfTest(SelfTestRegistry registry)
    {
        registry.RegisterDalamudSelfTestSteps([
            new LoginEventSelfTestStep(),
            new WaitFramesSelfTestStep(1000),
            new FrameworkTaskSchedulerSelfTestStep(),
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
            new LuminaSelfTestStep<Item>(true),
            new LuminaSelfTestStep<Level>(true),
            new LuminaSelfTestStep<Lumina.Excel.Sheets.Action>(true),
            new LuminaSelfTestStep<Quest>(true),
            new LuminaSelfTestStep<TerritoryType>(false),
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
