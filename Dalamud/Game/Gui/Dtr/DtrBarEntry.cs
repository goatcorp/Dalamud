using System;

using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.Dtr;

/// <summary>
/// Class representing an entry in the server info bar.
/// </summary>
public sealed unsafe class DtrBarEntry : IDisposable
{
    private bool shownBacking = true;
    private SeString? textBacking;

    /// <summary>
    /// Initializes a new instance of the <see cref="DtrBarEntry"/> class.
    /// </summary>
    /// <param name="title">The title of the bar entry.</param>
    /// <param name="textNode">The corresponding text node.</param>
    internal DtrBarEntry(string title, AtkTextNode* textNode)
    {
        this.Title = title;
        this.TextNode = textNode;
    }

    /// <summary>
    /// Gets the title of this entry.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Gets or sets the text of this entry.
    /// </summary>
    public SeString? Text
    {
        get => this.textBacking;
        set
        {
            this.textBacking = value;
            this.Dirty = true;
        }
    }
    
    /// <summary>
    /// Gets or sets a tooltip to be shown when the user mouses over the dtr entry.
    /// </summary>
    public SeString? Tooltip { get; set; }
    
    /// <summary>
    /// Gets or sets a action to be invoked when the user clicks on the dtr entry.
    /// </summary>
    public Action? OnClick { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entry is visible.
    /// </summary>
    public bool Shown
    {
        get => this.shownBacking;
        set
        {
            this.shownBacking = value;
            this.Dirty = true;
        }
    }

    /// <summary>
    /// Gets or sets the internal text node of this entry.
    /// </summary>
    internal AtkTextNode* TextNode { get; set; }

    /// <summary>
    /// Gets a value indicating whether this entry should be removed.
    /// </summary>
    internal bool ShouldBeRemoved { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entry is dirty.
    /// </summary>
    internal bool Dirty { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entry has just been added.
    /// </summary>
    internal bool Added { get; set; }

    /// <summary>
    /// Remove this entry from the bar.
    /// You will need to re-acquire it from DtrBar to reuse it.
    /// </summary>
    public void Remove()
    {
        this.ShouldBeRemoved = true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Remove();
    }
}
