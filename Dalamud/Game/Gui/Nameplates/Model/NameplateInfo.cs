using System.Collections.Generic;
using System.Linq;

namespace Dalamud.Game.Gui.Nameplates.Model;

/// <inheritdoc/>
internal class NameplateInfo : INameplateInfo
{
    /// <summary>
    /// Gets a list with all nameplate element.
    /// </summary>
    public List<NameplateElement> Elements { get; } = [];

    /// <inheritdoc/>
    public bool IsTitleAboveName { get; set; }

    /// <inheritdoc/>
    public bool IsTitleVisible { get; set; }

    /// <inheritdoc/>
    public NameplateStatusIcons IconID { get; set; }

    /// <inheritdoc/>
    public INameplateElement? GetElement(NameplateElementType type)
    {
        if (type == NameplateElementType.Unknown)
            return null;

        return this.Elements.FirstOrDefault(n => n.Type == type);
    }

    /// <inheritdoc/>
    public INameplateElement[] GetAllElement()
    {
        return [.. this.Elements];
    }
}
