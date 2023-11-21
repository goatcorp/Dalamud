using System.Linq;
using System.Numerics;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Internal;
using Dalamud.Storage.Assets;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// For major updates, an in-game Changelog window.
/// </summary>
internal sealed class ChangelogWindow : Window, IDisposable
{
    private const string WarrantsChangelogForMajorMinor = "9.0.";
    
    private const string ChangeLog =
        @"• Updated Dalamud for compatibility with Patch 6.5
• A lot of behind-the-scenes changes to make Dalamud and plugins more stable and reliable
• Added plugin collections, allowing you to create lists of plugins that can be enabled or disabled together
• Plugins can now add tooltips and interaction to the server info bar
• The Dalamud/plugin installer UI has been refreshed
";

    private readonly TitleScreenMenuWindow tsmWindow;

    private readonly DisposeSafety.ScopedFinalizer scopedFinalizer = new();
    private readonly IFontAtlas privateAtlas;
    private readonly Lazy<IFontHandle> bannerFont;
    private readonly Lazy<IDalamudTextureWrap> apiBumpExplainerTexture;
    private readonly Lazy<IDalamudTextureWrap> logoTexture;
    
    private readonly InOutCubic windowFade = new(TimeSpan.FromSeconds(2.5f))
    {
        Point1 = Vector2.Zero,
        Point2 = new Vector2(2f),
    };
    
    private readonly InOutCubic bodyFade = new(TimeSpan.FromSeconds(1f))
    {
        Point1 = Vector2.Zero,
        Point2 = Vector2.One,
    };
    
    private State state = State.WindowFadeIn;

    private bool needFadeRestart = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangelogWindow"/> class.
    /// </summary>
    /// <param name="tsmWindow">TSM window.</param>
    /// <param name="fontAtlasFactory">An instance of <see cref="FontAtlasFactory"/>.</param>
    /// <param name="assets">An instance of <see cref="DalamudAssetManager"/>.</param>
    public ChangelogWindow(
        TitleScreenMenuWindow tsmWindow,
        FontAtlasFactory fontAtlasFactory,
        DalamudAssetManager assets)
        : base("What's new in Dalamud?##ChangelogWindow", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse, true)
    {
        this.tsmWindow = tsmWindow;
        this.Namespace = "DalamudChangelogWindow";
        this.privateAtlas = this.scopedFinalizer.Add(
            fontAtlasFactory.CreateFontAtlas(this.Namespace, FontAtlasAutoRebuildMode.Async));
        this.bannerFont = new(
            () => this.scopedFinalizer.Add(
                this.privateAtlas.NewGameFontHandle(new(GameFontFamilyAndSize.MiedingerMid18))));

        this.apiBumpExplainerTexture = new(() => assets.GetDalamudTextureWrap(DalamudAsset.ChangelogApiBumpIcon));
        this.logoTexture = new(() => assets.GetDalamudTextureWrap(DalamudAsset.Logo));

        // If we are going to show a changelog, make sure we have the font ready, otherwise it will hitch
        if (WarrantsChangelog())
            _ = this.bannerFont;
    }

    private enum State
    {
        WindowFadeIn,
        ExplainerIntro,
        ExplainerApiBump,
        Links,
    }
    
    /// <summary>
    /// Check if a changelog should be shown.
    /// </summary>
    /// <returns>True if a changelog should be shown.</returns>
    public static bool WarrantsChangelog()
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var pm = Service<PluginManager>.GetNullable();
        var pmWantsChangelog = pm?.InstalledPlugins.Any() ?? true;
        return (string.IsNullOrEmpty(configuration.LastChangelogMajorMinor) ||
                (!WarrantsChangelogForMajorMinor.StartsWith(configuration.LastChangelogMajorMinor) &&
                 Util.AssemblyVersion.StartsWith(WarrantsChangelogForMajorMinor))) && pmWantsChangelog;
    }

    /// <inheritdoc/>
    public override void OnOpen()
    {
        Service<DalamudInterface>.Get().SetCreditsDarkeningAnimation(true);
        this.tsmWindow.AllowDrawing = false;

        _ = this.bannerFont;
        
        this.state = State.WindowFadeIn;
        this.windowFade.Reset();
        this.bodyFade.Reset();
        this.needFadeRestart = true;
        
        base.OnOpen();
    }

    /// <inheritdoc/>
    public override void OnClose()
    {
        base.OnClose();
        
        this.tsmWindow.AllowDrawing = true;
        Service<DalamudInterface>.Get().SetCreditsDarkeningAnimation(false);
    }

    /// <inheritdoc/>
    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
        
        base.PreDraw();

        if (this.needFadeRestart)
        {
            this.windowFade.Restart();
            this.needFadeRestart = false;
        }
        
        this.windowFade.Update();
        ImGui.SetNextWindowBgAlpha(Math.Clamp(this.windowFade.EasedPoint.X, 0, 0.9f));
        
        this.Size = new Vector2(900, 400);
        this.SizeCondition = ImGuiCond.Always;
        
        // Center the window on the main viewport
        var viewportSize = ImGuiHelpers.MainViewport.Size;
        var windowSize = this.Size!.Value * ImGuiHelpers.GlobalScale;
        ImGui.SetNextWindowPos(new Vector2(viewportSize.X / 2 - windowSize.X / 2, viewportSize.Y / 2 - windowSize.Y / 2));
    }

    /// <inheritdoc/>
    public override void PostDraw()
    {
        ImGui.PopStyleVar(3);
        base.PostDraw();
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        void Dismiss()
        {
            var configuration = Service<DalamudConfiguration>.Get();
            configuration.LastChangelogMajorMinor = WarrantsChangelogForMajorMinor;
            configuration.QueueSave();
        }
        
        var windowSize = ImGui.GetWindowSize();
        
        var dummySize = 10 * ImGuiHelpers.GlobalScale;
        ImGui.Dummy(new Vector2(dummySize));
        ImGui.SameLine();
        
        var logoContainerSize = new Vector2(windowSize.X * 0.2f - dummySize, windowSize.Y);
        using (var child = ImRaii.Child("###logoContainer", logoContainerSize, false))
        {
            if (!child)
                return;

            var logoSize = new Vector2(logoContainerSize.X);
            
            // Center the logo in the container
            ImGui.SetCursorPos(new Vector2(logoContainerSize.X / 2 - logoSize.X / 2, logoContainerSize.Y / 2 - logoSize.Y / 2));
            
            using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, Math.Clamp(this.windowFade.EasedPoint.X - 0.5f, 0f, 1f)))
                ImGui.Image(this.logoTexture.Value.ImGuiHandle, logoSize);
        }
        
        ImGui.SameLine();
        ImGui.Dummy(new Vector2(dummySize));
        ImGui.SameLine();
        
        using (var child = ImRaii.Child("###textContainer", new Vector2((windowSize.X * 0.8f) - dummySize * 4, windowSize.Y), false))
        {
            if (!child)
                return;
            
            ImGuiHelpers.ScaledDummy(20);
            
            using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, Math.Clamp(this.windowFade.EasedPoint.X - 1f, 0f, 1f)))
            {
                using var font = this.bannerFont.Value.Push();

                switch (this.state)
                {
                    case State.WindowFadeIn:
                    case State.ExplainerIntro:
                        ImGuiHelpers.CenteredText("New And Improved");
                        break;
                    
                    case State.ExplainerApiBump:
                        ImGuiHelpers.CenteredText("Plugin Updates");
                        break;
                    
                    case State.Links:
                        ImGuiHelpers.CenteredText("Enjoy!");
                        break;
                }
            }
            
            ImGuiHelpers.ScaledDummy(8);

            if (this.state == State.WindowFadeIn && this.windowFade.EasedPoint.X > 1.5f)
            {
                this.state = State.ExplainerIntro;
                this.bodyFade.Restart();
            }
            
            this.bodyFade.Update();
            using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, Math.Clamp(this.bodyFade.EasedPoint.X, 0, 1f)))
            {
                void DrawNextButton(State nextState)
                {
                    // Draw big, centered next button at the bottom of the window
                    var buttonHeight = 30 * ImGuiHelpers.GlobalScale;
                    var buttonText = "Next";
                    var buttonWidth = ImGui.CalcTextSize(buttonText).X + 40 * ImGuiHelpers.GlobalScale;
                    ImGui.SetCursorPosY(windowSize.Y - buttonHeight - (20 * ImGuiHelpers.GlobalScale));
                    ImGuiHelpers.CenterCursorFor((int)buttonWidth);
                
                    if (ImGui.Button(buttonText, new Vector2(buttonWidth, buttonHeight)))
                    {
                        this.state = nextState;
                        this.bodyFade.Restart();
                    }
                }
                
                switch (this.state)
                {
                    case State.WindowFadeIn:
                    case State.ExplainerIntro:
                        ImGui.TextWrapped($"Welcome to Dalamud v{Util.AssemblyVersion}!");
                        ImGuiHelpers.ScaledDummy(5);
                        ImGui.TextWrapped(ChangeLog);
                        ImGuiHelpers.ScaledDummy(5);
                        ImGui.TextWrapped("This changelog is a quick overview of the most important changes in this version.");
                        ImGui.TextWrapped("Please click next to see a quick guide to updating your plugins.");
                        
                        DrawNextButton(State.ExplainerApiBump);
                        break;
                    
                    case State.ExplainerApiBump:
                        ImGui.TextWrapped("Take care! Due to changes in this patch, all of your plugins need to be updated and were disabled automatically.");
                        ImGui.TextWrapped("This is normal and required for major game updates.");
                        ImGuiHelpers.ScaledDummy(5);
                        ImGui.TextWrapped("To update your plugins, open the plugin installer and click 'update plugins'. Updated plugins should update and then re-enable themselves.");
                        ImGuiHelpers.ScaledDummy(5);
                        ImGui.TextWrapped("Please keep in mind that not all of your plugins may already be updated for the new version.");
                        ImGui.TextWrapped("If some plugins are displayed with a red cross in the 'Installed Plugins' tab, they may not yet be available.");
                        
                        ImGuiHelpers.ScaledDummy(15);

                        ImGuiHelpers.CenterCursorFor(this.apiBumpExplainerTexture.Value.Width);
                        ImGui.Image(
                            this.apiBumpExplainerTexture.Value.ImGuiHandle,
                            this.apiBumpExplainerTexture.Value.Size);
                        
                        DrawNextButton(State.Links);
                        break;
                    
                    case State.Links:
                        ImGui.TextWrapped("If you note any issues or need help, please check the FAQ, and reach out on our Discord if you need help.");
                        ImGui.TextWrapped("Enjoy your time with the game and Dalamud!");
                        
                        ImGuiHelpers.ScaledDummy(45);
                        
                        bool CenteredIconButton(FontAwesomeIcon icon, string text)
                        {
                            var buttonWidth = ImGuiComponents.GetIconButtonWithTextWidth(icon, text);
                            ImGuiHelpers.CenterCursorFor((int)buttonWidth);
                            return ImGuiComponents.IconButtonWithText(icon, text);
                        }
                        
                        if (CenteredIconButton(FontAwesomeIcon.Download, "Open Plugin Installer"))
                        {
                            Service<DalamudInterface>.Get().OpenPluginInstaller();
                            this.IsOpen = false;
                            Dismiss();
                        }
                        
                        ImGuiHelpers.ScaledDummy(5);
                        
                        ImGuiHelpers.CenterCursorFor(
                            (int)(ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.Globe, "See the FAQ") +
                            ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.LaughBeam, "Join our Discord server") +
                            (5 * ImGuiHelpers.GlobalScale) + 
                            (ImGui.GetStyle().ItemSpacing.X * 4)));
                        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Globe, "See the FAQ"))
                        {
                            Util.OpenLink("https://goatcorp.github.io/faq/");
                        }
                        
                        ImGui.SameLine();
                        ImGuiHelpers.ScaledDummy(5);
                        ImGui.SameLine();
                        
                        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.LaughBeam, "Join our Discord server"))
                        {
                            Util.OpenLink("https://discord.gg/3NMcUV5");
                        }
                        
                        ImGuiHelpers.ScaledDummy(5);
                        
                        if (CenteredIconButton(FontAwesomeIcon.Heart, "Support what we care about"))
                        {
                            Util.OpenLink("https://goatcorp.github.io/faq/support");
                        }
                        
                        var buttonHeight = 30 * ImGuiHelpers.GlobalScale;
                        var buttonText = "Close";
                        var buttonWidth = ImGui.CalcTextSize(buttonText).X + 40 * ImGuiHelpers.GlobalScale;
                        ImGui.SetCursorPosY(windowSize.Y - buttonHeight - (20 * ImGuiHelpers.GlobalScale));
                        ImGuiHelpers.CenterCursorFor((int)buttonWidth);
                
                        if (ImGui.Button(buttonText, new Vector2(buttonWidth, buttonHeight)))
                        {
                            this.IsOpen = false;
                            Dismiss();
                        }
                        
                        break;
                }
            }
            
            // Draw close button in the top right corner
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 100f);
            var btnAlpha = Math.Clamp(this.windowFade.EasedPoint.X - 0.5f, 0f, 1f);
            ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.DalamudRed.WithAlpha(btnAlpha).Desaturate(0.3f));
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudWhite.WithAlpha(btnAlpha));
            
            var childSize = ImGui.GetWindowSize();
            var closeButtonSize = 15 * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPos(new Vector2(childSize.X - closeButtonSize - 5, 10 * ImGuiHelpers.GlobalScale));
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
            {
                Dismiss();
                this.IsOpen = false;
            }

            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("I don't care about this");
            }
        }
    }

    /// <summary>
    /// Dispose this window.
    /// </summary>
    public void Dispose()
    {
    }
}
