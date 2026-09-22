using System.Collections.Generic;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.ImGuiBackend;

/// <summary>
/// Holds an ordered, growable list of per-viewport draw-data snapshots captured for a single ImGui step.
/// </summary>
/// <remarks>
/// <para>
/// Each entry owns a <see cref="DrawDataSnapshot"/> and borrows a renderer handle that must stay valid throughout
/// rendering. Callers must exclude readers during capture, reset, and disposal. The backend captures the main
/// viewport at index 0 by convention; this class does not enforce capture order.
/// </para>
/// <para>
/// Entries are pooled by capture order, not viewport identity. <see cref="BeginCapture"/> resets only the logical
/// <see cref="Count"/>; storage is reused while entry, draw-list, and buffer capacities suffice.
/// </para>
/// </remarks>
internal sealed unsafe class ViewportSnapshot : IDisposable
{
    private readonly List<Entry> entries = [];
    private int count;

    /// <summary>
    /// Gets the number of viewport entries captured for the current step.
    /// </summary>
    public int Count => this.count;

    /// <summary>
    /// Gets the captured entry at the given index. Valid for <c>0 &lt;= index &lt; <see cref="Count"/></c>.
    /// </summary>
    /// <param name="index">The entry index. The backend uses index 0 for the main viewport.</param>
    /// <returns>The captured entry.</returns>
    public Entry this[int index] => this.entries[index];

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var entry in this.entries)
            entry.DrawData.Dispose();
        this.entries.Clear();
        this.count = 0;
    }

    /// <summary>
    /// Resets the logical entry count to zero, dropping all previously captured entries without freeing the
    /// pooled <see cref="DrawDataSnapshot"/> backing memory.
    /// </summary>
    public void BeginCapture() => this.count = 0;

    /// <summary>
    /// Deep-copies the given viewport's draw data into the next pooled entry and records the renderer state
    /// needed to draw and present it.
    /// </summary>
    /// <param name="drawData">The live draw-data pointer for this viewport (obtained after ImGui.Render()).</param>
    /// <param name="rendererUserData">
    /// The viewport's borrowed <c>RendererUserData</c> handle. Zero for the main
    /// viewport, which is composited via the renderer's main path rather than presented per-viewport.
    /// </param>
    /// <param name="isMainViewport">Whether this is the main viewport, which the caller must capture first.</param>
    public void Capture(ImDrawData* drawData, nint rendererUserData, bool isMainViewport)
    {
        Entry entry;
        if (this.count < this.entries.Count)
        {
            entry = this.entries[this.count];
        }
        else
        {
            entry = new Entry();
            this.entries.Add(entry);
        }

        entry.DrawData.CopyFrom(drawData);
        entry.RendererUserData = rendererUserData;
        entry.IsMainViewport = isMainViewport;
        this.count++;
    }

    /// <summary>
    /// A single captured viewport's frame.
    /// </summary>
    public sealed class Entry
    {
        /// <summary>Gets the owned deep copy of this viewport's draw data.</summary>
        public DrawDataSnapshot DrawData { get; } = new();

        /// <summary>Gets or sets the borrowed renderer user data handle (zero for the main viewport).</summary>
        public nint RendererUserData { get; set; }

        /// <summary>Gets or sets a value indicating whether this entry is the main viewport.</summary>
        public bool IsMainViewport { get; set; }
    }
}
