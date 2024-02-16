using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Plugin.Services;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using ImGuiNET;

using ImGuiScene;

using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Misc;

using Newtonsoft.Json;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Factory for the implementation of <see cref="IFontAtlas"/>.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService]
internal sealed partial class FontAtlasFactory
    : IServiceType, GamePrebakedFontHandle.IGameFontTextureProvider, IDisposable
{
    private static readonly Dictionary<string, uint> KnownFontFileDataHashes = new()
    {
        ["common/font/AXIS_96.fdt"] = 1486212503,
        ["common/font/AXIS_12.fdt"] = 1370045105,
        ["common/font/AXIS_14.fdt"] = 645957730,
        ["common/font/AXIS_18.fdt"] = 899094094,
        ["common/font/AXIS_36.fdt"] = 2537048938,
        ["common/font/Jupiter_16.fdt"] = 1642196098,
        ["common/font/Jupiter_20.fdt"] = 3053628263,
        ["common/font/Jupiter_23.fdt"] = 1536194944,
        ["common/font/Jupiter_45.fdt"] = 3473589216,
        ["common/font/Jupiter_46.fdt"] = 1370962087,
        ["common/font/Jupiter_90.fdt"] = 3661420529,
        ["common/font/Meidinger_16.fdt"] = 3700692128,
        ["common/font/Meidinger_20.fdt"] = 441419856,
        ["common/font/Meidinger_40.fdt"] = 203848091,
        ["common/font/MiedingerMid_10.fdt"] = 499375313,
        ["common/font/MiedingerMid_12.fdt"] = 1925552591,
        ["common/font/MiedingerMid_14.fdt"] = 1919733827,
        ["common/font/MiedingerMid_18.fdt"] = 1635778987,
        ["common/font/MiedingerMid_36.fdt"] = 1190559864,
        ["common/font/TrumpGothic_184.fdt"] = 973994576,
        ["common/font/TrumpGothic_23.fdt"] = 1967289381,
        ["common/font/TrumpGothic_34.fdt"] = 1777971886,
        ["common/font/TrumpGothic_68.fdt"] = 1170173741,
        ["common/font/font0.tex"] = 514269927,
        ["common/font/font1.tex"] = 3616607606,
        ["common/font/font2.tex"] = 4166651000,
        ["common/font/font3.tex"] = 1264942640,
        ["common/font/font4.tex"] = 3534300885,
        ["common/font/font5.tex"] = 1041916216,
        ["common/font/font6.tex"] = 1247097672,
    };

    private readonly DisposeSafety.ScopedFinalizer scopedFinalizer = new();
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly IReadOnlyDictionary<GameFontFamilyAndSize, Task<FileResource>> fdtFiles;
    private readonly IReadOnlyDictionary<string, Task<Task<TexFile>[]>> texFiles;
    private readonly IReadOnlyDictionary<string, Task<IDalamudTextureWrap?[]>> prebakedTextureWraps;
    private readonly Task<ushort[]> defaultGlyphRanges;
    private readonly DalamudAssetManager dalamudAssetManager;

    [ServiceManager.ServiceConstructor]
    private FontAtlasFactory(
        DataManager dataManager,
        Framework framework,
        InterfaceManager interfaceManager,
        DalamudAssetManager dalamudAssetManager)
    {
        this.Framework = framework;
        this.InterfaceManager = interfaceManager;
        this.dalamudAssetManager = dalamudAssetManager;
        this.SceneTask = Service<InterfaceManager.InterfaceManagerWithScene>
                         .GetAsync()
                         .ContinueWith(r => r.Result.Manager.Scene);

        var gffasInfo = Enum.GetValues<GameFontFamilyAndSize>()
                            .Select(
                                x =>
                                    (
                                        Font: x,
                                        Attr: x.GetAttribute<GameFontFamilyAndSizeAttribute>()))
                            .Where(x => x.Attr is not null)
                            .ToArray();
        var texPaths = gffasInfo.Select(x => x.Attr.TexPathFormat).Distinct().ToArray();

        this.fdtFiles = gffasInfo.ToImmutableDictionary(
            x => x.Font,
            x => Task.Run(() => dataManager.GetFile(x.Attr.Path)!));
        var channelCountsTask = texPaths.ToImmutableDictionary(
            x => x,
            x => Task.WhenAll(
                         gffasInfo.Where(y => y.Attr.TexPathFormat == x)
                                  .Select(y => this.fdtFiles[y.Font]))
                     .ContinueWith(
                         files => 1 + files.Result.Max(
                                      file =>
                                      {
                                          unsafe
                                          {
                                              using var pin = file.Data.AsMemory().Pin();
                                              var fdt = new FdtFileView(pin.Pointer, file.Data.Length);
                                              return fdt.MaxTextureIndex;
                                          }
                                      })));
        this.prebakedTextureWraps = channelCountsTask.ToImmutableDictionary(
            x => x.Key,
            x => x.Value.ContinueWith(y => new IDalamudTextureWrap?[y.Result]));
        this.texFiles = channelCountsTask.ToImmutableDictionary(
            x => x.Key,
            x => x.Value.ContinueWith(
                y => Enumerable
                     .Range(1, 1 + ((y.Result - 1) / 4))
                     .Select(z => Task.Run(() => dataManager.GetFile<TexFile>(string.Format(x.Key, z))!))
                     .ToArray()));
        this.defaultGlyphRanges =
            this.fdtFiles[GameFontFamilyAndSize.Axis12]
                .ContinueWith(
                    file =>
                    {
                        unsafe
                        {
                            using var pin = file.Result.Data.AsMemory().Pin();
                            var fdt = new FdtFileView(pin.Pointer, file.Result.Data.Length);
                            return fdt.ToGlyphRanges();
                        }
                    });

        Task.Run(this.CheckSanity);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to override configuration for <see cref="DefaultFontSpec"/>.
    /// </summary>
    public IFontSpec? DefaultFontSpecOverride { get; set; } = null;

    /// <summary>
    /// Gets the default font ID.
    /// </summary>
    public IFontSpec DefaultFontSpec =>
        this.DefaultFontSpecOverride
        ?? Service<DalamudConfiguration>.Get().DefaultFontSpec
#pragma warning disable CS0618 // Type or member is obsolete
        ?? (Service<DalamudConfiguration>.Get().UseAxisFontsFromGame
#pragma warning restore CS0618 // Type or member is obsolete
                ? new()
                {
                    FontId = new GameFontAndFamilyId(GameFontFamily.Axis),
                    SizePx = InterfaceManager.DefaultFontSizePx,
                }
                : new SingleFontSpec
                {
                    FontId = new DalamudAssetFontAndFamilyId(DalamudAsset.NotoSansJpMedium),
                    SizePx = InterfaceManager.DefaultFontSizePx + 1,
                });

    /// <summary>
    /// Gets the service instance of <see cref="Framework"/>.
    /// </summary>
    public Framework Framework { get; }

    /// <summary>
    /// Gets the service instance of <see cref="InterfaceManager"/>.<br />
    /// <see cref="Internal.InterfaceManager.Scene"/> may not yet be available.
    /// </summary>
    public InterfaceManager InterfaceManager { get; }

    /// <summary>
    /// Gets the async task for <see cref="RawDX11Scene"/> inside <see cref="InterfaceManager"/>.
    /// </summary>
    public Task<RawDX11Scene> SceneTask { get; }

    /// <summary>
    /// Gets the default glyph ranges (glyph ranges of <see cref="GameFontFamilyAndSize.Axis12"/>).
    /// </summary>
    public ushort[] DefaultGlyphRanges => ExtractResult(this.defaultGlyphRanges);

    /// <summary>
    /// Gets a value indicating whether game symbol font file is available.
    /// </summary>
    public bool HasGameSymbolsFontFile =>
        this.dalamudAssetManager.IsStreamImmediatelyAvailable(DalamudAsset.LodestoneGameSymbol);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.cancellationTokenSource.Cancel();
        this.scopedFinalizer.Dispose();
        this.cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Creates a new instance of a class that implements the <see cref="IFontAtlas"/> interface.
    /// </summary>
    /// <param name="atlasName">Name of atlas, for debugging and logging purposes.</param>
    /// <param name="autoRebuildMode">Specify how to auto rebuild.</param>
    /// <param name="isGlobalScaled">Whether the fonts in the atlas is global scaled.</param>
    /// <returns>The new font atlas.</returns>
    public IFontAtlas CreateFontAtlas(
        string atlasName,
        FontAtlasAutoRebuildMode autoRebuildMode,
        bool isGlobalScaled = true) =>
        new DalamudFontAtlas(this, atlasName, autoRebuildMode, isGlobalScaled);

    /// <summary>
    /// Adds the font from Dalamud Assets.
    /// </summary>
    /// <param name="toolkitPreBuild">The toolkitPostBuild.</param>
    /// <param name="asset">The font.</param>
    /// <param name="fontConfig">The font config.</param>
    /// <returns>The address and size.</returns>
    public ImFontPtr AddFont(
        IFontAtlasBuildToolkitPreBuild toolkitPreBuild,
        DalamudAsset asset,
        in SafeFontConfig fontConfig) =>
        toolkitPreBuild.AddFontFromStream(
            this.dalamudAssetManager.CreateStream(asset),
            fontConfig,
            false,
            $"Asset({asset})");

    /// <summary>
    /// Gets the <see cref="FdtReader"/> for the <see cref="GameFontFamilyAndSize"/>.
    /// </summary>
    /// <param name="gffas">The font family and size.</param>
    /// <returns>The <see cref="FdtReader"/>.</returns>
    public FdtReader GetFdtReader(GameFontFamilyAndSize gffas) => new(ExtractResult(this.fdtFiles[gffas]).Data);

    /// <inheritdoc/>
    public unsafe MemoryHandle CreateFdtFileView(GameFontFamilyAndSize gffas, out FdtFileView fdtFileView)
    {
        var arr = ExtractResult(this.fdtFiles[gffas]).Data;
        var handle = arr.AsMemory().Pin();
        try
        {
            fdtFileView = new(handle.Pointer, arr.Length);
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    public int GetFontTextureCount(string texPathFormat) =>
        ExtractResult(this.prebakedTextureWraps[texPathFormat]).Length;

    /// <inheritdoc/>
    public TexFile GetTexFile(string texPathFormat, int index) =>
        ExtractResult(ExtractResult(this.texFiles[texPathFormat])[index]);

    /// <inheritdoc/>
    public IDalamudTextureWrap NewFontTextureRef(string texPathFormat, int textureIndex)
    {
        lock (this.prebakedTextureWraps[texPathFormat])
        {
            var wraps = ExtractResult(this.prebakedTextureWraps[texPathFormat]);
            var fileIndex = textureIndex / 4;
            var channelIndex = FdtReader.FontTableEntry.TextureChannelOrder[textureIndex % 4];
            wraps[textureIndex] ??= this.GetChannelTexture(texPathFormat, fileIndex, channelIndex);
            return CloneTextureWrap(wraps[textureIndex]);
        }
    }

    private static T ExtractResult<T>(Task<T> t) => t.IsCompleted ? t.Result : t.GetAwaiter().GetResult();

    private static unsafe void ExtractChannelFromB8G8R8A8(
        Span<byte> target,
        ReadOnlySpan<byte> source,
        int channelIndex,
        bool targetIsB4G4R4A4)
    {
        var numPixels = Math.Min(source.Length / 4, target.Length / (targetIsB4G4R4A4 ? 2 : 4));

        fixed (byte* sourcePtrImmutable = source)
        {
            var rptr = sourcePtrImmutable + channelIndex;
            fixed (void* targetPtr = target)
            {
                if (targetIsB4G4R4A4)
                {
                    var wptr = (ushort*)targetPtr;
                    while (numPixels-- > 0)
                    {
                        *wptr = (ushort)((*rptr << 8) | 0x0FFF);
                        wptr++;
                        rptr += 4;
                    }
                }
                else
                {
                    var wptr = (uint*)targetPtr;
                    while (numPixels-- > 0)
                    {
                        *wptr = (uint)((*rptr << 24) | 0x00FFFFFF);
                        wptr++;
                        rptr += 4;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Clones a texture wrap, by getting a new reference to the underlying <see cref="ShaderResourceView"/> and the
    /// texture behind.
    /// </summary>
    /// <param name="wrap">The <see cref="IDalamudTextureWrap"/> to clone from.</param>
    /// <returns>The cloned <see cref="IDalamudTextureWrap"/>.</returns>
    private static IDalamudTextureWrap CloneTextureWrap(IDalamudTextureWrap wrap)
    {
        var srv = CppObject.FromPointer<ShaderResourceView>(wrap.ImGuiHandle);
        using var res = srv.Resource;
        using var tex2D = res.QueryInterface<Texture2D>();
        var description = tex2D.Description;
        return new DalamudTextureWrap(
            new D3DTextureWrap(
                srv.QueryInterface<ShaderResourceView>(),
                description.Width,
                description.Height));
    }

    private static unsafe void ExtractChannelFromB4G4R4A4(
        Span<byte> target,
        ReadOnlySpan<byte> source,
        int channelIndex,
        bool targetIsB4G4R4A4)
    {
        var numPixels = Math.Min(source.Length / 2, target.Length / (targetIsB4G4R4A4 ? 2 : 4));
        fixed (byte* sourcePtrImmutable = source)
        {
            var rptr = sourcePtrImmutable + (channelIndex / 2);
            var rshift = (channelIndex & 1) == 0 ? 0 : 4;
            fixed (void* targetPtr = target)
            {
                if (targetIsB4G4R4A4)
                {
                    var wptr = (ushort*)targetPtr;
                    while (numPixels-- > 0)
                    {
                        *wptr = (ushort)(((*rptr >> rshift) << 12) | 0x0FFF);
                        wptr++;
                        rptr += 2;
                    }
                }
                else
                {
                    var wptr = (uint*)targetPtr;
                    while (numPixels-- > 0)
                    {
                        var v = (*rptr >> rshift) & 0xF;
                        v |= v << 4;
                        *wptr = (uint)((v << 24) | 0x00FFFFFF);
                        wptr++;
                        rptr += 4;
                    }
                }
            }
        }
    }

    private async Task CheckSanity()
    {
        var invalidFiles = new Dictionary<string, Exception>();
        var texFileTasks = new Dictionary<string, Task<TexFile>>();
        var foundHashes = new Dictionary<string, uint>();
        foreach (var (gffas, fdtTask) in this.fdtFiles)
        {
            var fontAttr = gffas.GetAttribute<GameFontFamilyAndSizeAttribute>()!;
            try
            {
                foundHashes[fontAttr.Path] = Crc32.Get((await fdtTask).Data);

                foreach (var (task, index) in
                         (await this.texFiles[fontAttr.TexPathFormat]).Select((x, i) => (x, i)))
                    texFileTasks[fontAttr.TexPathFormat.Format(index)] = task;
            }
            catch (Exception e)
            {
                invalidFiles[fontAttr.Path] = e;
            }
        }

        foreach (var (path, texTask) in texFileTasks)
        {
            try
            {
                var hc = default(HashCode);
                hc.AddBytes((await texTask).Data);
                foundHashes[path] = Crc32.Get((await texTask).Data);
            }
            catch (Exception e)
            {
                invalidFiles[path] = e;
            }
        }

        foreach (var (path, hashCode) in foundHashes)
        {
            if (!KnownFontFileDataHashes.TryGetValue(path, out var expectedHashCode))
                continue;
            if (expectedHashCode != hashCode)
            {
                invalidFiles[path] = new InvalidDataException(
                    $"Expected 0x{expectedHashCode:X08}; got 0x{hashCode:X08}");
            }
        }

        var dconf = await Service<DalamudConfiguration>.GetAsync();
        var nm = await Service<NotificationManager>.GetAsync();
        var intm = (await Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync()).Manager;
        var ggui = await Service<GameGui>.GetAsync();
        var cstate = await Service<ClientState>.GetAsync();

        if (invalidFiles.Any())
        {
            Log.Warning("Found {n} font related file(s) with unexpected hash code values.", invalidFiles.Count);
            foreach (var (path, ex) in invalidFiles)
                Log.Warning(ex, "\t=> {path}", path);
            Log.Verbose(JsonConvert.SerializeObject(foundHashes));
            if (this.DefaultFontSpec is not SingleFontSpec { FontId: GameFontAndFamilyId })
                return;

            this.Framework.Update += FrameworkOnUpdate;

            void FrameworkOnUpdate(IFramework framework)
            {
                var charaSelect = ggui.GetAddonByName("CharaSelect", 1);
                var charaMake = ggui.GetAddonByName("CharaMake", 1);
                var titleDcWorldMap = ggui.GetAddonByName("TitleDCWorldMap", 1);

                // Show notification when TSM is visible, so that user can check whether a font looks bad
                if (cstate.IsLoggedIn
                    || charaMake != IntPtr.Zero
                    || charaSelect != IntPtr.Zero
                    || titleDcWorldMap != IntPtr.Zero)
                    return;

                this.Framework.Update -= FrameworkOnUpdate;

                var n = nm.AddNotification(
                    "Non-default game fonts detected. If things do not look right, you can use a different font. Running repairs from XIVLauncher is recommended.",
                    "Modded font warning",
                    NotificationType.Warning,
                    10000);
                n.UseMonospaceFont = true;
                n.Actions.Add(
                    (
                        "Use Noto Sans",
                        () =>
                        {
                            dconf.DefaultFontSpec =
                                new SingleFontSpec
                                {
                                    FontId = new DalamudAssetFontAndFamilyId(DalamudAsset.NotoSansJpMedium),
                                    SizePx = 17,
                                };
                            dconf.QueueSave();
                            intm.RebuildFonts();
                        }));
                n.Actions.Add(("Dismiss", () => n.Dismissed = true));
            }
        }
    }

    private IDalamudTextureWrap GetChannelTexture(string texPathFormat, int fileIndex, int channelIndex)
    {
        var texFile = ExtractResult(ExtractResult(this.texFiles[texPathFormat])[fileIndex]);
        var numPixels = texFile.Header.Width * texFile.Header.Height;

        _ = Service<InterfaceManager.InterfaceManagerWithScene>.Get();
        var targetIsB4G4R4A4 = this.InterfaceManager.SupportsDxgiFormat(Format.B4G4R4A4_UNorm);
        var bpp = targetIsB4G4R4A4 ? 2 : 4;
        var buffer = ArrayPool<byte>.Shared.Rent(numPixels * bpp);
        try
        {
            var sliceSpan = texFile.SliceSpan(0, 0, out _, out _, out _);
            switch (texFile.Header.Format)
            {
                case TexFile.TextureFormat.B4G4R4A4:
                    // Game ships with this format.
                    ExtractChannelFromB4G4R4A4(buffer, sliceSpan, channelIndex, targetIsB4G4R4A4);
                    break;
                case TexFile.TextureFormat.B8G8R8A8:
                    // In case of modded font textures.
                    ExtractChannelFromB8G8R8A8(buffer, sliceSpan, channelIndex, targetIsB4G4R4A4);
                    break;
                default:
                    // Unlikely.
                    ExtractChannelFromB8G8R8A8(buffer, texFile.ImageData, channelIndex, targetIsB4G4R4A4);
                    break;
            }

            return this.scopedFinalizer.Add(
                this.InterfaceManager.LoadImageFromDxgiFormat(
                    buffer,
                    texFile.Header.Width * bpp,
                    texFile.Header.Width,
                    texFile.Header.Height,
                    targetIsB4G4R4A4 ? Format.B4G4R4A4_UNorm : Format.B8G8R8A8_UNorm));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
