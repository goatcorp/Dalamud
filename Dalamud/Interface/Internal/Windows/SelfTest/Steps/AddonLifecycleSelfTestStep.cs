using System.Collections.Generic;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.SelfTest;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup AddonLifecycle Service.
/// </summary>
internal class AddonLifecycleSelfTestStep : ISelfTestStep
{
    private AddonLifecycle? service;
    private List<AddonLifecycleEventListener>? listeners;
    private TestStep currentStep = TestStep.CharacterRefresh;
    private bool listenersRegistered;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonLifecycleSelfTestStep"/> class.
    /// </summary>
    public AddonLifecycleSelfTestStep()
    {
    }

    private enum TestStep
    {
        CharacterRefresh,
        CharacterSetup,
        CharacterRequestedUpdate,
        CharacterUpdate,
        CharacterDraw,
        CharacterFinalize,
        Complete,
    }

    /// <inheritdoc/>
    public string Name => "Test AddonLifecycle";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        this.service ??= Service<AddonLifecycle>.Get();
        if (this.service is null) return SelfTestStepResult.Fail;

        if (!this.listenersRegistered)
        {
            this.listeners =
            [
                new(this.service, AddonEvent.PostSetup, "Character", this.PostSetup),
                new(this.service, AddonEvent.PostUpdate, "Character", this.PostUpdate),
                new(this.service, AddonEvent.PostDraw, "Character", this.PostDraw),
                new(this.service, AddonEvent.PostRefresh, "Character", this.PostRefresh),
                new(this.service, AddonEvent.PostRequestedUpdate, "Character", this.PostRequestedUpdate),
                new(this.service, AddonEvent.PreFinalize, "Character", this.PreFinalize),
            ];

            foreach (var listener in this.listeners)
            {
                this.service.RegisterListener(listener);
            }

            this.listenersRegistered = true;
        }

        switch (this.currentStep)
        {
            case TestStep.CharacterRefresh:
                ImGui.Text("Open Character Window."u8);
                break;

            case TestStep.CharacterSetup:
                ImGui.Text("Open Character Window."u8);
                break;

            case TestStep.CharacterRequestedUpdate:
                ImGui.Text("Change tabs, or un-equip/equip gear."u8);
                break;

            case TestStep.CharacterFinalize:
                ImGui.Text("Close Character Window."u8);
                break;

            case TestStep.CharacterUpdate:
            case TestStep.CharacterDraw:
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

        this.listeners = null;
        this.listenersRegistered = false;
    }

    private void PostSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        if (this.currentStep is TestStep.CharacterSetup) this.currentStep++;
    }

    private void PostUpdate(AddonEvent eventType, AddonArgs addonInfo)
    {
        if (this.currentStep is TestStep.CharacterUpdate) this.currentStep++;
    }

    private void PostDraw(AddonEvent eventType, AddonArgs addonInfo)
    {
        if (this.currentStep is TestStep.CharacterDraw) this.currentStep++;
    }

    private void PostRefresh(AddonEvent eventType, AddonArgs addonInfo)
    {
        if (this.currentStep is TestStep.CharacterRefresh) this.currentStep++;
    }

    private void PostRequestedUpdate(AddonEvent eventType, AddonArgs addonInfo)
    {
        if (this.currentStep is TestStep.CharacterRequestedUpdate) this.currentStep++;
    }

    private void PreFinalize(AddonEvent eventType, AddonArgs addonInfo)
    {
        if (this.currentStep is TestStep.CharacterFinalize) this.currentStep++;
    }
}
