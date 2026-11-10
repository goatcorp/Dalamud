using System.Diagnostics.CodeAnalysis;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

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
    [MemberNotNullWhen(true, nameof(AddonLifecycle))]
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

        foreach (var (eventType, addonListeners) in this.AddonLifecycle.EventListeners)
        {
            using var eventId = ImRaii.PushId(eventType.ToString());

            if (ImGui.CollapsingHeader(eventType.ToString()))
            {
                using var eventIndent = ImRaii.PushIndent();

                if (addonListeners.Count == 0)
                {
                    ImGui.Text("No Addons Registered for Event"u8);
                }

                foreach (var (addonName, listeners) in addonListeners)
                {
                    using var addonId = ImRaii.PushId(addonName);

                    if (ImGui.CollapsingHeader(addonName.IsNullOrEmpty() ? "GLOBAL" : addonName))
                    {
                        using var addonIndent = ImRaii.PushIndent();

                        if (listeners.Count == 0)
                        {
                            ImGui.Text("No Listeners Registered for Event"u8);
                        }

                        foreach (var listener in listeners)
                        {
                            ImGui.Text($"{listener.FunctionDelegate.Method.DeclaringType?.FullName ?? "Unknown Declaring Type"}::{listener.FunctionDelegate.Method.Name}");
                        }
                    }
                }
            }
        }
    }
}
