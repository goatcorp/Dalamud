using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for testing game prebaked fonts.
/// </summary>
internal class GamePrebakedFontsTestWidget : IDataWindowWidget, IDisposable
{
    private ImVectorWrapper<byte> testStringBuffer;
    private IFontAtlas? privateAtlas;
    private IReadOnlyDictionary<GameFontFamily, (GameFontStyle Size, Lazy<IFontHandle> Handle)[]>? fontHandles;
    private bool useGlobalScale;
    private bool useWordWrap;
    private bool useItalic;
    private bool useBold;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; }

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Game Prebaked Fonts";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load() => this.Ready = true;

    /// <inheritdoc/>
    public unsafe void Draw()
    {
        ImGui.AlignTextToFramePadding();
        fixed (byte* labelPtr = "Global Scale"u8)
        {
            var v = (byte)(this.useGlobalScale ? 1 : 0);
            if (ImGuiNative.igCheckbox(labelPtr, &v) != 0)
            {
                this.useGlobalScale = v != 0;
                this.ClearAtlas();
            }
        }

        ImGui.SameLine();
        fixed (byte* labelPtr = "Word Wrap"u8)
        {
            var v = (byte)(this.useWordWrap ? 1 : 0);
            if (ImGuiNative.igCheckbox(labelPtr, &v) != 0)
                this.useWordWrap = v != 0;
        }
        
        ImGui.SameLine();
        fixed (byte* labelPtr = "Italic"u8)
        {
            var v = (byte)(this.useItalic ? 1 : 0);
            if (ImGuiNative.igCheckbox(labelPtr, &v) != 0)
            {
                this.useItalic = v != 0;
                this.ClearAtlas();
            }
        }
        
        ImGui.SameLine();
        fixed (byte* labelPtr = "Bold"u8)
        {
            var v = (byte)(this.useBold ? 1 : 0);
            if (ImGuiNative.igCheckbox(labelPtr, &v) != 0)
            {
                this.useBold = v != 0;
                this.ClearAtlas();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset Text") || this.testStringBuffer.IsDisposed)
        {
            this.testStringBuffer.Dispose();
            this.testStringBuffer = ImVectorWrapper.CreateFromSpan(
                "(Game)-[Font] {Test}. 0123456789!! <氣気气きキ기>。"u8,
                minCapacity: 1024);
        }

        this.privateAtlas ??=
            Service<FontAtlasFactory>.Get().CreateFontAtlas(
                nameof(GamePrebakedFontsTestWidget),
                FontAtlasAutoRebuildMode.Async,
                this.useGlobalScale);
        this.fontHandles ??=
            Enum.GetValues<GameFontFamilyAndSize>()
                .Where(x => x.GetAttribute<GameFontFamilyAndSizeAttribute>() is not null)
                .Select(x => new GameFontStyle(x) { Italic = this.useItalic, Bold = this.useBold })
                .GroupBy(x => x.Family)
                .ToImmutableDictionary(
                    x => x.Key,
                    x => x.Select(y => (y, new Lazy<IFontHandle>(() => this.privateAtlas.NewGameFontHandle(y))))
                          .ToArray());

        fixed (byte* labelPtr = "Test Input"u8)
        {
            if (ImGuiNative.igInputTextMultiline(
                    labelPtr,
                    this.testStringBuffer.Data,
                    (uint)this.testStringBuffer.Capacity,
                    new(ImGui.GetContentRegionAvail().X, 32 * ImGuiHelpers.GlobalScale),
                    0,
                    null,
                    null) != 0)
            {
                var len = this.testStringBuffer.StorageSpan.IndexOf((byte)0);
                if (len + 4 >= this.testStringBuffer.Capacity)
                    this.testStringBuffer.EnsureCapacityExponential(len + 4);
                if (len < this.testStringBuffer.Capacity)
                {
                    this.testStringBuffer.LengthUnsafe = len;
                    this.testStringBuffer.StorageSpan[len] = default;
                }
            }
        }

        var offsetX = ImGui.CalcTextSize("99.9pt").X + (ImGui.GetStyle().FramePadding.X * 2);
        foreach (var (family, items) in this.fontHandles)
        {
            if (!ImGui.CollapsingHeader($"{family} Family"))
                continue;

            foreach (var (gfs, handle) in items)
            {
                ImGui.TextUnformatted($"{gfs.SizePt}pt");
                ImGui.SameLine(offsetX);
                ImGuiNative.igPushTextWrapPos(this.useWordWrap ? 0f : -1f);
                try
                {
                    if (handle.Value.LoadException is { } exc)
                    {
                        ImGui.TextUnformatted(exc.ToString());
                    }
                    else if (!handle.Value.Available)
                    {
                        fixed (byte* labelPtr = "Loading..."u8)
                            ImGuiNative.igTextUnformatted(labelPtr, labelPtr + 8 + ((Environment.TickCount / 200) % 3));
                    }
                    else
                    {
                        if (!this.useGlobalScale)
                            ImGuiNative.igSetWindowFontScale(1 / ImGuiHelpers.GlobalScale);
                        using var pushPop = handle.Value.Push();
                        ImGuiNative.igTextUnformatted(
                            this.testStringBuffer.Data,
                            this.testStringBuffer.Data + this.testStringBuffer.Length);
                    }
                }
                finally
                {
                    ImGuiNative.igPopTextWrapPos();
                    ImGuiNative.igSetWindowFontScale(1);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ClearAtlas();
        this.testStringBuffer.Dispose();
    }

    private void ClearAtlas()
    {
        this.fontHandles?.Values.SelectMany(x => x.Where(y => y.Handle.IsValueCreated).Select(y => y.Handle.Value))
            .AggregateToDisposable().Dispose();
        this.fontHandles = null;
        this.privateAtlas?.Dispose();
        this.privateAtlas = null;
    }
}
