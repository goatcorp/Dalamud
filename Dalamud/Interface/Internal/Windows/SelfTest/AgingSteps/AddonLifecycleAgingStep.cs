using System.Collections.Generic;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup AddonLifecycle Service.
/// </summary>
internal class AddonLifecycleAgingStep : IAgingStep
{
    private readonly List<AddonLifecycleEventListener> listeners;
    
    private AddonLifecycle? service;
    private TestStep currentStep = TestStep.CharacterRefresh;
    private bool listenersRegistered;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonLifecycleAgingStep"/> class.
    /// </summary>
    public AddonLifecycleAgingStep()
    {
        this.listeners = new List<AddonLifecycleEventListener>
        {
            new(AddonEvent.PostSetup, "Character", this.PostSetup),
            new(AddonEvent.PostUpdate, "Character", this.PostUpdate),
            new(AddonEvent.PostDraw, "Character", this.PostDraw),
            new(AddonEvent.PostRefresh, "Character", this.PostRefresh),
            new(AddonEvent.PostRequestedUpdate, "Character", this.PostRequestedUpdate),
            new(AddonEvent.PreFinalize, "Character", this.PreFinalize),
        };
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
            foreach (var listener in this.listeners)
            {
                this.service.RegisterListener(listener);
            }
            
            this.listenersRegistered = true;
        }

        switch (this.currentStep)
        {
            case TestStep.CharacterRefresh:
                ImGui.Text("Open Character Window.");
                break;

            case TestStep.CharacterSetup:
                ImGui.Text("Open Character Window.");
                break;

            case TestStep.CharacterRequestedUpdate:
                ImGui.Text("Change tabs, or un-equip/equip gear.");
                break;

            case TestStep.CharacterFinalize:
                ImGui.Text("Close Character Window.");
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
