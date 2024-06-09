using System.Collections.Generic;
using System.Linq;

namespace Dalamud.Game.Gui.Nameplates.Model;

/// <inheritdoc/>
internal class NameplateInfo : INameplateInfo
{
    /// <summary>
    /// Gets or sets a list with all nameplate nodes.
    /// </summary>
    public List<NameplateNode> Nodes { get; set; } = [];

    /// <inheritdoc/>
    public bool IsTitleAboveName { get; set; }

    /// <inheritdoc/>
    public bool IsTitleVisible { get; set; }

    /// <inheritdoc/>
    public NameplateStatusIcons IconID { get; set; }

    /// <inheritdoc/>
    public INameplateNode? GetNode(NameplateNodeName name)
    {
        if (name == NameplateNodeName.Unknown)
            return null;

        return this.Nodes.FirstOrDefault(n => n.Name == name);
    }

    /// <inheritdoc/>
    public INameplateNode[] GetAllNodes()
    {
        return [.. this.Nodes];
    }
}
