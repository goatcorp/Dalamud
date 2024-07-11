using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.Internal.SharedImmediateTextures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using ImGuiNET;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using TextureManager = Dalamud.Interface.Textures.Internal.TextureManager;

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
    private Assembly[]? inputManifestResourceAssemblyCandidates;
    private string[]? inputManifestResourceAssemblyCandidateNames;
    private int inputManifestResourceAssemblyIndex;
    private string[]? inputManifestResourceNameCandidates;
    private int inputManifestResourceNameIndex;
    private Vector2 inputTexUv0 = Vector2.Zero;
    private Vector2 inputTexUv1 = Vector2.One;
    private Vector4 inputTintCol = Vector4.One;
    private Vector2 inputTexScale = Vector2.Zero;
    private TextureManager textureManager = null!;
    private TextureModificationArgs textureModificationArgs;

    private ImGuiViewportTextureArgs viewportTextureArgs;
    private int viewportIndexInt;
    private string[]? supportedRenderTargetFormatNames;
    private DXGI_FORMAT[]? supportedRenderTargetFormats;
    private int renderTargetChoiceInt;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "tex", "texture" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Tex";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.addedTextures.AggregateToDisposable().Dispose();
        this.addedTextures.Clear();
        this.inputTexPath = "ui/loadingimage/-nowloading_base25_hr1.tex";
        this.inputFilePath = Path.Join(
            Service<Dalamud>.Get().StartInfo.AssetDirectory!,
            DalamudAsset.Logo.GetAttribute<DalamudAssetPathAttribute>()!.FileName);
        this.inputManifestResourceAssemblyCandidates = null;
        this.inputManifestResourceAssemblyCandidateNames = null;
        this.inputManifestResourceAssemblyIndex = 0;
        this.inputManifestResourceNameCandidates = null;
        this.inputManifestResourceNameIndex = 0;
        this.supportedRenderTargetFormats = null;
        this.supportedRenderTargetFormatNames = null;
        this.renderTargetChoiceInt = 0;
        this.textureModificationArgs = new()
        {
            Uv0 = new(0.25f),
            Uv1 = new(0.75f),
            NewWidth = 320,
            NewHeight = 240,
        };
        this.viewportTextureArgs = default;
        this.viewportIndexInt = 0;
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        this.textureManager = Service<TextureManager>.Get();
        var conf = Service<DalamudConfiguration>.Get();

        if (ImGui.Button("GC"))
            GC.Collect();

        var useTexturePluginTracking = conf.UseTexturePluginTracking;
        if (ImGui.Checkbox("Enable Texture Tracking", ref useTexturePluginTracking))
        {
            conf.UseTexturePluginTracking = useTexturePluginTracking;
            conf.QueueSave();
        }

        ImGui.PushID("loadedGameTextures");
        if (ImGui.CollapsingHeader(
                $"Loaded Game Textures: {this.textureManager.Shared.ForDebugGamePathTextures.Count:n0}###header"))
            this.DrawLoadedTextures(this.textureManager.Shared.ForDebugGamePathTextures);
        ImGui.PopID();

        ImGui.PushID("loadedFileTextures");
        if (ImGui.CollapsingHeader(
                $"Loaded File Textures: {this.textureManager.Shared.ForDebugFileSystemTextures.Count:n0}###header"))
            this.DrawLoadedTextures(this.textureManager.Shared.ForDebugFileSystemTextures);
        ImGui.PopID();

        ImGui.PushID("loadedManifestResourceTextures");
        if (ImGui.CollapsingHeader(
                $"Loaded Manifest Resource Textures: {this.textureManager.Shared.ForDebugManifestResourceTextures.Count:n0}###header"))
            this.DrawLoadedTextures(this.textureManager.Shared.ForDebugManifestResourceTextures);
        ImGui.PopID();

        lock (this.textureManager.Shared.ForDebugInvalidatedTextures)
        {
            ImGui.PushID("invalidatedTextures");
            if (ImGui.CollapsingHeader(
                    $"Invalidated: {this.textureManager.Shared.ForDebugInvalidatedTextures.Count:n0}###header"))
            {
                this.DrawLoadedTextures(this.textureManager.Shared.ForDebugInvalidatedTextures);
            }

            ImGui.PopID();
        }

        ImGui.Dummy(new(ImGui.GetTextLineHeightWithSpacing()));

        if (ImGui.CollapsingHeader(nameof(ITextureProvider.GetFromGameIcon)))
        {
            ImGui.PushID(nameof(this.DrawGetFromGameIcon));
            this.DrawGetFromGameIcon();
            ImGui.PopID();
        }

        if (ImGui.CollapsingHeader(nameof(ITextureProvider.GetFromGame)))
        {
            ImGui.PushID(nameof(this.DrawGetFromGame));
            this.DrawGetFromGame();
            ImGui.PopID();
        }

        if (ImGui.CollapsingHeader(nameof(ITextureProvider.GetFromFile)))
        {
            ImGui.PushID(nameof(this.DrawGetFromFile));
            this.DrawGetFromFile();
            ImGui.PopID();
        }

        if (ImGui.CollapsingHeader(nameof(ITextureProvider.GetFromManifestResource)))
        {
            ImGui.PushID(nameof(this.DrawGetFromManifestResource));
            this.DrawGetFromManifestResource();
            ImGui.PopID();
        }

        if (ImGui.CollapsingHeader(nameof(ITextureProvider.CreateFromImGuiViewportAsync)))
        {
            ImGui.PushID(nameof(this.DrawCreateFromImGuiViewportAsync));
            this.DrawCreateFromImGuiViewportAsync();
            ImGui.PopID();
        }

        if (ImGui.CollapsingHeader("UV"))
        {
            ImGui.PushID(nameof(this.DrawUvInput));
            this.DrawUvInput();
            ImGui.PopID();
        }

        if (ImGui.CollapsingHeader($"CropCopy##{this.DrawExistingTextureModificationArgs}"))
        {
            ImGui.PushID(nameof(this.DrawExistingTextureModificationArgs));
            this.DrawExistingTextureModificationArgs();
            ImGui.PopID();
        }

        ImGui.Dummy(new(ImGui.GetTextLineHeightWithSpacing()));

        Action? runLater = null;
        foreach (var t in this.addedTextures)
        {
            ImGui.PushID(t.Id);
            if (ImGui.CollapsingHeader($"Tex #{t.Id} {t}###header", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("X"))
                {
                    runLater = () =>
                    {
                        t.Dispose();
                        this.addedTextures.Remove(t);
                    };
                }

                ImGui.SameLine();
                if (ImGui.Button("Save"))
                {
                    _ = Service<DevTextureSaveMenu>.Get().ShowTextureSaveMenuAsync(
                        this.DisplayName,
                        $"Texture {t.Id}",
                        t.CreateNewTextureWrapReference(this.textureManager));
                }

                ImGui.SameLine();
                if (ImGui.Button("Copy Reference"))
                    runLater = () => this.addedTextures.Add(t.CreateFromSharedLowLevelResource(this.textureManager));

                ImGui.SameLine();
                if (ImGui.Button("CropCopy"))
                {
                    runLater = () =>
                    {
                        if (t.GetTexture(this.textureManager) is not { } source)
                            return;
                        if (this.supportedRenderTargetFormats is not { } supportedFormats)
                            return;
                        if (this.renderTargetChoiceInt < 0 || this.renderTargetChoiceInt >= supportedFormats.Length)
                            return;
                        var texTask = this.textureManager.CreateFromExistingTextureAsync(
                            source.CreateWrapSharingLowLevelResource(),
                            this.textureModificationArgs with
                            {
                                Format = supportedFormats[this.renderTargetChoiceInt],
                            });
                        this.addedTextures.Add(new() { Api10 = texTask });
                    };
                }

                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                unsafe
                {
                    if (t.GetTexture(this.textureManager) is { } source)
                    {
                        var punk = (IUnknown*)Service<TextureManager>.Get().Scene.GetTextureResource(source);
                        var rc = punk->AddRef() - 1;
                        punk->Release();

                        ImGui.TextUnformatted($"RC: Resource({rc})");
                        ImGui.TextUnformatted(source.ToString());
                    }
                    else
                    {
                        ImGui.TextUnformatted("RC: -");
                        ImGui.TextUnformatted(" ");
                    }
                }

                try
                {
                    if (t.GetTexture(this.textureManager) is { } tex)
                    {
                        var scale = new Vector2(tex.Width, tex.Height);
                        if (this.inputTexScale != Vector2.Zero)
                            scale *= this.inputTexScale;

                        ImGui.Image(tex.ImGuiHandle, scale, this.inputTexUv0, this.inputTexUv1, this.inputTintCol);
                    }
                    else
                    {
                        ImGui.TextUnformatted(t.DescribeError() ?? "Loading");
                    }
                }
                catch (Exception e)
                {
                    ImGui.TextUnformatted(e.ToString());
                }
            }

            ImGui.PopID();
        }

        runLater?.Invoke();
    }

    private unsafe void DrawLoadedTextures(ICollection<SharedImmediateTexture> textures)
    {
        var im = Service<InterfaceManager>.Get();
        if (!ImGui.BeginTable("##table", 6))
            return;

        const int numIcons = 4;
        float iconWidths;
        using (im.IconFontHandle?.Push())
        {
            iconWidths = ImGui.CalcTextSize(FontAwesomeIcon.Save.ToIconString()).X;
            iconWidths += ImGui.CalcTextSize(FontAwesomeIcon.Sync.ToIconString()).X;
            iconWidths += ImGui.CalcTextSize(FontAwesomeIcon.Trash.ToIconString()).X;
        }

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("000000").X);
        ImGui.TableSetupColumn("Source", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("RefCount", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("RefCount__").X);
        ImGui.TableSetupColumn("SelfRef", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("00.000___").X);
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
                    ImGui.PushID(row);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    this.TextCopiable($"{texture.InstanceIdForDebug:n0}", true, true);

                    ImGui.TableNextColumn();
                    this.TextCopiable(texture.SourcePathForDebug, false, true);

                    ImGui.TableNextColumn();
                    this.TextCopiable($"{texture.RefCountForDebug:n0}", true, true);

                    ImGui.TableNextColumn();
                    this.TextCopiable(remain <= 0 ? "-" : $"{remain:00.000}", true, true);

                    ImGui.TableNextColumn();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Save))
                    {
                        var name = Path.ChangeExtension(Path.GetFileName(texture.SourcePathForDebug), null);
                        _ = Service<DevTextureSaveMenu>.Get().ShowTextureSaveMenuAsync(
                            this.DisplayName,
                            name,
                            texture.RentAsync());
                    }

                    if (ImGui.IsItemHovered() && texture.GetWrapOrDefault(null) is { } immediate)
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

                    ImGui.PopID();
                }

                if (!valid)
                    break;
            }
        }

        clipper.Destroy();
        ImGui.EndTable();

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawGetFromGameIcon()
    {
        ImGui.InputText("Icon ID", ref this.iconId, 32);
        ImGui.Checkbox("HQ Item", ref this.hq);
        ImGui.Checkbox("Hi-Res", ref this.hiRes);

        ImGui.SameLine();
        if (ImGui.Button("Load Icon (Async)"))
        {
            this.addedTextures.Add(
                new(
                    Api10: this.textureManager
                               .Shared
                               .GetFromGameIcon(new(uint.Parse(this.iconId), this.hq, this.hiRes))
                               .RentAsync()));
        }

        ImGui.SameLine();
        if (ImGui.Button("Load Icon (Immediate)"))
            this.addedTextures.Add(new(Api10ImmGameIcon: new(uint.Parse(this.iconId), this.hq, this.hiRes)));

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawGetFromGame()
    {
        ImGui.InputText("Tex Path", ref this.inputTexPath, 255);

        ImGui.SameLine();
        if (ImGui.Button("Load Tex (Async)"))
            this.addedTextures.Add(new(Api10: this.textureManager.Shared.GetFromGame(this.inputTexPath).RentAsync()));

        ImGui.SameLine();
        if (ImGui.Button("Load Tex (Immediate)"))
            this.addedTextures.Add(new(Api10ImmGamePath: this.inputTexPath));

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawGetFromFile()
    {
        ImGui.InputText("File Path", ref this.inputFilePath, 255);

        ImGui.SameLine();
        if (ImGui.Button("Load File (Async)"))
            this.addedTextures.Add(new(Api10: this.textureManager.Shared.GetFromFile(this.inputFilePath).RentAsync()));

        ImGui.SameLine();
        if (ImGui.Button("Load File (Immediate)"))
            this.addedTextures.Add(new(Api10ImmFile: this.inputFilePath));

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawGetFromManifestResource()
    {
        if (this.inputManifestResourceAssemblyCandidateNames is null ||
            this.inputManifestResourceAssemblyCandidates is null)
        {
            this.inputManifestResourceAssemblyIndex = 0;
            this.inputManifestResourceAssemblyCandidates =
                AssemblyLoadContext
                    .All
                    .SelectMany(x => x.Assemblies)
                    .Distinct()
                    .OrderBy(x => x.GetName().FullName)
                    .ToArray();
            this.inputManifestResourceAssemblyCandidateNames =
                this.inputManifestResourceAssemblyCandidates
                    .Select(x => x.GetName().FullName)
                    .ToArray();
        }

        if (ImGui.Combo(
                "Assembly",
                ref this.inputManifestResourceAssemblyIndex,
                this.inputManifestResourceAssemblyCandidateNames,
                this.inputManifestResourceAssemblyCandidateNames.Length))
        {
            this.inputManifestResourceNameIndex = 0;
            this.inputManifestResourceNameCandidates = null;
        }

        var assembly =
            this.inputManifestResourceAssemblyIndex >= 0
            && this.inputManifestResourceAssemblyIndex < this.inputManifestResourceAssemblyCandidates.Length
                ? this.inputManifestResourceAssemblyCandidates[this.inputManifestResourceAssemblyIndex]
                : null;

        this.inputManifestResourceNameCandidates ??= assembly?.GetManifestResourceNames() ?? Array.Empty<string>();

        ImGui.Combo(
            "Name",
            ref this.inputManifestResourceNameIndex,
            this.inputManifestResourceNameCandidates,
            this.inputManifestResourceNameCandidates.Length);

        var name =
            this.inputManifestResourceNameIndex >= 0
            && this.inputManifestResourceNameIndex < this.inputManifestResourceNameCandidates.Length
                ? this.inputManifestResourceNameCandidates[this.inputManifestResourceNameIndex]
                : null;

        if (ImGui.Button("Refresh Assemblies"))
        {
            this.inputManifestResourceAssemblyIndex = 0;
            this.inputManifestResourceAssemblyCandidates = null;
            this.inputManifestResourceAssemblyCandidateNames = null;
            this.inputManifestResourceNameIndex = 0;
            this.inputManifestResourceNameCandidates = null;
        }

        if (assembly is not null && name is not null)
        {
            ImGui.SameLine();
            if (ImGui.Button("Load File (Async)"))
            {
                this.addedTextures.Add(
                    new(Api10: this.textureManager.Shared.GetFromManifestResource(assembly, name).RentAsync()));
            }

            ImGui.SameLine();
            if (ImGui.Button("Load File (Immediate)"))
                this.addedTextures.Add(new(Api10ImmManifestResource: (assembly, name)));
        }

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawCreateFromImGuiViewportAsync()
    {
        var viewports = ImGui.GetPlatformIO().Viewports;
        if (ImGui.BeginCombo(
                nameof(this.viewportTextureArgs.ViewportId),
                $"{this.viewportIndexInt}. {viewports[this.viewportIndexInt].ID:X08}"))
        {
            for (var i = 0; i < viewports.Size; i++)
            {
                var sel = this.viewportIndexInt == i;
                if (ImGui.Selectable($"#{i}: {viewports[i].ID:X08}", ref sel))
                {
                    this.viewportIndexInt = i;
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        var b = this.viewportTextureArgs.KeepTransparency;
        if (ImGui.Checkbox(nameof(this.viewportTextureArgs.KeepTransparency), ref b))
            this.viewportTextureArgs.KeepTransparency = b;

        b = this.viewportTextureArgs.AutoUpdate;
        if (ImGui.Checkbox(nameof(this.viewportTextureArgs.AutoUpdate), ref b))
            this.viewportTextureArgs.AutoUpdate = b;

        b = this.viewportTextureArgs.TakeBeforeImGuiRender;
        if (ImGui.Checkbox(nameof(this.viewportTextureArgs.TakeBeforeImGuiRender), ref b))
            this.viewportTextureArgs.TakeBeforeImGuiRender = b;

        var vec2 = this.viewportTextureArgs.Uv0;
        if (ImGui.InputFloat2(nameof(this.viewportTextureArgs.Uv0), ref vec2))
            this.viewportTextureArgs.Uv0 = vec2;

        vec2 = this.viewportTextureArgs.Uv1;
        if (ImGui.InputFloat2(nameof(this.viewportTextureArgs.Uv1), ref vec2))
            this.viewportTextureArgs.Uv1 = vec2;

        if (ImGui.Button("Create") && this.viewportIndexInt >= 0 && this.viewportIndexInt < viewports.Size)
        {
            this.addedTextures.Add(
                new()
                {
                    Api10 = this.textureManager.CreateFromImGuiViewportAsync(
                        this.viewportTextureArgs with { ViewportId = viewports[this.viewportIndexInt].ID },
                        null),
                });
        }
    }

    private void DrawUvInput()
    {
        ImGui.InputFloat2("UV0", ref this.inputTexUv0);
        ImGui.InputFloat2("UV1", ref this.inputTexUv1);
        ImGui.InputFloat4("Tint", ref this.inputTintCol);
        ImGui.InputFloat2("Scale", ref this.inputTexScale);

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawExistingTextureModificationArgs()
    {
        var b = this.textureModificationArgs.MakeOpaque;
        if (ImGui.Checkbox(nameof(this.textureModificationArgs.MakeOpaque), ref b))
            this.textureModificationArgs.MakeOpaque = b;

        if (this.supportedRenderTargetFormats is null)
        {
            this.supportedRenderTargetFormatNames = null;
            this.supportedRenderTargetFormats =
                Enum.GetValues<DXGI_FORMAT>()
                    .Where(this.textureManager.IsDxgiFormatSupportedForCreateFromExistingTextureAsync)
                    .ToArray();
            this.renderTargetChoiceInt = 0;
        }

        this.supportedRenderTargetFormatNames ??= this.supportedRenderTargetFormats.Select(Enum.GetName).ToArray();
        ImGui.Combo(
            nameof(this.textureModificationArgs.DxgiFormat),
            ref this.renderTargetChoiceInt,
            this.supportedRenderTargetFormatNames,
            this.supportedRenderTargetFormatNames.Length);

        Span<int> wh = stackalloc int[2];
        wh[0] = this.textureModificationArgs.NewWidth;
        wh[1] = this.textureModificationArgs.NewHeight;
        if (ImGui.InputInt2(
                $"{nameof(this.textureModificationArgs.NewWidth)}/{nameof(this.textureModificationArgs.NewHeight)}",
                ref wh[0]))
        {
            this.textureModificationArgs.NewWidth = wh[0];
            this.textureModificationArgs.NewHeight = wh[1];
        }

        var vec2 = this.textureModificationArgs.Uv0;
        if (ImGui.InputFloat2(nameof(this.textureModificationArgs.Uv0), ref vec2))
            this.textureModificationArgs.Uv0 = vec2;

        vec2 = this.textureModificationArgs.Uv1;
        if (ImGui.InputFloat2(nameof(this.textureModificationArgs.Uv1), ref vec2))
            this.textureModificationArgs.Uv1 = vec2;

        ImGuiHelpers.ScaledDummy(10);
    }

    private void TextCopiable(string s, bool alignRight, bool framepad)
    {
        var offset = ImGui.GetCursorScreenPos() + new Vector2(0, framepad ? ImGui.GetStyle().FramePadding.Y : 0);
        if (framepad)
            ImGui.AlignTextToFramePadding();
        if (alignRight)
        {
            var width = ImGui.CalcTextSize(s).X;
            var xoff = ImGui.GetColumnWidth() - width;
            offset.X += xoff;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + xoff);
            ImGui.TextUnformatted(s);
        }
        else
        {
            ImGui.TextUnformatted(s);
        }

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
        Task<IDalamudTextureWrap>? Api10 = null,
        GameIconLookup? Api10ImmGameIcon = null,
        string? Api10ImmGamePath = null,
        string? Api10ImmFile = null,
        (Assembly Assembly, string Name)? Api10ImmManifestResource = null) : IDisposable
    {
        private static int idCounter;

        public int Id { get; } = idCounter++;

        public void Dispose()
        {
            this.SharedResource?.Dispose();
            _ = this.Api10?.ToContentDisposedTask();
        }

        public string? DescribeError()
        {
            if (this.SharedResource is not null)
                return "Unknown error";
            if (this.Api10 is not null)
            {
                return !this.Api10.IsCompleted
                           ? null
                           : this.Api10.Exception?.ToString() ?? (this.Api10.IsCanceled ? "Canceled" : "Unknown error");
            }

            if (this.Api10ImmGameIcon is not null)
                return "Must not happen";
            if (this.Api10ImmGamePath is not null)
                return "Must not happen";
            if (this.Api10ImmFile is not null)
                return "Must not happen";
            if (this.Api10ImmManifestResource is not null)
                return "Must not happen";
            return "Not implemented";
        }

        public IDalamudTextureWrap? GetTexture(ITextureProvider tp)
        {
            if (this.SharedResource is not null)
                return this.SharedResource;
            if (this.Api10 is not null)
                return this.Api10.IsCompletedSuccessfully ? this.Api10.Result : null;
            if (this.Api10ImmGameIcon is not null)
                return tp.GetFromGameIcon(this.Api10ImmGameIcon.Value).GetWrapOrEmpty();
            if (this.Api10ImmGamePath is not null)
                return tp.GetFromGame(this.Api10ImmGamePath).GetWrapOrEmpty();
            if (this.Api10ImmFile is not null)
                return tp.GetFromFile(this.Api10ImmFile).GetWrapOrEmpty();
            if (this.Api10ImmManifestResource is not null)
            {
                return tp.GetFromManifestResource(
                    this.Api10ImmManifestResource.Value.Assembly,
                    this.Api10ImmManifestResource.Value.Name).GetWrapOrEmpty();
            }

            return null;
        }

        public async Task<IDalamudTextureWrap> CreateNewTextureWrapReference(ITextureProvider tp)
        {
            while (true)
            {
                if (this.GetTexture(tp) is { } textureWrap)
                    return textureWrap.CreateWrapSharingLowLevelResource();
                if (this.DescribeError() is { } err)
                    throw new(err);
                await Task.Delay(100);
            }
        }

        public TextureEntry CreateFromSharedLowLevelResource(ITextureProvider tp) =>
            new() { SharedResource = this.GetTexture(tp)?.CreateWrapSharingLowLevelResource() };

        public override string ToString()
        {
            if (this.SharedResource is not null)
                return $"{nameof(this.SharedResource)}: {this.SharedResource}";
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
            if (this.Api10ImmManifestResource is not null)
                return $"{nameof(this.Api10ImmManifestResource)}: {this.Api10ImmManifestResource}";
            return "Not implemented";
        }
    }
}
