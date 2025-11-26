using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Interface.Utility;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Debug widget for displaying AddonLifecycle data.
/// </summary>
public class AddonLifecycleWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["AddonLifecycle"];

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
            ImGui.Text("AddonLifecycle Reference is null, reload module."u8);
            return;
        }

        if (ImGui.CollapsingHeader("Listeners"u8))
        {
            ImGui.Indent();
            this.DrawEventListeners();
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

                if (listeners.Count == 0)
                {
                    ImGui.Text("No Listeners Registered for Event"u8);
                }

                if (ImGui.BeginTable("AddonLifecycleListenersTable"u8, 2))
                {
                    ImGui.TableSetupColumn("##AddonName"u8, ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("##MethodInvoke"u8, ImGuiTableColumnFlags.WidthStretch);

                    foreach (var listener in listeners)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(listener.AddonName is "" ? "GLOBAL" : listener.AddonName);

                        ImGui.TableNextColumn();
                        ImGui.Text($"{listener.FunctionDelegate.Method.DeclaringType?.FullName ?? "Unknown Declaring Type"}::{listener.FunctionDelegate.Method.Name}");
                    }

                    ImGui.EndTable();
                }

                ImGui.Unindent();
            }
        }
    }
}
