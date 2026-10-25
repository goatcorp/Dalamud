using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal;

namespace Dalamud.Interface.Fonts;

/// <summary>A dialog for choosing a font.</summary>
public sealed partial class FontChooserDialog
{
    // Draws the names of fonts in their own fonts, loading the fonts as they scroll into view.
    private sealed class FontPreviews
    {
        // Fonts that may switch from the list's font to their own in a frame, so that a scroll does not change many.
        private const int SwitchesPerFrame = 4;

        // Frames a font is kept after its last use.
        private const int KeepFrames = 600;

        // Frames between looks for fonts to drop.
        private const int SweepFrames = 60;

        // Loads started and not done, at most. Scrolling through the list would start one for every family otherwise.
        private const int MaxLoads = 4;

        private readonly Dictionary<IFontFaceId, Entry> entries = [];
        private readonly List<Entry> loading = [];
        private int frame = -1;
        private int lastSweepFrame;
        private int switchesLeft;

        public bool Selectable(int row, IFontFaceId? id, string name, bool selected)
        {
            ImGui.PushID(row);
            var clicked = ImGui.Selectable(
                "##preview"u8,
                selected,
                ImGuiSelectableFlags.NoAutoClosePopups,
                new(0, ImGui.GetTextLineHeight()));
            ImGui.PopID();

            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            if (!ImGui.IsRectVisible(min, max))
                return clicked;

            var drawList = ImGui.GetWindowDrawList();
            var font = id is null ? ImFontPtr.Null : this.GetFont(id);

            // Every list pads its rows by half an em on both sides, whatever font a row is drawn in.
            var padX = MathF.Floor(ImGui.GetFontSize() * 0.5f);
            var padY = MathF.Floor(ImGui.GetStyle().ItemSpacing.Y * 0.5f);
            drawList.PushClipRect(min + new Vector2(padX, 0), max - new Vector2(padX, 0), true);
            drawList.AddText(
                font.IsNull ? ImGui.Font : font,
                ImGui.GetFontSize(),
                min + new Vector2(padX, padY),
                ImGui.GetColorU32(ImGuiCol.Text),
                name);
            drawList.PopClipRect();
            return clicked;
        }

        public void Clear()
        {
            this.entries.Clear();
            this.loading.Clear();
        }

        private unsafe ImFontPtr GetFont(IFontFaceId id)
        {
            var now = ImGui.GetFrameCount();
            if (now != this.frame)
            {
                this.frame = now;
                this.switchesLeft = SwitchesPerFrame;
                for (var i = this.loading.Count - 1; i >= 0; i--)
                {
                    if (this.loading[i].Font.Entry.Load.IsCompleted)
                        this.loading.RemoveAt(i);
                }

                if (now - this.lastSweepFrame >= SweepFrames)
                    this.Sweep(now);
            }

            if (!id.PreviewsName)
                return ImFontPtr.Null;

            if (!this.entries.TryGetValue(id, out var entry))
            {
                if (this.loading.Count >= MaxLoads)
                    return ImFontPtr.Null;
                this.entries[id] = entry = new(TextStyle.From(id));
                if (!entry.Font.Entry.Load.IsCompleted)
                    this.loading.Add(entry);
            }

            entry.LastUsedFrame = now;
            if (!entry.Font.Entry.Load.IsCompletedSuccessfully)
                return ImFontPtr.Null;
            if (!entry.Shown)
            {
                if (this.switchesLeft == 0)
                    return ImFontPtr.Null;
                this.switchesLeft--;
                entry.Shown = true;
            }

            return entry.Font.Native;
        }

        private void Sweep(int now)
        {
            this.lastSweepFrame = now;
            foreach (var (id, entry) in this.entries)
            {
                if (now - entry.LastUsedFrame > KeepFrames)
                    this.entries.Remove(id);
                else
                    entry.Font.LastUsedFrame = FontClock.Frame;
            }
        }

        // The fonts are used without an ImFontRef, so that the resolver releases them soon after the dialog stops
        // using them, rather than once the GC collects the references.
        private sealed class Entry(in TextStyle style)
        {
            private readonly TextStyle style = style;

            // Gets the font, again if the resolver has released it.
            public FontImpl Font
            {
                get
                {
                    if (field is null || field.Released)
                        field = Service<FontResolver>.Get().GetImpl(this.style);
                    return field;
                }
            }

            public int LastUsedFrame { get; set; }

            public bool Shown { get; set; }
        }
    }
}
