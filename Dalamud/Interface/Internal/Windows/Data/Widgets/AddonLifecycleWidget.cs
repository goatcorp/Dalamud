using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Debug widget for displaying AddonLifecycle data.
/// </summary>
public class AddonLifecycleWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "AddonLifecycle" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Addon Lifecycle";
    
    /// <inheritdoc/>
    [MemberNotNullWhen(true, "AddonLifecycle")]
    public bool Ready { get; set; }
    
    private AddonLifecycle? AddonLifecycle { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        Service<AddonLifecycle>
            .GetAsync()
            .ContinueWith(
                r =>
                {
                    this.AddonLifecycle = r.Result;
                    this.Ready = true;
                });
    }
    
    /// <inheritdoc/>
    public void Draw()
    {
        if (!this.Ready)
        {
            ImGui.Text("AddonLifecycle Reference is null, reload module.");
            return;
        }

        if (ImGui.CollapsingHeader("Listeners"))
        {
            ImGui.Indent();
            this.DrawEventListeners();
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("ReceiveEvent Hooks"))
        {
            ImGui.Indent();
            this.DrawReceiveEventHooks();
            ImGui.Unindent();
        }
    }
    
    private void DrawEventListeners()
    {
        if (!this.Ready) return;
        
        foreach (var eventType in Enum.GetValues<AddonEvent>())
        {
            if (ImGui.CollapsingHeader(eventType.ToString()))
            {
                ImGui.Indent();
                var listeners = this.AddonLifecycle.EventListeners.Where(listener => listener.EventType == eventType).ToList();

                if (!listeners.Any())
                {
                    ImGui.Text("No Listeners Registered for Event");
                }
                
                if (ImGui.BeginTable("AddonLifecycleListenersTable", 2))
                {
                    ImGui.TableSetupColumn("##AddonName", ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("##MethodInvoke", ImGuiTableColumnFlags.WidthStretch);

                    foreach (var listener in listeners)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(listener.AddonName is "" ? "GLOBAL" : listener.AddonName);

                        ImGui.TableNextColumn();
                        ImGui.Text($"{listener.FunctionDelegate.Method.DeclaringType.FullName}::{listener.FunctionDelegate.Method.Name}");
                    }
                    
                    ImGui.EndTable();
                }
                
                ImGui.Unindent();
            }
        }
    }

    private void DrawReceiveEventHooks()
    {
        if (!this.Ready) return;

        var listeners = this.AddonLifecycle.ReceiveEventListeners;

        if (!listeners.Any())
        {
            ImGui.Text("No ReceiveEvent Hooks are Registered");
        }
        
        foreach (var receiveEventListener in this.AddonLifecycle.ReceiveEventListeners)
        {
            if (ImGui.CollapsingHeader(string.Join(", ", receiveEventListener.AddonNames)))
            {
                ImGui.Columns(2);

                ImGui.Text("Hook Address");
                ImGui.NextColumn();
                ImGui.Text(receiveEventListener.HookAddress.ToString("X"));

                ImGui.NextColumn();
                ImGui.Text("Hook Status");
                ImGui.NextColumn();
                if (receiveEventListener.Hook is null)
                {
                    ImGui.Text("Hook is null");
                }
                else
                {
                    var color = receiveEventListener.Hook.IsEnabled ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed;
                    var text = receiveEventListener.Hook.IsEnabled ? "Enabled" : "Disabled";
                    ImGui.TextColored(color, text);
                }
                
                ImGui.Columns(1);
            }
        }
    }
}
