using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Internal;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Utility;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using ImGuiNET;

using Lumina.Data.Files;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Spannables.Rendering.Internal;

/// <summary>A custom text renderer implementation.</summary>
[ServiceManager.EarlyLoadedService]
[PluginInterface]
[InterfaceVersion("1.0")]
#pragma warning disable SA1015
[ResolveVia<ISpannableRenderer>]
[SuppressMessage(
    "StyleCop.CSharp.SpacingRules",
    "SA1010:Opening square brackets should be spaced correctly",
    Justification = "bad")]
#pragma warning restore SA1015
internal sealed partial class SpannableRenderer : ISpannableRenderer, IInternalDisposableService
{
    /// <summary>The number of <see cref="InterfaceManager.CumulativePresentCalls"/> that a <see cref="ResolvedFonts"/>
    /// needs to be kept unused for it to get destroyed.</summary>
    private const long FontExpiryTicks = 600;

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly FontAtlasFactory fontAtlasFactory = Service<FontAtlasFactory>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly InterfaceManager interfaceManager = Service<InterfaceManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly TextureManager textureManager = Service<TextureManager>.Get();

    private readonly IFontAtlas[] atlases;
    private readonly List<ResolvedFonts> resolvedFonts = [];
    private readonly Dictionary<IFontHandle, ImFontPtr> fontPtrsCache = [];

    private int nextAtlasIndex;

    [ServiceManager.ServiceConstructor]
    private SpannableRenderer(InterfaceManager.InterfaceManagerWithScene imws)
    {
        var t = this.dataManager.GetFile("common/font/gfdata.gfd")!.Data;
        t.CopyTo((this.gfdFile = GC.AllocateUninitializedArray<byte>(t.Length, true)).AsSpan());
        this.gfdTextures =
            GfdTexturePaths
                .Select(x => this.textureManager.GetTexture(this.dataManager.GetFile<TexFile>(x)!))
                .ToArray();
        this.atlases = new IFontAtlas[16];
        for (var i = 0; i < this.atlases.Length; i++)
        {
            this.atlases[i] = this.fontAtlasFactory.CreateFontAtlas(
                $"{nameof(SpannableRenderer)}:atlas{i}",
                FontAtlasAutoRebuildMode.Async);
        }

        this.framework.Update += this.FrameworkOnUpdate;
    }

    /// <summary>Finalizes an instance of the <see cref="SpannableRenderer"/> class.</summary>
    ~SpannableRenderer() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public void DisposeService()
    {
        this.framework.Update -= this.FrameworkOnUpdate;
        foreach (var f in this.resolvedFonts)
            f.Dispose();
        this.resolvedFonts.Clear();
        foreach (var f in this.atlases)
            f.Dispose();
        this.atlases.AsSpan().Clear();
        (Interlocked.Exchange(ref this.textSpannableBuilderPool, null) as IDisposable)?.Dispose();
        this.ReleaseUnmanagedResources();
    }

    /// <inheritdoc/>
    public Spannable Draw(Spannable spannable, in RenderContext renderContext)
    {
        ThreadSafety.AssertMainThread();

        if (renderContext.RootOptions is not null)
        {
            renderContext.RootOptions.RenderScale = renderContext.RenderScale;
            renderContext.RootOptions.PreferredSize = renderContext.Size / renderContext.RenderScale;
            renderContext.RootOptions.VisibleSize = renderContext.RootOptions.PreferredSize;
            spannable.Options.CopyFrom(renderContext.RootOptions);
        }
        else
        {
            spannable.Options.RenderScale = renderContext.RenderScale;
            spannable.Options.PreferredSize = renderContext.Size / renderContext.RenderScale;
            spannable.Options.VisibleSize = spannable.Options.PreferredSize;
        }

        spannable.Renderer = this;
        spannable.ImGuiGlobalId = renderContext.ImGuiGlobalId;

        spannable.RenderPassPreDispatchEvents();

        if (renderContext.UseInteraction)
            spannable.RenderPassDispatchEventsAsRoot();

        spannable.RenderPassMeasure();

        var mtx = Matrix4x4.Multiply(
            renderContext.Transformation,
            Matrix4x4.Multiply(
                Matrix4x4.CreateScale(renderContext.RenderScale),
                Matrix4x4.CreateTranslation(new(renderContext.ScreenOffset, 0))));
        spannable.RenderPassPlace(Matrix4x4.Identity, mtx);

        if (renderContext.UseDrawing)
        {
            using (new ScopedTransformer(renderContext.DrawListPtr, mtx, Vector2.One, 1f))
                spannable.RenderPassDraw(renderContext.DrawListPtr);

            if (renderContext.PutDummyAfterRender)
            {
                var tf = Matrix4x4.Multiply(
                    Matrix4x4.CreateScale(new Vector3(renderContext.RenderScale)),
                    renderContext.Transformation);
                var lt = renderContext.ScreenOffset + Vector2.Transform(spannable.Boundary.LeftTop, tf);
                var rt = renderContext.ScreenOffset + Vector2.Transform(spannable.Boundary.RightTop, tf);
                var rb = renderContext.ScreenOffset + Vector2.Transform(spannable.Boundary.RightBottom, tf);
                var lb = renderContext.ScreenOffset + Vector2.Transform(spannable.Boundary.LeftBottom, tf);
                var minPos = Vector2.Min(Vector2.Min(lt, rt), Vector2.Min(lb, rb));
                var maxPos = Vector2.Max(Vector2.Max(lt, rt), Vector2.Max(lb, rb));
                if (minPos.X <= maxPos.X && minPos.Y <= maxPos.Y)
                {
                    ImGui.SetCursorScreenPos(minPos);
                    ImGui.Dummy(maxPos - minPos);
                }
            }
        }

        if (renderContext.UseInteraction)
            spannable.RenderPassPostDispatchEvents();

        return spannable;
    }

    /// <inheritdoc/>
    public unsafe bool TryGetFontData(float renderScale, scoped in TextStyle style, out TextStyleFontData fontData)
    {
        ThreadSafety.AssertMainThread();

        var currentFont = ImGui.GetFont();
        var intendedFontSize = style.FontSize switch
        {
            < 0f => -style.FontSize * currentFont.FontSize,
            > 0f => style.FontSize,
            _ => MathF.Round(this.fontAtlasFactory.DefaultFontSpec.SizePx * ImGuiHelpers.GlobalScale) /
                 ImGuiHelpers.GlobalScale,
        };

        IFontHandle? fh;
        bool fakeItalic, fakeBold;
        if (style.Font.FontFamilyId is { } familyId)
        {
            var i = 0;
            var absoluteFontSize = intendedFontSize;
            absoluteFontSize *= renderScale / ImGuiHelpers.GlobalScale;
            absoluteFontSize = MathF.Round(absoluteFontSize * 32f) / 32f;

            for (; i < this.resolvedFonts.Count; i++)
            {
                if (this.resolvedFonts[i].CanAccommodate(familyId, absoluteFontSize))
                    break;
            }

            if (i == this.resolvedFonts.Count)
                this.resolvedFonts.Add(new(this, absoluteFontSize, familyId));

            var rfont = this.resolvedFonts[i];
            rfont.MarkUsed(this.interfaceManager.CumulativePresentCalls);
            fh = rfont.GetEffectiveFont(style.Italic, style.Bold, out fakeItalic, out fakeBold);

            // Attempt to get the best already-loaded font.
            if (fh?.Available is not true)
            {
                ResolvedFonts? searching = null;
                var searchingDiff = float.NegativeInfinity;
                foreach (var x in this.resolvedFonts)
                {
                    var diff = x.TestFontSizeDifference(familyId, absoluteFontSize);
                    if (diff is float.NaN)
                        continue;
                    if (x.GetEffectiveFont(style.Italic, style.Bold, out _, out _)?.Available is not true)
                        continue;

                    if (diff >= 0)
                    {
                        if (diff < searchingDiff || searchingDiff < 0)
                        {
                            searching = x;
                            searchingDiff = diff;
                        }
                    }
                    else
                    {
                        if (diff > searchingDiff && searchingDiff < 0)
                        {
                            searching = x;
                            searchingDiff = diff;
                        }
                    }
                }

                if (searching is not null)
                    fh = searching.GetEffectiveFont(style.Italic, style.Bold, out fakeItalic, out fakeBold);
            }
        }
        else
        {
            familyId = null;
            fh = style.Font.GetEffectiveFont(style.Italic, style.Bold, out fakeItalic, out fakeBold);
        }

        if (fh?.Available is not true)
        {
            if (currentFont.NativePtr == InterfaceManager.DefaultFont.NativePtr
                && !ReferenceEquals(familyId, DalamudDefaultFontAndFamilyId.Instance))
            {
                return this.TryGetFontData(
                    renderScale,
                    style with { Font = new(DalamudDefaultFontAndFamilyId.Instance) },
                    out fontData);
            }

            if (currentFont.NativePtr == InterfaceManager.MonoFont.NativePtr
                && !ReferenceEquals(familyId, DalamudAssetFontAndFamilyId.From(DalamudAsset.InconsolataRegular)))
            {
                return this.TryGetFontData(
                    renderScale,
                    style with { Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.InconsolataRegular)) },
                    out fontData);
            }

            if (currentFont.NativePtr == InterfaceManager.IconFont.NativePtr
                && !ReferenceEquals(familyId, DalamudAssetFontAndFamilyId.From(DalamudAsset.FontAwesomeFreeSolid)))
            {
                return this.TryGetFontData(
                    renderScale,
                    style with { Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.FontAwesomeFreeSolid)) },
                    out fontData);
            }

            fontData = new(renderScale, style, currentFont, intendedFontSize, style.Italic, style.Bold);
            return false;
        }

        if (!this.fontPtrsCache.TryGetValue(fh, out var font))
        {
            // This function has to be called from the main thread, and the current contract requires that once a
            // font has been pushed, it must stay alive until the end of ImGui render.
            // ImGui.GetFont() will stay alive for the duration we need it to be, even if we dispose this here.
            using (fh.Push())
                this.fontPtrsCache.Add(fh, font = ImGui.GetFont());
        }

        fontData = new(renderScale, style, font, intendedFontSize, fakeItalic, fakeBold);
        return font.IsNotNullAndLoaded();
    }

    /// <summary>Clear the resources used by this instance.</summary>
    private void ReleaseUnmanagedResources()
    {
        this.DisposePooledObjects();
    }

    private void FrameworkOnUpdate(IFramework framework1)
    {
        this.fontPtrsCache.Clear();
        for (var i = 0; i < this.resolvedFonts.Count; i++)
        {
            if (!this.resolvedFonts[i].DisposeIfExpired(this.interfaceManager.CumulativePresentCalls))
                continue;

            if (i != this.resolvedFonts.Count - 1)
                this.resolvedFonts[i] = this.resolvedFonts[^1];
            this.resolvedFonts.RemoveAt(this.resolvedFonts.Count - 1);
            i--;
        }

        this.FrameworkOnUpdateReturnPooledObjects();
    }

    private sealed class ResolvedFonts : IDisposable
    {
        private readonly SpannableRenderer owner;
        private readonly float sizePreScale;
        private FontHandleVariantSet set;
        private long expiryTick;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ResolvedFonts(SpannableRenderer owner, float sizePreScale, IFontFamilyId fontFamilyId)
        {
            this.owner = owner;
            this.sizePreScale = sizePreScale;
            this.set = new(fontFamilyId);
        }

        public ref readonly IFontFamilyId FamilyId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this.set.FontFamilyId!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float TestFontSizeDifference(IFontFamilyId familyId, float wantedAbsSize) =>
            this.FamilyId.Equals(familyId)
                ? this.sizePreScale - wantedAbsSize
                : float.NaN;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanAccommodate(IFontFamilyId familyId, float absSize) =>
            Math.Abs(this.sizePreScale - absSize) < 0.00001f && this.FamilyId.Equals(familyId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkUsed(long tick) => this.expiryTick = tick + FontExpiryTicks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DisposeIfExpired(long tick)
        {
            if (this.expiryTick > tick)
                return false;
            this.Dispose();
            return true;
        }

        public void Dispose()
        {
            this.set.Normal?.Dispose();
            this.set.Bold?.Dispose();
            this.set.Italic?.Dispose();
            this.set.ItalicBold?.Dispose();
            this.set = default;
        }

        /// <inheritdoc cref="FontHandleVariantSet.GetEffectiveFont"/>
        public IFontHandle? GetEffectiveFont(bool italic, bool bold, out bool fauxItalic, out bool fauxBold)
        {
            var match =
                this.FamilyId.FindBestMatch(
                    bold
                        ? (int)DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_BOLD
                        : (int)DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL,
                    (int)DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL,
                    italic
                        ? (int)DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_ITALIC
                        : (int)DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL);

            var fontId = this.FamilyId.Fonts[match];
            if (fontId.Weight <= (int)DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL)
            {
                fauxBold = bold;
                bold = false;
            }
            else
            {
                fauxBold = false;
            }

            if (fontId.Style != (int)DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_ITALIC)
            {
                fauxItalic = italic;
                italic = false;
            }
            else
            {
                fauxItalic = false;
            }

            var fh = italic
                         ? bold ? this.set.ItalicBold : this.set.Italic
                         : bold
                             ? this.set.Bold
                             : this.set.Normal;
            if (fh is not null)
                return fh;

            var atlas = this.owner.atlases[this.owner.nextAtlasIndex];
            this.owner.nextAtlasIndex = (this.owner.nextAtlasIndex + 1) % this.owner.atlases.Length;

            fh = atlas.NewDelegateFontHandle(
                x => x.OnPreBuild(
                    tk =>
                    {
                        var conf = new SafeFontConfig { SizePx = this.sizePreScale, };
                        conf.MergeFont = tk.Font = fontId.AddToBuildToolkit(tk, conf);
                        tk.AttachExtraGlyphsForDalamudLanguage(conf);
                    }));
            if (italic && bold)
                this.set.ItalicBold = fh;
            else if (italic)
                this.set.Italic = fh;
            else if (bold)
                this.set.Bold = fh;
            else
                this.set.Normal = fh;
            return fh;
        }
    }
}
