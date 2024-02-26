using System.Linq;
using System.Threading.Tasks;

using Dalamud.Game.Text;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying ImGui test.
/// </summary>
internal class ImGuiWidget : IDataWindowWidget
{
    private NotificationTemplate notificationTemplate;

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
        this.notificationTemplate.Reset();
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

        ImGui.TextUnformatted(
            $"WindowSystem.TimeSinceLastAnyFocus: {WindowSystem.TimeSinceLastAnyFocus.TotalMilliseconds:0}ms");

        ImGui.Separator();

        ImGui.Checkbox("##manualContent", ref this.notificationTemplate.ManualContent);
        ImGui.SameLine();
        ImGui.InputText("Content##content", ref this.notificationTemplate.Content, 255);

        ImGui.Checkbox("##manualTitle", ref this.notificationTemplate.ManualTitle);
        ImGui.SameLine();
        ImGui.InputText("Title##title", ref this.notificationTemplate.Title, 255);

        ImGui.Checkbox("##manualMinimizedText", ref this.notificationTemplate.ManualMinimizedText);
        ImGui.SameLine();
        ImGui.InputText("MinimizedText##minimizedText", ref this.notificationTemplate.MinimizedText, 255);

        ImGui.Checkbox("##manualType", ref this.notificationTemplate.ManualType);
        ImGui.SameLine();
        ImGui.Combo(
            "Type##type",
            ref this.notificationTemplate.TypeInt,
            NotificationTemplate.TypeTitles,
            NotificationTemplate.TypeTitles.Length);

        ImGui.Combo(
            "Icon Source##iconSourceCombo",
            ref this.notificationTemplate.IconSourceInt,
            NotificationTemplate.IconSourceTitles,
            NotificationTemplate.IconSourceTitles.Length);
        switch (this.notificationTemplate.IconSourceInt)
        {
            case 1 or 2:
                ImGui.InputText(
                    "Icon Text##iconSourceText",
                    ref this.notificationTemplate.IconSourceText,
                    255);
                break;
            case 3 or 4:
                ImGui.Combo(
                    "Icon Source##iconSourceAssetCombo",
                    ref this.notificationTemplate.IconSourceAssetInt,
                    NotificationTemplate.AssetSources,
                    NotificationTemplate.AssetSources.Length);
                break;
            case 5 or 7:
                ImGui.InputText(
                    "Game Path##iconSourceText",
                    ref this.notificationTemplate.IconSourceText,
                    255);
                break;
            case 6 or 8:
                ImGui.InputText(
                    "File Path##iconSourceText",
                    ref this.notificationTemplate.IconSourceText,
                    255);
                break;
        }

        ImGui.Combo(
            "Initial Duration",
            ref this.notificationTemplate.InitialDurationInt,
            NotificationTemplate.InitialDurationTitles,
            NotificationTemplate.InitialDurationTitles.Length);

        ImGui.Combo(
            "Hover Extend Duration",
            ref this.notificationTemplate.HoverExtendDurationInt,
            NotificationTemplate.HoverExtendDurationTitles,
            NotificationTemplate.HoverExtendDurationTitles.Length);

        ImGui.Combo(
            "Progress",
            ref this.notificationTemplate.ProgressMode,
            NotificationTemplate.ProgressModeTitles,
            NotificationTemplate.ProgressModeTitles.Length);

        ImGui.Checkbox("Minimized", ref this.notificationTemplate.Minimized);

        ImGui.Checkbox("Show Indeterminate If No Expiry", ref this.notificationTemplate.ShowIndeterminateIfNoExpiry);

        ImGui.Checkbox("User Dismissable", ref this.notificationTemplate.UserDismissable);

        ImGui.Checkbox(
            "Action Bar (always on if not user dismissable for the example)",
            ref this.notificationTemplate.ActionBar);

        if (ImGui.Button("Add notification"))
        {
            var text =
                "Bla bla bla bla bla bla bla bla bla bla bla.\nBla bla bla bla bla bla bla bla bla bla bla bla bla bla.";

            NewRandom(out var title, out var type, out var progress);
            if (this.notificationTemplate.ManualTitle)
                title = this.notificationTemplate.Title;
            if (this.notificationTemplate.ManualContent)
                text = this.notificationTemplate.Content;
            if (this.notificationTemplate.ManualType)
                type = (NotificationType)this.notificationTemplate.TypeInt;

            var n = notifications.AddNotification(
                new()
                {
                    Content = text,
                    Title = title,
                    MinimizedText = this.notificationTemplate.ManualMinimizedText
                                        ? this.notificationTemplate.MinimizedText
                                        : null,
                    Type = type,
                    ShowIndeterminateIfNoExpiry = this.notificationTemplate.ShowIndeterminateIfNoExpiry,
                    Minimized = this.notificationTemplate.Minimized,
                    UserDismissable = this.notificationTemplate.UserDismissable,
                    InitialDuration =
                        this.notificationTemplate.InitialDurationInt == 0
                            ? TimeSpan.MaxValue
                            : NotificationTemplate.Durations[this.notificationTemplate.InitialDurationInt],
                    HoverExtendDuration =
                        this.notificationTemplate.HoverExtendDurationInt == 0
                            ? TimeSpan.Zero
                            : NotificationTemplate.Durations[this.notificationTemplate.HoverExtendDurationInt],
                    Progress = this.notificationTemplate.ProgressMode switch
                    {
                        0 => 1f,
                        1 => progress,
                        2 => 0f,
                        3 => 0f,
                        4 => -1f,
                        _ => 0.5f,
                    },
                    IconSource = this.notificationTemplate.IconSourceInt switch
                    {
                        1 => INotificationIconSource.From(
                            (SeIconChar)(this.notificationTemplate.IconSourceText.Length == 0
                                             ? 0
                                             : this.notificationTemplate.IconSourceText[0])),
                        2 => INotificationIconSource.From(
                            (FontAwesomeIcon)(this.notificationTemplate.IconSourceText.Length == 0
                                                  ? 0
                                                  : this.notificationTemplate.IconSourceText[0])),
                        3 => INotificationIconSource.From(
                            Service<DalamudAssetManager>.Get().GetDalamudTextureWrap(
                                Enum.Parse<DalamudAsset>(
                                    NotificationTemplate.AssetSources[
                                        this.notificationTemplate.IconSourceAssetInt])),
                            false),
                        4 => INotificationIconSource.From(
                            () =>
                                Service<DalamudAssetManager>.Get().GetDalamudTextureWrapAsync(
                                    Enum.Parse<DalamudAsset>(
                                        NotificationTemplate.AssetSources[
                                            this.notificationTemplate.IconSourceAssetInt]))),
                        5 => INotificationIconSource.FromGame(this.notificationTemplate.IconSourceText),
                        6 => INotificationIconSource.FromFile(this.notificationTemplate.IconSourceText),
                        7 => INotificationIconSource.From(
                            Service<TextureManager>.Get().GetTextureFromGame(this.notificationTemplate.IconSourceText),
                            false),
                        8 => INotificationIconSource.From(
                            Service<TextureManager>.Get().GetTextureFromFile(
                                new(this.notificationTemplate.IconSourceText)),
                            false),
                        _ => null,
                    },
                },
                true);
            switch (this.notificationTemplate.ProgressMode)
            {
                case 2:
                    Task.Run(
                        async () =>
                        {
                            for (var i = 0; i <= 10 && !n.IsDismissed; i++)
                            {
                                await Task.Delay(500);
                                n.Progress = i / 10f;
                            }
                        });
                    break;
                case 3:
                    Task.Run(
                        async () =>
                        {
                            for (var i = 0; i <= 10 && !n.IsDismissed; i++)
                            {
                                await Task.Delay(500);
                                n.Progress = i / 10f;
                            }

                            n.ExtendBy(NotificationConstants.DefaultDisplayDuration);
                            n.InitialDuration = NotificationConstants.DefaultDisplayDuration;
                        });
                    break;
            }

            if (this.notificationTemplate.ActionBar || !this.notificationTemplate.UserDismissable)
            {
                var nclick = 0;
                n.Click += _ => nclick++;
                n.DrawActions += an =>
                {
                    if (ImGui.Button("Update in place"))
                    {
                        NewRandom(out title, out type, out progress);
                        an.Title = title;
                        an.Type = type;
                        an.Progress = progress;
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
    }

    private static void NewRandom(out string? title, out NotificationType type, out float progress)
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

        if (rand.Next() % 2 == 0)
            progress = -1;
        else
            progress = rand.NextSingle();
    }

    private struct NotificationTemplate
    {
        public static readonly string[] IconSourceTitles =
        {
            "None (use Type)",
            "SeIconChar",
            "FontAwesomeIcon",
            "TextureWrap from DalamudAssets",
            "TextureWrapTask from DalamudAssets",
            "GamePath",
            "FilePath",
            "TextureWrap from GamePath",
            "TextureWrap from FilePath",
        };

        public static readonly string[] AssetSources =
            Enum.GetValues<DalamudAsset>()
                .Where(x => x.GetAttribute<DalamudAssetAttribute>()?.Purpose is DalamudAssetPurpose.TextureFromPng)
                .Select(Enum.GetName)
                .ToArray();

        public static readonly string[] ProgressModeTitles =
        {
            "Default",
            "Random",
            "Increasing",
            "Increasing & Auto Dismiss",
            "Indeterminate",
        };

        public static readonly string[] TypeTitles =
        {
            nameof(NotificationType.None),
            nameof(NotificationType.Success),
            nameof(NotificationType.Warning),
            nameof(NotificationType.Error),
            nameof(NotificationType.Info),
        };

        public static readonly string[] InitialDurationTitles =
        {
            "Infinite",
            "1 seconds",
            "3 seconds (default)",
            "10 seconds",
        };

        public static readonly string[] HoverExtendDurationTitles =
        {
            "Disable",
            "1 seconds",
            "3 seconds (default)",
            "10 seconds",
        };

        public static readonly TimeSpan[] Durations =
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            NotificationConstants.DefaultDisplayDuration,
            TimeSpan.FromSeconds(10),
        };

        public bool ManualContent;
        public string Content;
        public bool ManualTitle;
        public string Title;
        public bool ManualMinimizedText;
        public string MinimizedText;
        public int IconSourceInt;
        public string IconSourceText;
        public int IconSourceAssetInt;
        public bool ManualType;
        public int TypeInt;
        public int InitialDurationInt;
        public int HoverExtendDurationInt;
        public bool ShowIndeterminateIfNoExpiry;
        public bool Minimized;
        public bool UserDismissable;
        public bool ActionBar;
        public int ProgressMode;

        public void Reset()
        {
            this.ManualContent = false;
            this.Content = string.Empty;
            this.ManualTitle = false;
            this.Title = string.Empty;
            this.ManualMinimizedText = false;
            this.MinimizedText = string.Empty;
            this.IconSourceInt = 0;
            this.IconSourceText = "ui/icon/000000/000004_hr1.tex";
            this.IconSourceAssetInt = 0;
            this.ManualType = false;
            this.TypeInt = (int)NotificationType.None;
            this.InitialDurationInt = 2;
            this.HoverExtendDurationInt = 2;
            this.ShowIndeterminateIfNoExpiry = true;
            this.Minimized = true;
            this.UserDismissable = true;
            this.ActionBar = true;
            this.ProgressMode = 0;
        }
    }
}
