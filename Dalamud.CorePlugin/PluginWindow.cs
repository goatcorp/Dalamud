using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Unicode;

using Dalamud.CorePlugin.MyFonts;
using Dalamud.Interface.EasyFonts;
using Dalamud.Interface.GameFonts;
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
        private readonly List<FontChainEntry> entries = new();

        private string buffer = "Testing 12345 테스트 可能";
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

        /// <inheritdoc/>
        public void Dispose()
        {
            this.fontChainAtlas?.Dispose();
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
        }

        private int dle;

        /// <inheritdoc/>
        public override void Draw()
        {
            if (ImGui.Button("Test"))
            {
                this.fontChainAtlas?.Dispose();
                this.stopwatchLoad.Restart();
                this.fontChainAtlas = new();
                this.entries.Clear();
                this.entries.AddRange(
                    Enum.GetValues<GameFontFamilyAndSize>()
                        .Where(x => x != GameFontFamilyAndSize.Undefined)
                        .Select(x => new GameFontStyle(x))
                        .Select(x => new FontChainEntry(new(x.Family), x.SizePx))
                        .Take(0));
                var task = EasyFontUtils.GetSystemFontsAsync("ko", excludeSimulated: false);
                task.Wait();
                var result = task.Result;
                this.entries.AddRange(
                    result.SelectMany(x => x.Variants.Select(y => new FontChainEntry(y, 14f * 4 / 3))));
                this.entries.Add(new(new("Comic Sans MS", default(FontVariant)), 18f * 4 / 3));
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

            if (this.fontChainAtlas is null)
                return;

            this.dle = ImGui.GetWindowDrawList().CmdBuffer.Size;
            // ImGui.TextUnformatted("=====================");
            using (ImRaii.PushFont(this.fontChainAtlas[new(GameFontFamily.Axis), 12f * 4 / 3]))
            {
                this.fontChainAtlas.LoadGlyphs(this.buffer);
                ImGui.InputTextMultiline(
                    "Test Here",
                    ref this.buffer,
                    65536,
                    new(ImGui.GetContentRegionAvail().X, 80));
            }

            this.Test();
            // ImGui.TextUnformatted("=====================");
            using (ImRaii.PushFont(this.fontChainAtlas[this.chain]))
            {
                this.fontChainAtlas.LoadGlyphs(this.buffer);
                ImGui.TextUnformatted(this.buffer);
            }

            this.Test();
            // ImGui.TextUnformatted("=====================");
            foreach (var entry in this.entries)
            {
                using (ImRaii.PushFont(this.fontChainAtlas[entry.Ident, entry.SizePx]))
                {
                    var v = entry.Ident.System!.Value;
                    var s = $"{v.Name}: {v.Variant}: {this.buffer}";
                    this.fontChainAtlas.LoadGlyphs(s);
                    ImGui.TextUnformatted(s);
                }

                this.Test();
            }

            var ts = this.fontChainAtlas.AtlasPtr.Textures;
            foreach (var t in Enumerable.Range(0, ts.Size))
            {
                ImGui.Image(
                    ts[t].TexID,
                    new(
                        this.fontChainAtlas.AtlasPtr.TexWidth,
                        this.fontChainAtlas.AtlasPtr.TexHeight));

                this.Test();
            }

            this.stopwatchLoad.Stop();
            // ImGui.TextUnformatted($"Took {this.stopwatchLoad.ElapsedMilliseconds}ms");
        }

        private unsafe void Test()
        {
            var dl = ImGui.GetWindowDrawList();
            var vec = new ImVectorWrapper<ImDrawCmd>(&dl.NativePtr->CmdBuffer, null);
            foreach (var v in vec.AsSpan[dle..])
            {
                try
                {
                    Marshal.ReadIntPtr(v.TextureId);
                }
                catch (Exception e)
                {
                    Debugger.Break();
                }
            }
        }
    }
}
