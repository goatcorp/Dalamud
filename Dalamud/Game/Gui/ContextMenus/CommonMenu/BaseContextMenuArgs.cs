using System;
using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus.CommonMenu;

/// <summary>
/// The base class for context menu arguments
/// </summary>
public abstract class BaseContextMenuArgs {
    /// <summary>
    /// Pointer to the context menu addon.
    /// </summary>
    public IntPtr Addon { get; }

    /// <summary>
    /// Pointer to the context menu agent.
    /// </summary>
    public IntPtr Agent { get; }

    /// <summary>
    /// The name of the addon containing this context menu, if any.
    /// </summary>
    public string? ParentAddonName { get; }

    /// <summary>
    /// The object ID for this context menu. May be invalid (0xE0000000).
    /// </summary>
    public uint ObjectId { get; }

    /// <summary>
    /// The lower half of the content ID of the object for this context menu. May be zero.
    /// </summary>
    public uint ContentIdLower { get; }

    /// <summary>
    /// The text related to this context menu, usually an object name.
    /// </summary>
    public SeString? Text { get; }

    /// <summary>
    /// The world of the object this context menu is for, if any.
    /// </summary>
    public ushort ObjectWorld { get; }

    internal BaseContextMenuArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint objectId, uint contentIdLower, SeString? text, ushort objectWorld) {
        this.Addon = addon;
        this.Agent = agent;
        this.ParentAddonName = parentAddonName;
        this.ObjectId = objectId;
        this.ContentIdLower = contentIdLower;
        this.Text = text;
        this.ObjectWorld = objectWorld;
    }
}
