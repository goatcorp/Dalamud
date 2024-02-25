using System;
using System.Numerics;

using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;

using ImGuiNET;

namespace Dalamud.CorePlugin
{
    /// <summary>
    /// Class responsible for drawing the main plugin window.
    /// </summary>
    internal class PluginWindow : Window, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginWindow"/> class.
        /// </summary>
        /// <param name="pluginImpl"></param>
        public PluginWindow(PluginImpl pluginImpl)
            : base("CorePlugin")
        {
            this.PluginImpl = pluginImpl;
            this.IsOpen = true;

            this.Size = new Vector2(810, 520);
            this.SizeCondition = ImGuiCond.FirstUseEver;
        }

        public PluginImpl PluginImpl { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            if (ImGui.Button("Legacy"))
                this.PluginImpl.Interface.UiBuilder.AddNotification("asdf");
            if (ImGui.Button("Test"))
            {
                const string text =
                    "Bla bla bla bla bla bla bla bla bla bla bla.\nBla bla bla bla bla bla bla bla bla bla bla bla bla bla.";

                NewRandom(out var title, out var type);
                var n = this.PluginImpl.NotificationManager.AddNotification(
                    new()
                    {
                        Content = text,
                        Title = title,
                        Type = type,
                        Interactible = true,
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
}
