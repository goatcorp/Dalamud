using System;

using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus;

/// <summary>
/// The base class for context menu arguments.
/// </summary>
public abstract class BaseContextMenuArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseContextMenuArgs"/> class.
    /// </summary>
    /// <param name="addon">addon.</param>
    /// <param name="agent">agent.</param>
    /// <param name="parentAddonName">parentAddonName.</param>
    /// <param name="objectId">objectId.</param>
    /// <param name="contentIdLower">contentIdLower.</param>
    /// <param name="text">text.</param>
    /// <param name="objectWorld">objectWorld.</param>
    internal BaseContextMenuArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint objectId, uint contentIdLower, SeString? text, ushort objectWorld)
    {
        this.Addon = addon;
        this.Agent = agent;
        this.ParentAddonName = parentAddonName;
        this.ObjectId = objectId;
        this.ContentIdLower = contentIdLower;
        this.Text = text;
        this.ObjectWorld = objectWorld;
    }

    /// <summary>
    /// Gets pointer to the context menu addon.
    /// </summary>
    public IntPtr Addon { get; }

    /// <summary>
    /// Gets pointer to the context menu agent.
    /// </summary>
    public IntPtr Agent { get; }

    /// <summary>
    /// Gets the name of the addon containing this context menu, if any.
    /// </summary>
    public string? ParentAddonName { get; }

    /// <summary>
    /// Gets the object ID for this context menu. May be invalid (0xE0000000).
    /// </summary>
    public uint ObjectId { get; }

    /// <summary>
    /// Gets the lower half of the content ID of the object for this context menu. May be zero.
    /// </summary>
    public uint ContentIdLower { get; }

    /// <summary>
    /// Gets the text related to this context menu, usually an object name.
    /// </summary>
    public SeString? Text { get; }

    /// <summary>
    /// Gets the world of the object this context menu is for, if any.
    /// </summary>
    public ushort ObjectWorld { get; }
}
