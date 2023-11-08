using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using System.Threading.Tasks;

using Dalamud.CorePlugin.MyFonts;
using Dalamud.Hooking;
using Dalamud.Hooking.Internal;
using Dalamud.Interface.EasyFonts;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using ImGuiNET;

using JetBrains.Annotations;

using Vector2 = System.Numerics.Vector2;

namespace Dalamud.CorePlugin
{
    /// <summary>
    /// Class responsible for drawing the main plugin window.
    /// </summary>
    internal class PluginWindow : Window, IDisposable
    {
        private readonly Stopwatch stopwatchLoad = new();

        private unsafe ImGuiListClipperPtr entryClipper = new(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        private Task<List<FontChainEntry>> entries = Task.FromException<List<FontChainEntry>>(new());

        private string buffer = "ABCDE abcde 12345 가나다 漢字氣気 あかさたな アカサタナ";
        private FontChain chain = default;

        [CanBeNull]
        private FontChainAtlas fontChainAtlas;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginWindow"/> class.
        /// </summary>
        public PluginWindow()
            : base("CorePlugin")
        {
            this.IsOpen = true;

            this.Size = new Vector2(810, 520);
            this.SizeCondition = ImGuiCond.FirstUseEver;
        }

        private Hook<OnChangedTextureIdDelegate> OnChangedTextureIdHook { get; set; }

        private delegate void OnChangedTextureIdDelegate(ImDrawListPtr idlm);

        /// <inheritdoc/>
        public void Dispose()
        {
            this.fontChainAtlas?.Dispose();
            this.OnChangedTextureIdHook?.Dispose();
            this.entryClipper.Destroy();
            this.entryClipper = default;
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            const float entrySize = 14f * 4 / 3;
            if (ImGui.Button("Dispose"))
            {
                this.fontChainAtlas?.Dispose();
                this.fontChainAtlas = null;
            }

            ImGui.SameLine();
            if (ImGui.Button("New"))
            {
                this.fontChainAtlas?.Dispose();
                this.stopwatchLoad.Restart();
                this.fontChainAtlas = new();
                this.entries =
                    EasyFontUtils
                        .GetSystemFontsAsync("ko", excludeSimulated: false)
                        .ContinueWith(
                            res => Enum.GetValues<GameFontFamilyAndSize>()
                                       .Where(x => x != GameFontFamilyAndSize.Undefined)
                                       .Select(x => new GameFontStyle(x))
                                       .Select(x => new FontChainEntry(new(x.Family), x.SizePx))
                                       .Prepend(new(new(GameFontFamily.Axis), 24f * 4 / 3))
                                       .Concat(
                                           res.Result
                                              .SelectMany(
                                                  x => x.Variants.Select(y => new FontChainEntry(y, entrySize))))
                                       .ToList());
                this.chain = new(
                    new FontChainEntry[]
                    {
                        new(new("Comic Sans MS", default(FontVariant)), 18f * 4 / 3)
                        {
                            Ranges = new UnicodeRange[]
                            {
                                new(0, 'A' - 1),
                                new('Z' + 1, 0xFFFF - ('Z' + 1)),
                            },
                        },
                        new(new("Papyrus", default(FontVariant)), 18f * 4 / 3, -2),
                        new(new("Gulim", default(FontVariant)), 18f * 4 / 3, 0, 0, 3)
                        {
                            Ranges = new[]
                            {
                                UnicodeRanges.HangulJamo,
                                UnicodeRanges.HangulSyllables,
                                UnicodeRanges.HangulCompatibilityJamo,
                                UnicodeRanges.HangulJamoExtendedA,
                                UnicodeRanges.HangulJamoExtendedB,
                            },
                        },
                        new(new("Gungsuh", default(FontVariant)), 18f * 4 / 3, 0, 0, 3)
                        {
                            Ranges = new[]
                            {
                                UnicodeRanges.CjkCompatibility,
                                UnicodeRanges.CjkStrokes,
                                UnicodeRanges.CjkCompatibilityForms,
                                UnicodeRanges.CjkCompatibilityIdeographs,
                                UnicodeRanges.CjkUnifiedIdeographs,
                                UnicodeRanges.CjkUnifiedIdeographsExtensionA,
                            },
                        },
                    });
            }

            ImGui.SameLine();
            if (ImGui.Button("Debug"))
            {
                var addr = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                                  .Single(x => x.ModuleName == "cimgui.dll").BaseAddress + 0x66B10;
                this.OnChangedTextureIdHook?.Dispose();
                this.OnChangedTextureIdHook = Hook<OnChangedTextureIdDelegate>.FromAddress(
                    addr,
                    ptr =>
                    {
                        unsafe
                        {
                            foreach (ref var v in new ImVectorWrapper<ImDrawCmd>(&ptr.NativePtr->CmdBuffer, null)
                                         .AsSpan)
                            {
                                if (v.TextureId == 0)
                                    continue;
                                try
                                {
                                    Marshal.ReadIntPtr(v.TextureId);
                                }
                                catch (Exception)
                                {
                                    _ = NativeFunctions.MessageBoxW(
                                        Process.GetCurrentProcess().MainWindowHandle,
                                        "aaa",
                                        "aaa",
                                        NativeFunctions.MessageBoxType.Ok);
                                    Debugger.Break();
                                }
                            }

                            var prevBuffer = new ImVectorWrapper<ImDrawCmd>(&ptr.NativePtr->CmdBuffer, null).AsSpan
                                .ToArray();

                            this.OnChangedTextureIdHook!.Original(ptr);

                            foreach (ref var v in new ImVectorWrapper<ImDrawCmd>(&ptr.NativePtr->CmdBuffer, null)
                                         .AsSpan)
                            {
                                if (v.TextureId == 0)
                                    continue;
                                try
                                {
                                    Marshal.ReadIntPtr(v.TextureId);
                                }
                                catch (Exception)
                                {
                                    _ = NativeFunctions.MessageBoxW(
                                        Process.GetCurrentProcess().MainWindowHandle,
                                        "bbb",
                                        "bbb",
                                        NativeFunctions.MessageBoxType.Ok);
                                    Debugger.Break();
                                }
                            }
                        }
                    });
                this.OnChangedTextureIdHook.Enable();
            }

            ImGui.SameLine();
            ImGui.TextUnformatted($"Took {this.stopwatchLoad.ElapsedMilliseconds}ms");

            if (this.fontChainAtlas is null)
                return;

            using var dispose1 = this.fontChainAtlas.SuppressTextureUpdatesScoped();

            ImGui.TextUnformatted("=====================");
            using (ImRaii.PushFont(this.fontChainAtlas[new(GameFontFamily.Axis), 12f * 4 / 3]))
            {
                this.fontChainAtlas.LoadGlyphs(this.buffer);
                ImGui.InputTextMultiline(
                    "Test Here",
                    ref this.buffer,
                    65536,
                    new(ImGui.GetContentRegionAvail().X, 80));
            }

            ImGui.TextUnformatted("=====================");
            using (ImRaii.PushFont(this.fontChainAtlas[this.chain]))
            {
                this.fontChainAtlas.LoadGlyphs(this.buffer);
                ImGui.TextUnformatted(this.buffer);
            }

            ImGui.TextUnformatted("=====================");
            if (this.entries.IsCompletedSuccessfully)
            {
                var r = this.entries.Result;
                this.entryClipper.Begin(r.Count, entrySize);
                while (this.entryClipper.Step())
                {
                    for (var i = this.entryClipper.DisplayStart; i < this.entryClipper.DisplayEnd; i++)
                    {
                        if (i < 0)
                            continue;

                        var entry = r[i];
                        using (ImRaii.PushFont(this.fontChainAtlas[entry.Ident, entry.SizePx]))
                        {
                            var s = $"{entry}: {this.buffer}";
                            this.fontChainAtlas.LoadGlyphs(s);
                            ImGui.TextUnformatted(s);
                        }

                        this.fontChainAtlas.GetWrapper(entry.Ident, entry.SizePx).SanityCheck();
                    }
                }

                this.entryClipper.End();
            }

            ImGui.TextUnformatted("=====================");
            var ts = this.fontChainAtlas.AtlasPtr.Textures;
            foreach (var t in Enumerable.Range(0, ts.Size))
            {
                ImGui.Image(
                    ts[t].TexID,
                    new(
                        this.fontChainAtlas.AtlasPtr.TexWidth,
                        this.fontChainAtlas.AtlasPtr.TexHeight));
            }

            ImGui.TextUnformatted("=====================");
            this.stopwatchLoad.Stop();
        }
    }
}

/*
 *
// This is an IDisposable
private FontChainAtlas? privateAtlas;

...

void OnDraw() {
    // new privateAtlas can be done from any thread
    privateAtlas ??= new();  // or obtain one from UiBuilder? idk

    // These are POD structs
    var font1 = new FontChainEntry("Comic Sans MS", default(FontVariant)), 18f * 4 / 3)
    var font2 = new FontChainEntry(GameFontFamilyAndSize.Axis18);
    var font3 = new FontChainEntry("C:/somepath.ttf", 0);
    var fontCombined = new FontChain(new[] { font1, font2, font3 }) { LineHeight = 1.4f };

    // Use these to use only 1 font at a time
    // * ImRaii.PushFont(privateAtlas[font1.Ident, font1.Size])
    // * ImRaii.PushFont(privateAtlas[font2.Ident, font2.Size])
    // * ImRaii.PushFont(privateAtlas[font3.Ident, font3.Size])

    // Use this to use a font chain
    using (ImRaii.PushFont(privateAtlas[fontCombined])) {
        var text = "Test";
        privateAtlas.LoadGlyphs(text);
        ImGui.TextUnformatted(text);
    }
}
*/
