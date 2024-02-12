using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ImGuiFontChooserDialog;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using Serilog;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for testing game prebaked fonts.
/// </summary>
internal class GamePrebakedFontsTestWidget : IDataWindowWidget, IDisposable
{
    private ImVectorWrapper<byte> testStringBuffer;
    private IFontAtlas? privateAtlas;
    private SingleFontSpec fontSpec = new() { FontId = DalamudDefaultFontAndFamilyId.Instance };
    private IFontHandle? fontDialogHandle;
    private IReadOnlyDictionary<GameFontFamily, (GameFontStyle Size, Lazy<IFontHandle> Handle)[]>? fontHandles;
    private bool useGlobalScale;
    private bool useWordWrap;
    private bool useItalic;
    private bool useBold;
    private bool useMinimumBuild;

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
        fixed (byte* labelPtr = "Minimum Range"u8)
        {
            var v = (byte)(this.useMinimumBuild ? 1 : 0);
            if (ImGuiNative.igCheckbox(labelPtr, &v) != 0)
            {
                this.useMinimumBuild = v != 0;
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

        ImGui.SameLine();
        if (ImGui.Button("Test Lock"))
            Task.Run(this.TestLock);

        ImGui.SameLine();
        if (ImGui.Button("Choose Editor Font"))
        {
            var fcd = new SingleFontChooserDialog(
                Service<FontAtlasFactory>.Get().CreateFontAtlas(
                    $"{nameof(GamePrebakedFontsTestWidget)}:EditorFont",
                    FontAtlasAutoRebuildMode.Async));
            fcd.SelectedFont = this.fontSpec;
            fcd.IgnorePreviewGlobalScale = !this.useGlobalScale;
            Service<InterfaceManager>.Get().Draw += fcd.Draw;
            fcd.ResultTask.ContinueWith(
                r => Service<Framework>.Get().RunOnFrameworkThread(
                    () =>
                    {
                        Service<InterfaceManager>.Get().Draw -= fcd.Draw;
                        fcd.Dispose();

                        _ = r.Exception;
                        if (!r.IsCompletedSuccessfully)
                            return;

                        this.fontSpec = r.Result;
                        this.fontDialogHandle?.Dispose();
                        this.fontDialogHandle = null;
                    }));
        }

        this.privateAtlas ??=
            Service<FontAtlasFactory>.Get().CreateFontAtlas(
                nameof(GamePrebakedFontsTestWidget),
                FontAtlasAutoRebuildMode.Async,
                this.useGlobalScale);
        this.fontDialogHandle ??= this.fontSpec.CreateFontHandle(this.privateAtlas);

        fixed (byte* labelPtr = "Test Input"u8)
        {
            if (!this.useGlobalScale)
                ImGuiNative.igSetWindowFontScale(1 / ImGuiHelpers.GlobalScale);
            using (this.fontDialogHandle.Push())
            {
                if (ImGuiNative.igInputTextMultiline(
                        labelPtr,
                        this.testStringBuffer.Data,
                        (uint)this.testStringBuffer.Capacity,
                        new(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 3),
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

                    if (this.useMinimumBuild)
                        _ = this.privateAtlas?.BuildFontsAsync();
                }
            }

            if (!this.useGlobalScale)
                ImGuiNative.igSetWindowFontScale(1);
        }

        this.fontHandles ??=
            Enum.GetValues<GameFontFamilyAndSize>()
                .Where(x => x.GetAttribute<GameFontFamilyAndSizeAttribute>() is not null)
                .Select(x => new GameFontStyle(x) { Italic = this.useItalic, Bold = this.useBold })
                .GroupBy(x => x.Family)
                .ToImmutableDictionary(
                    x => x.Key,
                    x => x.Select(
                              y => (y, new Lazy<IFontHandle>(
                                           () => this.useMinimumBuild
                                                     ? this.privateAtlas.NewDelegateFontHandle(
                                                         e =>
                                                             e.OnPreBuild(
                                                                 tk => tk.AddGameGlyphs(
                                                                     y,
                                                                     Encoding.UTF8.GetString(
                                                                         this.testStringBuffer.DataSpan).ToGlyphRange(),
                                                                     default)))
                                                     : this.privateAtlas.NewGameFontHandle(y))))
                          .ToArray());

        var offsetX = ImGui.CalcTextSize("99.9pt").X + (ImGui.GetStyle().FramePadding.X * 2);
        var counter = 0;
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
                        if (counter++ % 2 == 0)
                        {
                            using var pushPop = handle.Value.Push();
                            ImGuiNative.igTextUnformatted(
                                this.testStringBuffer.Data,
                                this.testStringBuffer.Data + this.testStringBuffer.Length);
                        }
                        else
                        {
                            handle.Value.Push();
                            ImGuiNative.igTextUnformatted(
                                this.testStringBuffer.Data,
                                this.testStringBuffer.Data + this.testStringBuffer.Length);
                            handle.Value.Pop();
                        }
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
        this.fontDialogHandle?.Dispose();
        this.fontDialogHandle = null;
        this.privateAtlas?.Dispose();
        this.privateAtlas = null;
    }

    private async void TestLock()
    {
        if (this.fontHandles is not { } fontHandlesCopy)
            return;

        Log.Information($"{nameof(GamePrebakedFontsTestWidget)}: {nameof(this.TestLock)} waiting for build");

        await using var garbage = new DisposeSafety.ScopedFinalizer();
        var fonts = new List<ImFontPtr>();
        IFontHandle[] handles;
        try
        {
            handles = fontHandlesCopy.Values.SelectMany(x => x).Select(x => x.Handle.Value).ToArray();
            foreach (var handle in handles)
            {
                await handle.WaitAsync();
                var locked = handle.Lock();
                garbage.Add(locked);
                fonts.Add(locked.ImFont);
            }
        }
        catch (ObjectDisposedException)
        {
            Log.Information($"{nameof(GamePrebakedFontsTestWidget)}: {nameof(this.TestLock)} cancelled");
            return;
        }

        Log.Information($"{nameof(GamePrebakedFontsTestWidget)}: {nameof(this.TestLock)} waiting in lock");
        await Task.Delay(5000);

        foreach (var (font, handle) in fonts.Zip(handles))
            TestSingle(font, handle);

        return;

        unsafe void TestSingle(ImFontPtr fontPtr, IFontHandle handle)
        {
            var dim = default(Vector2);
            var test = "Test string"u8;
            fixed (byte* pTest = test)
                ImGuiNative.ImFont_CalcTextSizeA(&dim, fontPtr, fontPtr.FontSize, float.MaxValue, 0, pTest, null, null);
            Log.Information($"{nameof(GamePrebakedFontsTestWidget)}: {handle} => {dim}");
        }
    }
}
