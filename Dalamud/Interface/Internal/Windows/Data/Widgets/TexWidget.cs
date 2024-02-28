using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Internal.SharedImmediateTextures;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying texture test.
/// </summary>
internal class TexWidget : IDataWindowWidget
{
    private readonly List<TextureEntry> addedTextures = new();

    private string iconId = "18";
    private bool hiRes = true;
    private bool hq = false;
    private string inputTexPath = string.Empty;
    private string inputFilePath = string.Empty;
    private Vector2 inputTexUv0 = Vector2.Zero;
    private Vector2 inputTexUv1 = Vector2.One;
    private Vector4 inputTintCol = Vector4.One;
    private Vector2 inputTexScale = Vector2.Zero;
    private TextureManager textureManager = null!;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "tex", "texture" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Tex";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    private ITextureProvider TextureManagerForApi9 => this.textureManager!;

    /// <inheritdoc/>
    public void Load()
    {
        this.addedTextures.AggregateToDisposable().Dispose();
        this.addedTextures.Clear();
        this.inputTexPath = "ui/loadingimage/-nowloading_base25_hr1.tex";
        this.inputFilePath = Path.Join(
            Service<Dalamud>.Get().StartInfo.AssetDirectory!,
            DalamudAsset.Logo.GetAttribute<DalamudAssetPathAttribute>()!.FileName);
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        this.textureManager = Service<TextureManager>.Get();

        if (ImGui.Button("GC"))
            GC.Collect();

        ImGui.PushID("loadedGameTextures");
        if (ImGui.CollapsingHeader($"Loaded Game Textures: {this.textureManager.GamePathTexturesForDebug.Count:g}###header"))
            this.DrawLoadedTextures(this.textureManager.GamePathTexturesForDebug);
        ImGui.PopID();

        ImGui.PushID("loadedFileTextures");
        if (ImGui.CollapsingHeader($"Loaded File Textures: {this.textureManager.FileSystemTexturesForDebug.Count:g}###header"))
            this.DrawLoadedTextures(this.textureManager.FileSystemTexturesForDebug);
        ImGui.PopID();

        lock (this.textureManager.InvalidatedTexturesForDebug)
        {
            ImGui.PushID("invalidatedTextures");
            if (ImGui.CollapsingHeader($"Invalidated: {this.textureManager.InvalidatedTexturesForDebug.Count:g}###header"))
            {
                this.DrawLoadedTextures(this.textureManager.InvalidatedTexturesForDebug);
            }

            ImGui.PopID();
        }

        if (ImGui.CollapsingHeader("Load Game File by Icon ID", ImGuiTreeNodeFlags.DefaultOpen))
            this.DrawIconInput();

        if (ImGui.CollapsingHeader("Load Game File by Path", ImGuiTreeNodeFlags.DefaultOpen))
            this.DrawGamePathInput();

        if (ImGui.CollapsingHeader("Load File", ImGuiTreeNodeFlags.DefaultOpen))
            this.DrawFileInput();

        if (ImGui.CollapsingHeader("UV"))
            this.DrawUvInput();

        TextureEntry? toRemove = null;
        TextureEntry? toCopy = null;
        foreach (var t in this.addedTextures)
        {
            ImGui.PushID(t.Id);
            if (ImGui.CollapsingHeader($"Tex #{t.Id} {t}###header", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("X"))
                    toRemove = t;

                ImGui.SameLine();
                if (ImGui.Button("Copy"))
                    toCopy = t;

                try
                {
                    if (t.GetTexture(this.textureManager) is { } tex)
                    {
                        var scale = new Vector2(tex.Width, tex.Height);
                        if (this.inputTexScale != Vector2.Zero)
                            scale = this.inputTexScale;

                        ImGui.Image(tex.ImGuiHandle, scale, this.inputTexUv0, this.inputTexUv1, this.inputTintCol);
                    }
                    else
                    {
                        ImGui.TextUnformatted(t.DescribeError());
                    }
                }
                catch (Exception e)
                {
                    ImGui.TextUnformatted(e.ToString());
                }
            }

            ImGui.PopID();
        }

        if (toRemove != null)
        {
            toRemove.Dispose();
            this.addedTextures.Remove(toRemove);
        }

        if (toCopy != null)
        {
            this.addedTextures.Add(toCopy.CreateFromSharedLowLevelResource(this.textureManager));
        }
    }

    private unsafe void DrawLoadedTextures(ICollection<SharedImmediateTexture> textures)
    {
        var im = Service<InterfaceManager>.Get();
        if (!ImGui.BeginTable("##table", 6))
            return;

        const int numIcons = 3;
        float iconWidths;
        using (im.IconFontHandle?.Push())
        {
            iconWidths = ImGui.CalcTextSize(FontAwesomeIcon.Image.ToIconString()).X;
            iconWidths += ImGui.CalcTextSize(FontAwesomeIcon.Sync.ToIconString()).X;
            iconWidths += ImGui.CalcTextSize(FontAwesomeIcon.Trash.ToIconString()).X;
        }

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("000000").X);
        ImGui.TableSetupColumn("Source", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("RefCount", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("RefCount__").X);
        ImGui.TableSetupColumn("SelfRef", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("00.000___").X);
        ImGui.TableSetupColumn("CanRevive", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("CanRevive__").X);
        ImGui.TableSetupColumn(
            "Actions",
            ImGuiTableColumnFlags.WidthFixed,
            iconWidths +
            (ImGui.GetStyle().FramePadding.X * 2 * numIcons) +
            (ImGui.GetStyle().ItemSpacing.X * 1 * numIcons));
        ImGui.TableHeadersRow();

        var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        clipper.Begin(textures.Count);

        using (var enu = textures.GetEnumerator())
        {
            var row = 0;
            while (clipper.Step())
            {
                var valid = true;
                for (; row < clipper.DisplayStart && valid; row++)
                    valid = enu.MoveNext();

                if (!valid)
                    break;

                for (; row < clipper.DisplayEnd; row++)
                {
                    valid = enu.MoveNext();
                    if (!valid)
                        break;

                    ImGui.TableNextRow();

                    if (enu.Current is not { } texture)
                    {
                        // Should not happen
                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted("?");
                        continue;
                    }

                    var remain = texture.SelfReferenceExpiresInForDebug;

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    this.TextRightAlign($"{texture.InstanceIdForDebug:n0}");

                    ImGui.TableNextColumn();
                    this.TextCopiable(texture.SourcePathForDebug, true);

                    ImGui.TableNextColumn();
                    this.TextRightAlign($"{texture.RefCountForDebug:n0}");

                    ImGui.TableNextColumn();
                    this.TextRightAlign(remain <= 0 ? "-" : $"{remain:00.000}");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(texture.HasRevivalPossibility ? "Yes" : "No");

                    ImGui.TableNextColumn();
                    ImGuiComponents.IconButton(FontAwesomeIcon.Image);
                    if (ImGui.IsItemHovered() && texture.GetWrap(null) is { } immediate)
                    {
                        ImGui.BeginTooltip();
                        ImGui.Image(immediate.ImGuiHandle, immediate.Size);
                        ImGui.EndTooltip();
                    }

                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Sync))
                        this.textureManager.InvalidatePaths(new[] { texture.SourcePathForDebug });
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip($"Call {nameof(ITextureSubstitutionProvider.InvalidatePaths)}.");

                    ImGui.SameLine();
                    if (remain <= 0)
                        ImGui.BeginDisabled();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                        texture.ReleaseSelfReference(true);
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        ImGui.SetTooltip("Release self-reference immediately.");
                    if (remain <= 0)
                        ImGui.EndDisabled();
                }

                if (!valid)
                    break;
            }
        }

        clipper.Destroy();
        ImGui.EndTable();

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawIconInput()
    {
        ImGui.InputText("Icon ID", ref this.iconId, 32);
        ImGui.Checkbox("HQ Item", ref this.hq);
        ImGui.Checkbox("Hi-Res", ref this.hiRes);
#pragma warning disable CS0618 // Type or member is obsolete
        if (ImGui.Button("Load Icon (API9)"))
        {
            var flags = ITextureProvider.IconFlags.None;
            if (this.hq)
                flags |= ITextureProvider.IconFlags.ItemHighQuality;
            if (this.hiRes)
                flags |= ITextureProvider.IconFlags.HiRes;
            this.addedTextures.Add(new(Api9: this.TextureManagerForApi9.GetIcon(uint.Parse(this.iconId), flags)));
        }
#pragma warning restore CS0618 // Type or member is obsolete

        ImGui.SameLine();
        if (ImGui.Button("Load Icon (Async)"))
        {
            this.addedTextures.Add(
                new(
                    Api10: this.textureManager
                               .GetFromGameIcon(new(uint.Parse(this.iconId), this.hq, this.hiRes))
                               .RentAsync()));
        }

        ImGui.SameLine();
        if (ImGui.Button("Load Icon (Immediate)"))
            this.addedTextures.Add(new(Api10ImmGameIcon: new(uint.Parse(this.iconId), this.hq, this.hiRes)));

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawGamePathInput()
    {
        ImGui.InputText("Tex Path", ref this.inputTexPath, 255);

#pragma warning disable CS0618 // Type or member is obsolete
        if (ImGui.Button("Load Tex (API9)"))
            this.addedTextures.Add(new(Api9: this.TextureManagerForApi9.GetTextureFromGame(this.inputTexPath)));
#pragma warning restore CS0618 // Type or member is obsolete

        ImGui.SameLine();
        if (ImGui.Button("Load Tex (Async)"))
            this.addedTextures.Add(new(Api10: this.textureManager.GetFromGame(this.inputTexPath).RentAsync()));

        ImGui.SameLine();
        if (ImGui.Button("Load Tex (Immediate)"))
            this.addedTextures.Add(new(Api10ImmGamePath: this.inputTexPath));

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawFileInput()
    {
        ImGui.InputText("File Path", ref this.inputFilePath, 255);

#pragma warning disable CS0618 // Type or member is obsolete
        if (ImGui.Button("Load File (API9)"))
            this.addedTextures.Add(new(Api9: this.TextureManagerForApi9.GetTextureFromFile(new(this.inputFilePath))));
#pragma warning restore CS0618 // Type or member is obsolete

        ImGui.SameLine();
        if (ImGui.Button("Load File (Async)"))
            this.addedTextures.Add(new(Api10: this.textureManager.GetFromFile(this.inputFilePath).RentAsync()));

        ImGui.SameLine();
        if (ImGui.Button("Load File (Immediate)"))
            this.addedTextures.Add(new(Api10ImmFile: this.inputFilePath));

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawUvInput()
    {
        ImGui.InputFloat2("UV0", ref this.inputTexUv0);
        ImGui.InputFloat2("UV1", ref this.inputTexUv1);
        ImGui.InputFloat4("Tint", ref this.inputTintCol);
        ImGui.InputFloat2("Scale", ref this.inputTexScale);

        ImGuiHelpers.ScaledDummy(10);
    }

    private void TextRightAlign(string s)
    {
        var width = ImGui.CalcTextSize(s).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - width);
        ImGui.TextUnformatted(s);
    }

    private void TextCopiable(string s, bool framepad = false)
    {
        var offset = ImGui.GetCursorScreenPos() + new Vector2(0, framepad ? ImGui.GetStyle().FramePadding.Y : 0);
        if (framepad)
            ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(s);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetNextWindowPos(offset - ImGui.GetStyle().WindowPadding);
            var vp = ImGui.GetWindowViewport();
            var wrx = (vp.WorkPos.X + vp.WorkSize.X) - offset.X;
            ImGui.SetNextWindowSizeConstraints(Vector2.One, new(wrx, float.MaxValue));
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(wrx);
            ImGui.TextWrapped(s.Replace("%", "%%"));
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(s);
            Service<NotificationManager>.Get().AddNotification(
                $"Copied {ImGui.TableGetColumnName()} to clipboard.",
                this.DisplayName,
                NotificationType.Success);
        }
    }

    private record TextureEntry(
        IDalamudTextureWrap? SharedResource = null,
        IDalamudTextureWrap? Api9 = null,
        Task<IDalamudTextureWrap>? Api10 = null,
        GameIconLookup? Api10ImmGameIcon = null,
        string? Api10ImmGamePath = null,
        string? Api10ImmFile = null) : IDisposable
    {
        private static int idCounter;

        public int Id { get; } = idCounter++;

        public void Dispose()
        {
            this.SharedResource?.Dispose();
            this.Api9?.Dispose();
            _ = this.Api10?.ToContentDisposedTask();
        }

        public string DescribeError()
        {
            if (this.SharedResource is not null)
                return "Unknown error";
            if (this.Api9 is not null)
                return "Unknown error";
            if (this.Api10 is not null)
            {
                return !this.Api10.IsCompleted
                           ? "Loading"
                           : this.Api10.Exception?.ToString() ?? (this.Api10.IsCanceled ? "Canceled" : "Unknown error");
            }

            if (this.Api10ImmGameIcon is not null)
                return "Must not happen";
            if (this.Api10ImmGamePath is not null)
                return "Must not happen";
            if (this.Api10ImmFile is not null)
                return "Must not happen";
            return "Not implemented";
        }

        public IDalamudTextureWrap? GetTexture(ITextureProvider tp)
        {
            if (this.SharedResource is not null)
                return this.SharedResource;
            if (this.Api9 is not null)
                return this.Api9;
            if (this.Api10 is not null)
                return this.Api10.IsCompletedSuccessfully ? this.Api10.Result : null;
            if (this.Api10ImmGameIcon is not null)
                return tp.GetFromGameIcon(this.Api10ImmGameIcon.Value).GetWrap();
            if (this.Api10ImmGamePath is not null)
                return tp.GetFromGame(this.Api10ImmGamePath).GetWrap();
            if (this.Api10ImmFile is not null)
                return tp.GetFromFile(this.Api10ImmFile).GetWrap();
            return null;
        }

        public TextureEntry CreateFromSharedLowLevelResource(ITextureProvider tp) =>
            new() { SharedResource = this.GetTexture(tp)?.CreateWrapSharingLowLevelResource() };

        public override string ToString()
        {
            if (this.SharedResource is not null)
                return $"{nameof(this.SharedResource)}: {this.SharedResource}";
            if (this.Api9 is not null)
                return $"{nameof(this.Api9)}: {this.Api9}";
            if (this.Api10 is { IsCompletedSuccessfully: true })
                return $"{nameof(this.Api10)}: {this.Api10.Result}";
            if (this.Api10 is not null)
                return $"{nameof(this.Api10)}: {this.Api10}";
            if (this.Api10ImmGameIcon is not null)
                return $"{nameof(this.Api10ImmGameIcon)}: {this.Api10ImmGameIcon}";
            if (this.Api10ImmGamePath is not null)
                return $"{nameof(this.Api10ImmGamePath)}: {this.Api10ImmGamePath}";
            if (this.Api10ImmFile is not null)
                return $"{nameof(this.Api10ImmFile)}: {this.Api10ImmFile}";
            return "Not implemented";
        }
    }
}
