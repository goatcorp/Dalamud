using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.Fools26;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Lazy")]
internal class Fools26ResultWindow : Window, IDisposable
{
    private readonly DisposeSafety.ScopedFinalizer scopedFinalizer = new();
    private readonly IFontAtlas privateAtlas;
    private readonly Lazy<IFontHandle> bannerFont;
    private readonly Lazy<IFontHandle> ratingFont;

    private readonly InOutCubic titleFade = new(TimeSpan.FromSeconds(0.5f))
    {
        Point1 = Vector2.Zero,
        Point2 = Vector2.One,
    };

    private bool needFadeRestart = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fools26ResultWindow"/> class.
    /// </summary>
    public Fools26ResultWindow()
        : base(
            "Verification Succeeded",
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse,
            true)
    {
        this.Namespace = "DalamudChangelogWindow";
        this.privateAtlas = this.scopedFinalizer.Add(
            Service<FontAtlasFactory>.Get().CreateFontAtlas("fools result", FontAtlasAutoRebuildMode.Async));
        this.bannerFont = new(() => this.scopedFinalizer.Add(
                                  this.privateAtlas.NewGameFontHandle(new(GameFontFamilyAndSize.MiedingerMid18))));
        this.ratingFont = new(() => this.scopedFinalizer.Add(
                                  this.privateAtlas.NewGameFontHandle(new(GameFontFamilyAndSize.Jupiter23))));
    }

    public override void OnOpen()
    {
        _ = this.bannerFont;

        this.titleFade.Reset();
        this.needFadeRestart = true;

        base.OnOpen();
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
        ImGui.SetNextWindowBgAlpha(1f);

        base.PreDraw();

        if (this.needFadeRestart)
        {
            this.titleFade.Restart();
            this.needFadeRestart = false;
        }

        this.titleFade.Update();

        this.Size = new Vector2(900, 400);
        this.SizeCondition = ImGuiCond.Always;

        // Center the window on the main viewport
        var viewportPos = ImGuiHelpers.MainViewport.Pos;
        var viewportSize = ImGuiHelpers.MainViewport.Size;
        var windowSize = this.Size!.Value * ImGuiHelpers.GlobalScale;
        ImGui.SetNextWindowPos(
            new Vector2(
                viewportPos.X + viewportSize.X / 2 - windowSize.X / 2,
                viewportPos.Y + viewportSize.Y / 2 - windowSize.Y / 2));
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(3);

        base.PostDraw();
    }

    public override void Draw()
    {
        var windowSize = ImGui.GetWindowSize();

        var verifyWindow = Service<DalamudInterface>.Get().Fools26VerifyWindow;

        var dummySize = 0 * ImGuiHelpers.GlobalScale;
        ImGui.Dummy(new Vector2(dummySize));
        ImGui.SameLine();

        var logoContainerSize = new Vector2(windowSize.X * 0.25f - dummySize, windowSize.Y);
        using (var child = ImRaii.Child("###logoContainer"u8, logoContainerSize, false))
        {
            if (!child)
                return;

            var screenshot = verifyWindow.Screenshot;
            if (screenshot != null)
            {
                var dl = ImGui.GetWindowDrawList();

                var padding = 10f * ImGuiHelpers.GlobalScale;
                var destSize = Math.Min(logoContainerSize.X, logoContainerSize.Y) - (padding * 2);

                var childPos = ImGui.GetCursorScreenPos();
                var destMin = childPos + new Vector2(
                                  (logoContainerSize.X - destSize) / 2f,
                                  (logoContainerSize.Y - destSize) / 2f);
                var destMax = destMin + new Vector2(destSize, destSize);

                var srcSize = screenshot.Size;

                var srcCenter = new Vector2(srcSize.X / 2f, srcSize.Y / 1.7f);
                var cropSize = Math.Min(srcSize.X, srcSize.Y) * 0.75f; // show 75% of smaller dimension to trim more
                var srcMinPx = srcCenter - new Vector2(cropSize / 2f, cropSize / 2f);
                var srcMaxPx = srcCenter + new Vector2(cropSize / 2f, cropSize / 2f);

                var uv0 = srcMinPx / srcSize;
                var uv1 = srcMaxPx / srcSize;

                const uint tintCol = 0xFFFFFFFF;
                var rounding = destSize / 2f; // full circle
                dl.AddImageRounded(screenshot.Handle, destMin, destMax, uv0, uv1, tintCol, rounding);

                var assetManager = Service<DalamudAssetManager>.Get();
                var installedIcon = assetManager.GetDalamudTextureWrap(DalamudAsset.InstalledIcon, assetManager.Empty4X4);
                dl.AddImage(installedIcon.Handle, destMin, destMax, Vector2.Zero, Vector2.One, tintCol);
            }
        }

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(dummySize));
        ImGui.SameLine();

        using (var child = ImRaii.Child(
                   "###textContainer"u8,
                   new Vector2((windowSize.X * 0.7f) - dummySize * 4, windowSize.Y),
                   false))
        {
            if (!child)
                return;

            ImGuiHelpers.ScaledDummy(20);

            var titleFadeVal = this.titleFade.EasedPoint.X;
            using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, Math.Clamp(titleFadeVal, 0f, 1f)))
            {
                using var font = this.bannerFont.Value.Push();

                ImGuiHelpers.CenteredText("Verification succeeded!");
            }

            ImGuiHelpers.ScaledDummy(8);

            void DrawCloseButton()
            {
                // Draw big, centered next button at the bottom of the window
                var buttonHeight = 30 * ImGuiHelpers.GlobalScale;
                var buttonText = "Close";
                var buttonWidth = ImGui.CalcTextSize(buttonText).X + 40 * ImGuiHelpers.GlobalScale;
                ImGui.SetCursorPosY(windowSize.Y - buttonHeight - (20 * ImGuiHelpers.GlobalScale));
                ImGuiHelpers.CenterCursorFor((int)buttonWidth);

                if (ImGui.Button(buttonText, new Vector2(buttonWidth, buttonHeight)))
                {
                    this.IsOpen = false;
                }
            }

            ImGui.TextWrapped("Thank you for verifying your character to help us comply!");
            ImGuiHelpers.ScaledDummy(10);
            ImGui.TextWrapped("But this is just an April Fools' joke, you didn't actually have to do anything.");
            ImGui.TextWrapped("What your character looks like is none of our business, and never will be!");
            ImGuiHelpers.ScaledDummy(25);
            ImGuiHelpers.CenteredText("Your character has been verified to be:");
            ImGuiHelpers.ScaledDummy(5);
            using (this.ratingFont.Value.Push())
            {
                ImGuiHelpers.CenteredText(verifyWindow.Rating ?? "Undefined");
            }

            DrawCloseButton();
        }
    }

    public void Dispose()
    {
        this.scopedFinalizer.Dispose();
    }
}
