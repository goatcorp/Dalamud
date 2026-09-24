using System.Collections.Generic;

using Dalamud.Game.Tooltip.Classes;
using Dalamud.Game.Tooltip.Entries;
using Dalamud.Game.Tooltip.Nodes;
using Dalamud.Game.Tooltip.StyleOptions;

using FFXIVClientStructs.FFXIV.Common.Math;

using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Tooltip.TooltipArgTypes;

/// <summary>
/// Base class for ITooltip Tooltip Types.
/// </summary>
public abstract class TooltipArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipArgs"/> class.
    /// </summary>
    internal TooltipArgs()
    {
    }

    /// <summary>
    /// Gets the type of these args.
    /// </summary>
    public abstract TooltipType Type { get; }

    /// <summary>
    /// Gets or sets the name of the plugin generating this entry.
    /// </summary>
    internal ReadOnlySeString SourcePluginName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of string entries defined by the user of this args object.
    /// </summary>
    private List<TooltipEntry> TooltipEntries { get; } = [];

    /// <summary>
    /// Adds a line of text to the tooltip.
    /// </summary>
    /// <param name="text">Text to add.</param>
    /// <param name="options">Text styling options to use.</param>
    public void AddText(ReadOnlySeString text, TextOptions? options = null)
        => this.TooltipEntries.Add(new StringTooltipEntry
        {
            String = text,
            Options = options ?? new TextOptions(),
        });

    /// <summary>
    /// Adds an image to the tooltip.
    /// </summary>
    /// <param name="imagePath">
    /// Path to the image you want to show,
    /// if it's not rooted, will load from the game,
    /// if rooted will load from file system.
    /// </param>
    /// <remarks>
    /// When providing path don't include theme specifier 'img#/', nor '_hr1', these will be resolved automatically.
    /// </remarks>
    /// <remarks>
    /// When providing image size, maximum width is 342px. The loaded texture will be fitted to the image node.
    /// </remarks>
    /// <param name="imageSize">Sets the image size used to display.</param>
    /// <param name="options">Image styling options to use.</param>
    public void AddImage(string imagePath, Vector2 imageSize, ImageOptions? options = null)
        => this.TooltipEntries.Add(new ImageTooltipEntry
        {
            ImagePath = imagePath,
            Options = options ?? new ImageOptions(),
            Size = imageSize,
        });

    /// <summary>
    /// Builds the actual tooltip node for this entry, with size computed and everything.
    /// </summary>
    /// <returns>Null if this args object doesn't want its own node. Otherwise, a constructed node.</returns>
    internal CustomTooltipNode? BuildTooltipNode()
    {
        if (this.TooltipEntries.Count is 0)
        {
            return null;
        }

        var tooltipNode = new CustomTooltipNode
        {
            SourcePluginName = this.SourcePluginName,
        };

        foreach (var entry in this.TooltipEntries)
        {
            tooltipNode.AddEntry(entry);
        }

        return tooltipNode;
    }
}
