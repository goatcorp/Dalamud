using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

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
            const string text = "Bla bla bla bla bla bla bla bla bla bla bla.\nBla bla bla bla bla bla bla bla bla bla bla bla bla bla.";

            NewRandom(out var title, out var type);
            var n = notifications.AddNotification(
                new()
                {
                    Content = text,
                    Title = title,
                    Type = type,
                    Interactible = true,
                    ClickIsDismiss = false,
                    Expiry = DateTime.MaxValue,
                });

            var nclick = 0;
            n.Click += _ => nclick++;
            n.DrawActions += an =>
            {
                if (ImGui.Button("Update in place"))
                {
                    NewRandom(out title, out type);
                    an.Update(an.CloneNotification() with { Title = title, Type = type });
                }

                if (an.IsMouseHovered)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Dismiss"))
                        an.DismissNow();
                }

                ImGui.AlignTextToFramePadding();
                ImGui.SameLine();
                ImGui.TextUnformatted($"Clicked {nclick} time(s)");
            };
        }
    }

    private static void NewRandom(out string? title, out NotificationType type)
    {
        var rand = new Random();

        title = rand.Next(0, 7) switch
        {
            0 => "This is a toast",
            1 => "Truly, a toast",
            2 => "I am testing this toast",
            3 => "I hope this looks right",
            4 => "Good stuff",
            5 => "Nice",
            _ => null,
        };

        type = rand.Next(0, 5) switch
        {
            0 => NotificationType.Error,
            1 => NotificationType.Warning,
            2 => NotificationType.Info,
            3 => NotificationType.Success,
            4 => NotificationType.None,
            _ => NotificationType.None,
        };
    }
}
