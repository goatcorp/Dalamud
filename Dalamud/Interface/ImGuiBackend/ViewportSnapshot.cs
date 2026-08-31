using System.Collections.Generic;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.ImGuiBackend;

/// <summary>
/// Holds an ordered, growable list of per-viewport draw-data snapshots captured for a single ImGui step.
/// </summary>
/// <remarks>
/// <para>
/// Each entry owns a deep copy of one viewport's draw data (see <see cref="DrawDataSnapshot"/>) plus the
/// information the renderer needs to draw and present that viewport. Entry index 0 is always the main viewport.
/// </para>
/// <para>
/// The backing <see cref="DrawDataSnapshot"/> instances are pooled and reused across frames: the list grows as
/// the viewport count grows, but never shrinks its capacity. <see cref="BeginCapture"/> only resets the logical
/// <see cref="Count"/> to zero, so steady-state capture incurs no native allocation churn.
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
    /// <param name="index">The entry index. Index 0 is the main viewport.</param>
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
    /// The viewport's <c>RendererUserData</c> handle (the renderer-private viewport data). Zero for the main
    /// viewport, which is composited via the renderer's main path rather than presented per-viewport.
    /// </param>
    /// <param name="isMainViewport">Whether this is the main viewport (entry 0).</param>
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

        /// <summary>Gets or sets the viewport's renderer user data handle (zero for the main viewport).</summary>
        public nint RendererUserData { get; set; }

        /// <summary>Gets or sets a value indicating whether this entry is the main viewport.</summary>
        public bool IsMainViewport { get; set; }
    }
}
