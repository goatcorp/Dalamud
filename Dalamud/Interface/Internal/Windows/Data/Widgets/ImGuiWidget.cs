using System;

using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Widget for displaying ImGui test.
/// </summary>
internal class ImGuiWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "imgui" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "ImGui"; 

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var interfaceManager = Service<InterfaceManager>.Get();
        var notifications = Service<NotificationManager>.Get();

        ImGui.Text("Monitor count: " + ImGui.GetPlatformIO().Monitors.Size);
        ImGui.Text("OverrideGameCursor: " + interfaceManager.OverrideGameCursor);

        ImGui.Button("THIS IS A BUTTON###hoverTestButton");
        interfaceManager.OverrideGameCursor = !ImGui.IsItemHovered();

        ImGui.Separator();

        ImGui.TextUnformatted($"WindowSystem.TimeSinceLastAnyFocus: {WindowSystem.TimeSinceLastAnyFocus.TotalMilliseconds:0}ms");

        ImGui.Separator();

        if (ImGui.Button("Add random notification"))
        {
            var rand = new Random();

            var title = rand.Next(0, 5) switch
            {
                0 => "This is a toast",
                1 => "Truly, a toast",
                2 => "I am testing this toast",
                3 => "I hope this looks right",
                4 => "Good stuff",
                5 => "Nice",
                _ => null,
            };

            var type = rand.Next(0, 4) switch
            {
                0 => NotificationType.Error,
                1 => NotificationType.Warning,
                2 => NotificationType.Info,
                3 => NotificationType.Success,
                4 => NotificationType.None,
                _ => NotificationType.None,
            };

            const string text = "Bla bla bla bla bla bla bla bla bla bla bla.\nBla bla bla bla bla bla bla bla bla bla bla bla bla bla.";

            notifications.AddNotification(text, title, type);
        }
    }
}
