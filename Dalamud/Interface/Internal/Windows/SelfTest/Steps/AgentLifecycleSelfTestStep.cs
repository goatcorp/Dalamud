using System.Collections.Generic;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;
using Dalamud.Plugin.SelfTest;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup AgentLifecycle Service.
/// </summary>
internal class AgentLifecycleSelfTestStep : ISelfTestStep
{
    private readonly List<AgentLifecycleEventListener> listeners;

    private AgentLifecycle? service;
    private TestStep currentStep = TestStep.ReceiveEvent;
    private bool listenersRegistered;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentLifecycleSelfTestStep"/> class.
    /// </summary>
    public AgentLifecycleSelfTestStep()
    {
        this.listeners =
        [
            // ReceiveEvent is gonna be what is most commonly used, and is the easiest to test
            // Other events require performing actions that may be considered unreasonable for a test, such as switching job/changing level
            new AgentLifecycleEventListener(AgentEvent.PreReceiveEvent, AgentId.ConfigCharacter, this.PreReceiveEvent),
        ];
    }

    private enum TestStep
    {
        ReceiveEvent,
        Complete,
    }

    /// <inheritdoc/>
    public string Name => "Test AgentLifecycle";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        this.service ??= Service<AgentLifecycle>.Get();
        if (this.service is null) return SelfTestStepResult.Fail;

        if (!this.listenersRegistered)
        {
            foreach (var listener in this.listeners)
            {
                this.service.RegisterListener(listener);
            }

            this.listenersRegistered = true;
        }

        switch (this.currentStep)
        {
            case TestStep.ReceiveEvent:
                ImGui.Text("Open Character Configuration Window."u8);
                break;

            case TestStep.Complete:
            default:
                // Nothing to report to tester.
                break;
        }

        return this.currentStep is TestStep.Complete ? SelfTestStepResult.Pass : SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        foreach (var listener in this.listeners)
        {
            this.service?.UnregisterListener(listener);
        }
    }

    private void PreReceiveEvent(AgentEvent type, AgentArgs args)
    {
        if (this.currentStep is TestStep.ReceiveEvent) this.currentStep++;
    }
}
