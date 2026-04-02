using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Widgets;

/// <summary>
/// Class responsible for drawing image tester.
/// </summary>
internal class ImageTester
{
    private readonly PluginInstallerWindow pluginInstaller;
    private readonly string[] testerImagePaths = new string[5];

    private Task<IDalamudTextureWrap>?[]? testerImages;
    private Task<IDalamudTextureWrap>? testerIcon;

    private bool testerError;
    private bool testerUpdateAvailable;

    private string testerIconPath = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageTester"/> class.
    /// </summary>
    /// <param name="pluginInstaller">The plugin installer.</param>
    public ImageTester(PluginInstallerWindow pluginInstaller)
    {
        this.pluginInstaller = pluginInstaller;
    }

    /// <summary>
    /// Resets the stored paths for the image tester.
    /// </summary>
    public void Reset()
    {
        for (var i = 0; i < this.testerImagePaths.Length; i++)
        {
            this.testerImagePaths[i] = string.Empty;
        }
    }

    /// <summary>
    /// Draw image tester widget.
    /// </summary>
    public void Draw()
    {
        var sectionSize = ImGuiHelpers.GlobalScale * 66;
        var startCursor = ImGui.GetCursorPos();

        using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0))
        using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.5f, 0.5f, 0.5f, 0.1f)))
        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.5f, 0.5f, 0.2f)))
        using (ImRaii.PushColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 0.5f, 0.5f, 0.35f)))
        {
            ImGui.Button($"###pluginTesterCollapsibleBtn", new Vector2(ImGui.GetWindowWidth() - (ImGuiHelpers.GlobalScale * 35), sectionSize));
        }

        ImGui.SetCursorPos(startCursor);

        var hasIcon = this.testerIcon?.IsCompletedSuccessfully is true;

        var iconTex = this.pluginInstaller.imageCache.DefaultIcon;
        if (hasIcon) iconTex = this.testerIcon.Result;

        var iconSize = ImGuiHelpers.ScaledVector2(64, 64);

        var cursorBeforeImage = ImGui.GetCursorPos();
        ImGui.Image(iconTex.Handle, iconSize);
        ImGui.SameLine();

        if (this.testerError)
        {
            ImGui.SetCursorPos(cursorBeforeImage);
            ImGui.Image(this.pluginInstaller.imageCache.TroubleIcon.Handle, iconSize);
            ImGui.SameLine();
        }
        else if (this.testerUpdateAvailable)
        {
            ImGui.SetCursorPos(cursorBeforeImage);
            ImGui.Image(this.pluginInstaller.imageCache.UpdateIcon.Handle, iconSize);
            ImGui.SameLine();
        }

        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        var cursor = ImGui.GetCursorPos();
        // Name
        ImGui.Text("My Cool Plugin"u8);

        // Download count
        var downloadCountText = PluginInstallerLocs.PluginBody_AuthorWithDownloadCount("Plugin Enjoyer", 69420);

        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudGrey3, downloadCountText);

        cursor.Y += ImGui.GetTextLineHeightWithSpacing();
        ImGui.SetCursorPos(cursor);

        // Description
        ImGui.TextWrapped("This plugin does very many great things."u8);

        startCursor.Y += sectionSize;
        ImGui.SetCursorPos(startCursor);

        ImGuiHelpers.ScaledDummy(5);

        using (ImRaii.PushIndent())
        {
            // Description
            ImGui.TextWrapped("This is a description.\nIt has multiple lines.\nTruly descriptive."u8);

            ImGuiHelpers.ScaledDummy(5);

            // Controls
            var disabled = this.pluginInstaller.AnyOperationInProgress;
            const string versionString = "1.0.0.0";

            if (disabled)
            {
                ImGuiComponents.DisabledButton(PluginInstallerLocs.PluginButton_InstallVersion(versionString));
            }
            else
            {
                var buttonText = PluginInstallerLocs.PluginButton_InstallVersion(versionString);
                ImGui.Button($"{buttonText}##{buttonText}testing");
            }

            VisitRepoUrlButton.Draw("https://google.com", true);

            this.DrawTestImages();
        }

        ImGuiHelpers.ScaledDummy(20);

        ImGui.InputText("Icon Path"u8, ref this.testerIconPath, 1000);
        if (this.testerIcon != null)
        {
            CheckImageSize(this.testerIcon, PluginImageCache.PluginIconWidth, PluginImageCache.PluginIconHeight, true);
        }

        for (var i = 0; i < this.testerImagePaths.Length; ++i)
        {
            ImGui.InputText($"Image {i} Path", ref this.testerImagePaths[i], 1000);
            if (this.testerImages?.Length > i)
            {
                CheckImageSize(this.testerImages[i], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
            }
        }

        // todo: check and remove this -midorikami
        // ImGui.InputText("Image 1 Path"u8, ref this.testerImagePaths[0], 1000);
        // if (this.testerImages?.Length > 0)
        //     CheckImageSize(this.testerImages[0], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        // ImGui.InputText("Image 2 Path"u8, ref this.testerImagePaths[1], 1000);
        // if (this.testerImages?.Length > 1)
        //     CheckImageSize(this.testerImages[1], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        // ImGui.InputText("Image 3 Path"u8, ref this.testerImagePaths[2], 1000);
        // if (this.testerImages?.Length > 2)
        //     CheckImageSize(this.testerImages[2], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        // ImGui.InputText("Image 4 Path"u8, ref this.testerImagePaths[3], 1000);
        // if (this.testerImages?.Length > 3)
        //     CheckImageSize(this.testerImages[3], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        // ImGui.InputText("Image 5 Path"u8, ref this.testerImagePaths[4], 1000);
        // if (this.testerImages?.Length > 4)
        //     CheckImageSize(this.testerImages[4], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);

        var tm = Service<TextureManager>.Get();
        if (ImGui.Button("Load"u8))
        {
            try
            {
                if (this.testerIcon != null)
                {
                    this.testerIcon.Dispose();
                    this.testerIcon = null;
                }

                if (!this.testerIconPath.IsNullOrEmpty())
                {
                    this.testerIcon = tm.Shared.GetFromFile(this.testerIconPath).RentAsync();
                }

                this.testerImages = new Task<IDalamudTextureWrap>?[this.testerImagePaths.Length];

                for (var i = 0; i < this.testerImagePaths.Length; i++)
                {
                    if (this.testerImagePaths[i].IsNullOrEmpty())
                        continue;

                    _ = this.testerImages[i]?.ToContentDisposedTask();
                    this.testerImages[i] = tm.Shared.GetFromFile(this.testerImagePaths[i]).RentAsync();
                }
            }
            catch (Exception ex)
            {
                PluginInstallerWindow.Log.Error(ex, "Could not load plugin images for testing.");
            }
        }

        ImGui.Checkbox("Failed"u8, ref this.testerError);
        ImGui.Checkbox("Has Update"u8, ref this.testerUpdateAvailable);
    }

    private static void DrawTestingImagePreviewButton(IDalamudTextureWrap image, float thumbFactor, string popupId)
    {
        using var pushStyle = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);

        float xAct = image.Width;
        float yAct = image.Height;
        const float xMax = PluginImageCache.PluginImageWidth;
        const float yMax = PluginImageCache.PluginImageHeight;

        // scale image if undersized
        if (xAct < xMax && yAct < yMax)
        {
            var scale = Math.Min(xMax / xAct, yMax / yAct);
            xAct *= scale;
            yAct *= scale;
        }

        var size = ImGuiHelpers.ScaledVector2(xAct / thumbFactor, yAct / thumbFactor);
        if (ImGui.ImageButton(image.Handle, size))
        {
            ImGui.OpenPopup(popupId);
        }
    }

    private static void DrawTestingImagePopup(string popupId, IDalamudTextureWrap image)
    {
        using var borderSize = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 0);
        using var windowPadding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);

        using var popup = ImRaii.Popup(popupId);
        if (!popup)
        {
            return;
        }

        if (ImGui.ImageButton(image.Handle, new Vector2(image.Width, image.Height)))
        {
            ImGui.CloseCurrentPopup();
        }
    }

    private static void CheckImageSize(Task<IDalamudTextureWrap>? imageTask, int maxWidth, int maxHeight, bool requireSquare)
    {
        if (imageTask == null)
            return;

        if (!imageTask.IsCompleted)
        {
            ImGui.Text("Loading..."u8);
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);

        if (imageTask.Exception is { } exc)
        {
            ImGui.Text(exc.ToString());
        }
        else
        {
            var image = imageTask.Result;
            if (image.Width > maxWidth || image.Height > maxHeight)
            {
                ImGui.Text(
                    $"Image is larger than the maximum allowed resolution ({image.Width}x{image.Height} > {maxWidth}x{maxHeight})");
            }

            if (requireSquare && image.Width != image.Height)
                ImGui.Text($"Image must be square! Current size: {image.Width}x{image.Height}");
        }

        ImGui.PopStyleColor();
    }

    private void DrawTestImages()
    {
        if (this.testerImages == null)
        {
            return;
        }

        ImGuiHelpers.ScaledDummy(5);

        const float thumbFactor = 2.7f;
        const float scrollbarSize = 15;

        using var styleSize = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, scrollbarSize);
        using var styleBg = ImRaii.PushColor(ImGuiCol.ScrollbarBg, Vector4.Zero);

        var width = ImGui.GetWindowWidth();

        using var child = ImRaii.Child(
            "pluginTestingImageScrolling"u8,
            new Vector2(width - (70 * ImGuiHelpers.GlobalScale), (PluginImageCache.PluginImageHeight / thumbFactor) + scrollbarSize),
            false,
            ImGuiWindowFlags.HorizontalScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoBackground);

        if (!child)
        {
            return;
        }

        if (this.testerImages is not { Length: > 0 })
        {
            return;
        }

        for (var i = 0; i < this.testerImages.Length; i++)
        {
            var popupId = $"pluginTestingImage{i}";
            var imageTask = this.testerImages[i];

            if (imageTask is null)
            {
                continue;
            }

            if (!imageTask.IsCompleted)
            {
                ImGui.Text("Loading..."u8);
                continue;
            }

            if (imageTask.Exception is not null)
            {
                ImGui.Text(imageTask.Exception.ToString());
                continue;
            }

            var image = imageTask.Result;

            DrawTestingImagePopup(popupId, image);
            DrawTestingImagePreviewButton(image, thumbFactor, popupId);

            if (i < this.testerImages.Length - 1)
            {
                ImGui.SameLine();
                ImGuiHelpers.ScaledDummy(5);
                ImGui.SameLine();
            }
        }
    }
}
