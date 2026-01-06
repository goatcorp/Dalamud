using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.Internal.SharedImmediateTextures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;

using TextureManager = Dalamud.Interface.Textures.Internal.TextureManager;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying texture test.
/// </summary>
internal class TexWidget : IDataWindowWidget
{
    // TODO: move tracking implementation to PluginStats where applicable,
    //       and show stats over there instead of TexWidget.
    private static readonly Dictionary<
        DrawBlameTableColumnUserId,
        Func<TextureManager.IBlameableDalamudTextureWrap, IComparable>> DrawBlameTableColumnColumnComparers = new()
    {
        [DrawBlameTableColumnUserId.Plugins] = static x => string.Join(", ", x.OwnerPlugins.Select(y => y.Name)),
        [DrawBlameTableColumnUserId.Name] = static x => x.Name,
        [DrawBlameTableColumnUserId.Size] = static x => x.RawSpecs.EstimatedBytes,
        [DrawBlameTableColumnUserId.Format] = static x => x.Format,
        [DrawBlameTableColumnUserId.Width] = static x => x.Width,
        [DrawBlameTableColumnUserId.Height] = static x => x.Height,
        [DrawBlameTableColumnUserId.NativeAddress] = static x => x.ResourceAddress,
    };

    private readonly List<TextureEntry> addedTextures = [];

    private string allLoadedTexturesTableName = "##table";
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

    private enum DrawBlameTableColumnUserId
    {
        NativeAddress = 1,
        Actions,
        Name,
        Width,
        Height,
        Format,
        Size,
        Plugins,
        ColumnCount,
    }

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["tex", "texture"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Tex";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.allLoadedTexturesTableName = "##table" + Environment.TickCount64;
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

        if (ImGui.Button("GC"u8))
            GC.Collect();

        var useTexturePluginTracking = conf.UseTexturePluginTracking;
        if (ImGui.Checkbox("Enable Texture Tracking"u8, ref useTexturePluginTracking))
        {
            conf.UseTexturePluginTracking = useTexturePluginTracking;
            conf.QueueSave();
        }

        lock (this.textureManager.BlameTracker)
        {
            var allBlames = this.textureManager.BlameTracker;
            ImGui.PushID("blames"u8);
            var sizeSum = allBlames.Sum(static x => Math.Max(0, x.RawSpecs.EstimatedBytes));
            if (ImGui.CollapsingHeader(
                    $"All Loaded Textures: {allBlames.Count:n0} ({Util.FormatBytes(sizeSum)})###header"))
                this.DrawBlame(allBlames);
            ImGui.PopID();
        }

        ImGui.PushID("loadedGameTextures"u8);
        if (ImGui.CollapsingHeader(
                $"Loaded Game Textures: {this.textureManager.Shared.ForDebugGamePathTextures.Count:n0}###header"))
            this.DrawLoadedTextures(this.textureManager.Shared.ForDebugGamePathTextures);
        ImGui.PopID();

        ImGui.PushID("loadedFileTextures"u8);
        if (ImGui.CollapsingHeader(
                $"Loaded File Textures: {this.textureManager.Shared.ForDebugFileSystemTextures.Count:n0}###header"))
            this.DrawLoadedTextures(this.textureManager.Shared.ForDebugFileSystemTextures);
        ImGui.PopID();

        ImGui.PushID("loadedManifestResourceTextures"u8);
        if (ImGui.CollapsingHeader(
                $"Loaded Manifest Resource Textures: {this.textureManager.Shared.ForDebugManifestResourceTextures.Count:n0}###header"))
            this.DrawLoadedTextures(this.textureManager.Shared.ForDebugManifestResourceTextures);
        ImGui.PopID();

        lock (this.textureManager.Shared.ForDebugInvalidatedTextures)
        {
            ImGui.PushID("invalidatedTextures"u8);
            if (ImGui.CollapsingHeader(
                    $"Invalidated: {this.textureManager.Shared.ForDebugInvalidatedTextures.Count:n0}###header"))
            {
                this.DrawLoadedTextures(this.textureManager.Shared.ForDebugInvalidatedTextures);
            }

            ImGui.PopID();
        }

        ImGui.Dummy(new(ImGui.GetTextLineHeightWithSpacing()));

        if (!this.textureManager.HasClipboardImage())
        {
            ImGuiComponents.DisabledButton("Paste from Clipboard");
        }
        else if (ImGui.Button("Paste from Clipboard"u8))
        {
            this.addedTextures.Add(new(Api10: this.textureManager.CreateFromClipboardAsync()));
        }

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

        if (ImGui.CollapsingHeader("UV"u8))
        {
            ImGui.PushID(nameof(this.DrawUvInput));
            this.DrawUvInput();
            ImGui.PopID();
        }

        if (ImGui.CollapsingHeader($"CropCopy##{nameof(this.DrawExistingTextureModificationArgs)}"))
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
                if (ImGui.Button("X"u8))
                {
                    runLater = () =>
                    {
                        t.Dispose();
                        this.addedTextures.Remove(t);
                    };
                }

                ImGui.SameLine();
                if (ImGui.Button("Save"u8))
                {
                    _ = Service<DevTextureSaveMenu>.Get().ShowTextureSaveMenuAsync(
                        this.DisplayName,
                        $"Texture {t.Id}",
                        t.CreateNewTextureWrapReference(this.textureManager));
                }

                ImGui.SameLine();
                if (ImGui.Button("Copy Reference"u8))
                    runLater = () => this.addedTextures.Add(t.CreateFromSharedLowLevelResource(this.textureManager));

                ImGui.SameLine();
                if (ImGui.Button("CropCopy"u8))
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
                        var psrv = (ID3D11ShaderResourceView*)source.Handle.Handle;
                        var rcsrv = psrv->AddRef() - 1;
                        psrv->Release();

                        var pres = default(ID3D11Resource*);
                        psrv->GetResource(&pres);
                        var rcres = pres->AddRef() - 1;
                        pres->Release();
                        pres->Release();

                        ImGui.Text($"RC: Resource({rcres})/View({rcsrv})");
                        ImGui.Text($"{source.Width} x {source.Height} | {source}");
                    }
                    else
                    {
                        ImGui.Text("RC: -");
                        ImGui.Text(string.Empty);
                    }
                }

                try
                {
                    if (t.GetTexture(this.textureManager) is { } tex)
                    {
                        var scale = new Vector2(tex.Width, tex.Height);
                        if (this.inputTexScale != Vector2.Zero)
                            scale *= this.inputTexScale;

                        ImGui.Image(tex.Handle, scale, this.inputTexUv0, this.inputTexUv1, this.inputTintCol);
                    }
                    else
                    {
                        ImGui.Text(t.DescribeError() ?? "Loading");
                    }
                }
                catch (Exception e)
                {
                    ImGui.Text(e.ToString());
                }
            }

            ImGui.PopID();
        }

        runLater?.Invoke();
    }

    /// <summary>Adds a texture wrap for debug display purposes.</summary>
    /// <param name="textureTask">Task returning a texture.</param>
    public void AddTexture(Task<IDalamudTextureWrap> textureTask) => this.addedTextures.Add(new(Api10: textureTask));

    private unsafe void DrawBlame(List<TextureManager.IBlameableDalamudTextureWrap> allBlames)
    {
        var im = Service<InterfaceManager>.Get();

        var shouldSortAgain = ImGui.Button("Sort again"u8);

        ImGui.SameLine();
        if (ImGui.Button("Reset Columns"u8))
            this.allLoadedTexturesTableName = "##table" + Environment.TickCount64;

        if (!ImGui.BeginTable(
                this.allLoadedTexturesTableName,
                (int)DrawBlameTableColumnUserId.ColumnCount,
                ImGuiTableFlags.Sortable | ImGuiTableFlags.SortTristate | ImGuiTableFlags.SortMulti |
                ImGuiTableFlags.Reorderable | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoBordersInBodyUntilResize |
                ImGuiTableFlags.NoSavedSettings))
            return;

        const int numIcons = 1;
        float iconWidths;
        using (im.IconFontHandle?.Push())
            iconWidths = ImGui.CalcTextSize(FontAwesomeIcon.Save.ToIconString()).X;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn(
            "Address"u8,
            ImGuiTableColumnFlags.WidthFixed,
            ImGui.CalcTextSize("0x7F0000000000"u8).X,
            (uint)DrawBlameTableColumnUserId.NativeAddress);
        ImGui.TableSetupColumn(
            "Actions"u8,
            ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort,
            iconWidths +
            (ImGui.GetStyle().FramePadding.X * 2 * numIcons) +
            (ImGui.GetStyle().ItemSpacing.X * 1 * numIcons),
            (uint)DrawBlameTableColumnUserId.Actions);
        ImGui.TableSetupColumn(
            "Name"u8,
            ImGuiTableColumnFlags.WidthStretch,
            0f,
            (uint)DrawBlameTableColumnUserId.Name);
        ImGui.TableSetupColumn(
            "Width"u8,
            ImGuiTableColumnFlags.WidthFixed,
            ImGui.CalcTextSize("000000"u8).X,
            (uint)DrawBlameTableColumnUserId.Width);
        ImGui.TableSetupColumn(
            "Height"u8,
            ImGuiTableColumnFlags.WidthFixed,
            ImGui.CalcTextSize("000000"u8).X,
            (uint)DrawBlameTableColumnUserId.Height);
        ImGui.TableSetupColumn(
            "Format"u8,
            ImGuiTableColumnFlags.WidthFixed,
            ImGui.CalcTextSize("R32G32B32A32_TYPELESS"u8).X,
            (uint)DrawBlameTableColumnUserId.Format);
        ImGui.TableSetupColumn(
            "Size"u8,
            ImGuiTableColumnFlags.WidthFixed,
            ImGui.CalcTextSize("123.45 MB"u8).X,
            (uint)DrawBlameTableColumnUserId.Size);
        ImGui.TableSetupColumn(
            "Plugins"u8,
            ImGuiTableColumnFlags.WidthFixed,
            ImGui.CalcTextSize("Aaaaaaaaaa Aaaaaaaaaa Aaaaaaaaaa"u8).X,
            (uint)DrawBlameTableColumnUserId.Plugins);
        ImGui.TableHeadersRow();

        var sortSpecs = ImGui.TableGetSortSpecs();
        if (sortSpecs.Handle is not null && (sortSpecs.SpecsDirty || shouldSortAgain))
        {
            allBlames.Sort(
                static (a, b) =>
                {
                    var sortSpecs = ImGui.TableGetSortSpecs();
                    var specs = new Span<ImGuiTableColumnSortSpecs>(sortSpecs.Handle->Specs, sortSpecs.SpecsCount);
                    Span<bool> sorted = stackalloc bool[(int)DrawBlameTableColumnUserId.ColumnCount];
                    foreach (ref var spec in specs)
                    {
                        if (!DrawBlameTableColumnColumnComparers.TryGetValue(
                                (DrawBlameTableColumnUserId)spec.ColumnUserID,
                                out var comparableGetter))
                            continue;
                        sorted[(int)spec.ColumnUserID] = true;
                        var ac = comparableGetter(a);
                        var bc = comparableGetter(b);
                        var c = ac.CompareTo(bc);
                        if (c != 0)
                            return spec.SortDirection == ImGuiSortDirection.Ascending ? c : -c;
                    }

                    foreach (var (col, comparableGetter) in DrawBlameTableColumnColumnComparers)
                    {
                        if (sorted[(int)col])
                            continue;
                        var ac = comparableGetter(a);
                        var bc = comparableGetter(b);
                        var c = ac.CompareTo(bc);
                        if (c != 0)
                            return c;
                    }

                    return 0;
                });
            sortSpecs.SpecsDirty = false;
        }

        var clipper = ImGui.ImGuiListClipper();
        clipper.Begin(allBlames.Count);

        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var wrap = allBlames[i];
                ImGui.TableNextRow();
                ImGui.PushID(i);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                this.TextColumnCopiable($"0x{wrap.ResourceAddress:X}", true, true);

                ImGui.TableNextColumn();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Save))
                {
                    _ = Service<DevTextureSaveMenu>.Get().ShowTextureSaveMenuAsync(
                        this.DisplayName,
                        $"{wrap.Handle.Handle:X16}",
                        Task.FromResult(wrap.CreateWrapSharingLowLevelResource()));
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Image(wrap.Handle, wrap.Size);
                    ImGui.EndTooltip();
                }

                ImGui.TableNextColumn();
                this.TextColumnCopiable(wrap.Name, false, true);

                ImGui.TableNextColumn();
                this.TextColumnCopiable($"{wrap.Width:n0}", true, true);

                ImGui.TableNextColumn();
                this.TextColumnCopiable($"{wrap.Height:n0}", true, true);

                ImGui.TableNextColumn();
                this.TextColumnCopiable(Enum.GetName(wrap.Format)?[12..] ?? wrap.Format.ToString(), false, true);

                ImGui.TableNextColumn();
                var bytes = wrap.RawSpecs.EstimatedBytes;
                this.TextColumnCopiable(bytes < 0 ? "?" : $"{bytes:n0}", true, true);

                ImGui.TableNextColumn();
                lock (wrap.OwnerPlugins)
                    this.TextColumnCopiable(string.Join(", ", wrap.OwnerPlugins.Select(static x => x.Name)), false, true);

                ImGui.PopID();
            }
        }

        clipper.Destroy();
        ImGui.EndTable();

        ImGuiHelpers.ScaledDummy(10);
    }

    private unsafe void DrawLoadedTextures(ICollection<SharedImmediateTexture> textures)
    {
        var im = Service<InterfaceManager>.Get();
        if (!ImGui.BeginTable("##table"u8, 6))
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
        ImGui.TableSetupColumn("ID"u8, ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("000000"u8).X);
        ImGui.TableSetupColumn("Source"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("RefCount"u8, ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("RefCount__"u8).X);
        ImGui.TableSetupColumn("SelfRef"u8, ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("00.000___"u8).X);
        ImGui.TableSetupColumn(
            "Actions"u8,
            ImGuiTableColumnFlags.WidthFixed,
            iconWidths +
            (ImGui.GetStyle().FramePadding.X * 2 * numIcons) +
            (ImGui.GetStyle().ItemSpacing.X * 1 * numIcons));
        ImGui.TableHeadersRow();

        var clipper = ImGui.ImGuiListClipper();
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
                        ImGui.Text("?"u8);
                        continue;
                    }

                    var remain = texture.SelfReferenceExpiresInForDebug;
                    ImGui.PushID(row);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    this.TextColumnCopiable($"{texture.InstanceIdForDebug:n0}", true, true);

                    ImGui.TableNextColumn();
                    this.TextColumnCopiable(texture.SourcePathForDebug, false, true);

                    ImGui.TableNextColumn();
                    this.TextColumnCopiable($"{texture.RefCountForDebug:n0}", true, true);

                    ImGui.TableNextColumn();
                    this.TextColumnCopiable(remain <= 0 ? "-" : $"{remain:00.000}", true, true);

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
                        ImGui.Image(immediate.Handle, immediate.Size);
                        ImGui.EndTooltip();
                    }

                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Sync))
                        this.textureManager.InvalidatePaths([texture.SourcePathForDebug]);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip($"Call {nameof(ITextureSubstitutionProvider.InvalidatePaths)}.");

                    ImGui.SameLine();
                    if (remain <= 0)
                        ImGui.BeginDisabled();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                        texture.ReleaseSelfReference(true);
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        ImGui.SetTooltip("Release self-reference immediately."u8);
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
        ImGui.InputText("Icon ID"u8, ref this.iconId, 32);
        ImGui.Checkbox("HQ Item"u8, ref this.hq);
        ImGui.Checkbox("Hi-Res"u8, ref this.hiRes);

        ImGui.SameLine();
        if (ImGui.Button("Load Icon (Async)"u8))
        {
            this.addedTextures.Add(
                new(
                    Api10: this.textureManager
                               .Shared
                               .GetFromGameIcon(new(uint.Parse(this.iconId), this.hq, this.hiRes))
                               .RentAsync()));
        }

        ImGui.SameLine();
        if (ImGui.Button("Load Icon (Immediate)"u8))
            this.addedTextures.Add(new(Api10ImmGameIcon: new(uint.Parse(this.iconId), this.hq, this.hiRes)));

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawGetFromGame()
    {
        ImGui.InputText("Tex Path"u8, ref this.inputTexPath, 255);

        ImGui.SameLine();
        if (ImGui.Button("Load Tex (Async)"u8))
            this.addedTextures.Add(new(Api10: this.textureManager.Shared.GetFromGame(this.inputTexPath).RentAsync()));

        ImGui.SameLine();
        if (ImGui.Button("Load Tex (Immediate)"u8))
            this.addedTextures.Add(new(Api10ImmGamePath: this.inputTexPath));

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawGetFromFile()
    {
        ImGui.InputText("File Path"u8, ref this.inputFilePath, 255);

        ImGui.SameLine();
        if (ImGui.Button("Load File (Async)"u8))
            this.addedTextures.Add(new(Api10: this.textureManager.Shared.GetFromFile(this.inputFilePath).RentAsync()));

        ImGui.SameLine();
        if (ImGui.Button("Load File (Immediate)"u8))
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
                this.inputManifestResourceAssemblyCandidateNames))
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
            this.inputManifestResourceNameCandidates);

        var name =
            this.inputManifestResourceNameIndex >= 0
            && this.inputManifestResourceNameIndex < this.inputManifestResourceNameCandidates.Length
                ? this.inputManifestResourceNameCandidates[this.inputManifestResourceNameIndex]
                : null;

        if (ImGui.Button("Refresh Assemblies"u8))
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
            if (ImGui.Button("Load File (Async)"u8))
            {
                this.addedTextures.Add(
                    new(Api10: this.textureManager.Shared.GetFromManifestResource(assembly, name).RentAsync()));
            }

            ImGui.SameLine();
            if (ImGui.Button("Load File (Immediate)"u8))
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

        if (ImGui.Button("Create"u8) && this.viewportIndexInt >= 0 && this.viewportIndexInt < viewports.Size)
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
            this.supportedRenderTargetFormatNames);

        Span<int> wh = stackalloc int[2];
        wh[0] = this.textureModificationArgs.NewWidth;
        wh[1] = this.textureModificationArgs.NewHeight;
        if (ImGui.InputInt(
                $"{nameof(this.textureModificationArgs.NewWidth)}/{nameof(this.textureModificationArgs.NewHeight)}",
                wh))
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
