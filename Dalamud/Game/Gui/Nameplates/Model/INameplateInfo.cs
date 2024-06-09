using System.Collections.Generic;

namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Represents a nameplate by providing some information about the visual look.
/// </summary>
public interface INameplateInfo
{
    /// <summary>
    /// Gets or sets a value indicating whether the title is shown above the name.
    /// <br/>If the value is true, the title is shown above the name. Otherwise the title is shown below the name.
    bool IsTitleAboveName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the title is visible.
    /// <br/>If the title value is true, the title is visible. Otherwise the title text is not visible and ignored.
    /// </summary>
    bool IsTitleVisible { get; set; }

    /// <summary>
    /// Gets or sets the icon shown on the nameplate. Mostly used for the status icon.
    /// </summary>
    NameplateStatusIcons IconID { get; set; }

    /// <summary>
    /// Searches for a node by a given common name.
    /// </summary>
    /// <param name="name">The common namplate node name to serch for.<br/>Note: <see cref="NameplateNodeName.Unknown"/> is not supported.</param>
    /// <returns>Returns the found node, or null if nohting has been found.</returns>
    INameplateNode? GetNode(NameplateNodeName name);

    /// <summary>
    /// Gets all available nameplate nodes.
    /// </summary>
    /// <returns>Returns an array of all nameplate nodes.</returns>
    INameplateNode[] GetAllNodes();
}
