using System.Collections.Generic;
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
    private readonly HashSet<IActiveNotification> notifications = new();
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
        this.notifications.RemoveWhere(x => x.DismissReason.HasValue);

        var interfaceManager = Service<InterfaceManager>.Get();
        var nm = Service<NotificationManager>.Get();

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
            "Icon##iconCombo",
            ref this.notificationTemplate.IconInt,
            NotificationTemplate.IconTitles,
            NotificationTemplate.IconTitles.Length);
        switch (this.notificationTemplate.IconInt)
        {
            case 1 or 2:
                ImGui.InputText(
                    "Icon Text##iconText",
                    ref this.notificationTemplate.IconText,
                    255);
                break;
            case 5 or 6:
                ImGui.Combo(
                    "Asset##iconAssetCombo",
                    ref this.notificationTemplate.IconAssetInt,
                    NotificationTemplate.AssetSources,
                    NotificationTemplate.AssetSources.Length);
                break;
            case 3 or 7:
                ImGui.InputText(
                    "Game Path##iconText",
                    ref this.notificationTemplate.IconText,
                    255);
                break;
            case 4 or 8:
                ImGui.InputText(
                    "File Path##iconText",
                    ref this.notificationTemplate.IconText,
                    255);
                break;
        }

        ImGui.Combo(
            "Initial Duration",
            ref this.notificationTemplate.InitialDurationInt,
            NotificationTemplate.InitialDurationTitles,
            NotificationTemplate.InitialDurationTitles.Length);

        ImGui.Combo(
            "Extension Duration",
            ref this.notificationTemplate.HoverExtendDurationInt,
            NotificationTemplate.HoverExtendDurationTitles,
            NotificationTemplate.HoverExtendDurationTitles.Length);

        ImGui.Combo(
            "Progress",
            ref this.notificationTemplate.ProgressMode,
            NotificationTemplate.ProgressModeTitles,
            NotificationTemplate.ProgressModeTitles.Length);

        ImGui.Checkbox("Respect UI Hidden", ref this.notificationTemplate.RespectUiHidden);

        ImGui.Checkbox("Minimized", ref this.notificationTemplate.Minimized);

        ImGui.Checkbox("Show Indeterminate If No Expiry", ref this.notificationTemplate.ShowIndeterminateIfNoExpiry);

        ImGui.Checkbox("User Dismissable", ref this.notificationTemplate.UserDismissable);

        ImGui.Checkbox(
            "Action Bar (always on if not user dismissable for the example)",
            ref this.notificationTemplate.ActionBar);

        ImGui.Checkbox("Leave Textures Open", ref this.notificationTemplate.LeaveTexturesOpen);

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

            var n = nm.AddNotification(
                new()
                {
                    Content = text,
                    Title = title,
                    MinimizedText = this.notificationTemplate.ManualMinimizedText
                                        ? this.notificationTemplate.MinimizedText
                                        : null,
                    Type = type,
                    ShowIndeterminateIfNoExpiry = this.notificationTemplate.ShowIndeterminateIfNoExpiry,
                    RespectUiHidden = this.notificationTemplate.RespectUiHidden,
                    Minimized = this.notificationTemplate.Minimized,
                    UserDismissable = this.notificationTemplate.UserDismissable,
                    InitialDuration =
                        this.notificationTemplate.InitialDurationInt == 0
                            ? TimeSpan.MaxValue
                            : NotificationTemplate.Durations[this.notificationTemplate.InitialDurationInt],
                    ExtensionDurationSinceLastInterest =
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
                    Icon = this.notificationTemplate.IconInt switch
                    {
                        1 => INotificationIcon.From(
                            (SeIconChar)(this.notificationTemplate.IconText.Length == 0
                                             ? 0
                                             : this.notificationTemplate.IconText[0])),
                        2 => INotificationIcon.From(
                            (FontAwesomeIcon)(this.notificationTemplate.IconText.Length == 0
                                                  ? 0
                                                  : this.notificationTemplate.IconText[0])),
                        3 => INotificationIcon.FromGame(this.notificationTemplate.IconText),
                        4 => INotificationIcon.FromFile(this.notificationTemplate.IconText),
                        _ => null,
                    },
                });

            this.notifications.Add(n);

            var dam = Service<DalamudAssetManager>.Get();
            var tm = Service<TextureManager>.Get();
            switch (this.notificationTemplate.IconInt)
            {
                case 5:
                    n.SetIconTexture(
                        DisposeLoggingTextureWrap.Wrap(
                            dam.GetDalamudTextureWrap(
                                Enum.Parse<DalamudAsset>(
                                    NotificationTemplate.AssetSources[this.notificationTemplate.IconAssetInt]))),
                        this.notificationTemplate.LeaveTexturesOpen);
                    break;
                case 6:
                    n.SetIconTexture(
                        dam.GetDalamudTextureWrapAsync(
                               Enum.Parse<DalamudAsset>(
                                   NotificationTemplate.AssetSources[this.notificationTemplate.IconAssetInt]))
                           .ContinueWith(
                               r => r.IsCompletedSuccessfully
                                        ? Task.FromResult<IDalamudTextureWrap>(DisposeLoggingTextureWrap.Wrap(r.Result))
                                        : r).Unwrap(),
                        this.notificationTemplate.LeaveTexturesOpen);
                    break;
                case 7:
                    n.SetIconTexture(
                        DisposeLoggingTextureWrap.Wrap(tm.GetTextureFromGame(this.notificationTemplate.IconText)),
                        this.notificationTemplate.LeaveTexturesOpen);
                    break;
                case 8:
                    n.SetIconTexture(
                        DisposeLoggingTextureWrap.Wrap(tm.GetTextureFromFile(new(this.notificationTemplate.IconText))),
                        this.notificationTemplate.LeaveTexturesOpen);
                    break;
            }

            switch (this.notificationTemplate.ProgressMode)
            {
                case 2:
                    Task.Run(
                        async () =>
                        {
                            for (var i = 0; i <= 10 && !n.DismissReason.HasValue; i++)
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
                            for (var i = 0; i <= 10 && !n.DismissReason.HasValue; i++)
                            {
                                await Task.Delay(500);
                                n.Progress = i / 10f;
                            }

                            n.ExtendBy(NotificationConstants.DefaultDuration);
                            n.InitialDuration = NotificationConstants.DefaultDuration;
                        });
                    break;
            }

            if (this.notificationTemplate.ActionBar || !this.notificationTemplate.UserDismissable)
            {
                var nclick = 0;
                var testString = "input";

                n.Click += _ => nclick++;
                n.DrawActions += an =>
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted($"{nclick}");

                    ImGui.SameLine();
                    if (ImGui.Button("Update"))
                    {
                        NewRandom(out title, out type, out progress);
                        an.Notification.Title = title;
                        an.Notification.Type = type;
                        an.Notification.Progress = progress;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Dismiss"))
                        an.Notification.DismissNow();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(an.MaxCoord.X - ImGui.GetCursorPosX());
                    ImGui.InputText("##input", ref testString, 255);
                };
            }
        }
        
        ImGui.SameLine();
        if (ImGui.Button("Replace images using setter"))
        {
            foreach (var n in this.notifications)
            {
                var i = (uint)Random.Shared.NextInt64(0, 200000);
                n.IconTexture = DisposeLoggingTextureWrap.Wrap(Service<TextureManager>.Get().GetIcon(i));
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
        public static readonly string[] IconTitles =
        {
            "None (use Type)",
            "SeIconChar",
            "FontAwesomeIcon",
            "GamePath",
            "FilePath",
            "TextureWrap from DalamudAssets",
            "TextureWrap from DalamudAssets(Async)",
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
            NotificationConstants.DefaultDuration,
            TimeSpan.FromSeconds(10),
        };

        public bool ManualContent;
        public string Content;
        public bool ManualTitle;
        public string Title;
        public bool ManualMinimizedText;
        public string MinimizedText;
        public int IconInt;
        public string IconText;
        public int IconAssetInt;
        public bool ManualType;
        public int TypeInt;
        public int InitialDurationInt;
        public int HoverExtendDurationInt;
        public bool ShowIndeterminateIfNoExpiry;
        public bool RespectUiHidden;
        public bool Minimized;
        public bool UserDismissable;
        public bool ActionBar;
        public bool LeaveTexturesOpen;
        public int ProgressMode;

        public void Reset()
        {
            this.ManualContent = false;
            this.Content = string.Empty;
            this.ManualTitle = false;
            this.Title = string.Empty;
            this.ManualMinimizedText = false;
            this.MinimizedText = string.Empty;
            this.IconInt = 0;
            this.IconText = "ui/icon/000000/000004_hr1.tex";
            this.IconAssetInt = 0;
            this.ManualType = false;
            this.TypeInt = (int)NotificationType.None;
            this.InitialDurationInt = 2;
            this.HoverExtendDurationInt = 2;
            this.ShowIndeterminateIfNoExpiry = true;
            this.Minimized = true;
            this.UserDismissable = true;
            this.ActionBar = true;
            this.LeaveTexturesOpen = true;
            this.ProgressMode = 0;
            this.RespectUiHidden = true;
        }
    }

    private sealed class DisposeLoggingTextureWrap : IDalamudTextureWrap
    {
        private readonly IDalamudTextureWrap inner;

        public DisposeLoggingTextureWrap(IDalamudTextureWrap inner) => this.inner = inner;

        public nint ImGuiHandle => this.inner.ImGuiHandle;

        public int Width => this.inner.Width;

        public int Height => this.inner.Height;

        public static DisposeLoggingTextureWrap? Wrap(IDalamudTextureWrap? inner) => inner is null ? null : new(inner);

        public void Dispose()
        {
            this.inner.Dispose();
            Service<NotificationManager>.Get().AddNotification(
                "Texture disposed",
                "ImGui Widget",
                NotificationType.Info);
        }
    }
}
